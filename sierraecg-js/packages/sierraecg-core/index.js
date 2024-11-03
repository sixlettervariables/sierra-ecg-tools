'use strict';

const LzwReader = require('./lib/lzw');
const { Ecg, Lead } = require('./lib/sierraecg');
const XliReader = require('./lib/xli');

module.exports = {
    Ecg,
    Lead,
    LzwReader,
    XliReader,
};
