public class HybridHistory
{
    private readonly Stack<HybridHistoryEntry> _undoStack =
        new();

    private readonly Stack<HybridHistoryEntry> _redoStack =
        new();

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

    public void Execute(
        ProjectState project,
        HybridHistoryEntry entry)
    {
        entry.Apply(
            project);

        _undoStack.Push(
            entry);

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
            _undoStack.Peek();

        entry.Undo(
            project);

        _undoStack.Pop();

        if (
            entry.IsHistoryJump &&
            entry.GroupedEntries
                is not null)
        {
            var originals =
                entry.GroupedEntries;

            for (
                var i =
                    originals.Count - 1;
                i >= 0;
                i--)
            {
                _redoStack.Push(
                    originals[i]);
            }
        }
        else
        {
            _redoStack.Push(
                entry);
        }

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
            _redoStack.Peek();

        entry.Apply(
            project);

        _redoStack.Pop();

        if (
            entry.IsHistoryJump &&
            entry.GroupedEntries
                is not null)
        {
            foreach (
                var original in
                entry.GroupedEntries)
            {
                _undoStack.Push(
                    original);
            }
        }
        else
        {
            _undoStack.Push(
                entry);
        }

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

        var entriesNewestFirst =
            _undoStack
                .Take(actionCount)
                .ToList();

        var undoneEntries =
            new List<HybridHistoryEntry>(
                actionCount);

        try
        {
            foreach (
                var entry in
                entriesNewestFirst)
            {
                entry.Undo(
                    project);

                undoneEntries.Add(
                    entry);
            }
        }
        catch
        {
            for (
                var i =
                    undoneEntries.Count - 1;
                i >= 0;
                i--)
            {
                undoneEntries[i]
                    .Apply(project);
            }

            throw;
        }

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            _undoStack.Pop();
        }

        entriesNewestFirst.Reverse();

        var groupedEntry =
            HybridHistoryEntry.CreateHistoryJump(
                entriesNewestFirst);

        _redoStack.Push(
            groupedEntry);

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

        var entries =
            _redoStack
                .Take(actionCount)
                .ToList();

        var appliedEntries =
            new List<HybridHistoryEntry>(
                actionCount);

        try
        {
            foreach (
                var entry in
                entries)
            {
                entry.Apply(
                    project);

                appliedEntries.Add(
                    entry);
            }
        }
        catch
        {
            for (
                var i =
                    appliedEntries.Count - 1;
                i >= 0;
                i--)
            {
                appliedEntries[i]
                    .Undo(project);
            }

            throw;
        }

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            _redoStack.Pop();
        }

        var groupedEntry =
            HybridHistoryEntry.CreateHistoryJump(
                entries);

        _undoStack.Push(
            groupedEntry);

        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}