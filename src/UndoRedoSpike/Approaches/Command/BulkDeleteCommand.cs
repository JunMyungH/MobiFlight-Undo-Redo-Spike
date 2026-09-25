public class BulkDeleteCommand : IUndoableCommand
{
    private readonly ProjectState _project;

    private readonly HashSet<Guid> _itemIds;

    private readonly List<DeletedItem> _deletedItems =
        new();

    private bool _executed;

    public BulkDeleteCommand(
        ProjectState project,
        IEnumerable<ConfigItem> items)
    {
        _project = project;

        _itemIds = items
            .Select(item => item.Id)
            .ToHashSet();
    }

    public void Execute()
    {
        if (_executed)
        {
            throw new InvalidOperationException(
                "Command has already been executed.");
        }

        for (var i = 0;
             i < _project.ConfigItems.Count;
             i++)
        {
            var item =
                _project.ConfigItems[i];

            if (_itemIds.Contains(item.Id))
            {
                _deletedItems.Add(
                    new DeletedItem(
                        item,
                        i));
            }
        }

        for (var i =
             _project.ConfigItems.Count - 1;
             i >= 0;
             i--)
        {
            if (_itemIds.Contains(
                _project.ConfigItems[i].Id))
            {
                _project.ConfigItems.RemoveAt(i);
            }
        }

        _executed = true;
    }

    public void Undo()
    {
        EnsureExecuted();

        foreach (var deleted in
                 _deletedItems.OrderBy(
                     item => item.Index))
        {
            _project.ConfigItems.Insert(
                deleted.Index,
                deleted.Item);
        }
    }

    public void Redo()
    {
        EnsureExecuted();

        foreach (var deleted in
                 _deletedItems.OrderByDescending(
                     item => item.Index))
        {
            var index =
                _project.ConfigItems.FindIndex(
                    item =>
                        item.Id ==
                        deleted.Item.Id);

            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"ConfigItem {deleted.Item.Id} was not found.");
            }

            _project.ConfigItems.RemoveAt(index);
        }
    }

    private void EnsureExecuted()
    {
        if (!_executed)
        {
            throw new InvalidOperationException(
                "Command must be executed before Undo or Redo.");
        }
    }

    private sealed record DeletedItem(
        ConfigItem Item,
        int Index);
}