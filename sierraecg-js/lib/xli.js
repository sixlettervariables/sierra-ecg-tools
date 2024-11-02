/** xli.js: Simple XLI Decompressor.
 *
 * @author Christopher A. Watford <christopher.watford@gmail.com>
 * 
 * Copyright (c) 2014 Christopher A. Watford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
'use strict';

const LzwReader = require('./lzw');

const kLzwBitsPerCode = 10;

class XliReader {
  /**
   * @param {Buffer | String} input 
   */
  constructor(input) {
    this.input = Buffer.from(input);
    this.offset = 0;
  }

  extractLeads() {
    const leads = [];
    while (this.offset < this.input.length) {
      const chunk = this._readChunk();
      leads.push(chunk.values);

      this.offset += chunk.size;
    }

    return leads;
  }

  _readChunk() {
    const self = this;
    const header = this.input.subarray(this.offset + 0, this.offset + 8);
    const size = header.readInt32LE(0);
    const code = header.readInt16LE(4);
    const delta = header.readInt16LE(6);
    // console.debug('chunk-header: { size: %d, code: %d, delta: %d }', size, code, delta);
  
    const compressedBlock = this.input.subarray(this.offset + 8, this.offset + 8 + size);
    // console.debug('compressed size %d', compressedBlock.length);

    const reader = new LzwReader(compressedBlock, { bits: kLzwBitsPerCode });
  
    const output = reader.decode();
    const unpacked = self._unpack(output);
    const values = self._decodeDeltas(unpacked, delta);
    return ({ size: header.length + compressedBlock.length, values });
  }

  /**
   * @param {Buffer} bytes 
   * @returns {number[]}
   */
  _unpack(bytes) {
    const unpacked = new Array(Math.floor(bytes.length / 2));
    for (var ii = 0; ii < unpacked.length; ii++) {
      unpacked[ii] = (((bytes[ii] << 8) | bytes[ii + unpacked.length]) << 16) >> 16;
    }

    return unpacked;
  }

  /**
   * 
   * @param {Array<Number>} deltas 
   * @param {Number} lastValue 
   * @returns {Array<Number>}
   */
  _decodeDeltas(deltas, lastValue) {
    const values = deltas.slice();
    let x = values[0],
        y = values[1];
    for (var ii = 2; ii < values.length; ii++) {
      const z = (y * 2) - x - lastValue;
      lastValue = values[ii] - 64;
      values[ii] = z;
      x = y;
      y = z;
    }
  
    return values;
  }
}

module.exports = XliReader;
