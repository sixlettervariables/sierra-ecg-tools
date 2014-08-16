package org.sierraecg;

import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;

import javax.xml.bind.JAXBException;

public class Test {

	/**
	 * @param args
	 */
	public static void main(String[] args) {
		for (String arg : args) {
			File input = new File(arg);
			try {
				SierraEcgFiles.transform(input, new File(input.getCanonicalPath() + ".decoded.xml"));
			} catch (FileNotFoundException e) {
				System.out.println("No such file: " + input);
				e.printStackTrace();
			} catch (IOException e) {
				System.out.println("Error reading file: " + input);
				e.printStackTrace();
			} catch (JAXBException e) {
				System.out.println("Error reading/writing XML: " + input);
				e.printStackTrace();
			} catch (Exception e) {
				System.out.println("Fatal error");
				e.printStackTrace();
			}
		}
	}
}
