/** sierraecg.js: Sierra ECG Reader.
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

const xml2js = require('xml2js');

const XliReader = require('./xli');

class Ecg {
  /**
   * @param {string} type 
   * @param {Lead[]} leads 
   * @param {string} version
   * @param {any} originalXml
   */
  constructor(type, leads, version, originalXml) {
    this.type = type;
    this.leads = leads;
    this.version = version;
    this.originalXml = originalXml;
  }

  /**
   * Creates an Ecg instance from an XML document.
   * @param {string} text Philips SierraECG XML document (stringy format)
   * @returns {Promise<Ecg>} an Ecg instance from the XML document given in xdoc.
   */
  static async fromXmlAsync(text) {
    const parser = new xml2js.Parser();
    const xdoc = await parser.parseStringPromise(text);
    return _createEcgFromXdoc(xdoc);
  }
}

class Lead {
  /**
   * @param {string} name 
   * @param {number[]} data 
   * @param {boolean} enabled 
   */
  constructor(name, data, enabled) {
    this.name = name;
    this.data = data;
    this.enabled = enabled;
  }
}

  /**
   * Creates an Ecg instance from an XML document.
   * @param {any} xdoc Philips SierraECG XML document (xml2js format)
   * @returns {Ecg} an Ecg instance from the XML document given in xdoc.
   */
  function _createEcgFromXdoc(xdoc) {
    const { xml, version, numberOfLeads, leadLabels, parsedWaveforms } = _parseXdoc(xdoc);

    const reader = new XliReader(parsedWaveforms);
    const rawLeads = reader.extractLeads();
  
    // We need to calculate a number of leads from the limb leads
    _recalculateLeads(rawLeads);

    const leads = rawLeads.map((lead, index) => {
      const enabled = index < numberOfLeads;
      return new Lead(_nameifyLead(index, leadLabels), lead, enabled);
    });
  
    return new Ecg('12-Lead', leads, version, xml);
  }

  /**
   * Parses an XML document in the Philips SierraECG format.
   * @param {any} xdoc SierraECG XML document as a JavaScript object.
   * @returns {{ xml: any, version?: string, numberOfLeads: number, leadLabels: string[], parsedWaveforms: Uint8Array }}
   */
function _parseXdoc(xdoc) {
  let version, type;

  if (xdoc.restingecgdata.documentinfo[0].documenttype[0]) {
    type = xdoc.restingecgdata.documentinfo[0].documenttype[0];
    if (!(type === 'SierraECG' || type === 'PhilipsECG')) {
      throw new Error('Unsupported XML type ' + xdoc.restingecgdata.documentinfo[0].documenttype[0]);
    }
  }

  if (xdoc.restingecgdata.documentinfo[0].documentversion[0]) {
    version = xdoc.restingecgdata.documentinfo[0].documentversion[0];
    switch (version) {
      case '1.04.01':
        version = '1.04';
        break;
      case '1.03':
      case '1.04':
        break;
      default:
        throw new Error('Unsupported SierraECG versions ' + version);
    }
  }

  const parsedwaveforms = xdoc.restingecgdata.waveforms[0].parsedwaveforms[0];
  if (!parsedwaveforms) {
    throw new Error('Invalid Sierra ECG XML document');
  }

  let isXli = false, leadLabels = [], numberOfLeads = 0;

  // 1. determine encoding
  const isBase64 = parsedwaveforms.$.dataencoding === 'Base64';

  // 2. determine compression and report type
  if (version === '1.03') {
    isXli = (parsedwaveforms.$.compressflag === 'True') &&
            (parsedwaveforms.$.compressmethod === 'XLI');

    const signalCharacteristics = xdoc.restingecgdata.dataacquisition[0].signalcharacteristics[0];
    if (signalCharacteristics.leadset[0] === 'STD-12') {
      leadLabels = kStd12Leads.slice();
    }

    numberOfLeads = parseInt(signalCharacteristics.numberchannelsvalid[0], 10);
  }
  else if (version === '1.04') {
    isXli = parsedwaveforms.$.compression === 'XLI';
    numberOfLeads = parseInt(parsedwaveforms.$.numberofleads, 10);
    leadLabels = parsedwaveforms.$.leadlabels.split(' ');
  }

  if (isBase64 && isXli) {
    // TODO: Base64 decode to Uint8Array without `Buffer`
    const b64 = Buffer.from(parsedwaveforms._, 'base64');
    return ({ xml: xdoc, version, numberOfLeads, leadLabels, parsedWaveforms: b64 });
  }
  else {
    throw new Error('Unsupported compression method on XML');
  }
}


function _recalculateLeads(leads) {
  let ii;

  const leadI = leads[0];
  const leadII = leads[1];
  const leadIII = leads[2];
  const leadAVR = leads[3];
  const leadAVL = leads[4];
  const leadAVF = leads[5];

  // lead III
  for (ii = 0; ii < leadIII.length; ++ii) {
    leadIII[ii] = leadII[ii] - leadI[ii] - leadIII[ii];
  }

  // lead aVR
  for (ii = 0; ii < leadAVR.length; ++ii) {
    leadAVR[ii] = -leadAVR[ii] - Math.floor((leadI[ii] + leadII[ii]) / 2);
  }

  // lead aVL
  for (ii = 0; ii < leadAVL.length; ++ii) {
    leadAVL[ii] = Math.floor((leadI[ii] - leadIII[ii]) / 2) - leadAVL[ii];
  }

  // lead aVF
  for (ii = 0; ii < leadAVF.length; ++ii) {
    leadAVF[ii] = Math.floor((leadII[ii] + leadIII[ii]) / 2) - leadAVF[ii];
  }
}

const kStd12Leads = ['I', 'II', 'III', 'aVR', 'aVL', 'aVF', 'V1', 'V2', 'V3', 'V4', 'V5', 'V6'];

/**
 * 
 * @param {Number} index 
 * @param {Array<string>} leads 
 * @returns {string}
 */
function _nameifyLead(index, leads) {
  leads || (leads = kStd12Leads);

  if (index < leads.length) return leads[index];
  return 'Channel ' + (index + 1);
}

module.exports = {
  Ecg,
  Lead,
};
