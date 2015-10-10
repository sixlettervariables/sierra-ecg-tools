//
// Simply provides forwards for sierraecg.
//
'use strict';

var sierraEcg = require('sierraecg');

var readSierraEcgXmlString = function SierraEcg_ReadStringNoXml(xml, cb) {
    return sierraEcg.readString(xml, function (err, result) {
      if (err) {
        return cb(err);
      }
      else {
        // save on bytes, caller sent us the XML so they don't need it as JSON
        delete result.originalXml;

        return cb(null, result);
      }
    });
}

module.exports = {
  readFile: sierraEcg.readFile,
  readString: readSierraEcgXmlString
};
