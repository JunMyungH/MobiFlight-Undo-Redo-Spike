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
            _undoStack.Pop();

        entry.Undo(
            project);

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
            _redoStack.Pop();

        entry.Apply(
            project);

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
            new List<HybridHistoryEntry>(
                actionCount);

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            var entry =
                _undoStack.Pop();

            entry.Undo(
                project);

            entriesNewestFirst.Add(
                entry);
        }

        entriesNewestFirst.Reverse();

        var groupedEntry =
            HybridHistoryEntry.CreateHistoryJump(
                entriesNewestFirst,
                $"History Jump ({actionCount} actions)");

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
            new List<HybridHistoryEntry>(
                actionCount);

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            var entry =
                _redoStack.Pop();

            entry.Apply(
                project);

            entries.Add(
                entry);
        }

        var groupedEntry =
            HybridHistoryEntry.CreateHistoryJump(
                entries,
                $"History Jump ({actionCount} actions)");

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