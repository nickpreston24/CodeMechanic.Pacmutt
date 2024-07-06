public class NpmUpdate
{
    public string Package { get; set; } = string.Empty; //npm package name
    public string Current { get; set; } = string.Empty; // current ver.
    public string Latest { get; set; } = string.Empty; // latest ver.
    public string Location { get; set; } = string.Empty; // current project folder
}