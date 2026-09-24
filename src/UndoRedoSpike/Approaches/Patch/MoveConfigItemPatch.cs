public class MoveConfigItemPatch : IPatchOperation
{
    private readonly Guid _itemId;
    private readonly int _fromIndex;
    private readonly int _toIndex;

    public string Description =>
        "move ConfigItem";

    public MoveConfigItemPatch(
        Guid itemId,
        int fromIndex,
        int toIndex)
    {
        _itemId = itemId;
        _fromIndex = fromIndex;
        _toIndex = toIndex;
    }

    public void Apply(ProjectState project)
    {
        Move(project, _toIndex);
    }

    public void Undo(ProjectState project)
    {
        Move(project, _fromIndex);
    }

    private void Move(
        ProjectState project,
        int targetIndex)
    {
        var currentIndex =
            project.ConfigItems.FindIndex(
                item => item.Id == _itemId);

        if (currentIndex < 0)
        {
            throw new InvalidOperationException(
                $"ConfigItem {_itemId} was not found.");
        }

        var item =
            project.ConfigItems[currentIndex];

        project.ConfigItems.RemoveAt(currentIndex);

        project.ConfigItems.Insert(
            targetIndex,
            item);
    }
}