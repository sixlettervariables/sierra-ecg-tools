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
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;

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

        static string[] validVersions = new[] { "1.03", "1.04" };

        #endregion Statics

        /// <summary>
        /// Preprocess a <see cref="Stream"/>, returning an <see cref="XDocument"/> containing
        /// Sierra ECG XML.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> with a Sierra ECG XML document.</param>
        /// <param name="disableVersionCheck"><see langword="true"/> if the preprocessor should
        /// ignore the version information associated with the XML.</param>
        /// <returns>Preprocessed Sierra ECG XML.</returns>
        public static XDocument Preprocess(Stream stream, bool disableVersionCheck = false)
        {
            return Preprocess(XDocument.Load(stream), disableVersionCheck);
        }

        /// <summary>
        /// Preprocess an <see cref="XDocument"/> containing Sierra ECG XML.
        /// </summary>
        /// <param name="xdoc">Sierra ECG XML document.</param>
        /// <param name="disableVersionCheck"><see langword="true"/> if the preprocessor should
        /// ignore the version information associated with the XML.</param>
        /// <returns><paramref name="xdoc"/> with all encoded and compressed data expanded.</returns>
        public static XDocument Preprocess(XDocument xdoc, bool disableVersionCheck = false)
        {
            if (xdoc == null)
            {
                throw new ArgumentNullException("xdoc");
            }

            Preprocess(xdoc.Root, disableVersionCheck);

            return xdoc;
        }

        /// <summary>
        /// Preprocess an <see cref="XElement"/> containing Sierra ECG XML.
        /// </summary>
        /// <param name="root">Sierra ECG XML fragment.</param>
        /// <param name="disableVersionCheck"><see langword="true"/> if the preprocessor should
        /// ignore the version information associated with the XML.</param>
        /// <returns><paramref name="root"/> with all encoded and compressed data expanded.</returns>
        public static XElement Preprocess(XElement root, bool disableVersionCheck = false)
        {
            if (root.Name != XERestingEcgData)
            {
                throw new ArgumentException("Unknown XML ECG type: " + root.Name);
            }

            // check that this is at least Sierra ECG 1.03
            if (!CheckVersion(root, disableVersionCheck))
            {
                throw new ArgumentException("Unknown XML ECG version");
            }

            PreprocessParsedWaveforms(root);

            PreprocessRepresentativeBeats(root);

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
            return ExtractLeads(xdoc.Root);
        }

        private static DecodedLead[] ExtractLeads(XElement root)
        {
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

        private static void PreprocessParsedWaveforms(XElement root)
        {
            // Retrieve parsedwaveforms element and check if the data is encoded
            var parsedWaveforms = root.Descendants(XEParsedWaveforms)
                                      .SingleOrDefault();
            if (parsedWaveforms != null)
            {
                object decoded = null;

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
                
                var isCompressed = (bool)parsedWaveforms.Attribute("compressflag");
                var compression = (string)parsedWaveforms.Attribute("compressmethod");
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

                        // CAW: "False" must be used as the "false" is not recognized by many of the tools
                        parsedWaveforms.SetAttributeValue("compressflag", "False");

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

        private static void PreprocessRepresentativeBeats(XElement root)
        {
            var repBeats = root.Element(ns + "waveforms")
                               .Element(ns + "repbeats");

            // The representative beats may or may not be present, but we have to decode each one
            // independently. They will not be compressed.
            var representativeBeats = root.Descendants(XERepBeat);
            foreach (var repBeat in representativeBeats)
            {
                object decoded = null;

                var encoding = (string)repBeats.Attribute("dataencoding");
                switch (encoding)
                {
                    case "Plain":
                        decoded = repBeat.Value;
                        break;
                    case "Base64":
                        var bytes = Convert.FromBase64String(repBeat.Value);
                        var lead = new short[bytes.Length / 2];
                        Buffer.BlockCopy(bytes, 0, lead, 0, bytes.Length);
                        decoded = String.Join(
                            Environment.NewLine,
                            lead.Select((vv, ix) => new { I = ix, V = vv })
                                    .GroupBy(xx => xx.I / 25)
                                    .Select(gg => String.Join(" ", gg.Select(xx => xx.V))));
                        break;
                    default:
                        // no can decode boss man
                        throw new InvalidOperationException("Unknown data encoding: " + encoding);
                }

                repBeat.SetValue(decoded);
            }

            if (repBeats != null)
            {
                repBeats.SetAttributeValue("dataencoding", "Plain");
            }
        }

        /// <summary>
        /// Check to see if this is a SierraECG version 1.03 or 1.04 file.
        /// </summary>
        /// <param name="root">XML containing a SierraECG compliant schema.</param>
        /// <param name="disableVersionCheck"><see langword="true"/> if no version check
        /// should be performed; otherwise <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the file is of a supported version;
        /// otherwise <see langword="false"/>.</returns>
        private static bool CheckVersion(XElement root, bool disableVersionCheck)
        {
            //<documentinfo>
            //  <documentname>03d494f0-94f0-13d4-b58f-00095c028bdc.xml</documentname>
            //  <documenttype>SierraECG</documenttype>
            //  <documentversion>1.03</documentversion>
            //</documentinfo>
            Debug.Assert(root != null, "Null XElement root");

            var documentInfo = root.Element(XEDocumentInfo);
            if (documentInfo == null)
            {
                return false;
            }
            else
            {
                var type = documentInfo.Element(ns + "documenttype");
                if (type == null || (!disableVersionCheck && (string)type != "SierraECG"))
                {
                    return false;
                }

                var version = documentInfo.Element(ns + "documentversion");
                if (version == null || (!disableVersionCheck && !validVersions.Contains((string)version)))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
