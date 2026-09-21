public class ConfigItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public bool Active { get; set; }

    public ConfigItem Clone()
    {
        return new ConfigItem
        {
            Id = Id,
            Name = Name,
            Active = Active
        };
    }
}