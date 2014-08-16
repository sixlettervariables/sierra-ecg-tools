/** jsierraecg - SierraEcgReaderImpl_1_04.java
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

import javax.xml.bind.JAXBContext;
import javax.xml.bind.JAXBException;
import javax.xml.bind.Marshaller;
import javax.xml.bind.Unmarshaller;

import org.sierraecg.codecs.Base64;
import org.sierraecg.codecs.XliDecompressor;
import org.sierraecg.schema.jaxb._1_04.Parsedwaveforms;
import org.sierraecg.schema.jaxb._1_04.Restingecgdata;
import org.sierraecg.schema.jaxb._1_04.TYPEdataencoding;

import org.w3c.dom.Node;

public class SierraEcgTransformerImpl_1_04 extends SierraEcgTransformer {

	private Restingecgdata input;

	public SierraEcgTransformerImpl_1_04(Node node) throws JAXBException {
		super();
		JAXBContext context = JAXBContext.newInstance("org.sierraecg.schema.jaxb._1_04");
		Unmarshaller reader = context.createUnmarshaller();
		this.input = (Restingecgdata)reader.unmarshal(node);
	}

	public DecodedLead[] extractLeads() throws IOException {
		Parsedwaveforms parsedwaveforms = input.getWaveforms().getParsedwaveforms();
		
		InputStream in = new ByteArrayInputStream(parsedwaveforms.getValue().getBytes());
		if (parsedwaveforms.getDataencoding() == TYPEdataencoding.BASE_64) {
			in = new Base64.InputStream(in);
		}
		
		ArrayList<int[]> leadData;
		if (parsedwaveforms.getCompression().equals("XLI")) {
			leadData = decompressXli(in);
		}
		else {
			leadData = new ArrayList<int[]>();
		}
		
		String reportType;
		if (parsedwaveforms.getNumberofleads().intValue() == 12) {
			// map to 1.03 form which is used by DecodedLead
			reportType = "STD-12";
		}
		else {
			reportType = parsedwaveforms.getNumberofleads().toString()
					   + " "
					   + parsedwaveforms.getLeadlabels();
		}

		DecodedLead[] leads = DecodedLead.createFromLeadSet(reportType, leadData);
		
		return leads;
	}

	public DecodedLead[] transform(File output) throws IOException, JAXBException {
		DecodedLead[] leads = extractLeads();
		
		StringBuffer buffer = new StringBuffer();
        for (DecodedLead lead : leads) {
        	for (int count = 0; count < lead.size(); ++count) {
        		buffer.append(lead.get(count));
        		if (count % 25 > 0 || count == 0) {
        			buffer.append(" ");
        		} 
        		else {
        			buffer.append("\n");
        		}
        	}
        }
        
        Parsedwaveforms parsedwaveforms = this.input.getWaveforms().getParsedwaveforms();
        parsedwaveforms.setDataencoding(TYPEdataencoding.PLAIN);
        parsedwaveforms.setCompression(null);
        parsedwaveforms.setValue(buffer.toString());
        
		JAXBContext context = JAXBContext.newInstance("org.sierraecg.schema.jaxb._1_04");
        Marshaller writer = context.createMarshaller();
        writer.setProperty(Marshaller.JAXB_FORMATTED_OUTPUT, true);
        writer.marshal(this.input, output);

        return leads;
	}
}
