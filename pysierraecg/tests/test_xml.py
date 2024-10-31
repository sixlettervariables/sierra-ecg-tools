from typing import List, Optional

import pytest

from sierraecg import UnsupportedXmlFileError, read_file


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
    "filename, expected_leads, expected_midpoints",
    [
        (
            "tests/fixtures/1_03/129DYPRG.XML",
            12,
            [-9, -17, -7, 13, -1, -12, 0, -1, -3, -4, -7, -6],
        ),
        (
            "tests/fixtures/1_04/3191723_ZZDEMOPTONLY_1-04_orig.xml",
            12,
            [-11, -54, -43, 33, 16, -50, 7, -4, -14, -41, -35, -33],
        ),
        (
            "tests/fixtures/1_04/ad4d3d80-d165_1-04_orig.xml",
            12,
            [32, 46, 14, -39, 9, 30, -74, 320, -1633, 197, 26, 55],
        ),
        (
            "tests/fixtures/1_04_01/2020-5-18_15-48-11.xml",
            12,
            [-12, 78, 90, -33, -51, 84, 14, 3, -24, -20, -13, -21],
        ),
    ],
)
def test_assert_leads(filename: str, expected_leads: int, expected_midpoints: List[int]) -> None:
    f = read_file(filename)
    assert len(f.leads) == expected_leads
    assert f.leads[0].label == "I"
    assert len(f.leads[0].samples) == 5500
    assert f.leads[0].samples[2750] == expected_midpoints[0]
    assert f.leads[1].label == "II"
    assert len(f.leads[1].samples) == 5500
    assert f.leads[1].samples[2750] == expected_midpoints[1]
    assert f.leads[2].label == "III"
    assert len(f.leads[2].samples) == 5500
    assert f.leads[2].samples[2750] == expected_midpoints[2]
    assert f.leads[3].label == "aVR"
    assert len(f.leads[3].samples) == 5500
    assert f.leads[3].samples[2750] == expected_midpoints[3]
    assert f.leads[4].label == "aVL"
    assert len(f.leads[4].samples) == 5500
    assert f.leads[4].samples[2750] == expected_midpoints[4]
    assert f.leads[5].label == "aVF"
    assert len(f.leads[5].samples) == 5500
    assert f.leads[5].samples[2750] == expected_midpoints[5]
    assert f.leads[6].label == "V1"
    assert len(f.leads[6].samples) == 5500
    assert f.leads[6].samples[2750] == expected_midpoints[6]
    assert f.leads[7].label == "V2"
    assert len(f.leads[7].samples) == 5500
    assert f.leads[7].samples[2750] == expected_midpoints[7]
    assert f.leads[8].label == "V3"
    assert len(f.leads[8].samples) == 5500
    assert f.leads[8].samples[2750] == expected_midpoints[8]
    assert f.leads[9].label == "V4"
    assert len(f.leads[9].samples) == 5500
    assert f.leads[9].samples[2750] == expected_midpoints[9]
    assert f.leads[10].label == "V5"
    assert len(f.leads[10].samples) == 5500
    assert f.leads[10].samples[2750] == expected_midpoints[10]
    assert f.leads[11].label == "V6"
    assert len(f.leads[11].samples) == 5500
    assert f.leads[11].samples[2750] == expected_midpoints[11]


@pytest.mark.parametrize(
    "filename, expected_labels, expected_samples, expected_duration, expected_midpoints",
    [
        (
            "tests/fixtures/1_03/129DYPRG.XML",
            [],
            None,
            None,
            [],
        ),
        (
            "tests/fixtures/1_03/repbeats_example.xml",
            ["I", "II", "III", "aVR", "aVL", "aVF", "V1", "V2", "V3", "V4", "V5", "V6"],
            1200,
            1200,
            [-11, -36, -32, 25, 5, -29, 12, 1, -20, -51, -58, -52],
        ),
        (
            "tests/fixtures/1_04/3191723_ZZDEMOPTONLY_1-04_orig.xml",
            ["I", "II", "III", "aVR", "aVL", "aVF", "V1", "V2", "V3", "V4", "V5", "V6"],
            1200,
            2400,
            # CAW: this seems off
            [32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767],
        ),
        (
            "tests/fixtures/1_04/ad4d3d80-d165_1-04_orig.xml",
            ["I", "II", "III", "aVR", "aVL", "aVF", "V1", "V2", "V3", "V4", "V5", "V6"],
            1200,
            2400,
            # CAW: this seems off
            [32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767, 32767],
        ),
        (
            "tests/fixtures/1_04_01/2020-5-18_15-48-11.xml",
            ["I", "II", "III", "aVR", "aVL", "aVF", "V1", "V2", "V3", "V4", "V5", "V6"],
            1200,
            1200,
            [-11, -36, -32, 25, 5, -29, 12, 1, -20, -51, -58, -52],
        ),
    ],
)
def test_assert_repbeats(
    filename: str,
    expected_labels: List[str],
    expected_samples: Optional[int],
    expected_duration: Optional[int],
    expected_midpoints: List[int],
) -> None:
    f = read_file(filename, include_repbeats=True)
    assert len(f.repbeats) == len(expected_labels)
    for i, expected_label in enumerate(expected_labels):
        repbeat = f.repbeats[expected_label]
        assert repbeat is not None
        assert repbeat.label == expected_label
        if expected_samples is not None and expected_duration is not None:
            assert repbeat.duration == expected_duration
            assert len(repbeat.samples) == expected_samples
            assert repbeat.samples[int(expected_samples / 2)] == expected_midpoints[i]


def test_no_repbeats() -> None:
    f = read_file("tests/fixtures/1_04_01/2020-5-18_15-48-11.xml")
    assert len(f.repbeats) == 0

    f = read_file("tests/fixtures/1_04_01/2020-5-18_15-48-11.xml", include_repbeats=False)
    assert len(f.repbeats) == 0
