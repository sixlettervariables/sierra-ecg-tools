// <copyright file="SierraEcg.cs">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SierraEcg
{
    /// <summary>
    /// Provides utility methods to preprocess the Philips Medical Systems Sierra ECG
    /// format, decoding all Base64 encoding and XLI compressed data.
    /// </summary>
    public sealed class SierraEcgFile
    {
        #region Statics

        static XNamespace ns = @"http://www3.medical.philips.com";

        static XName XERestingEcgData = ns + @"restingecgdata";

        static XName XEParsedWaveforms = ns + @"parsedwaveforms";

        static XName XEDocumentInfo = ns + @"documentinfo";

        static XName XERepBeat = ns + @"repbeat";

        static HashSet<Version> validVersions = new HashSet<Version>(new[] { new Version("1.03"), new Version("1.04"), new Version("1.04.01") });

        #endregion Statics

        /// <summary>
        /// Preprocess a <see cref="Stream"/>, returning an <see cref="XDocument"/> containing
        /// Sierra ECG XML.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> with a Sierra ECG XML document.</param>
        /// <returns>Preprocessed Sierra ECG XML.</returns>
        public static XDocument Preprocess(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            return Preprocess(XDocument.Load(stream));
        }

        /// <summary>
        /// Preprocess an <see cref="XDocument"/> containing Sierra ECG XML.
        /// </summary>
        /// <param name="xdoc">Sierra ECG XML document.</param>
        /// <returns><paramref name="xdoc"/> with all encoded and compressed data expanded.</returns>
        public static XDocument Preprocess(XDocument xdoc)
        {
            if (xdoc == null)
            {
                throw new ArgumentNullException("xdoc");
            }

            Preprocess(xdoc.Root);

            return xdoc;
        }

        /// <summary>
        /// Preprocess an <see cref="XElement"/> containing Sierra ECG XML.
        /// </summary>
        /// <param name="root">Sierra ECG XML fragment.</param>
        /// <returns><paramref name="root"/> with all encoded and compressed data expanded.</returns>
        public static XElement Preprocess(XElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException("root");
            }
            else if (root.Name != XERestingEcgData)
            {
                throw new ArgumentException("Unknown XML ECG type: " + root.Name);
            }

            // check that this is at least Sierra ECG 1.03
            var version = DetermineVersion(root);
            if (version == null)
            {
                throw new ArgumentException("Unknown XML ECG version");
            }

            PreprocessParsedWaveforms(root, version);

            PreprocessRepresentativeBeats(root, version);

            return root;
        }

        /// <summary>
        /// Extracts the sample data per lead in Sierra ECG XML.
        /// </summary>
        /// <param name="xdoc">Sierra ECG XML document.</param>
        /// <returns>An array of <see cref="DecodedLead"/> objects representing the
        /// acquired leads.</returns>
        public static DecodedLead[] ExtractLeads(XDocument xdoc)
        {
            if (xdoc == null)
            {
                throw new ArgumentNullException("xdoc");
            }

            return ExtractLeads(xdoc.Root);
        }

        private static DecodedLead[] ExtractLeads(XElement root)
        {
            Debug.Assert(root != null);

            // Retrieve parsedwaveforms element and check if the data is encoded
            var parsedWaveforms = root.Descendants(XEParsedWaveforms)
                                      .SingleOrDefault();
            if (parsedWaveforms != null)
            {
                var signalCharacteristics = root.Element(ns + "dataacquisition")
                                                .Element(ns + "signalcharacteristics");
                var leadsUsed = (string)signalCharacteristics.Element(ns + "acquisitiontype");
                var goodChannels = (int)signalCharacteristics.Element(ns + "numberchannelsallocated");
                var sampleRate = (int)signalCharacteristics.Element(ns + "samplerate");
                var duration = (int)parsedWaveforms.Element(ns + "durationperchannel");

                var samples = duration * (sampleRate / 1000);

                object decoded = null;
                var encoding = (string)parsedWaveforms.Attribute("dataencoding");
                switch (encoding)
                {
                    case "Plain":
                        return parsedWaveforms.Value
                                              .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select((vv, ix) => new { I = ix, V = Int32.Parse(vv) })
                                              .GroupBy(xx => xx.I / samples)
                                              .Select(gg => new DecodedLead(gg.Key + 1, gg.Select(xx => xx.V).ToArray()))
                                              .ToArray();

                    case "Base64":
                        decoded = Convert.FromBase64String(parsedWaveforms.Value);
                        break;
                    default:
                        // no can decode boss man
                        throw new InvalidOperationException("Unknown data encoding: " + encoding);
                }

                var isCompressed = (bool)parsedWaveforms.Attribute("compressflag");
                var compression = (string)parsedWaveforms.Attribute("compressmethod");
                if (isCompressed)
                {
                    if (compression == "XLI" && decoded is byte[])
                    {
                        using (var xli = new XliDecompressor((byte[])decoded))
                        {
                            var decodedData = new List<int[]>();

                            int[] payload;
                            while (null != (payload = xli.ReadLeadPayload()))
                            {
                                decodedData.Add(payload);

                                // the DXL algorithm can interpret 16 channels, so often all 16
                                // will be in the file, but the last 4 will be 0's besides
                                // their calibration marks at the end. This is a quick check to skip
                                // what we don't care about.
                                if (decodedData.Count >= goodChannels)
                                    break;
                            }

                            return DecodedLead.ReinterpretLeads(leadsUsed, decodedData).ToArray();
                        }
                    }
                    else
                    {
                        // no can decompress boss man
                        throw new InvalidOperationException("Unknown compression method: " + compression);
                    }
                }

                // handle the case where we've decoded Base64 only data
                if (decoded is byte[])
                {
                    var bytes = (byte[])decoded;
                    var leads = new List<DecodedLead>();
                    for (int offset = 0, lead = 0; offset < bytes.Length; offset += samples * 2)
                    {
                        var data = new short[samples];
                        Buffer.BlockCopy(bytes, offset, data, 0, samples * 2);

                        leads.Add(new DecodedLead(++lead, data.Select(dd => (int)dd).ToArray()));
                    }

                    return leads.ToArray();
                }
            }

            throw new NotSupportedException("For whatever reason this format was not supported");
        }

        private static void PreprocessParsedWaveforms(XElement root, Version schemaVersion)
        {
            // Retrieve parsedwaveforms element and check if the data is encoded
            var parsedWaveforms = root.Descendants(XEParsedWaveforms)
                                      .SingleOrDefault();
            if (parsedWaveforms != null)
            {
                object decoded = null;

                //SierraECGSchema_1_04_01:
                // @dataencoding: how the data is encoded: eg., "Base64".
                // Use "Plain" for sample values in ascii: "10 20 35...." .
                var encoding = (string)parsedWaveforms.Attribute("dataencoding");
                switch (encoding)
                {
                    case "Plain":
                        decoded = parsedWaveforms.Value;
                        break;
                    case "Base64":
                        decoded = Convert.FromBase64String(parsedWaveforms.Value);
                        parsedWaveforms.SetAttributeValue("dataencoding", "Plain");
                        break;
                    default:
                        // no can decode boss man
                        throw new InvalidOperationException("Unknown data encoding: " + encoding);
                }
                
                bool isCompressed;
                string compression;
                if (schemaVersion < new Version("1.04"))
                {
                    isCompressed = (bool)parsedWaveforms.Attribute("compressflag");
                    compression = (string)parsedWaveforms.Attribute("compressmethod");
                }
                else
                {
                    compression = (string)parsedWaveforms.Attribute("compression");
                    isCompressed = compression == "XLI";
                }

                if (isCompressed)
                {
                    var signalCharacteristics = root.Element(ns + "dataacquisition")
                                                    .Element(ns + "signalcharacteristics");
                    var leadsUsed = (string)signalCharacteristics.Element(ns + "acquisitiontype");
                    var goodChannels = (int)signalCharacteristics.Element(ns + "numberchannelsallocated");

                    if (compression == "XLI" && decoded is byte[])
                    {
                        List<DecodedLead> leads;
                        using (var xli = new XliDecompressor((byte[])decoded))
                        {
                            var decodedData = new List<int[]>();

                            int[] payload;
                            while (null != (payload = xli.ReadLeadPayload()))
                            {
                                decodedData.Add(payload);

                                // the DXL algorithm can interpret 16 channels, so often all 16
                                // will be in the file, but the last 4 will be 0's besides
                                // their calibration marks at the end. This is a quick check to skip
                                // what we don't care about.
                                if (decodedData.Count >= goodChannels)
                                    break;
                            }

                            leads = DecodedLead.ReinterpretLeads(leadsUsed, decodedData).ToList();
                        }

                        if (schemaVersion < new Version("1.04"))
                        {
                            // CAW: "False" must be used as the "false" is not recognized by many of the tools
                            parsedWaveforms.SetAttributeValue("compressflag", "False");
                        }
                        else
                        {
                            //SierraECGSchema_1_04_01:
                            // @compression: name the type of compression if the data is compressed
                            // (e.g., "XLI" for standard Philips cardiograph compression; if not compressed, omit this attribute).
                            parsedWaveforms.SetAttributeValue("compression", null);
                        }

                        // CAW: the default is 25 data points per line
                        decoded = String.Join(
                            Environment.NewLine,
                            leads.SelectMany(
                                lead => lead.Select((vv, ix) => new { I = ix, V = vv })
                                            .GroupBy(xx => xx.I / 25)
                                            .Select(gg => String.Join(" ", gg.Select(xx => xx.V)))));
                    }
                    else
                    {
                        // no can decompress boss man
                        throw new InvalidOperationException("Unknown compression method: " + compression);
                    }
                }

                // handle the case where we've decoded Base64 only data
                if (decoded is byte[])
                {
                    var bytes = (byte[])decoded;
                    var lead = new short[bytes.Length / 2];
                    Buffer.BlockCopy(bytes, 0, lead, 0, bytes.Length);
                    decoded = String.Join(
                        Environment.NewLine,
                        lead.Select((vv, ix) => new { I = ix, V = vv })
                                .GroupBy(xx => xx.I / 25)
                                .Select(gg => String.Join(" ", gg.Select(xx => xx.V))));
                }

                parsedWaveforms.SetValue(decoded);
            }
        }

        private static void PreprocessRepresentativeBeats(XElement root, Version schemaVersion)
        {
            Debug.Assert(root != null);
            Debug.Assert(schemaVersion != null);

            var repBeats = root.Element(ns + "waveforms")
                               .Element(ns + "repbeats");

            var encoding = (string)repBeats.Attribute("dataencoding");
            string compression = null;
            if (schemaVersion >= new Version("1.04"))
            {
                // @compression is present if data is compressed, and describes method (eg., "Huffman")
                compression = repBeats.Attribute("compression") != null
                            ? (string)repBeats.Attribute("compression")
                            : null;
            }

            // The representative beats may or may not be present, but we have to decode each one
            // independently.
            var representativeBeats = root.Descendants(XERepBeat);
            foreach (var repBeat in representativeBeats)
            {
                object decoded = null;

                XElement waveform;
                if (schemaVersion < new Version("1.04"))
                {
                    //1.03: repBeat itself holds the encoded data
                    waveform = repBeat;
                }
                else
                {
                    //1.04+: <waveform> element under the repbeat holds the data
                    waveform = repBeat.Element(ns + "waveform");
                }

                switch (encoding)
                {
                    case "Plain":
                        decoded = waveform.Value;
                        break;

                    case "Base64":
                        if (compression == null)
                        {
                            var bytes = Convert.FromBase64String(waveform.Value);
                            var lead = new short[bytes.Length / 2];
                            Buffer.BlockCopy(bytes, 0, lead, 0, bytes.Length);
                            decoded = String.Join(
                                Environment.NewLine,
                                lead.Select((vv, ix) => new { I = ix, V = vv })
                                        .GroupBy(xx => xx.I / 25)
                                        .Select(gg => String.Join(" ", gg.Select(xx => xx.V))));
                        }
                        else
                        {
                            // we cannot decode these as we're not aware of the compression method
                            Trace.WriteLine(
                                "[PreprocessRepresentativeBeats] <repbeats> compressed using "
                                + compression
                                + ", aborting.");
                            return;
                        }
                        break;

                    default:
                        // no can decode boss man
                        throw new InvalidOperationException("Unknown data encoding: " + encoding);
                }

                waveform.SetValue(decoded);
            }

            if (repBeats != null)
            {
                repBeats.SetAttributeValue("dataencoding", "Plain");
            }
        }

        /// <summary>
        /// Determines the schema version of the file.
        /// </summary>
        /// <param name="root">XML containing a SierraECG compliant schema.</param>
        /// <returns>The version of the file, if it is of a supported version;
        /// otherwise <see langword="null"/>.</returns>
        private static Version DetermineVersion(XElement root)
        {
            Debug.Assert(root != null);

            //1.03:
            //<documentinfo>
            //  <documentname>03d494f0-94f0-13d4-b58f-00095c028bdc.xml</documentname>
            //  <documenttype>SierraECG</documenttype>
            //  <documentversion>1.03</documentversion>
            //</documentinfo>
            //
            //1.04 / 1.04.01:
            //<documentinfo>
            //  <documentname>7eacbc80-0549-11df-4823-000738300029.xml</documentname>
            //  <filename>\Storage Card\PhilipsArchiveInternal\7eacbc80-0549-11df-4823-000738300029.xml</filename>
            //  <documenttype>PhilipsECG</documenttype>
            //  <documentversion>1.04</documentversion>
            //  <editor />
            //  <comments />
            //</documentinfo>
            var documentInfo = root.Element(XEDocumentInfo);
            if (documentInfo == null)
            {
                return null;
            }
            else
            {
                var type = documentInfo.Element(ns + "documenttype");
                var version = documentInfo.Element(ns + "documentversion");
                if (type == null || version == null)
                {
                    return null;
                }

                // check schema type
                switch ((string)type)
                {
                    case "SierraECG":
                    case "PhilipsECG":
                        // go to version check
                        break;
                    default:
                        return null;
                }

                // check schema version
                var foundVersion = new Version((string)version);
                if (validVersions.Contains(foundVersion))
                {
                    return foundVersion;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
