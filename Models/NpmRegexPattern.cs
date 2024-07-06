using CodeMechanic.RegularExpressions;

public class NpmRegexPattern : RegexEnumBase
{
    // Version bump
    public static NpmRegexPattern Bump = new NpmRegexPattern(1, nameof(Bump),
        @"(?<Package>[@\w\/-]+)\s*(?<Current>(\d+\.\d+\.\d+|MISSING)(-\w+\d*)*)\s+(?<Wanted>\d+\.\d+\.\d+(-\w+\d*)*)\s*(?<Latest>\d+\.\d+\.\d+(-\w+\d*)*)\s+(?<Location>[\w-]+)",
        "https://regex101.com/r/Nnn8Bt/1"); // updated to fix npm installs

    protected NpmRegexPattern(int id, string name, string pattern, string uri = "") : base(id, name, pattern, uri)
    {
    }
}