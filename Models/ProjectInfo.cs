using CodeMechanic.FileSystem;

public record ProjectInfo
{
    // todo: extract this as well, and update to latest.
    public string[] raw_using_statements { get; set; } = Array.Empty<string>();
    public string directory_path { get; set; }
    public Grepper.GrepResult grep_result { get; set; } = new();
    public VersionInfo version_info { get; set; } = new();
}