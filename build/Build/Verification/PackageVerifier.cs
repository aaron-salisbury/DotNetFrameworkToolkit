using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Build.Verification;

internal static class PackageVerifier
{
    internal const string PACKAGE_ID = "AaronSalisbury.DotNetFrameworkToolkit";
    private const string LIBRARY_PATH = "lib/net20/DotNetFrameworkToolkit";

    public static void Verify(string repository, string packagePath, string symbolsPath)
    {
        ReleaseVersion version = ReleaseVersion.Load(repository);
        string output = Path.Combine(repository, "src", "DotNetFrameworkToolkit", "bin", "Release", "net20");
        using (ZipArchive package = ZipFile.OpenRead(packagePath))
        {
            VerifyEntries(package, new[] { "readme.md", "package-icon.png", LIBRARY_PATH + ".dll", LIBRARY_PATH + ".xml" });
            VerifyManifest(repository, package, version, false);
            Compare(Read(package, "readme.md"), File.ReadAllBytes(Path.Combine(repository, "readme.md")), "Package README");
            Compare(Read(package, "package-icon.png"), File.ReadAllBytes(Path.Combine(repository, "content", "package-icon.png")), "Package icon");
            ImageVerifier.VerifyPng(Read(package, "package-icon.png"), 128, 128);
            byte[] dll = Read(package, LIBRARY_PATH + ".dll");
            Compare(dll, File.ReadAllBytes(Path.Combine(output, "DotNetFrameworkToolkit.dll")), "Release DLL");
            VerifyAssembly(dll, version);
            byte[] xml = Read(package, LIBRARY_PATH + ".xml");
            Compare(xml, File.ReadAllBytes(Path.Combine(output, "DotNetFrameworkToolkit.xml")), "XML documentation");
            using (MemoryStream stream = new(xml, false))
            {
                XDocument doc = XDocument.Load(stream);
                Require(doc.Root?.Element("assembly")?.Element("name")?.Value == "DotNetFrameworkToolkit" && doc.Descendants("member").Any(), "XML documentation must describe the toolkit and contain members.");
            }
            using (ZipArchive symbols = ZipFile.OpenRead(symbolsPath))
            {
                VerifyEntries(symbols, new[] { LIBRARY_PATH + ".pdb" }, new[] { LIBRARY_PATH + ".xml" });
                VerifyManifest(repository, symbols, version, true);
                byte[] pdb = Read(symbols, LIBRARY_PATH + ".pdb");
                Compare(pdb, File.ReadAllBytes(Path.Combine(output, "DotNetFrameworkToolkit.pdb")), "Release PDB");
                VerifySymbols(dll, pdb);
                if (symbols.GetEntry(LIBRARY_PATH + ".xml") != null)
                {
                    Compare(Read(symbols, LIBRARY_PATH + ".xml"), xml, "Symbol XML documentation");
                }
            }
        }
    }

