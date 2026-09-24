public class ReplaceActivePatch : IPatchOperation
{
    private readonly Guid _itemId;
    private readonly bool _before;
    private readonly bool _after;

    public string Description =>
        "replace Active";

    public ReplaceActivePatch(
        Guid itemId,
        bool before,
        bool after)
    {
        _itemId = itemId;
        _before = before;
        _after = after;
    }

    public void Apply(ProjectState project)
    {
        FindItem(project).Active = _after;
    }

    public void Undo(ProjectState project)
    {
        FindItem(project).Active = _before;
    }

    private ConfigItem FindItem(ProjectState project)
    {
        return project.ConfigItems.FirstOrDefault(
            item => item.Id == _itemId)
            ?? throw new InvalidOperationException(
                $"ConfigItem {_itemId} was not found.");
    }
}