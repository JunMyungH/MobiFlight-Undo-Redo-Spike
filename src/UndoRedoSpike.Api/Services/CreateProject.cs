namespace UndoRedoSpike.Api.Services
{
    public static class CreateProject
    {
        public static ProjectState Create(int itemCount)
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
}
