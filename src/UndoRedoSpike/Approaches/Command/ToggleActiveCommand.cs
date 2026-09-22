public class ToggleActiveCommand : IUndoableCommand
{
    private readonly ConfigItem _item;

    private bool _before;
    private bool _after;
    private bool _executed;

    public ToggleActiveCommand(ConfigItem item)
    {
        _item = item;
    }

    public void Execute()
    {
        _before = _item.Active;
        _after = !_before;

        _item.Active = _after;

        _executed = true;
    }

    public void Undo()
    {
        EnsureExecuted();

        _item.Active = _before;
    }

    public void Redo()
    {
        EnsureExecuted();

        _item.Active = _after;
    }

    private void EnsureExecuted()
    {
        if (!_executed)
            throw new InvalidOperationException(
                "Command must be executed before Undo or Redo.");
    }
}