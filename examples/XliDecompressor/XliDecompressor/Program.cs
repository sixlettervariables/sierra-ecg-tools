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
using NDesk.Options;
using SierraEcg;
using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;

#if NET8_0_OR_GREATER
using System.Formats.Tar;
#else
using SierraEcg.IO;
#endif

XNamespace ns = "http://www3.medical.philips.com";

Configuration config = new()
{
    AddDecodedByStatement = true
};

OptionSet optionSet = new()
{
    { "s|statement=", "Add an interpretive statement (may use more than once)",
        config.AddStatement },
    { "no-patient-info", "Remove patient specific information",
        _ => config.RemovePatientInformation = true },
    { "no-device-info", "Remove device specific information",
        _ => config.RemoveDeviceInformation = true },
    { "no-watermark", "Do not add 'Decoded by...' statement",
        _ => config.AddDecodedByStatement = false },
    { "?|h|help", "Show this message", _ => config.ShowHelp = true },
};

List<string> extra = optionSet.Parse(args);
if (extra.Count == 0 || config.ShowHelp)
{
    Console.Error.WriteLine("XliDecompressor.exe [OPTIONS] <input files (XML, TGZ, GZ, TAR)>");
    optionSet.WriteOptionDescriptions(Console.Error);
    return -1;
}
else
{
    foreach (string file in extra)
    {
        bool tar = false;
        bool gzip = false;
        string ext = Path.GetExtension(file).ToUpperInvariant();

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

#if NET8_0_OR_GREATER                
                using TarReader reader = new(stream);

                // Look through the TGZ/TAR.GZ file for any XML file
                TarEntry? entry;
                while (null != (entry = reader.GetNextEntry()))
                {
                    if (IsLikelyXmlFile(entry.Name) && entry.DataStream is not null)
                    {
                        ExtractData(entry.DataStream, entry.Name);
                    }
                }
#else // NET48
                using TarFile reader = new(stream);

                // Look through the TGZ/TAR.GZ file for any XML file
                foreach (TarEntry entry in reader.EnumerateEntries(entry => IsLikelyXmlFile(entry.Name)))
                {
                    ExtractData(entry.DataStream, entry.Name);
                }
#endif

                static bool IsLikelyXmlFile(string? fileName)
                {
                    return !string.IsNullOrWhiteSpace(fileName)
                        && 0 == string.Compare(Path.GetExtension(fileName), ".xml", StringComparison.OrdinalIgnoreCase);
                }

                void ExtractData(Stream? stream, string? fileName)
                {
                    if (stream is not null && fileName is not null)
                    {
                        Console.WriteLine("[Info] Extracting: {0}", fileName);
                        XDocument xdoc = XDocument.Load(stream);
                        if (xdoc.Descendants(ns + "parsedwaveforms").Any())
                        {
                            xdoc = UpdateFile(xdoc, config);

                            string output = Path.ChangeExtension(Path.GetFileName(fileName), ".decomp.xml");
                            Console.WriteLine("[Info] Saving: {0}", output);
                            xdoc.Save(output);
                        }
                    }
                }
            }
            else
            {
                XDocument xdoc = UpdateFile(XDocument.Load(stream), config);

                string output = Path.ChangeExtension(Path.GetFileName(file), ".decomp.xml");
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

    return 0;
}


/// <summary>
/// Updates a Sierra ECG XML file, removing any XLI compression used.
/// </summary>
/// <param name="xdoc">XML document containing the Sierra ECG XML file.</param>
/// <param name="statements">Additional interpretive statements to add to the ECG.</param>
/// <param name="addDecodedBy"><see langword="true"/> if we should watermark the ECG with
/// the name of the program which decoded it; otherwise <see langword="false"/>.</param>
XDocument UpdateFile(XDocument xdoc, Configuration config)
{
    xdoc = SierraEcgFile.Preprocess(xdoc);

    XElement? lastStatement = xdoc.Element(ns + "restingecgdata")
                                 ?.Element(ns + "interpretations")
                                 ?.Elements(ns + "interpretation")
                                 ?.Last()
                                 ?.Elements(ns + "statement")
                                 ?.LastOrDefault();
    if (lastStatement is not null)
    {
        foreach (string statement in config.Statements)
        {
            string[] parts = statement.Split(';');
            lastStatement.AddAfterSelf(CreateStatement(parts.First(), parts.LastOrDefault()));
        }

        // Add a statement advising how the data was decoded
        if (config.AddDecodedByStatement)
        {
            Version? version = Assembly.GetExecutingAssembly().GetName().Version;
            lastStatement.AddAfterSelf(CreateStatement("Decoded by XliDecompressor " + version, DateTime.Now.ToString()));
        }
    }

    if (config.RemovePatientInformation)
    {
        XElement? patientData = xdoc.Root?.Element(ns + "patient")
                                         ?.Element(ns + "generalpatientdata");
        if (patientData is not null)
        {
            XElement? patientId = patientData.Element(ns + "patientid");
            patientId?.ReplaceWith(new XElement(ns + "patientid", 0));

            XElement? name = patientData.Element(ns + "name");
            name?.ReplaceWith(
                new XElement(
                    ns + "name",
                    new XElement(ns + "lastname"),
                    new XElement(ns + "firstname")));
        }

        XElement? userdefines = xdoc.Root?.Element(ns + "userdefines");
        if (userdefines is not null)
        {
            foreach (XElement userdefine in userdefines.Elements(ns + "userdefine"))
            {
                XElement? label = userdefine.Element(ns + "label");
                if (label is not null && 0 == string.Compare((string)label, "Incident Id", StringComparison.OrdinalIgnoreCase))
                {
                    XElement? value = userdefine.Element(ns + "value");
                    value?.ReplaceWith(new XElement(ns + "value", 0));
                }
            }
        }
    }

    if (config.RemoveDeviceInformation)
    {
        XElement? machine = xdoc.Root?.Element(ns + "dataacquisition")
                                     ?.Element(ns + "machine");
        if (machine is not null)
        {
            machine.SetAttributeValue("machineid", 0);
            machine.SetAttributeValue("detaildescription", string.Empty);
        }

        XElement? deviceInfo = xdoc.Root?.Element(ns + "dataacquisition")
                                        ?.Element(ns + "acquirer");
        if (deviceInfo is not null)
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
XElement CreateStatement(string left, string? right)
    => new(ns + "statement",
        new XElement(ns + "statementcode"),
        new XElement(ns + "leftstatement", left),
        new XElement(ns + "rightstatement", right ?? ""));

class Configuration
{
    private readonly List<string> _statements = [];

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

    public IEnumerable<string> Statements => _statements;

    public void AddStatement(string statement)
    {
        _statements.Add(statement ?? throw new ArgumentNullException(nameof(statement)));
    }
}
