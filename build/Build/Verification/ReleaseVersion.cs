using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Path = System.IO.Path;

namespace Build.Verification;

internal sealed class ReleaseVersion
{
    public string PackageVersion { get; }
    public Version AssemblyVersion { get; }
    public Version FileVersion { get; }

    private ReleaseVersion(string packageVersion, Version assemblyVersion, Version fileVersion)
    {
        PackageVersion = packageVersion;
        AssemblyVersion = assemblyVersion;
        FileVersion = fileVersion;
    }

    public static ReleaseVersion Load(string repository)
    {
        XElement root = XDocument.Load(Path.Combine(repository, "version.props")).Root!;
        string Read(string name)
        {
            return root.Descendants().Single(element => element.Name.LocalName == name).Value;
        }
        return Parse(Read("PackageVersion"), Read("AssemblyVersion"), Read("AssemblyFileVersion"));
    }

    internal static ReleaseVersion Parse(string package, string assembly, string file)
    {
        if (!Regex.IsMatch(package, @"\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?\z"))
        {
            throw new InvalidOperationException("PackageVersion must be major.minor.patch with an optional SemVer prerelease; build metadata is not supported.");
        }
        string[] versionParts = package.Split('-');
        if (versionParts.Length > 1)
        {
            string suffix = package.Substring(package.IndexOf('-') + 1);
            foreach (string identifier in suffix.Split('.'))
            {
                if (identifier.Length > 1 && identifier[0] == '0' && identifier.All(char.IsDigit))
                {
                    throw new InvalidOperationException("Numeric prerelease identifiers cannot have leading zeroes.");
                }
            }
        }
        if (!Version.TryParse(assembly, out Version? assemblyVersion) || assemblyVersion.Revision < 0 || !Version.TryParse(file, out Version? fileVersion) || file != versionParts[0] + ".0")
        {
            throw new InvalidOperationException("Use a four-part AssemblyVersion and AssemblyFileVersion equal to the package's numeric version plus .0.");
        }
        foreach (Version value in new[] { assemblyVersion, fileVersion })
        {
            if (value.Major > 65534 || value.Minor > 65534 || value.Build > 65534 || value.Revision > 65534)
            {
                throw new InvalidOperationException("Assembly/file version components must fit the compiler's 0–65534 range.");
            }
        }
        return new ReleaseVersion(package, assemblyVersion, fileVersion);
    }

    public void ValidateTag(string? requestedTag, string? githubRefType, string? githubRefName)
    {
        string expected = "v" + PackageVersion;
        if (requestedTag != null && requestedTag != expected)
        {
            throw new InvalidOperationException("Release tag must equal " + expected + ".");
        }
        if (githubRefType == "tag" && githubRefName != expected)
        {
            throw new InvalidOperationException("GitHub tag must equal " + expected + ".");
        }
    }

    public static void VerifyRules()
    {
        ReleaseVersion stable = Parse("0.2.9", "1.0.0.0", "0.2.9.0");
        stable.ValidateTag("v0.2.9", "branch", "master");
        stable.ValidateTag(null, "tag", "v0.2.9");
        ReleaseVersion preview = Parse("0.2.9-preview.1", "1.0.0.0", "0.2.9.0");
        preview.ValidateTag("v0.2.9-preview.1", "tag", "v0.2.9-preview.1");
        ExpectFailure(() => stable.ValidateTag("v0.2.8", null, null));
        ExpectFailure(() => stable.ValidateTag(null, "tag", "v0.2.8"));
        ExpectFailure(() => Parse("0.02.9", "1.0.0.0", "0.2.9.0"));
        ExpectFailure(() => Parse("0.2.9-preview.01", "1.0.0.0", "0.2.9.0"));
        ExpectFailure(() => Parse("0.2.9+build", "1.0.0.0", "0.2.9.0"));
        ExpectFailure(() => Parse("0.2.9", "1.0.0", "0.2.9.0"));
        ExpectFailure(() => Parse("0.2.9", "1.0.0.0", "0.2.8.0"));
        ExpectFailure(() => Parse("0.2.9", "65535.0.0.0", "0.2.9.0"));
    }

    private static void ExpectFailure(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException("Version policy accepted an invalid version/tag fixture.");
    }
}
