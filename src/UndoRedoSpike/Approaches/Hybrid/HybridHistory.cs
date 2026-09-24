public class HybridHistory
{
    private readonly Stack<HybridHistoryEntry> _undoStack = new();
    private readonly Stack<HybridHistoryEntry> _redoStack = new();

    public bool CanUndo =>
        _undoStack.Count > 0;

    public bool CanRedo =>
        _redoStack.Count > 0;

    public int UndoCount =>
        _undoStack.Count;

    public int RedoCount =>
        _redoStack.Count;

    public IReadOnlyList<string> UndoEntryDetails =>
        _undoStack
            .Select(entry => entry.Describe())
            .ToList();

    public IReadOnlyList<string> RedoEntryDetails =>
        _redoStack
            .Select(entry => entry.Describe())
            .ToList();

    public void Execute(
        ProjectState project,
        HybridHistoryEntry entry)
    {
        entry.Apply(project);

        _undoStack.Push(entry);
        _redoStack.Clear();
    }

    public bool Undo(ProjectState project)
    {
        if (_undoStack.Count == 0)
            return false;

        var entry =
            _undoStack.Peek();

        entry.Undo(project);

        _undoStack.Pop();
        _redoStack.Push(entry);

        return true;
    }

    public bool Redo(ProjectState project)
    {
        if (_redoStack.Count == 0)
            return false;

        var entry =
            _redoStack.Peek();

        entry.Apply(project);

        _redoStack.Pop();
        _undoStack.Push(entry);

        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}