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

const XliReader = require('./xli');

class Ecg {
  constructor(type, leads) {
    this.type = type;
    this.leads = leads;
    this.version = null;
    this.originalXml = null;
  }
}

class Lead {
  constructor(name, data, enabled) {
    this.name = name;
    this.data = data;
    this.enabled = enabled;
  }
}

function SierraEcg_ParseXml(xdoc) {
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
    const b64 = Buffer.from(parsedwaveforms._, 'base64');
    return ({ xml: xdoc, version, numberOfLeads, leadLabels, parsedWaveforms: b64 });
  }
  else {
    throw new Error('Unsupported compression method on XML');
  }
}

function SierraEcg_DecodeXliAsync(ecg) {
  return new Promise(function (resolve, reject) {
    const reader = new XliReader(ecg.parsedWaveforms);
    reader.extractLeads(function (err, leads) {
      if (err) return reject(err);
      ecg.leads = leads;

      // get rid of our old crap
      delete ecg.parsedWaveforms;

      return resolve(ecg);
    });
  });
}

function SierraEcg_UpdateLeads(ecg) {
  let ii;

  const leadI = ecg.leads[0];
  const leadII = ecg.leads[1];
  const leadIII = ecg.leads[2];
  const leadAVR = ecg.leads[3];
  const leadAVL = ecg.leads[4];
  const leadAVF = ecg.leads[5];

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

  return ecg;
}

const kStd12Leads = ['I', 'II', 'III', 'aVR', 'aVL', 'aVF', 'V1', 'V2', 'V3', 'V4', 'V5', 'V6'];
/**
 * 
 * @param {Number} index 
 * @param {Array<string>} leads 
 * @returns {string}
 */
function nameifyLead(index, leads) {
  leads || (leads = kStd12Leads);

  if (index < leads.length) return leads[index];
  return 'Channel ' + (index + 1);
}

function SierraEcg_CreateObjects(ecg) {
  const leads = ecg.leads.map(function (lead, index) {
    const enabled = index < ecg.numberOfLeads;
    return new Lead(nameifyLead(index, ecg.leadLabels), lead, enabled);
  });

  const obj = new Ecg('12-Lead', leads);
  obj.version = ecg.version;
  obj.originalXml = ecg.xml;

  return obj;
}

module.exports = {
  Ecg,
  Lead,
  parseXml: SierraEcg_ParseXml,
  decodeXliAsync: SierraEcg_DecodeXliAsync,
  updateLeads: SierraEcg_UpdateLeads,
  createObjects: SierraEcg_CreateObjects
};
