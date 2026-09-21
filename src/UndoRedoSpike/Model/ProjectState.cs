public class ProjectState
{
    public List<ConfigItem> ConfigItems { get; set; } = new();

    public ProjectState Clone()
    {
        return new ProjectState
        {
            ConfigItems = ConfigItems
                .Select(item => item.Clone())
                .ToList()
        };
    }
}