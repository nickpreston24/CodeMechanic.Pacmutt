using System.Text.RegularExpressions;
using CodeMechanic.Types;

public class VersionRegex : Enumeration
{
    public Regex CompiledRegex { get; }

    public static VersionRegex Version { get; set; } = new VersionRegex(1, nameof(Version),
        @"Include=""(?<package_name>[\w\d.]+?)""\s*Version=""(?<version_number>(\d+\.\d+\.\d+|\w+))""");

    // https://regex101.com/r/JS9kCx/1
    public static VersionRegex Release { get; set; } = new VersionRegex(1, nameof(Release),
        @"(?<major_release>\d+)\.(?<minor_release>\d{1,3})\.(?<patch>\d{1,3})");

    public VersionRegex(int id, string name, string pattern) : base(id, name)
    {
        CompiledRegex = new Regex(pattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
    }
}