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
import org.sierraecg.schema.Parsedwaveforms;
import org.sierraecg.schema.Restingecgdata;
import org.sierraecg.schema.TYPEcompress;
import org.sierraecg.schema.TYPEdataencoding;
import org.sierraecg.schema.TYPEflag;
import org.sierraecg.schema.TYPEreporttype;

public final class SierraEcgFiles {
	
	private SierraEcgFiles() {
	}
	
	private static Restingecgdata preprocess(JAXBContext context, File input) throws JAXBException, IOException {
		Unmarshaller reader = context.createUnmarshaller();
		Restingecgdata restingecgdata = (Restingecgdata)reader.unmarshal(input);
		DecodedLead[] leads = extractLeads(restingecgdata);
		
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
        
        Parsedwaveforms parsedwaveforms = restingecgdata.getWaveforms().getParsedwaveforms();
        parsedwaveforms.setDataencoding(TYPEdataencoding.PLAIN);
        parsedwaveforms.setCompressflag(TYPEflag.FALSE);
        parsedwaveforms.setValue(buffer.toString());
        
        return restingecgdata;
	}
	
	public static Restingecgdata preprocess(File input) throws IOException, JAXBException {
		JAXBContext context = JAXBContext.newInstance("org.sierraecg.schema");
		
		return preprocess(context, input);
	}
	
	public static void preprocess(File input, File output) throws IOException, JAXBException {
		JAXBContext context = JAXBContext.newInstance("org.sierraecg.schema");
		
		Restingecgdata restingecgdata = preprocess(context, input);
        
        Marshaller writer = context.createMarshaller();
        writer.setProperty(Marshaller.JAXB_FORMATTED_OUTPUT, true);
        writer.marshal(restingecgdata, output);
	}
	
	public static DecodedLead[] extractLeads(Restingecgdata input) throws IOException {
		Parsedwaveforms parsedwaveforms = input.getWaveforms().getParsedwaveforms();
		
		InputStream in = new ByteArrayInputStream(parsedwaveforms.getValue().getBytes());
		if (parsedwaveforms.getDataencoding() == TYPEdataencoding.BASE_64) {
			in = new Base64.InputStream(in);
		}
		
		ArrayList<int[]> leadData = new ArrayList<int[]>();
		if (parsedwaveforms.getCompressflag() == TYPEflag.TRUE
		 && parsedwaveforms.getCompressmethod() == TYPEcompress.XLI) {
			XliDecompressor xli = new XliDecompressor(in);
			int[] payload;
			while (null != (payload = xli.readLeadPayload())) {
				leadData.add(payload);
			}
		}
		
		TYPEreporttype reporttype = input.getReportinfo().getReporttype();
		DecodedLead[] leads = DecodedLead.createFromLeadSet(reporttype.value(), leadData);
		
		return leads;
	}
	
	public static DecodedLead[] extractLeads(File input) throws IOException, JAXBException {
		JAXBContext context = JAXBContext.newInstance("org.sierraecg.schema");
		Unmarshaller reader = context.createUnmarshaller();
		Restingecgdata restingecgdata = (Restingecgdata)reader.unmarshal(input);
		
		return extractLeads(restingecgdata);
	}
}
