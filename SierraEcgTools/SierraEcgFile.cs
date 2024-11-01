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
using System.Diagnostics;
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

        private static readonly XNamespace ns = @"http://www3.medical.philips.com";

        private static readonly XName XERestingEcgData = ns + @"restingecgdata";

        private static readonly XName XEParsedWaveforms = ns + @"parsedwaveforms";

        private static readonly XName XEDocumentInfo = ns + @"documentinfo";

        private static readonly XName XERepBeat = ns + @"repbeat";

        private static readonly HashSet<Version> s_validVersions = [new Version("1.03"), new Version("1.04"), new Version("1.04.01")];

        #endregion Statics

        /// <summary>
        /// Preprocess a <see cref="Stream"/>, returning an <see cref="XDocument"/> containing
        /// Sierra ECG XML.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> with a Sierra ECG XML document.</param>
        /// <returns>Preprocessed Sierra ECG XML.</returns>
        public static XDocument Preprocess(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
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
            if (xdoc is null)
            {
                throw new ArgumentNullException(nameof(xdoc));
            }

            Preprocess(xdoc.Root ?? throw new InvalidSierraEcgFileException("Missing root element"));

            return xdoc;
        }

        /// <summary>
        /// Preprocess an <see cref="XElement"/> containing Sierra ECG XML.
        /// </summary>
        /// <param name="root">Sierra ECG XML fragment.</param>
        /// <returns><paramref name="root"/> with all encoded and compressed data expanded.</returns>
        public static XElement Preprocess(XElement root)
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }
            else if (root.Name != XERestingEcgData)
            {
                throw new ArgumentException("Unknown XML ECG type: " + root.Name, nameof(root));
            }

            // check that this is at least Sierra ECG 1.03
            Version version = DetermineVersion(root)
                ?? throw new ArgumentException("Unknown XML ECG version", nameof(root));

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
            if (xdoc is null)
            {
                throw new ArgumentNullException(nameof(xdoc));
            }

            return ExtractLeads(xdoc.Root ?? throw new InvalidSierraEcgFileException("Missing root element"));
        }

        private static DecodedLead[] ExtractLeads(XElement root)
        {
            // Retrieve parsedwaveforms element and check if the data is encoded
            XElement? parsedWaveforms = root.Descendants(XEParsedWaveforms)
                                            .SingleOrDefault();
            if (parsedWaveforms is not null)
            {
                XElement signalCharacteristics = root.ElementOrThrow(ns + "dataacquisition", ns + "signalcharacteristics");
                string leadsUsed = (string)signalCharacteristics.ElementOrThrow(ns + "acquisitiontype");
                int goodChannels = (int)signalCharacteristics.ElementOrThrow(ns + "numberchannelsallocated");
                int sampleRate = (int)signalCharacteristics.ElementOrThrow(ns + "samplerate");
                int duration = (int)parsedWaveforms.ElementOrThrow(ns + "durationperchannel");

                int samples = duration * (sampleRate / 1000);

                object? decoded = null;
                string encoding = (string?)parsedWaveforms.Attribute("dataencoding")
                    ?? throw new InvalidSierraEcgFileException("Missing required //parsedwaveforms[@dataencoding] attribute");
                switch (encoding)
                {
                    case "Plain":
                        return parsedWaveforms.Value
                                              .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                                              .Select((vv, ix) => new { I = ix, V = int.Parse(vv) })
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

                bool isCompressed = (bool)parsedWaveforms.AttributeOrThrow("compressflag");
                string compression = (string)parsedWaveforms.AttributeOrThrow("compressmethod");
                if (isCompressed)
                {
                    if (compression == "XLI" && decoded is byte[] v)
                    {
                        using XliDecompressor xli = new(v);
                        List<int[]> decodedData = [];

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
                    else
                    {
                        // no can decompress boss man
                        throw new InvalidSierraEcgFileException("Unknown compression method: " + compression);
                    }
                }

                // handle the case where we've decoded Base64 only data
                if (decoded is byte[] bytes)
                {
                    List<DecodedLead> leads = [];
                    for (int offset = 0, lead = 0; offset < bytes.Length; offset += samples * 2)
                    {
                        short[] data = new short[samples];
                        Buffer.BlockCopy(bytes, offset, data, 0, samples * 2);

                        leads.Add(new DecodedLead(++lead, data.Select(dd => (int)dd).ToArray()));
                    }

                    return [.. leads];
                }
            }

            throw new NotSupportedException("For whatever reason this format was not supported");
        }

        private static void PreprocessParsedWaveforms(XElement root, Version schemaVersion)
        {
            // Retrieve parsedwaveforms element and check if the data is encoded
            XElement? parsedWaveforms = root.Descendants(XEParsedWaveforms)
                                            .SingleOrDefault();
            if (parsedWaveforms is not null)
            {
                object? decoded = null;

                //SierraECGSchema_1_04_01:
                // @dataencoding: how the data is encoded: eg., "Base64".
                // Use "Plain" for sample values in ascii: "10 20 35...." .
                string encoding = (string)parsedWaveforms.AttributeOrThrow("dataencoding");
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
                        throw new InvalidSierraEcgFileException("Unknown data encoding: " + encoding);
                }
                
                bool isCompressed;
                string compression;
                if (schemaVersion < new Version("1.04"))
                {
                    isCompressed = (bool)parsedWaveforms.AttributeOrThrow("compressflag");
                    compression = (string)parsedWaveforms.AttributeOrThrow("compressmethod");
                }
                else
                {
                    compression = (string)parsedWaveforms.AttributeOrThrow("compression");
                    isCompressed = compression == "XLI";
                }

                if (isCompressed)
                {
                    XElement signalCharacteristics = root.ElementOrThrow(ns + "dataacquisition", ns + "signalcharacteristics");

                    string leadsUsed = (string)signalCharacteristics.ElementOrThrow(ns + "acquisitiontype");
                    int goodChannels = (int)signalCharacteristics.ElementOrThrow(ns + "numberchannelsallocated");

                    if (compression == "XLI" && decoded is byte[] v)
                    {
                        List<DecodedLead> leads;
                        using (XliDecompressor xli = new(v))
                        {
                            List<int[]> decodedData = [];

                            int[] payload;
                            while (null != (payload = xli.ReadLeadPayload()))
                            {
                                decodedData.Add(payload);

                                // the DXL algorithm can interpret 16 channels, so often all 16
                                // will be in the file, but the last 4 will be 0's besides
                                // their calibration marks at the end. This is a quick check to skip
                                // what we don't care about.
                                if (decodedData.Count >= goodChannels)
                                {
                                    break;
                                }
                            }

                            leads = [ ..DecodedLead.ReinterpretLeads(leadsUsed, decodedData) ];
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
                        decoded = string.Join(
                            Environment.NewLine,
                            leads.SelectMany(
                                lead => lead.Select((vv, ix) => new { I = ix, V = vv })
                                            .GroupBy(xx => xx.I / 25)
                                            .Select(gg => string.Join(" ", gg.Select(xx => xx.V)))));
                    }
                    else
                    {
                        // no can decompress boss man
                        throw new InvalidSierraEcgFileException("Unknown compression method: " + compression);
                    }
                }

                // handle the case where we've decoded Base64 only data
                if (decoded is byte[] bytes)
                {
                    short[] lead = new short[bytes.Length / 2];
                    Buffer.BlockCopy(bytes, 0, lead, 0, bytes.Length);
                    decoded = string.Join(
                        Environment.NewLine,
                        lead.Select((vv, ix) => new { I = ix, V = vv })
                                .GroupBy(xx => xx.I / 25)
                                .Select(gg => string.Join(" ", gg.Select(xx => xx.V))));
                }

                parsedWaveforms.SetValue(decoded);
            }
        }

        private static void PreprocessRepresentativeBeats(XElement root, Version schemaVersion)
        {
            XElement? repBeats = root.Element(ns + "waveforms")
                                     ?.Element(ns + "repbeats");
            if (repBeats is null)
            {
                return;
            }

            string encoding = (string)repBeats.AttributeOrThrow("dataencoding");
            string? compression = null;
            if (schemaVersion >= new Version("1.04"))
            {
                // @compression is present if data is compressed, and describes method (eg., "Huffman")
                XAttribute? compressionAttr = repBeats.Attribute("compression");
                compression = compressionAttr is not null
                            ? (string)compressionAttr
                            : null;
            }

            // The representative beats may or may not be present, but we have to decode each one
            // independently.
            IEnumerable<XElement> representativeBeats = root.Descendants(XERepBeat);
            foreach (XElement repBeat in representativeBeats)
            {
                object? decoded = null;

                XElement waveform;
                if (schemaVersion < new Version("1.04"))
                {
                    //1.03: repBeat itself holds the encoded data
                    waveform = repBeat;
                }
                else
                {
                    //1.04+: <waveform> element under the repbeat holds the data
                    waveform = repBeat.ElementOrThrow(ns + "waveform");
                }

                switch (encoding)
                {
                    case "Plain":
                        decoded = waveform.Value;
                        break;

                    case "Base64":
                        if (compression is null)
                        {
                            byte[] bytes = Convert.FromBase64String(waveform.Value);
                            short[] lead = new short[bytes.Length / 2];
                            Buffer.BlockCopy(bytes, 0, lead, 0, bytes.Length);
                            decoded = string.Join(
                                Environment.NewLine,
                                lead.Select((vv, ix) => new { I = ix, V = vv })
                                        .GroupBy(xx => xx.I / 25)
                                        .Select(gg => string.Join(" ", gg.Select(xx => xx.V))));
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
                        throw new InvalidSierraEcgFileException("Unknown data encoding: " + encoding);
                }

                waveform.SetValue(decoded);
            }

            repBeats?.SetAttributeValue("dataencoding", "Plain");
        }

        /// <summary>
        /// Determines the schema version of the file.
        /// </summary>
        /// <param name="root">XML containing a SierraECG compliant schema.</param>
        /// <returns>The version of the file, if it is of a supported version;
        /// otherwise <see langword="null"/>.</returns>
        private static Version? DetermineVersion(XElement root)
        {
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
            XElement? documentInfo = root.Element(XEDocumentInfo);
            if (documentInfo is null)
            {
                return null;
            }
            else
            {
                XElement? type = documentInfo.Element(ns + "documenttype");
                XElement? version = documentInfo.Element(ns + "documentversion");
                if (type is null || version is null)
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
                Version foundVersion = new((string)version);
                if (s_validVersions.Contains(foundVersion))
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
