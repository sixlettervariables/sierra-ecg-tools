// <copyright file="XliDecompressor.cs">
//  Copyright (c) 2011 Christopher A. Watford
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.
// </copyright>
// <author>Christopher A. Watford [christopher.watford@gmail.com]</author>
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using SierraEcg.Compression;

namespace SierraEcg
{
    /// <summary>
    /// Provides methods and properties used to decompress XLI encoded streams.
    /// </summary>
    public sealed class XliDecompressor : IDisposable
    {
        /// <summary>
        /// <see cref="Stream"/> containing XLI compressed data.
        /// </summary>
        private Stream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="XliDecompressor"/> class
        /// from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="buffer">An array of bytes containing XLI compressed data.</param>
        public XliDecompressor(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            this.stream = new MemoryStream(buffer, writable: false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XliDecompressor"/> class
        /// from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="buffer">An array of bytes containing XLI compressed data.</param>
        /// <param name="offset">The offset into <paramref name="buffer"/> at which the decompression begins.</param>
        /// <param name="count">The length of <paramref name="buffer"/> in bytes. </param>
        public XliDecompressor(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            else if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            else if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            else if (buffer.Length - offset < count)
            {
                throw new ArgumentException("The buffer length minus offset is less than count.", "count");
            }

            this.stream = new MemoryStream(buffer, offset, count, writable: false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XliDecompressor"/> class
        /// from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> containing XLI compressed data.</param>
        public XliDecompressor(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
        }

        /// <summary>
        /// Reads one lead payload at a time from the XLI compressed stream.
        /// </summary>
        /// <returns>Uncompressed lead data.</returns>
        public int[] ReadLeadPayload()
        {
            var chunkHeader = new byte[8];
            if (chunkHeader.Length != this.stream.Read(chunkHeader, 0, chunkHeader.Length))
            {
                throw new InvalidOperationException("Not enough data to read header from the stream");
            }

            // Each chunk begins with an 8 byte header consisting of:
            //   - 32-bit integer describing the length of the chunk
            //   - 16-bit integer (unknown payload, seemingly always 1)
            //   - 16-bit integer describing the first delta code
            //
            // After this header exists LZW-compressed delta codes, using 10-bit code words:
            // 0        2        4        6        8  ...
            // +--------+--------+--------+--------+--------+--------+--------+--------+--------+
            // | Size            |  Unk.  | Delta  | LZW compressed deltas (10-bit codes)       |
            // +--------+--------+--------+--------+                                            |
            // | ...                                                               [Size bytes] |
            // +--------+--------+--------+--------+--------+--------+--------+--------+--------+

            var size = BitConverter.ToInt32(chunkHeader, 0);
            // CAW: var unknown = BitConverter.ToInt16(chunkHeader, 4);
            var start = BitConverter.ToInt16(chunkHeader, 6);

            var chunk = new byte[size];
            if (chunk.Length != this.stream.Read(chunk, 0, size))
            {
                throw new InvalidOperationException("Not enough data in the compressed chunk");
            }

            // LZW 10-bit codes
            using (var decompressor = new LzwDecompressor(chunk, 10))
            {
                var output = decompressor.Decompress()
                                         .ToArray();

                // Once the data is decompressed it is packed [HIWORDS...LOWORDS]
                // and needs to be reconstituted.
                var deltas = Unpack(output);

                return DecodeDeltas(deltas, start);
            }
        }

        /// <summary>
        /// Takes the decompressed, deinterleaved byte array and unpacks the array into
        /// the original 16-bit delta codes. Hi-words are taken from the first half
        /// of the chunk and lo-words are taken from the second half.
        /// </summary>
        /// <remarks>The 16-bit delta codes are deinterleaved this way to improve compression
        /// as the first byte of most of the delta codes is 0.</remarks>
        /// <param name="bytes">Decompressed chunk data.</param>
        /// <returns>Array of 16-bit interleaved delta codes.</returns>
        static short[] Unpack(byte[] bytes)
        {
            Debug.Assert(bytes != null);

            short[] output = new short[(bytes.Length + 1) / 2];
            for (int ii = 0, jj = output.Length; ii < output.Length; ++ii, ++jj)
            {
                output[ii] = (short)((bytes[ii] << 8) | (jj < bytes.Length ? (int)bytes[jj] : 0));
            }

            return output;
        }

        /// <summary>
        /// Decodes the delta compression used by the XLI format.
        /// </summary>
        /// <param name="input">Delta codes.</param>
        /// <param name="initialValue">Initial delta code from the chunk header.</param>
        /// <returns>32-bit integer array of decoded values.</returns>
        static int[] DecodeDeltas(short[] input, short initialValue)
        {
            Debug.Assert(input != null);

            // we are using 32-bit integers to avoid .Net unsigned/signed problems
            var output = new int[input.Length];
            for (int ii = 0; ii < input.Length; ++ii)
            {
                output[ii] = input[ii];
            }

            // delta codes are a linear combination of the previous two values
            int x = output[0];
            int y = output[1];
            int lastValue = initialValue;
            for (int ii = 2; ii < output.Length; ++ii)
            {
                int z = (y + y) - x - lastValue;
                lastValue = output[ii] - 64; // bias 64
                output[ii] = z;
                x = y;
                y = z;
            }

            return output;
        }

        /// <summary>
        /// Disposes the underlying <see cref="Stream"/>.
        /// </summary>
        public void Dispose()
        {
            if (this.stream != null)
            {
                this.stream.Dispose();
            }
        }
    }
}
