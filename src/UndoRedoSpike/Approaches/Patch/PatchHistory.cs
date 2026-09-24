public class PatchHistory
{
    private readonly Stack<PatchTransaction> _undoStack = new();
    private readonly Stack<PatchTransaction> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public IReadOnlyList<string> UndoEntryDetails =>
        _undoStack
            .Select(Describe)
            .ToList();

    public IReadOnlyList<string> RedoEntryDetails =>
        _redoStack
            .Select(Describe)
            .ToList();

    public void Execute(
        ProjectState project,
        PatchTransaction transaction)
    {
        transaction.Apply(project);

        _undoStack.Push(transaction);
        _redoStack.Clear();
    }

    public bool Undo(ProjectState project)
    {
        if (_undoStack.Count == 0)
            return false;

        var transaction = _undoStack.Peek();

        transaction.Undo(project);

        _undoStack.Pop();
        _redoStack.Push(transaction);

        return true;
    }

    public bool Redo(ProjectState project)
    {
        if (_redoStack.Count == 0)
            return false;

        var transaction = _redoStack.Peek();

        transaction.Apply(project);

        _redoStack.Pop();
        _undoStack.Push(transaction);

        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private static string Describe(
        PatchTransaction transaction)
    {
        return string.Join(
            " + ",
            transaction.Operations
                .Select(operation =>
                    operation.Description));
    }
}