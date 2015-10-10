/**
 * awsm-sierraecg: Decode compressed SierraECG and PhilipsECG XML files.
 * @returns JSON description of the decoded ECG
 */

 var sierraEcg = require('awsm-sierraecg');

// Export For Lambda Handler
module.exports.run = function(event, context, cb) {
  return readSierraEcgXml(event.body, cb);
};

// Your Code
var readSierraEcgXml = function(xml, cb) {
  return sierraEcg.readString(xml, function (err, result) {
    if (err) {
      console.error(err);
    }

    return cb(err, result);
  });
};
