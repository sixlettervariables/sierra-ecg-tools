// <copyright file="ExampleMain.cs">
//  Copyright (c) 2011 Christopher A. Watford
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.
// </copyright>
// <author>Christopher A. Watford [christopher.watford@gmail.com]</author>
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NDesk.Options;

using SierraEcg.IO;
using System.Reflection;

namespace SierraEcg
{
    class ExampleMain
    {
        static XNamespace ns = "http://www3.medical.philips.com";

        #region Runtime Configuration

        class Configuration
        {
            private List<string> statements = new List<string>();

            public bool RemovePatientInformation
            {
                get;
                set;
            }

            public bool RemoveDeviceInformation
            {
                get;
                set;
            }

            public bool AddDecodedByStatement
            {
                get;
                set;
            }

            public bool ShowHelp
            {
                get;
                set;
            }

            public IEnumerable<string> Statements
            {
                get { return this.statements; }
            }

            public void AddStatement(string statement)
            {
                this.statements.Add(statement);
            }
        }

        #endregion Runtime Configuration

        public static void Main(string[] args)
        {
            var config = new Configuration()
            {
                AddDecodedByStatement = true
            };

            var optionSet = new OptionSet()
            {
                { "s|statement=", "Add an interpretive statement (may use more than once)",
                    arg => config.AddStatement(arg) },
                { "no-patient-info", "Remove patient specific information",
                    _ => config.RemovePatientInformation = true },
                { "no-device-info", "Remove device specific information",
                    _ => config.RemoveDeviceInformation = true },
                { "no-watermark", "Do not add 'Decoded by...' statement",
                    _ => config.AddDecodedByStatement = false },
                { "?|h|help", "Show this message", _ => config.ShowHelp = true },
            };

            var extra = optionSet.Parse(args);
            if (!extra.Any() || config.ShowHelp)
            {
                Console.Error.WriteLine("XliDecompressor.exe [OPTIONS] <input files (XML, TGZ, GZ, TAR)>");
                optionSet.WriteOptionDescriptions(Console.Error);
                Environment.Exit(-1);
            }
            else
            {
                foreach (string file in extra)
                {
                    bool tar = false;
                    bool gzip = false;
                    var ext = Path.GetExtension(file).ToUpperInvariant();

                    switch (ext)
                    {
                        case ".TGZ":
                            tar = true;
                            gzip = true;
                            break;
                        case ".GZ":
                            gzip = true;
                            tar = file.EndsWith(".TAR.GZ");
                            break;
                        case ".TAR":
                            tar = true;
                            break;
                    }

                    try
                    {
                        Stream stream = File.OpenRead(file);
                        if (gzip)
                        {
                            stream = new GZipStream(stream, CompressionMode.Decompress);
                        }

                        if (tar)
                        {
                            using (var reader = new TarFile(stream))
                            {
                                Func<string, bool> isLikelyXmlFile = 
                                    fn => 0 == String.Compare(Path.GetExtension(fn), ".XML", ignoreCase: true);

                                // Look through the TGZ/TAR.GZ file for any XML file
                                foreach (var entry in reader.EnumerateEntries(entry => isLikelyXmlFile(entry.Name)))
                                {
                                    Console.WriteLine("[Info] Extracting: {0}", entry.Name);
                                    var xdoc = XDocument.Load(reader.Current);
                                    if (xdoc.Descendants(ns + "parsedwaveforms").Any())
                                    {
                                        xdoc = UpdateFile(xdoc, config);
                                        
                                        var output = Path.ChangeExtension(Path.GetFileName(entry.Name), ".decomp.xml");
                                        Console.WriteLine("[Info] Saving: {0}", output);
                                        xdoc.Save(output);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var xdoc = UpdateFile(XDocument.Load(stream), config);

                            var output = Path.ChangeExtension(Path.GetFileName(file), ".decomp.xml");
                            Console.WriteLine("[Info] Saving: {0}", output);
                            xdoc.Save(output);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("[Error] Could not decompress '{0}'", file);
                        Console.Error.WriteLine("{0}", ex);
                        Environment.ExitCode = -2;
                    }
                }
            }
        }

        /// <summary>
        /// Updates a Sierra ECG XML file, removing any XLI compression used.
        /// </summary>
        /// <param name="xdoc">XML document containing the Sierra ECG XML file.</param>
        /// <param name="statements">Additional interpretive statements to add to the ECG.</param>
        /// <param name="addDecodedBy"><see langword="true"/> if we should watermark the ECG with
        /// the name of the program which decoded it; otherwise <see langword="false"/>.</param>
        static XDocument UpdateFile(XDocument xdoc, Configuration config)
        {
            xdoc = SierraEcgFile.Preprocess(xdoc);

            var lastStatement = xdoc.Element(ns + "restingecgdata")
                                    .Element(ns + "interpretations")
                                    .Elements(ns + "interpretation")
                                    .Last()
                                    .Elements(ns + "statement")
                                    .LastOrDefault();
            if (lastStatement != null)
            {
                foreach (var statement in config.Statements)
                {
                    var parts = statement.Split(';');
                    lastStatement.AddAfterSelf(CreateStatement(parts.First(), parts.LastOrDefault()));
                }

                // Add a statement advising how the data was decoded
                if (config.AddDecodedByStatement)
                {
                    var version = Assembly.GetExecutingAssembly().GetName().Version;
                    lastStatement.AddAfterSelf(CreateStatement("Decoded by XliDecompressor " + version, DateTime.Now.ToString()));
                }
            }

            if (config.RemovePatientInformation)
            {
                var patientData = xdoc.Root.Element(ns + "patient")
                                           .Element(ns + "generalpatientdata");
                if (patientData != null)
                {
                    var patientId = patientData.Element(ns + "patientid");
                    if (patientId != null)
                    {
                        patientId.ReplaceWith(new XElement(ns + "patientid", 0));
                    }

                    var name = patientData.Element(ns + "name");
                    if (name != null)
                    {
                        name.ReplaceWith(
                            new XElement(
                                ns + "name",
                                new XElement(ns + "lastname"),
                                new XElement(ns + "firstname")));
                    }
                }

                var userdefines = xdoc.Root.Element(ns + "userdefines");
                if (userdefines != null)
                {
                    foreach (var userdefine in userdefines.Elements(ns + "userdefine"))
                    {
                        var label = userdefine.Element(ns + "label");
                        if (label != null && 0 == String.Compare((string)label, "Incident Id", true))
                        {
                            var value = userdefine.Element(ns + "value");
                            if (value != null)
                            {
                                value.ReplaceWith(new XElement(ns + "value", 0));
                            }
                        }
                    }
                }
            }

            if (config.RemoveDeviceInformation)
            {
                var machine = xdoc.Root.Element(ns + "dataacquisition")
                                       .Element(ns + "machine");
                if (machine != null)
                {
                    machine.SetAttributeValue("machineid", 0);
                    machine.SetAttributeValue("detaildescription", String.Empty);
                }

                var deviceInfo = xdoc.Root.Element(ns + "dataacquisition")
                                          .Element(ns + "acquirer");
                if (deviceInfo != null)
                {
                    deviceInfo.ReplaceWith(
                        new XElement(
                            ns + "acquirer",
                            new XElement(ns + "operatorid"),
                            new XElement(ns + "departmentid"),
                            new XElement(ns + "institutionname"),
                            new XElement(ns + "institutionlocationid")));
                }
            }

            return xdoc;
        }

        /// <summary>
        /// Creates a &lt;statement&gt; element to add to an ECG.
        /// </summary>
        /// <param name="left">Left hand side message for the statement.</param>
        /// <param name="right">Right hand side message for the statement.</param>
        /// <returns>A new &lt;statement&gt; element to add to the interpretive statements block.</returns>
        static XElement CreateStatement(string left, string right)
        {
            return new XElement(
                ns + "statement",
                new XElement(ns + "statementcode"),
                new XElement(ns + "leftstatement", left),
                new XElement(ns + "rightstatement", right));
        }
    }
}
