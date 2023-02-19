from base64 import b64decode
from math import floor
from typing import List, Optional, Tuple, Union, cast
from xml.dom.minidom import Attr, Document

from defusedxml import minidom
import numpy as np
import numpy.typing as npt

from sierraecg.xli import xli_decode


class UnsupportedXmlFileError(RuntimeError):
    """Raised when the XML file format is unsupported"""


class MissingXmlElementError(RuntimeError):
    """Raised when a required XML element is missing"""


class MissingXmlAttributeError(RuntimeError):
    """Raised when a required XML attribute is missing"""


class EcgLead:
    """Represents an ECG Lead"""

    label = ""
    sampling_freq = 0
    duration = 0
    samples: npt.NDArray[np.int16] = np.array([], dtype=np.int16)


class SierraEcgFile:
    """Represents a Sierra ECG File"""

    doc_type = ""
    doc_ver = ""
    leads: List[EcgLead] = []


def read_file(filename: str) -> SierraEcgFile:
    xdom = minidom.parse(filename)
    root = get_node(xdom, "restingecgdata")
    (doc_type, doc_ver) = assert_version(root)

    leads = assert_leads(root)

    lead_i = leads[0].samples
    lead_ii = leads[1].samples
    lead_iii = leads[2].samples
    lead_avr = leads[3].samples
    lead_avl = leads[4].samples
    lead_avf = leads[5].samples

    for i in range(len(lead_iii)):
        lead_iii[i] = lead_ii[i] - lead_i[i] - lead_iii[i]

    for i in range(len(lead_avr)):
        lead_avr[i] = -lead_avr[i] - floor((lead_i[i] + lead_ii[i]) / 2)

    for i in range(len(lead_avl)):
        lead_avl[i] = floor((lead_i[i] - lead_iii[i]) / 2) - lead_avl[i]

    for i in range(len(lead_avf)):
        lead_avf[i] = floor((lead_ii[i] + lead_iii[i]) / 2) - lead_avf[i]

    sierra_ecg_file = SierraEcgFile()
    sierra_ecg_file.doc_type = doc_type
    sierra_ecg_file.doc_ver = doc_ver
    sierra_ecg_file.leads = leads

    return sierra_ecg_file


def assert_version(elt: Document) -> Tuple[str, str]:
    doc_info = get_node(elt, "documentinfo")
    doc_type = get_text(get_node(doc_info, "documenttype"))
    doc_ver = get_text(get_node(doc_info, "documentversion"))
    if doc_type not in ["SierraECG", "PhilipsECG"] or doc_ver not in [
        "1.03",
        "1.04",
        "1.04.01",
        "1.04.02",
    ]:
        raise UnsupportedXmlFileError(f"Files of type {doc_type} {doc_ver} are unsupported")

    return (doc_type, doc_ver)


def assert_leads(elt: Document) -> List[EcgLead]:
    signal_details = get_node(get_node(elt, "dataacquisition"), "signalcharacteristics")
    parsed_waveforms = get_node(elt, "parsedwaveforms")

    sampling_freq = int(get_text(get_node(signal_details, "samplingrate")))
    duration = int(get_attr(parsed_waveforms, "durationperchannel"))

    labels = get_or_create_labels(signal_details, parsed_waveforms)
    waveform_data = get_waveform_data(signal_details, parsed_waveforms, labels)

    leads: List[EcgLead] = []
    for index, label in enumerate(labels):
        lead = EcgLead()
        lead.label = label
        lead.sampling_freq = sampling_freq
        lead.duration = duration
        lead.samples = waveform_data[index]
        leads.append(lead)

    return leads


def get_waveform_data(
    signal_details: Document, parsed_waveforms: Document, labels: List[str]
) -> List[npt.NDArray[np.int16]]:
    sampling_freq = int(get_text(get_node(signal_details, "samplingrate")))
    duration = int(get_attr(parsed_waveforms, "durationperchannel"))
    sample_count = int(duration * (sampling_freq / 1000))

    encoding = get_attr(parsed_waveforms, "dataencoding")
    waveform_data = None
    if encoding == "Base64":
        waveform_data = read_base64_encoding(get_text(parsed_waveforms))
    else:
        raise UnsupportedXmlFileError(f"Waveform data encoding unspported: {encoding}")

    compression_method = infer_compression(parsed_waveforms)
    if compression_method != "Uncompressed":
        if compression_method == "XLI":
            return xli_decode(waveform_data, labels)
        else:
            raise UnsupportedXmlFileError(
                f"Waveform data compression algorithm unspported: {compression_method}"
            )

    return split_leads(waveform_data, len(labels), sample_count)


def read_base64_encoding(text: str) -> bytes:
    return b64decode(text)


def infer_compression(parsed_waveforms: Document) -> str:
    return get_attr(
        parsed_waveforms,
        "compressmethod",
        get_attr(parsed_waveforms, "compression", "Uncompressed"),
    )


def split_leads(
    waveform_data: bytes, lead_count: int, samples: int
) -> List[npt.NDArray[np.int16]]:
    all_samples: npt.NDArray[np.int16] = np.frombuffer(waveform_data, dtype=np.int16)
    leads: List[npt.NDArray[np.int16]] = []

    offset = 0
    while offset < lead_count * samples:
        lead = all_samples[offset : offset + samples]
        leads.append(lead)
        offset += samples

    return leads


def get_or_create_labels(signal_details: Document, parsed_waveforms: Document) -> List[str]:
    lead_labels = get_attr(parsed_waveforms, "leadlabels", "")
    if lead_labels != "":
        lead_count = int(get_attr(parsed_waveforms, "numberofleads"))
        return lead_labels.split(" ")[:lead_count]
    else:
        good_channels = int(get_text(get_node(signal_details, "numberchannelsallocated")))
        leads_used = get_text(get_node(signal_details, "acquisitiontype"))
        return [get_lead_name(leads_used, x + 1) for x in range(good_channels)]


def get_lead_name(leads_used: str, index: int) -> str:
    if leads_used in ["STD-12", "10-WIRE"]:
        if index == 1:
            return "I"
        elif index == 2:
            return "II"
        elif index == 3:
            return "III"
        elif index == 4:
            return "aVR"
        elif index == 5:
            return "aVL"
        elif index == 6:
            return "aVF"
        elif index > 6 and index <= 12:
            return f"V{index - 6}"

    return f"Channel {index}"


def get_node(xdoc: Document, tag_name: str) -> Document:
    xelt = get_opt_node(xdoc, tag_name)
    if xelt is None:
        raise MissingXmlElementError(tag_name)
    return xelt


def get_opt_node(xdoc: Document, tag_name: str) -> Optional[Document]:
    for xelt in xdoc.getElementsByTagName(tag_name):
        return cast(Document, xelt)
    return None


def get_attr(xdoc: Document, attr_name: str, default: Optional[str] = None) -> str:
    if attr_name in xdoc.attributes:
        return get_text(xdoc.attributes[attr_name])
    if default is None:
        raise MissingXmlAttributeError(attr_name)
    return default


def get_text(xdoc: Union[Document, Attr]) -> str:
    rc = []
    for node in xdoc.childNodes:
        if node.nodeType == node.TEXT_NODE:
            rc.append(node.data)
    return "".join(rc)
