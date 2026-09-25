public class BulkToggleCommand : IUndoableCommand
{
    private readonly List<ConfigItem> _items;
    private readonly List<Change> _changes = new();
    private bool _executed;

    public BulkToggleCommand(
        IEnumerable<ConfigItem> items)
    {
        _items = items.ToList();
    }

    public void Execute()
    {
        if (_executed)
        {
            throw new InvalidOperationException(
                "Command has already been executed.");
        }

        foreach (var item in _items)
        {
            var before = item.Active;
            var after = !before;

            _changes.Add(
                new Change(
                    item,
                    before,
                    after));

            item.Active = after;
        }

        _executed = true;
    }

    private void EnsureExecuted()
    {
        if (!_executed)
        {
            throw new InvalidOperationException(
                "Command must be executed before Undo or Redo.");
        }
    }

    public void Redo()
    {
        EnsureExecuted();

        foreach (var change in _changes)
        {
            change.Item.Active =
                change.After;
        }
    }

    public void Undo()
    {
        EnsureExecuted();

        foreach (var change in _changes)
        {
            change.Item.Active =
                change.Before;
        }
    }

    private sealed record Change(
        ConfigItem Item,
        bool Before,
        bool After);
}