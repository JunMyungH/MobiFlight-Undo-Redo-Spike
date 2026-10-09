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
        Action<ProjectState> mutation,
        string actionName = "Project Change")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            actionName);

        var before =
            project.Clone();

        try
        {
            mutation(project);
        }
        catch
        {
            Restore(project, before);
            throw;
        }

        if (SameState(before, project))
        {
            return;
        }

        _undoStack.Push(
            SnapshotHistoryEntry.Single(
                before,
                actionName));

        _redoStack.Clear();
    }

    public bool Undo(
        ProjectState project)
    {
        if (!_undoStack.TryPeek(out var entry))
        {
            return false;
        }

        if (
            entry.IsHistoryJump &&
            entry.GroupedEntries
                is not null)
        {
            Restore(
                project,
                entry.Snapshot);

            _undoStack.Pop();

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

        _undoStack.Pop();

        _redoStack.Push(
            SnapshotHistoryEntry.Single(
                current,
                entry.ActionName));

        return true;
    }

    public bool Redo(
        ProjectState project)
    {
        if (!_redoStack.TryPeek(out var entry))
        {
            return false;
        }

        if (
            entry.IsHistoryJump &&
            entry.GroupedEntries
                is not null)
        {
            Restore(
                project,
                entry.Snapshot);

            _redoStack.Pop();

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

        _redoStack.Pop();

        _undoStack.Push(
            SnapshotHistoryEntry.Single(
                current,
                entry.ActionName));

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

        var selectedEntries =
            _undoStack
                .Take(actionCount)
                .ToList();

        var destination =
            selectedEntries[^1].Snapshot;

        Restore(project, destination);

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            _undoStack.Pop();
        }

        _redoStack.Push(
            SnapshotHistoryEntry.HistoryJump(
                current,
                selectedEntries));

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

        var selectedEntries =
            _redoStack
                .Take(actionCount)
                .ToList();

        var destination =
            selectedEntries[^1].Snapshot;

        Restore(project, destination);

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            _redoStack.Pop();
        }

        _undoStack.Push(
            SnapshotHistoryEntry.HistoryJump(
                current,
                selectedEntries));

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
        var restoredItems = 
            snapshot.ConfigItems
                .Select(
                    item =>
                        item.Clone())
                .ToList();

        target.ConfigItems = restoredItems;
    }

    private static bool SameState(
        ProjectState before,
        ProjectState after)
    {
        if (before.ConfigItems.Count !=
            after.ConfigItems.Count)
        {
            return false;
        }

        for (var i = 0;
            i < before.ConfigItems.Count;
            i++)
        {
            var oldItem = before.ConfigItems[i];
            var newItem = after.ConfigItems[i];

            if (oldItem.Id != newItem.Id ||
                oldItem.Name != newItem.Name ||
                oldItem.Active != newItem.Active)
            {
                return false;
            }
        }
        return true;
    }
}