public class DuplicateConfigItemCommand
    : IUndoableCommand
{
    private readonly ProjectState _project;
    private readonly Guid _sourceItemId;

    private ConfigItem? _createdItem;
    private int _insertIndex = -1;
    private bool _executed;

    public DuplicateConfigItemCommand(
        ProjectState project,
        Guid sourceItemId)
    {
        _project = project;
        _sourceItemId = sourceItemId;
    }

    public void Execute()
    {
        if (_executed)
        {
            throw new InvalidOperationException(
                "Command has already been executed.");
        }

        var sourceIndex =
            _project.ConfigItems.FindIndex(
                item => item.Id == _sourceItemId);

        if (sourceIndex < 0)
        {
            throw new InvalidOperationException(
                $"ConfigItem {_sourceItemId} was not found.");
        }

        var source =
            _project.ConfigItems[sourceIndex];

        _createdItem =
            new ConfigItem
            {
                Name = $"{source.Name} (Copy)",
                Active = source.Active
            };

        _insertIndex =
            sourceIndex + 1;

        _project.ConfigItems.Insert(
            _insertIndex,
            _createdItem);

        _executed = true;
    }

    public void Undo()
    {
        EnsureExecuted();

        if (_createdItem is null)
        {
            throw new InvalidOperationException(
                "Created item was not stored.");
        }

        var index =
            _project.ConfigItems.FindIndex(
                item =>
                    item.Id ==
                    _createdItem.Id);

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"ConfigItem {_createdItem.Id} was not found.");
        }

        _project.ConfigItems.RemoveAt(index);
    }

    public void Redo()
    {
        EnsureExecuted();

        if (_createdItem is null)
        {
            throw new InvalidOperationException(
                "Created item was not stored.");
        }

        _project.ConfigItems.Insert(
            _insertIndex,
            _createdItem);
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