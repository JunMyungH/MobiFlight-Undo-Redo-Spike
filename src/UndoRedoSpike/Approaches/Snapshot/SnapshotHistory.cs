public class SnapshotHistory
{
    private readonly Stack<SnapshotHistoryEntry>
        _undoStack = new();

    private readonly Stack<SnapshotHistoryEntry>
        _redoStack = new();

    public bool CanUndo =>
        _undoStack.Count > 0;

    public bool CanRedo =>
        _redoStack.Count > 0;

    public int UndoCount =>
        _undoStack.Count;

    public int RedoCount =>
        _redoStack.Count;

    public int NextUndoActionCount =>
    _undoStack.TryPeek(
        out var entry)
        ? entry.ActionCount
        : 0;

    public int NextRedoActionCount =>
        _redoStack.TryPeek(
            out var entry)
            ? entry.ActionCount
            : 0;

    public IReadOnlyList<string> UndoEntryDetails =>
        _undoStack
            .Select(
                entry =>
                    entry.Describe())
            .ToList();

    public IReadOnlyList<string> RedoEntryDetails =>
        _redoStack
            .Select(
                entry =>
                    entry.Describe())
            .ToList();

    public int StoredConfigItemCopies =>
        _undoStack.Sum(
            entry =>
                entry.StoredConfigItemCopies)
        +
        _redoStack.Sum(
            entry =>
                entry.StoredConfigItemCopies);

    public void Execute(
        ProjectState project,
        Action<ProjectState> mutation)
    {
        var before =
            project.Clone();

        mutation(project);

        _undoStack.Push(
            SnapshotHistoryEntry.Single(
                before));

        _redoStack.Clear();
    }

    public bool Undo(
        ProjectState project)
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        var entry =
            _undoStack.Pop();

        if (
            entry.IsHistoryJump &&
            entry.GroupedEntries
                is not null)
        {
            Restore(
                project,
                entry.Snapshot);

            RestoreEntries(
                _redoStack,
                entry.GroupedEntries);

            return true;
        }

        var current =
            project.Clone();

        Restore(
            project,
            entry.Snapshot);

        _redoStack.Push(
            SnapshotHistoryEntry.Single(
                current));

        return true;
    }

    public bool Redo(
        ProjectState project)
    {
        if (_redoStack.Count == 0)
        {
            return false;
        }

        var entry =
            _redoStack.Pop();

        if (
            entry.IsHistoryJump &&
            entry.GroupedEntries
                is not null)
        {
            Restore(
                project,
                entry.Snapshot);

            RestoreEntries(
                _undoStack,
                entry.GroupedEntries);

            return true;
        }

        var current =
            project.Clone();

        Restore(
            project,
            entry.Snapshot);

        _undoStack.Push(
            SnapshotHistoryEntry.Single(
                current));

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

        var removedEntries =
            new List<SnapshotHistoryEntry>(
                actionCount);

        SnapshotHistoryEntry? target =
            null;

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            target =
                _undoStack.Pop();

            removedEntries.Add(
                target);
        }

        if (target is null)
        {
            return false;
        }

        Restore(
            project,
            target.Snapshot);

        _redoStack.Push(
            SnapshotHistoryEntry.HistoryJump(
                current,
                removedEntries));

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

        var removedEntries =
            new List<SnapshotHistoryEntry>(
                actionCount);

        SnapshotHistoryEntry? target =
            null;

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            target =
                _redoStack.Pop();

            removedEntries.Add(
                target);
        }

        if (target is null)
        {
            return false;
        }

        Restore(
            project,
            target.Snapshot);

        _undoStack.Push(
            SnapshotHistoryEntry.HistoryJump(
                current,
                removedEntries));

        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private static void RestoreEntries(
        Stack<SnapshotHistoryEntry> targetStack,
        IReadOnlyList<SnapshotHistoryEntry> entriesNewestFirst)
    {
        for (
            var i =
                entriesNewestFirst.Count - 1;
            i >= 0;
            i--)
        {
            targetStack.Push(
                entriesNewestFirst[i]);
        }
    }

    private static void Restore(
        ProjectState target,
        ProjectState snapshot)
    {
        target.ConfigItems =
            snapshot.ConfigItems
                .Select(
                    item =>
                        item.Clone())
                .ToList();
    }
}