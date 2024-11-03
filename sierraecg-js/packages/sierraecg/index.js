/** index.js: Sierra ECG Reader.
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

const fs = require('node:fs/promises');

const { Ecg } = require('@sierraecg/core');

/**
 * Read a Philips SierraECG file from disk.
 * @param {string} filename 
 * @param {(reason: any, ecg: Ecg) => void} cb 
 * @param {*} options 
 */
function readFile(filename, cb, options) {
  readFileAsync(filename, options)
    .then(ecg => cb(null, ecg))
    .catch(err => cb(err));
}

/**
 * Read a Philips SierraECG file from disk.
 * @param {string} filename 
 * @param {*} options 
 * @returns {Promise<Ecg>}
 */
async function readFileAsync(filename, options) {
  const buffer = await fs.readFile(filename, options);
  const ecg = await Ecg.fromXmlAsync(buffer);
  return ecg;
}

/**
 * Read a Philips SierraECG file from an XML string.
 * @param {string} value 
 * @param {(reason: any, ecg: Ecg) => void} cb 
 */
function readString(value, cb) {
  readStringAsync(value)
    .then(ecg => cb(null, ecg))
    .catch(err => cb(err));
}

/**
 * Read a Philips SierraECG file from an XML string.
 * @param {string} value 
 * @returns {Promise<Ecg>}
 */
function readStringAsync(value) {
  return Ecg.fromXmlAsync(value);
}

module.exports = {
  readFile,
  readFileAsync,
  readString,
  readStringAsync,
};
