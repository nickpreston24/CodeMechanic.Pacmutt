public record UpgradeStats
{
    public int successful_upgrades { get; set; } = -1;
    public int failed_upgrades { get; set; } = -1;
    public int total_projects { get; set; } = -1;
}