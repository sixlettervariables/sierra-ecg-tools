from typing import List

import numpy as np
import numpy.typing as npt

from sierraecg.lzw import LzwDecoder


def xli_decode(data: bytes, labels: List[str]) -> List[npt.NDArray[np.int16]]:
    samples: List[npt.NDArray[np.int16]] = []
    offset = 0
    while offset < len(data):
        header = data[offset : offset + 8]
        offset += 8

        size = int.from_bytes(header[0:4], byteorder="little", signed=True)
        start = int.from_bytes(header[6:], byteorder="little", signed=True)
        chunk = data[offset : offset + size]
        offset += size

        decoder = LzwDecoder(chunk, bits=10)

        buffer = []
        while -1 != (b := decoder.read()):
            buffer.append(b & 0xFF)

        if len(buffer) % 2 == 1:
            buffer.append(0)

        deltas = xli_decode_deltas(buffer, start)
        samples.append(deltas)

    return samples


def xli_decode_deltas(buffer: List[int], first: int) -> npt.NDArray[np.int16]:
    deltas = xli_unpack(buffer)
    x = np.int16(deltas[0])
    y = np.int16(deltas[1])
    last = np.int16(first)
    for i in range(2, len(deltas)):
        z = np.int16((y + y) - x - last)
        last = np.int16(deltas[i] - 64)
        deltas[i] = z
        x = y
        y = z
    return deltas


def xli_unpack(buffer: List[int]) -> npt.NDArray[np.int16]:
    unpacked: npt.NDArray[np.int16] = np.array(
        [0 for _ in range(int(len(buffer) / 2))], dtype=np.int16
    )
    for i in range(len(unpacked)):
        joined_bytes = (((buffer[i] << 8) | buffer[len(unpacked) + i]) << 16) >> 16
        unpacked[i] = np.array(joined_bytes).astype(np.int16)
    return unpacked
