public class CommandHistory
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public IReadOnlyList<string> UndoEntryTypes =>
    _undoStack
        .Select(command => command.GetType().Name)
        .ToList();

    public IReadOnlyList<string> RedoEntryTypes =>
        _redoStack
            .Select(command => command.GetType().Name)
            .ToList();

    public void Execute(IUndoableCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0)
            return false;

        var command = _undoStack.Pop();

        command.Undo();
        _redoStack.Push(command);

        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0)
            return false;

        var command = _redoStack.Pop();

        command.Redo();
        _undoStack.Push(command);

        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}