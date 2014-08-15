package org.sierraecg;

import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.FileNotFoundException;
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

public class Test {

	/**
	 * @param args
	 */
	public static void main(String[] args) {
		for (String arg : args) {
			File input = new File(arg);
			try {
				SierraEcgFiles.preprocess(input, new File(input.getCanonicalPath() + ".decoded.xml"));
			} catch (FileNotFoundException e) {
				System.out.println("No such file: " + input);
				e.printStackTrace();
			} catch (IOException e) {
				System.out.println("Error reading file: " + input);
				e.printStackTrace();
			} catch (JAXBException e) {
				System.out.println("Error reading/writing XML: " + input);
				e.printStackTrace();
			}
		}
	}
}