    private static void VerifyEntries(ZipArchive archive, string[] required, string[]? optional = null)
    {
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/'))
            {
                continue;
            }
            Require(paths.Add(entry.FullName), "Duplicate/case-colliding package entry: " + entry.FullName);
            bool metadata = entry.FullName == "[Content_Types].xml" || entry.FullName == "_rels/.rels" || entry.FullName == PACKAGE_ID + ".nuspec" || (entry.FullName.StartsWith("package/services/metadata/core-properties/", StringComparison.Ordinal) && entry.FullName.EndsWith(".psmdcp", StringComparison.Ordinal));
            Require(metadata || required.Contains(entry.FullName, StringComparer.Ordinal) || (optional?.Contains(entry.FullName, StringComparer.Ordinal) ?? false), "Unexpected package payload: " + entry.FullName);
            Require(entry.Length > 0, "Empty package entry: " + entry.FullName);
        }
        foreach (string path in required.Concat(new[] { PACKAGE_ID + ".nuspec", "[Content_Types].xml", "_rels/.rels" }))
        {
            Require(archive.GetEntry(path) != null, "Missing package entry: " + path);
        }
    }

    private static void VerifyManifest(string repository, ZipArchive archive, ReleaseVersion version, bool symbols)
    {
        using (MemoryStream stream = new(Read(archive, PACKAGE_ID + ".nuspec"), false))
        {
            XElement root = XDocument.Load(stream).Root!;
            XNamespace ns = root.Name.Namespace;
            XElement metadata = root.Element(ns + "metadata") ?? throw new InvalidDataException("Missing package metadata.");
            Require(metadata.Element(ns + "id")?.Value == PACKAGE_ID, "Package ID mismatch.");
            Require(metadata.Element(ns + "version")?.Value == version.PackageVersion, "Package version mismatch.");
            Require(metadata.Element(ns + "license")?.Value == "MIT", "Package license mismatch.");
            if (!symbols)
            {
                Require(metadata.Element(ns + "icon")?.Value == "package-icon.png" && metadata.Element(ns + "readme")?.Value == "readme.md", "Package icon/readme metadata mismatch.");
            }
            XElement? source = metadata.Element(ns + "repository");
            Require(source?.Attribute("type")?.Value == "git" && source.Attribute("url")?.Value == "https://github.com/aaron-salisbury/DotNetFrameworkToolkit" && source.Attribute("commit")?.Value == GetSourceCommit(repository), "Repository metadata must identify the current source commit.");
            List<XElement> groups = metadata.Element(ns + "dependencies")?.Elements(ns + "group").ToList() ?? new();
            Require(groups.Count == 1 && new[] { "net20", ".NETFramework2.0", ".NETFramework,Version=v2.0" }.Contains(groups[0].Attribute("targetFramework")?.Value), "Exactly one net20 dependency group is required.");
            Dictionary<string, string> dependencies = groups[0].Elements(ns + "dependency").ToDictionary(item => item.Attribute("id")!.Value, item => item.Attribute("version")!.Value, StringComparer.OrdinalIgnoreCase);
            XElement config = XDocument.Load(Path.Combine(repository, "src", "DotNetFrameworkToolkit", "packages.config")).Root!;
            Dictionary<string, string> runtime = config.Elements("package").Where(item => item.Attribute("developmentDependency")?.Value != "true").ToDictionary(item => item.Attribute("id")!.Value, item => "[" + item.Attribute("version")!.Value + "]", StringComparer.OrdinalIgnoreCase);
            Require(runtime.Count == dependencies.Count && runtime.All(pair => dependencies.TryGetValue(pair.Key, out string? value) && value == pair.Value), "Package dependencies must exactly match restored runtime dependencies, excluding development tools.");
            Require(dependencies.ContainsKey("SqlServerCompact") && !dependencies.ContainsKey("System.Data.SqlServerCe_unofficial"), "The package must reference Microsoft's SQL CE package.");
            if (symbols)
            {
                Require(metadata.Element(ns + "packageTypes")?.Elements(ns + "packageType").Any(item => item.Attribute("name")?.Value == "SymbolsPackage") == true, "Symbol package type is missing.");
            }
        }
    }

    private static void VerifyAssembly(byte[] dll, ReleaseVersion version)
    {
        using (MemoryStream stream = new(dll, false))
        {
            using (PEReader pe = new(stream))
            {
                MetadataReader reader = pe.GetMetadataReader();
                AssemblyDefinition assembly = reader.GetAssemblyDefinition();
                Require(reader.GetString(assembly.Name) == "DotNetFrameworkToolkit" && assembly.Version == version.AssemblyVersion, "Assembly identity/version mismatch.");
                Require(reader.MetadataVersion == "v2.0.50727", "Packaged library must retain CLR 2.0 metadata.");
                Dictionary<string, string?> attributes = new();
                foreach (CustomAttributeHandle handle in assembly.GetCustomAttributes())
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(handle);
                    if (attribute.Constructor.Kind != HandleKind.MemberReference)
                    {
                        continue;
                    }
                    MemberReference constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    if (constructor.Parent.Kind != HandleKind.TypeReference)
                    {
                        continue;
                    }
                    TypeReference type = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                    string name = reader.GetString(type.Name);
                    BlobReader blob = reader.GetBlobReader(attribute.Value);
                    Require(blob.ReadUInt16() == 1, "Invalid custom attribute encoding.");
                    if (name == "AssemblyFileVersionAttribute" || name == "AssemblyInformationalVersionAttribute")
                    {
                        attributes[name] = blob.ReadSerializedString();
                    }
                    if (name == "DebuggableAttribute")
                    {
                        Require((blob.ReadInt32() & 256) == 0, "Package contains an unoptimized Debug assembly.");
                    }
                }
                Require(attributes.GetValueOrDefault("AssemblyFileVersionAttribute") == version.FileVersion.ToString() && attributes.GetValueOrDefault("AssemblyInformationalVersionAttribute") == version.PackageVersion, "File/informational version mismatch.");
            }
        }
    }

    private static void VerifySymbols(byte[] dll, byte[] pdb)
    {
        using (MemoryStream dllStream = new(dll, false))
        {
            using (PEReader pe = new(dllStream))
            {
                using (MemoryStream pdbStream = new(pdb, false))
                {
                    using (MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream))
                    {
                        MetadataReader reader = provider.GetMetadataReader();
                        ReadOnlySpan<byte> id = reader.DebugMetadataHeader!.Id.AsSpan();
                        DebugDirectoryEntry entry = pe.ReadDebugDirectory().Single(value => value.Type == DebugDirectoryEntryType.CodeView);
                        CodeViewDebugDirectoryData codeView = pe.ReadCodeViewDebugDirectoryData(entry);
                        Require(codeView.Guid == new Guid(id[..16]) && entry.Stamp == BinaryPrimitives.ReadUInt32LittleEndian(id[16..]) && codeView.Age == 1, "Portable PDB does not match the DLL debug identity.");
                        Require(reader.Documents.Count > 0, "Portable PDB has no source documents.");
                        DebugDirectoryEntry checksumEntry = pe.ReadDebugDirectory().Single(value => value.Type == DebugDirectoryEntryType.PdbChecksum);
                        PdbChecksumDebugDirectoryData checksum = pe.ReadPdbChecksumDebugDirectoryData(checksumEntry);
                        Require(checksum.AlgorithmName == "SHA256" && checksum.Checksum.AsSpan().SequenceEqual(SHA256.HashData(pdb)), "Portable PDB checksum does not match the DLL.");
                    }
                }
            }
        }
    }

    public static string GetSourceCommit(string repository)
    {
        using (Process process = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD")
        {
            WorkingDirectory = repository,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Could not start git."))
        {
            string commit = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            Require(process.ExitCode == 0 && commit.Length == 40 && commit.All(Uri.IsHexDigit), "Could not determine source commit.");
            return commit;
        }
    }

    public static void VerifyRejectionCases(string repository, string package, string symbols, string directory)
    {
        Directory.CreateDirectory(directory);
        try
        {
            void Reject(string source, string removed, string? added = null, byte[]? replacement = null, bool symbolFixture = false)
            {
                string fixture = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".zip");
                using (ZipArchive input = ZipFile.OpenRead(source))
                {
                    using (ZipArchive output = ZipFile.Open(fixture, ZipArchiveMode.Create))
                    {
                        foreach (ZipArchiveEntry entry in input.Entries)
                        {
                            if (entry.FullName != removed)
                            {
                                using (Stream destination = output.CreateEntry(entry.FullName).Open())
                                {
                                    using (Stream original = entry.Open())
                                    {
                                        original.CopyTo(destination);
                                    }
                                }
                            }
                        }
                        if (added != null)
                        {
                            using (Stream destination = output.CreateEntry(added).Open())
                            {
                                destination.Write(replacement!);
                            }
                        }
                    }
                }
                try
                {
                    Verify(repository, symbolFixture ? package : fixture, symbolFixture ? fixture : symbols);
                }
                catch (InvalidDataException)
                {
                    return;
                }
                throw new InvalidDataException("Package validator accepted a corrupt fixture.");
            }
            Reject(package, LIBRARY_PATH + ".xml");
            Reject(package, string.Empty, "lib/net20/DotNetFrameworkToolkit.Tests.dll", new byte[] { 1 });
            Reject(package, LIBRARY_PATH + ".dll", LIBRARY_PATH + ".dll", new byte[] { 1 });
            Reject(package, string.Empty, "readme.md", new byte[] { 1 });
            Reject(symbols, LIBRARY_PATH + ".pdb", symbolFixture: true);
            Reject(symbols, LIBRARY_PATH + ".pdb", LIBRARY_PATH + ".pdb", new byte[] { 1 }, true);
            using (ZipArchive source = ZipFile.OpenRead(package))
            {
                byte[] manifest = Read(source, PACKAGE_ID + ".nuspec");
                XDocument doc;
                using (MemoryStream stream = new(manifest, false))
                {
                    doc = XDocument.Load(stream);
                }
                doc.Descendants().First(element => element.Name.LocalName == "dependency").SetAttributeValue("version", "[99.0.0]");
                Reject(package, PACKAGE_ID + ".nuspec", PACKAGE_ID + ".nuspec", System.Text.Encoding.UTF8.GetBytes(doc.ToString()));
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    public static string HashFile(string path)
    {
        using (Stream stream = File.OpenRead(path))
        {
            return Convert.ToHexString(SHA256.HashData(stream));
        }
    }

    private static byte[] Read(ZipArchive archive, string path)
    {
        ZipArchiveEntry entry = archive.GetEntry(path) ?? throw new InvalidDataException("Missing package entry: " + path);
        using (Stream stream = entry.Open())
        {
            using (MemoryStream buffer = new())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }
    }

    private static void Compare(byte[] actual, byte[] expected, string description)
    {
        Require(actual.AsSpan().SequenceEqual(expected), description + " differs from its verified build/source input.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }
}
