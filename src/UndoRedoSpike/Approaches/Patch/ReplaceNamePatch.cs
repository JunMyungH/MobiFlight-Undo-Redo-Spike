public class ReplaceNamePatch : IPatchOperation
{
    private readonly Guid _itemId;
    private readonly string _before;
    private readonly string _after;

    public string Description =>
        "replace Name";

    public ReplaceNamePatch(
        Guid itemId,
        string before,
        string after)
    {
        _itemId = itemId;
        _before = before;
        _after = after;
    }

    public void Apply(ProjectState project)
    {
        FindItem(project).Name = _after;
    }

    public void Undo(ProjectState project)
    {
        FindItem(project).Name = _before;
    }

    private ConfigItem FindItem(ProjectState project)
    {
        return project.ConfigItems.FirstOrDefault(
            item => item.Id == _itemId)
            ?? throw new InvalidOperationException(
                $"ConfigItem {_itemId} was not found.");
    }
}