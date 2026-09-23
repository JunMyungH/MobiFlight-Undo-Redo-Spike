public class CompoundEditCommand : IUndoableCommand
{
    private readonly ProjectState _project;
    private readonly Guid _itemId;

    private string _beforeName = "";
    private string _afterName = "";

    private bool _beforeActive;
    private bool _afterActive;

    private int _beforeIndex = -1;
    private int _afterIndex = -1;

    private bool _executed;

    public CompoundEditCommand(
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
        {
            throw new InvalidOperationException(
                $"ConfigItem {_itemId} was not found.");
        }

        var item = _project.ConfigItems[index];

        _beforeName = item.Name;
        _beforeActive = item.Active;
        _beforeIndex = index;

        _afterName = $"{item.Name} (Edited)";
        _afterActive = !item.Active;
        _afterIndex = _project.ConfigItems.Count - 1;

        item.Name = _afterName;
        item.Active = _afterActive;

        MoveItem(_afterIndex);

        _executed = true;
    }

    public void Undo()
    {
        EnsureExecuted();

        var item = FindItem();

        item.Name = _beforeName;
        item.Active = _beforeActive;

        MoveItem(_beforeIndex);
    }

    public void Redo()
    {
        EnsureExecuted();

        var item = FindItem();

        item.Name = _afterName;
        item.Active = _afterActive;

        MoveItem(_afterIndex);
    }

    private ConfigItem FindItem()
    {
        return _project.ConfigItems.FirstOrDefault(
            item => item.Id == _itemId)
            ?? throw new InvalidOperationException(
                $"ConfigItem {_itemId} was not found.");
    }

    private void MoveItem(int targetIndex)
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

        _project.ConfigItems.RemoveAt(currentIndex);

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