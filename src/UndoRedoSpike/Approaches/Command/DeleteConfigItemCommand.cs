public class DeleteConfigItemCommand : IUndoableCommand
{
    private readonly ProjectState _project;
    private readonly Guid _itemId;

    private ConfigItem? _deletedItem;
    private int _originalIndex = -1;
    private bool _executed;

    public DeleteConfigItemCommand(
        ProjectState project,
        Guid itemId)
    {
        _project = project;
        _itemId = itemId;
    }

    public void Execute()
    {
        var index = _project.ConfigItems.FindIndex(
            item => item.Id == _itemId);

        if (index < 0)
            throw new InvalidOperationException(
                $"ConfigItem {_itemId} was not found.");

        _originalIndex = index;
        _deletedItem = _project.ConfigItems[index];

        _project.ConfigItems.RemoveAt(index);

        _executed = true;
    }

    public void Undo()
    {
        EnsureExecuted();

        if (_deletedItem is null)
            throw new InvalidOperationException(
                "Deleted item was not stored.");

        _project.ConfigItems.Insert(
            _originalIndex,
            _deletedItem);
    }

    public void Redo()
    {
        EnsureExecuted();

        var index = _project.ConfigItems.FindIndex(
            item => item.Id == _itemId);

        if (index < 0)
            throw new InvalidOperationException(
                $"ConfigItem {_itemId} was not found.");

        _project.ConfigItems.RemoveAt(index);
    }

    private void EnsureExecuted()
    {
        if (!_executed)
            throw new InvalidOperationException(
                "Command must be executed before Undo or Redo.");
    }
}