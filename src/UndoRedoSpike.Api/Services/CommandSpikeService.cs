public class CommandSpikeService
{
    public ProjectState Project { get; private set; }
    public CommandHistory History { get; } = new();

    public CommandSpikeService()
    {
        Project = CreateProject(3);
    }

    public void Reset(int itemCount)
    {
        Project = CreateProject(itemCount);
        History.Clear();
    }

    private static ProjectState CreateProject(int itemCount)
    {
        if (itemCount == 3)
        {
            return new ProjectState
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
        }

        return new ProjectState
        {
            ConfigItems = Enumerable
                .Range(1, itemCount)
                .Select(index => new ConfigItem
                {
                    Name = $"Config Item {index}",
                    Active = index % 2 == 0
                })
                .ToList()
        };
    }
}