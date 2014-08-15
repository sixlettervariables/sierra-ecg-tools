// <copyright file="LzwDecompressor.cs">
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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SierraEcg.Compression
{
    /// <summary>
    /// Provides methods and properties used to decompress LZW encoded streams.
    /// </summary>
    /// <remarks>
    /// Based on Mark Nelson's C++ LZW implementation.
    /// http://marknelson.us/2011/11/08/lzw-revisited/
    /// </remarks>
    public sealed class LzwDecompressor : IDisposable
    {
        #region Fields

        /// <summary>
        /// Number of bits in a code word.
        /// </summary>
        private readonly int bits;

        /// <summary>
        /// Maximum code word.
        /// </summary>
        private readonly int maxCode;

        /// <summary>
        /// Compressed input stream.
        /// </summary>
        private Stream input;

        /// <summary>
        /// Number of bits currently in the buffer.
        /// </summary>
        private int bitsRead;

        /// <summary>
        /// Code word input buffer.
        /// </summary>
        private uint buffer;

        #endregion Fields

        #region ctors

        /// <summary>
        /// Initializes a new instance of the <see cref="LzwDecompressor"/> class from
        /// the given compressed <see cref="Stream"/> and the <paramref name="bits"/>
        /// used in the codes.
        /// </summary>
        /// <param name="buffer">LZW compressed data.</param>
        /// <param name="bits">Number of bits per code.</param>
        public LzwDecompressor(byte[] buffer, int bits)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            else if (bits < 4 || bits > 16)
            {
                throw new ArgumentOutOfRangeException("bits", bits, "Code word size must be at least 4 and less than or equal to 16");
            }

            this.bits = bits;
            this.input = new MemoryStream(buffer);
            this.maxCode = (1 << bits) - 2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LzwDecompressor"/> class from
        /// the given compressed <see cref="Stream"/> and the <paramref name="bits"/>
        /// used in the codes.
        /// </summary>
        /// <param name="stream">LZW compressed data.</param>
        /// <param name="bits">Number of bits per code.</param>
        public LzwDecompressor(Stream stream, int bits)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            else if (bits < 4 || bits > 16)
            {
                throw new ArgumentOutOfRangeException("bits", bits, "Code word size must be at least 4 and less than or equal to 16");
            }

            this.bits = bits;
            this.input = stream;
            this.maxCode = (1 << bits) - 2;
        }

        #endregion ctors

        /// <summary>
        /// Appends an item onto the end of an array.
        /// </summary>
        /// <param name="left">Array, may be empty.</param>
        /// <param name="item">Item to append to the end of the array.</param>
        /// <returns>Concatenation of <paramref name="left"/> with <paramref name="item"/>.</returns>
        static T[] Append<T>(T[] left, T item)
        {
            var bytes = new T[left.Length + 1];
            Buffer.BlockCopy(left, 0, bytes, 0, left.Length);
            bytes[left.Length] = item;

            return bytes;
        }

        /// <summary>
        /// Decompress the underyling <see cref="Stream"/>.
        /// </summary>
        /// <returns>An enumeration of bytes corresponding to the decompressed data.</returns>
        public IEnumerable<byte> Decompress()
        {
            var strings = Enumerable.Range(0, 256)
                                    .ToDictionary(xx => (uint)xx, xx => new[] { (byte)xx });

            byte[] previous = new byte[0];
            uint code;
            uint nextCode = 256;

            while (this.Read(out code))
            {
                if (code >= this.maxCode + 1) break;

                // helps handle string+character+string+character+string
                if (!strings.ContainsKey(code))
                {
                    strings[code] = Append(previous, previous[0]);
                }

                foreach (var chr in strings[code])
                    yield return chr;

                if (previous.Length > 0 && nextCode <= this.maxCode)
                {
                    strings[nextCode++] = Append(previous, strings[code][0]);
                }

                previous = strings[code];
            }
        }

        /// <summary>
        /// Read the next code from the underlying <see cref="Stream"/>.
        /// </summary>
        /// <param name="code">Code extracted.</param>
        /// <returns><see langword="true"/> if the data is valid; otherwise <see langword="false"/>.</returns>
        private bool Read(out uint code)
        {
            while (bitsRead <= 24)
            {
                buffer |= (uint)(this.input.ReadByte() << (24 - bitsRead));
                bitsRead += 8;
            }

            code = (buffer >> (32 - this.bits)) & 0x0000FFFF;
            buffer <<= this.bits;
            bitsRead -= this.bits;

            return this.input.Position < this.input.Length;
        }

        #region IDisposable members

        /// <summary>
        /// Disposes the underlying <see cref="Stream"/>.
        /// </summary>
        public void Dispose()
        {
            if (this.input != null)
                this.input.Dispose();
        }

        #endregion IDisposable members
    }
}
