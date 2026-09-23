public class SnapshotHistory
{
    private readonly Stack<ProjectState> _undoStack = new();
    private readonly Stack<ProjectState> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public IReadOnlyList<int> UndoSnapshotSizes =>
    _undoStack
        .Select(snapshot => snapshot.ConfigItems.Count)
        .ToList();

    public IReadOnlyList<int> RedoSnapshotSizes =>
        _redoStack
            .Select(snapshot => snapshot.ConfigItems.Count)
            .ToList();

    public int StoredConfigItemCopies =>
        _undoStack.Sum(snapshot => snapshot.ConfigItems.Count)
        + _redoStack.Sum(snapshot => snapshot.ConfigItems.Count);

    public void Execute(ProjectState project, Action<ProjectState> mutation)
    {
        var before = project.Clone();

        mutation(project);

        _undoStack.Push(before);
        _redoStack.Clear();
    }

    private static void Restore(ProjectState target, ProjectState snapshot)
    {
        target.ConfigItems = snapshot.ConfigItems
            .Select(item => item.Clone())
            .ToList();
    }

    public bool Undo(ProjectState project)
    {
        if (_undoStack.Count == 0)
            return false;

        var current = project.Clone();
        var previous = _undoStack.Pop();

        Restore(project, previous);

        _redoStack.Push(current);

        return true;
    }

    public bool Redo(ProjectState project)
    {
        if (_redoStack.Count == 0)
            return false;

        var current = project.Clone();
        var next = _redoStack.Pop();

        Restore(project, next);

        _undoStack.Push(current);

        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}