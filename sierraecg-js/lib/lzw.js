/** lzw.js: Simple LZW reader
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

const debug = require('debug')('lzw');

class LzwReader {
/**
 * @param {Buffer | String} input 
 * @param {{ bits?: number }} options 
 */
  constructor(input, options) {
    /* jshint -W030 */
    options || (options = {});
    /* jshint +W030 */
    if (!input) {
      throw new Error('Missing required argument {input}');
    }
    else if (options.bits && (options.bits > 16 || options.bits < 10)) {
      throw new Error('Argument out of range: {options.bits} must be at least 10 and no more than 16');
    }

    this.input = Buffer.from(input);
    this.offset = 0;

    debug('compressed size %d bytes', this.input.length);

    this.bits = options.bits || 16;
    this.maxCode = (1 << this.bits) - 2;

    this.chunkSize = Math.floor(8 * 1024 / this.bits);

    debug('initialized with %d bits per code-word', this.bits);

    this.bitCount = 0;
    this.buffer = 0;

    this.previous = [];

    this.nextCode = 256;
    this.strings = new Map();
    for (var ii = 0; ii < this.nextCode; ii++) {
      this.strings.set(ii, new Code(ii, ii));
    }
  }

  decodeSync() {
    let code, output = [];
    while (-1 !== (code = this._readCode())) {
        if (code > this.maxCode) {
            debug('code exceeds max (%d > %d), ending', code, this.maxCode);
            break;
        }

        if (!this.strings.has(code)) {
            const value = [ ...this.previous.slice(), this.previous[0] ];
            this.strings.set(code, new Code(code, value));
        }

        const current = this.strings.get(code);
        output = current.appendTo(output);

        if (this.previous.length > 0 && this.nextCode <= this.maxCode) {
            const nc = this.nextCode++;
            const value = [ ...this.previous, current.value[0] ];
            this.strings.set(nc, new Code(nc, value));
        }

        this.previous = current.value;
    }

    debug('decompressed %d bytes', output.length);
    return Buffer.from(output);
  }

  /**
   * 
   * @param {(reason: any, output: Buffer) => void} cb 
   */
  decode(cb) {
    const self = this;
    let code, output = [], codesRead = 0, goingNext = false;
    function next() {
      goingNext = false;
      while (-1 !== (code = self._readCode())) {
        if (code > self.maxCode) {
          debug('code exceeds max (%d > %d), ending', code, self.maxCode);
          break;
        }
  
        if (!self.strings.has(code)) {
          const value = [ ...self.previous, self.previous[0] ];
          self.strings.set(code, new Code(code, value));
        }
  
        const current = self.strings.get(code);
        output = current.appendTo(output);
  
        if (self.previous.length > 0 && self.nextCode <= self.maxCode) {
          const nc = self.nextCode++;
          const value = [ ...self.previous, current.value[0] ];
          self.strings.set(nc, new Code(nc, value));
        }
  
        self.previous = current.value;
  
        codesRead++;
        if (codesRead >= self.chunkSize) {
          goingNext = true;
          codesRead = 0;
          break;
        }
      }
  
      if (!goingNext) {
        process.nextTick(function () {
          debug('decompressed %d bytes', output.length);
          cb(null, Buffer.from(output));
        });
      }
      else {
        debug('waiting until next tick to decompress');
        setImmediate(next);
      }
    }
    
    next();
  }

  _readCode() {
    let EOF = false;
    while (this.bitCount <= 24) {
        if (this.offset >= this.input.length) {
            debug('EOF found @%d', this.offset);
            EOF = true;
            break;
        }

        const next = this.input[this.offset++];
        this.buffer |= ((next & 0xFF) << (24 - this.bitCount)) & 0xFFFFFFFF;
        this.bitCount += 8;
    }

    if (EOF && this.bitCount < this.bits) {
        debug('EOF without enough bits to return a code (%d bits left)', this.bitCount);
        return -1;
    }
    else {
        // CAW: the most important thing you'll ever do in life is use the
        //      Zero-fill right shift operator.
        const code = ((this.buffer >>> (32 - this.bits)) & 0x0000FFFF);
        this.buffer = (((this.buffer & 0xFFFFFFFF) << this.bits) & 0xFFFFFFFF);
        this.bitCount -= this.bits;
        debug('code [%d]', code);
        return code;
    }
  }
}

module.exports = LzwReader;

class Code {
  /**
   * 
   * @param {number} code 
   * @param {number[] | number} value 
   */
  constructor(code, value) {
    this.code = code;
    this.value = Array.isArray(value) ? value.slice() : [value];
  }

  /**
   * 
   * @param {number[]} output 
   * @returns {number[]}
   */
  appendTo(output) {
    output.push(...this.value);

    return output;
  }
}
