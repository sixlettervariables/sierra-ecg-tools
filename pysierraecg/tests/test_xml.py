import pytest

from sierraecg.lib import UnsupportedXmlFileError, read_file


@pytest.mark.parametrize(
    "filename, expected_doc_type, expected_doc_ver",
    [
        ("tests/fixtures/1_03/129DYPRG.XML", "SierraECG", "1.03"),
        ("tests/fixtures/1_04/3191723_ZZDEMOPTONLY_1-04_orig.xml", "PhilipsECG", "1.04"),
        ("tests/fixtures/1_04/ad4d3d80-d165_1-04_orig.xml", "PhilipsECG", "1.04"),
        ("tests/fixtures/1_04_01/2020-5-18_15-48-11.xml", "PhilipsECG", "1.04.01"),
    ],
)
def test_read_file(filename: str, expected_doc_type: str, expected_doc_ver: str) -> None:
    f = read_file(filename)
    assert f.doc_type == expected_doc_type
    assert f.doc_ver == expected_doc_ver


@pytest.mark.parametrize(
    "filename, expected_message",
    [
        ("tests/fixtures/invalid-doc-type.xml", "Files of type MortaraECG 1.03 are unsupported"),
        ("tests/fixtures/invalid-doc-version.xml", "Files of type SierraECG 1.05 are unsupported"),
    ],
)
def test_assert_version(filename: str, expected_message: str) -> None:
    with pytest.raises(UnsupportedXmlFileError) as e_info:
        read_file(filename)
        assert str(e_info.value) == expected_message


@pytest.mark.parametrize(
    "filename, expected_leads",
    [
        ("tests/fixtures/1_03/129DYPRG.XML", 12),
        ("tests/fixtures/1_04/3191723_ZZDEMOPTONLY_1-04_orig.xml", 12),
        ("tests/fixtures/1_04/ad4d3d80-d165_1-04_orig.xml", 12),
        ("tests/fixtures/1_04_01/2020-5-18_15-48-11.xml", 12),
    ],
)
def test_assert_leads(filename: str, expected_leads: int) -> None:
    f = read_file(filename)
    assert len(f.leads) == expected_leads
    assert f.leads[0].label == "I"
    assert len(f.leads[0].samples) == 5500
    assert f.leads[1].label == "II"
    assert len(f.leads[1].samples) == 5500
    assert f.leads[2].label == "III"
    assert len(f.leads[2].samples) == 5500
    assert f.leads[3].label == "aVR"
    assert len(f.leads[3].samples) == 5500
    assert f.leads[4].label == "aVL"
    assert len(f.leads[4].samples) == 5500
    assert f.leads[5].label == "aVF"
    assert len(f.leads[5].samples) == 5500
    assert f.leads[6].label == "V1"
    assert len(f.leads[6].samples) == 5500
    assert f.leads[7].label == "V2"
    assert len(f.leads[7].samples) == 5500
    assert f.leads[8].label == "V3"
    assert len(f.leads[8].samples) == 5500
    assert f.leads[9].label == "V4"
    assert len(f.leads[9].samples) == 5500
    assert f.leads[10].label == "V5"
    assert len(f.leads[10].samples) == 5500
    assert f.leads[11].label == "V6"
    assert len(f.leads[11].samples) == 5500
