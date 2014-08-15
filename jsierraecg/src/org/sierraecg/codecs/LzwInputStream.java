/** jsierraecg - LzwInputStream.java
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

import java.io.FilterInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.util.Arrays;
import java.util.TreeMap;

/**
 * @author Christopher A. Watford
 *
 */
public class LzwInputStream extends FilterInputStream {

	private int bits;
	private int maxCode;
	private int bitCount;
	private long bitBuffer;
	
	public LzwInputStream(InputStream in, int bits) {
		super(in);
		
		this.bits = bits;
		this.maxCode = (1 << bits) - 2;
		
		for (int code = 0; code < 256; ++code) {
			this.strings.put(code, new byte[] { (byte)code });
		}
	}
	
	private int readCodeWord() throws IOException {
		int code;
		while (this.bitCount <= 24) {
			int input = super.read();
			if (input < 0) {
				return input;
			}
			this.bitBuffer |= ((input & 0xFFL) << (24L - this.bitCount)) & 0xFFFFFFFFL;
			this.bitCount += 8;
		}
		
		code = (int)((this.bitBuffer >> (32 - this.bits)) & 0x0000FFFFL);
		this.bitBuffer = ((((long)this.bitBuffer & 0xFFFFFFFFL) << this.bits) & 0xFFFFFFFFL);
		this.bitCount -= this.bits;
		
		return code;
	}
	
	private byte[] previous = new byte[0];
	private int nextCode = 256;
	private TreeMap<Integer,byte[]> strings = new TreeMap<Integer,byte[]>();
	
	private byte[] internalRead() throws IOException {
		int code;
		while (-1 != (code = this.readCodeWord())) {
			if (code >= this.maxCode + 1) {
				break;
			}
			
			byte[] data;
			if (!this.strings.containsKey(code)) {
				data = Arrays.copyOf(this.previous, this.previous.length + 1);
				data[this.previous.length] = this.previous[0];
				this.strings.put(code, data);
			}
			else {
				data = this.strings.get(code);
			}
			
			if (this.previous.length > 0 && nextCode <= this.maxCode) {
				byte[] nextData = Arrays.copyOf(this.previous, this.previous.length + 1);
				nextData[this.previous.length] = data[0];
				this.strings.put(this.nextCode++, nextData);
			}
			
			this.previous = data;
			return data;
		}
		
		return new byte[0];
	}
	
	private byte[] current;
	private int pos = -1;

	public int read() throws IOException {
		if (this.current == null || pos == this.current.length) {
			this.current = this.internalRead();
			this.pos = 0;
		}
		
		return this.current.length > 0 ? (this.current[this.pos++] & 0xFF) : -1;
	}
	
	public int read(byte[] buffer, int offset, int count) throws IOException {
		int read;
		for (read = 0; read <= count; read++) {
			int b = this.read();
			if (b == -1) break;
			
			buffer[offset + read] = (byte)(b & 0xFF);
		}
		
		return read > 0 ? read : -1;
	}
	
	public long skip(long n) throws IOException {
		for (long skipped = 0; skipped < n; ++skipped) {
			if (-1 == this.read()) return skipped;
		}
	
		return n;
	}
	
	public void mark(int readlimit) {
	}
	
	public void reset() throws IOException {
		throw new IOException("reset() is not supported by LzwInputStream");
	}
	
	public boolean markSupported() {
		return false;
	}
}
