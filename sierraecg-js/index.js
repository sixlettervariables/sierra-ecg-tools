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

const path = require('path');

const Promise = require('bluebird');
const fs = Promise.promisifyAll(require('fs'));

const xml2js = require('xml2js');
const _xparser = new xml2js.Parser();
const parseXml = Promise.promisify(_xparser.parseString);

const sierraEcg = require('./lib/sierraecg');

function readFile(filename, cb, options) {
  var ext = path.extname(filename).toLowerCase();
  switch (ext) {
    case '.xml':
      break;
    default:
      throw new Error('Unsupported file type');
  }

  const deferred = fs.readFileAsync(filename, options)
    .then(parseXml)
    .then(sierraEcg.parseXml)
    .then(sierraEcg.decodeXli)
    .then(sierraEcg.updateLeads)
    .then(sierraEcg.createObjects);

  return deferred.nodeify(cb);
}

function readString(value, cb) {
  const deferred = parseXml(value)
    .then(sierraEcg.parseXml)
    .then(sierraEcg.decodeXli)
    .then(sierraEcg.updateLeads)
    .then(sierraEcg.createObjects);
    
  return deferred.nodeify(cb);
}

module.exports = {
  readFile: readFile,
  readString: readString
};
