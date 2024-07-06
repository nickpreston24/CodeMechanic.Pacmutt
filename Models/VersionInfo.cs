public class VersionInfo
{
    public string package_name { get; set; } = string.Empty;
    public string version_number { get; set; } = string.Empty;
    public string lastest_version_number => $"{major_release}.{minor_release}.{patch}"; //{ get; set; } = string.Empty;
    public string oldest_version_number { get; set; } = string.Empty;

    public int major_release { get; set; } = 1;
    public int minor_release { get; set; } = 0;
    public int patch { get; set; } = 0;
}