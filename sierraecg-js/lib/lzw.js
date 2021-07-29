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

var debug = require('debug')('lzw');

/**
 * @constructor
 * @param {Buffer | String} input 
 * @param {*} options 
 */
function LzwReader(input, options) {
    /* jshint -W030 */
    options || (options = {});
    /* jshint +W030 */

    if (!input) {
        throw new Error('Missing required argument {input}');
    }
    else if (options.bits && (options.bits > 16 || options.bits < 10)) {
        throw new Error('Argument out of range: {options.bits} must be at least 10 and no more than 16');
    }

    this.input = Buffer.isBuffer(input) ? input.slice() : new Buffer(input);
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
    this.strings = {};
    for (var ii = 0; ii < this.nextCode; ++ii) {
        this.strings[ii] = new Code(ii, ii);
    }
}

module.exports = LzwReader;

var LZWP = LzwReader.prototype;

LZWP.readCode = function LzwReader_ReadCode() {
    var EOF = false;
    while (this.bitCount <= 24) {
        if (this.offset >= this.input.length) {
            debug('EOF found @%d', this.offset);
            EOF = true;
            break;
        }

        var next = this.input[this.offset++];
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
        var code = ((this.buffer >>> (32 - this.bits)) & 0x0000FFFF);
        this.buffer = (((this.buffer & 0xFFFFFFFF) << this.bits) & 0xFFFFFFFF);
        this.bitCount -= this.bits;
        debug('code [%d]', code);
        return code;
    }
};

LZWP.decodeSync = function LzwReader_DecodeSync() {
    var code, value, output = [];
    while (-1 !== (code = this.readCode())) {
        if (code > this.maxCode) {
            debug('code exceeds max (%d > %d), ending', code, this.maxCode);
            break;
        }

        if (!this.strings.hasOwnProperty(code)) {
            value = this.previous.slice();
            value.push(this.previous[0]);
            this.strings[code] = new Code(code, value);
        }

        output = this.strings[code].appendTo(output);

        if (this.previous.length > 0 && this.nextCode <= this.maxCode) {
            value = this.previous.slice();
            value.push(this.strings[code].value[0]);
            var nc = this.nextCode++;
            this.strings[nc] = new Code(nc, value);
        }

        this.previous = this.strings[code].value;
    }

    debug('decompressed %d bytes', output.length);
    return new Buffer(output);
};

LZWP.decode = function LzwReader_Decode(cb) {
  var self = this;
  var code, value, output = [], codesRead = 0, goingNext = false;
  function next() {
    goingNext = false;
    while (-1 !== (code = self.readCode())) {
      if (code > self.maxCode) {
        debug('code exceeds max (%d > %d), ending', code, self.maxCode);
        break;
      }

      if (!self.strings.hasOwnProperty(code)) {
        value = self.previous.slice();
        value.push(self.previous[0]);
        self.strings[code] = new Code(code, value);
      }

      output = self.strings[code].appendTo(output);

      if (self.previous.length > 0 && self.nextCode <= self.maxCode) {
        value = self.previous.slice();
        value.push(self.strings[code].value[0]);
        var nc = self.nextCode++;
        self.strings[nc] = new Code(nc, value);
      }

      self.previous = self.strings[code].value;

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
        return cb(null, new Buffer(output));
      });
    }
    else {
      debug('waiting until next tick to decompress');
      setImmediate(next);
    }
  }
  
  return next();
};

function Code(code, value) {
    if (!(this instanceof Code)) return new Code(code, value);
    this.code = code;
    this.value = Array.isArray(value) ? value.slice() : [value];
}
var CP = Code.prototype;

CP.appendTo = function Code_AppendTo(output) {
    for (var ii = 0; ii < this.value.length; ++ii) {
        output.push(this.value[ii]);
    }

    return output;
};
