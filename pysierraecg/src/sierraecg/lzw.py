import array
from typing import Dict, Literal, Optional


class LzwDecoder(object):
    """Provides a decoder for LZW compression"""

    buffer: bytes
    offset: int = 0
    bits: int = 0
    max_code: int = 0

    bit_count: int = 0
    bit_buffer: int = 0

    previous: array.array = array.array("B", [])
    next_code: int = 256
    strings: Dict[int, array.array] = {}

    current: Optional[array.array] = None
    position: int = 0

    def __init__(self, buffer: bytes, bits: int):
        self.buffer = buffer
        self.bits = bits
        self.max_code = (1 << bits) - 2
        self.strings = {code: array.array("B", [code]) for code in range(256)}

    def read(self) -> int:
        if self.current is None or self.position == len(self.current):
            self.current = self._read_next_string()
            self.position = 0

        if len(self.current) > 0:
            byte = self.current[self.position] & 0xFF
            self.position += 1
            return byte

        return -1

    def read_bytes(self, count: int) -> array.array:
        return array.array("B", [self.read() for _ in range(count)])

    def _read_next_string(self) -> array.array:
        code = self._read_codepoint()
        if code >= 0 and code <= self.max_code:
            data: array.array
            if code not in self.strings:
                data = self.previous[:]
                data.append(self.previous[0])
                self.strings[code] = data
            else:
                data = self.strings[code]

            if len(self.previous) > 0 and self.next_code <= self.max_code:
                next_data = self.previous[:]
                next_data.append(data[0])
                self.strings[self.next_code] = next_data
                self.next_code += 1

            self.previous = data
            return data

        return array.array("B", [])

    def _read_codepoint(self) -> int:
        code = 0
        while self.bit_count <= 24:
            if self.offset < len(self.buffer):
                next_byte = self.buffer[self.offset]
                self.offset += 1
                self.bit_buffer |= ((next_byte & 0xFF) << (24 - self.bit_count)) & 0xFFFFFFFF
                self.bit_count += 8
            else:
                return -1

        code = (self.bit_buffer >> (32 - self.bits)) & 0x0000FFFF
        self.bit_buffer = ((self.bit_buffer & 0xFFFFFFFF) << self.bits) & 0xFFFFFFFF
        self.bit_count -= self.bits

        return code
