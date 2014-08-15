/** jsierraecg - XliDecompressor.java
 *  Copyright (c) 2011 Christopher A. Watford
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy of
 *  this software and associated documentation files (the "Software"), to deal in
 *  the Software without restriction, including without limitation the rights to
 *  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
 *  of the Software, and to permit persons to whom the Software is furnished to do
 *  so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */
package org.sierraecg.codecs;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.Arrays;

public class XliDecompressor {
	private InputStream in;
	
	public XliDecompressor(InputStream in) {
		this.in = in;
	}
	
	public int[] readLeadPayload() throws IOException {
		byte[] chunkHeader = new byte[8];
		if (chunkHeader.length != this.in.read(chunkHeader)) {
			return null;
		}
		
        // Each chunk begins with an 8 byte header consisting of:
        //   - 32-bit integer describing the length of the chunk
        //   - 16-bit integer (unknown payload, seemingly always 1)
        //   - 16-bit integer describing the first delta code
        //
        // After this header exists LZW-compressed delta codes, using 10-bit code words:
        // 0        2        4        6        8  ...
        // +--------+--------+--------+--------+--------+--------+--------+--------+--------+
        // | Size            |  Unk.  | Delta  | LZW compressed deltas (10-bit codes)       |
        // +--------+--------+--------+--------+                                            |
        // | ...                                                               [Size bytes] |
        // +--------+--------+--------+--------+--------+--------+--------+--------+--------+
		ByteBuffer header = ByteBuffer.wrap(chunkHeader);
		header.order(ByteOrder.LITTLE_ENDIAN);
		
		int size = header.getInt(0);
		/*short unknown = header.getShort(4);*/
		short start = header.getShort(6);
		
		byte[] chunk = new byte[size];
		if (chunk.length != this.in.read(chunk)) {
			return null;
		}
		
		LzwInputStream lzw = new LzwInputStream(new ByteArrayInputStream(chunk), 10);
		
		int b;
		ArrayList<Byte> bytes = new ArrayList<Byte>();
		while (-1 != (b = lzw.read())) {
			bytes.add((byte)(b & 0xFF));
		}
		
		if (bytes.size() % 2 == 1) bytes.add((byte)0);
		
		return this.decodeDeltas(this.unpack(bytes), start);
	}

	private int[] unpack(ArrayList<Byte> bytes) {
		int[] actual = new int[bytes.size() / 2];
		for (int ii = 0; ii < actual.length; ++ii) {
			int hi = (bytes.get(ii) << 8) & 0xFFFF;
			int lo = bytes.get(actual.length + ii) & 0xFF;
			actual[ii] = (short)(hi | lo);
		}
		
		return actual;
	}
	
	private int[] decodeDeltas(int[] input, short initialValue) {
		int[] deltas = Arrays.copyOf(input, input.length);
		int x = deltas[0];
		int y = deltas[1];
		int lastValue = initialValue;
		for (int ii = 2; ii < deltas.length; ++ii) {
			int z = (y + y) - x - lastValue;
			lastValue = deltas[ii] - 64;
			deltas[ii] = z;
			x = y;
			y = z;
		}
		
		return deltas;
	}
}
