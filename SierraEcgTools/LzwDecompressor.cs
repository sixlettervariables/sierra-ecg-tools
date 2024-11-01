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
        private readonly int _bits;

        /// <summary>
        /// Maximum code word.
        /// </summary>
        private readonly int _maxCode;

        /// <summary>
        /// Compressed input stream.
        /// </summary>
        private readonly Stream _input;

        /// <summary>
        /// Number of bits currently in the buffer.
        /// </summary>
        private int _bitsRead;

        /// <summary>
        /// Code word input buffer.
        /// </summary>
        private uint _buffer;

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
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            else if (bits < 4 || bits > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(bits), bits, "Code word size must be at least 4 and less than or equal to 16");
            }

            _bits = bits;
            _input = new MemoryStream(buffer);
            _maxCode = (1 << bits) - 2;
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
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            else if (bits < 4 || bits > 16)
            {
                throw new ArgumentOutOfRangeException(nameof(bits), bits, "Code word size must be at least 4 and less than or equal to 16");
            }

            _bits = bits;
            _input = stream;
            _maxCode = (1 << bits) - 2;
        }

        #endregion ctors

        /// <summary>
        /// Decompress the underyling <see cref="Stream"/>.
        /// </summary>
        /// <returns>An enumeration of bytes corresponding to the decompressed data.</returns>
        public IEnumerable<byte> Decompress()
        {
            Dictionary<uint, byte[]> strings = Enumerable.Range(0, 256)
                                                         .ToDictionary(xx => (uint)xx, xx => new[] { (byte)xx });

            byte[] previous = [];
            uint nextCode = 256;

            while (Read(out uint code))
            {
                if (code >= _maxCode + 1)
                {
                    break;
                }

                // helps handle string+character+string+character+string
                if (!strings.ContainsKey(code))
                {
                    strings[code] = [..previous, previous[0]];
                }

                foreach (byte chr in strings[code])
                {
                    yield return chr;
                }

                if (previous.Length > 0 && nextCode <= _maxCode)
                {
                    strings[nextCode++] = [..previous, strings[code][0]];
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
            while (_bitsRead <= 24)
            {
                _buffer |= (uint)(_input.ReadByte() << (24 - _bitsRead));
                _bitsRead += 8;
            }

            code = (_buffer >> (32 - _bits)) & 0x0000FFFF;
            _buffer <<= _bits;
            _bitsRead -= _bits;

            return _input.Position < _input.Length;
        }

        #region IDisposable members

        /// <summary>
        /// Disposes the underlying <see cref="Stream"/>.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _input?.Dispose();
            }
            catch { /* CAW: ignored */ }
        }

        #endregion IDisposable members
    }
}
