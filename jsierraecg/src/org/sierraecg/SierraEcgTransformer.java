/** jsierraecg - SierraEcgTransformer.java
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
package org.sierraecg;

import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;

import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.ParserConfigurationException;

import javax.xml.bind.JAXBException;
import javax.xml.xpath.XPath;
import javax.xml.xpath.XPathConstants;
import javax.xml.xpath.XPathFactory;
import javax.xml.xpath.XPathExpressionException;

import org.ibm.xml.UniversalNamespaceCache;

import org.sierraecg.codecs.Base64;
import org.sierraecg.codecs.XliDecompressor;

import org.w3c.dom.Document;
import org.w3c.dom.Node;
import org.w3c.dom.NodeList;

import org.xml.sax.SAXException;

public abstract class SierraEcgTransformer {

	public abstract DecodedLead[] transform(File output) throws IOException, JAXBException;

	public abstract DecodedLead[] extractLeads() throws IOException, JAXBException;

	protected ArrayList<int[]> decompressXli(InputStream in) throws IOException {
		ArrayList<int[]> leadData = new ArrayList<int[]>();

		XliDecompressor xli = new XliDecompressor(in);
		int[] payload;
		while (null != (payload = xli.readLeadPayload())) {
			leadData.add(payload);
		}

		return leadData;
	}

	public static SierraEcgTransformer create(File input) throws IOException, JAXBException, XPathExpressionException, ParserConfigurationException, SAXException {
		DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
		factory.setNamespaceAware(true);
		DocumentBuilder builder = factory.newDocumentBuilder();

		Document xdoc = builder.parse(input);
		xdoc.getDocumentElement().normalize();

		XPath xpath = XPathFactory.newInstance().newXPath();
		xpath.setNamespaceContext(new UniversalNamespaceCache(xdoc, true));

		Node node = (Node)xpath.evaluate("//DEFAULT:documentversion",
			xdoc, XPathConstants.NODE);
		if (node != null) {
			switch (node.getTextContent()) {
				case "1.03":
					return new SierraEcgTransformerImpl_1_03(xdoc.getDocumentElement());
				case "1.04":
				case "1.04.01":
					return new SierraEcgTransformerImpl_1_04(xdoc.getDocumentElement());

				default:
					throw new UnsupportedOperationException("Unsupported Sierra ECG XML file " + node.getTextContent());
			}
		}

		throw new UnsupportedOperationException("File does not appear to be a valid Sierra ECG XML file");
	}
}
