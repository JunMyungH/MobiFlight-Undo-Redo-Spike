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

    public bool UndoTo(
        ProjectState project,
        int actionCount)
        {
            if (
                actionCount < 1 ||
                actionCount >
                    _undoStack.Count)
            {
                return false;
            }

            var current =
                project.Clone();

            ProjectState? target =
                null;

            for (
                var i = 0;
                i < actionCount;
                i++)
            {
                target =
                    _undoStack.Pop();
            }

            if (target is null)
            {
                return false;
            }

            Restore(
                project,
                target);
            _redoStack.Push(
                current);

            return true;
        }

        public bool RedoTo(
            ProjectState project,
            int actionCount)
        {
            if (
                actionCount < 1 ||
                actionCount >
                    _redoStack.Count)
            {
                return false;
            }

            var current =
                project.Clone();

            ProjectState? target =
                null;

            for (
                var i = 0;
                i < actionCount;
                i++)
            {
                target =
                    _redoStack.Pop();
            }

            if (target is null)
            {
                return false;
            }

            Restore(
                project,
                target);

            _undoStack.Push(
                current);

            return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}