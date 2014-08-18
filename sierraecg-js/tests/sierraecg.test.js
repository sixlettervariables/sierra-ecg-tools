/** sierraecg.js: Tests for sierraecg.js
 *
 */
var test = require('tape');

var sierraEcg = require('../');

test('sierraecg', function (t) {
  t.test('can read SierraECG 1.03 file', function (t) {
    var now = new Date().getTime();
    sierraEcg.readFile('./tests/fixtures/129DYPRG.XML', function (err, ecg) {
      console.log('parsed in %d ms', new Date().getTime() - now);

      t.error(err, 'Should not throw an error');
      t.ok(ecg, 'ECG should exist');

      t.equal(ecg.version, '1.03', 'Version should be 1.03');

      t.equal(ecg.type, '12-Lead');
      t.ok(ecg.leads, 'ECG Leads Should Exist');
      t.equal(ecg.leads.length, 16);

      t.equal(ecg.leads[0].name, 'I', 'leads[0] should be "I"');
      t.equal(ecg.leads[0].data.length, 5500, 'Lead I should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead I should be enabled');
      t.equal(ecg.leads[1].name, 'II', 'leads[1] should be "II"');
      t.equal(ecg.leads[1].data.length, 5500, 'Lead II should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead III should be enabled');
      t.equal(ecg.leads[2].name, 'III', 'leads[2] should be "III"');
      t.equal(ecg.leads[2].data.length, 5500, 'Lead III should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead III should be enabled');
      t.equal(ecg.leads[3].name, 'aVR', 'leads[3] should be "aVR"');
      t.equal(ecg.leads[3].data.length, 5500, 'Lead aVR should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead aVR should be enabled');
      t.equal(ecg.leads[4].name, 'aVL', 'leads[4] should be "aVL"');
      t.equal(ecg.leads[4].data.length, 5500, 'Lead aVL should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead aVL should be enabled');
      t.equal(ecg.leads[5].name, 'aVF', 'leads[5] should be "aVF"');
      t.equal(ecg.leads[5].data.length, 5500, 'Lead aVF should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead aVF should be enabled');
      t.equal(ecg.leads[6].name, 'V1', 'leads[6] should be "V1"');
      t.equal(ecg.leads[6].data.length, 5500, 'Lead V1 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V1 should be enabled');
      t.equal(ecg.leads[7].name, 'V2', 'leads[7] should be "V2"');
      t.equal(ecg.leads[7].data.length, 5500, 'Lead V2 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V2 should be enabled');
      t.equal(ecg.leads[8].name, 'V3', 'leads[8] should be "V3"');
      t.equal(ecg.leads[8].data.length, 5500, 'Lead V3 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V3 should be enabled');
      t.equal(ecg.leads[9].name, 'V4', 'leads[9] should be "V4"');
      t.equal(ecg.leads[9].data.length, 5500, 'Lead V4 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V4 should be enabled');
      t.equal(ecg.leads[10].name, 'V5', 'leads[10] should be "V5"');
      t.equal(ecg.leads[10].data.length, 5500, 'Lead V5 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V5 should be enabled');
      t.equal(ecg.leads[11].name, 'V6', 'leads[11] should be "V6"');
      t.equal(ecg.leads[11].data.length, 5500, 'Lead V6 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V6 should be enabled');

      t.equal(ecg.leads[12].enabled, false, 'Channel 13 should NOT be enabled');
      t.equal(ecg.leads[13].enabled, false, 'Channel 14 should NOT be enabled');
      t.equal(ecg.leads[14].enabled, false, 'Channel 15 should NOT be enabled');
      t.equal(ecg.leads[15].enabled, false, 'Channel 16 should NOT be enabled');

      t.ok(ecg.originalXml, 'Original XML for the ECG should exist');

      t.end();
    });
  });

  t.test('can read SierraECG 1.04 file', function (t) {
    var now = new Date().getTime();
    sierraEcg.readFile('./tests/fixtures/3191723_ZZDEMOPTONLY_1-04_orig.xml', function (err, ecg) {
      console.log('parsed in %d ms', new Date().getTime() - now);

      t.error(err, 'Should not throw an error');
      t.ok(ecg, 'ECG should exist');

      t.equal(ecg.version, '1.04', 'Version should be 1.04');

      t.equal(ecg.type, '12-Lead');
      t.ok(ecg.leads, 'ECG Leads Should Exist');
      t.equal(ecg.leads.length, 16);

      t.equal(ecg.leads[0].name, 'I', 'leads[0] should be "I"');
      t.equal(ecg.leads[0].data.length, 5500, 'Lead I should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead I should be enabled');
      t.equal(ecg.leads[1].name, 'II', 'leads[1] should be "II"');
      t.equal(ecg.leads[1].data.length, 5500, 'Lead II should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead III should be enabled');
      t.equal(ecg.leads[2].name, 'III', 'leads[2] should be "III"');
      t.equal(ecg.leads[2].data.length, 5500, 'Lead III should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead III should be enabled');
      t.equal(ecg.leads[3].name, 'aVR', 'leads[3] should be "aVR"');
      t.equal(ecg.leads[3].data.length, 5500, 'Lead aVR should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead aVR should be enabled');
      t.equal(ecg.leads[4].name, 'aVL', 'leads[4] should be "aVL"');
      t.equal(ecg.leads[4].data.length, 5500, 'Lead aVL should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead aVL should be enabled');
      t.equal(ecg.leads[5].name, 'aVF', 'leads[5] should be "aVF"');
      t.equal(ecg.leads[5].data.length, 5500, 'Lead aVF should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead aVF should be enabled');
      t.equal(ecg.leads[6].name, 'V1', 'leads[6] should be "V1"');
      t.equal(ecg.leads[6].data.length, 5500, 'Lead V1 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V1 should be enabled');
      t.equal(ecg.leads[7].name, 'V2', 'leads[7] should be "V2"');
      t.equal(ecg.leads[7].data.length, 5500, 'Lead V2 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V2 should be enabled');
      t.equal(ecg.leads[8].name, 'V3', 'leads[8] should be "V3"');
      t.equal(ecg.leads[8].data.length, 5500, 'Lead V3 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V3 should be enabled');
      t.equal(ecg.leads[9].name, 'V4', 'leads[9] should be "V4"');
      t.equal(ecg.leads[9].data.length, 5500, 'Lead V4 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V4 should be enabled');
      t.equal(ecg.leads[10].name, 'V5', 'leads[10] should be "V5"');
      t.equal(ecg.leads[10].data.length, 5500, 'Lead V5 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V5 should be enabled');
      t.equal(ecg.leads[11].name, 'V6', 'leads[11] should be "V6"');
      t.equal(ecg.leads[11].data.length, 5500, 'Lead V6 should have 5500 samples');
      t.equal(ecg.leads[0].enabled, true, 'Lead V6 should be enabled');

      t.equal(ecg.leads[12].enabled, false, 'Channel 13 should NOT be enabled');
      t.equal(ecg.leads[13].enabled, false, 'Channel 14 should NOT be enabled');
      t.equal(ecg.leads[14].enabled, false, 'Channel 15 should NOT be enabled');
      t.equal(ecg.leads[15].enabled, false, 'Channel 16 should NOT be enabled');

      t.ok(ecg.originalXml, 'Original XML for the ECG should exist');

      t.end();
    });
  });

  t.end();
});
