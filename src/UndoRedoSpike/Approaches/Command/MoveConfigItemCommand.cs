public class MoveConfigItemCommand
    : IUndoableCommand
{
    private readonly ProjectState _project;
    private readonly Guid _itemId;
    private readonly int _fromIndex;
    private readonly int _toIndex;

    private bool _executed;

    public MoveConfigItemCommand(
        ProjectState project,
        Guid itemId,
        int fromIndex,
        int toIndex)
    {
        _project = project;
        _itemId = itemId;
        _fromIndex = fromIndex;
        _toIndex = toIndex;
    }

    public void Execute()
    {
        if (_executed)
        {
            throw new InvalidOperationException(
                "Command has already been executed.");
        }

        Move(_toIndex);
        _executed = true;
    }

    public void Undo()
    {
        EnsureExecuted();
        Move(_fromIndex);
    }

    public void Redo()
    {
        EnsureExecuted();
        Move(_toIndex);
    }

    private void Move(int targetIndex)
    {
        var currentIndex =
            _project.ConfigItems.FindIndex(
                item => item.Id == _itemId);

        if (currentIndex < 0)
        {
            throw new InvalidOperationException(
                $"ConfigItem {_itemId} was not found.");
        }

        var item =
            _project.ConfigItems[currentIndex];

        _project.ConfigItems.RemoveAt(
            currentIndex);

        _project.ConfigItems.Insert(
            targetIndex,
            item);
    }

    private void EnsureExecuted()
    {
        if (!_executed)
        {
            throw new InvalidOperationException(
                "Command must be executed before Undo or Redo.");
        }
    }
}