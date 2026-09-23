public class SnapshotSpikeService
{
    public ProjectState Project { get; } = new()
    {
        ConfigItems =
        [
            new ConfigItem
            {
                Name = "Landing Light",
                Active = true
            },
            new ConfigItem
            {
                Name = "Gear Indicator",
                Active = false
            },
            new ConfigItem
            {
                Name = "Flaps",
                Active = true
            }
        ]
    };

    public SnapshotHistory History { get; } = new();
}