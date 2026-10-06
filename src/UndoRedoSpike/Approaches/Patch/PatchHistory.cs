public class PatchHistory
{
    private readonly Stack<PatchTransaction> _undoStack =
        new();

    private readonly Stack<PatchTransaction> _redoStack =
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
            out var transaction)
            ? transaction.ActionCount
            : 0;

    public int NextRedoActionCount =>
        _redoStack.TryPeek(
            out var transaction)
            ? transaction.ActionCount
            : 0;

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

        _undoStack.Push(
            transaction);

        _redoStack.Clear();
    }

    public bool Undo(
    ProjectState project)
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        var transaction =
            _undoStack.Peek();

        transaction.Undo(
            project);

        _undoStack.Pop();

        if (
            transaction.IsHistoryJump &&
            transaction.GroupedTransactions
                is not null)
        {
            var originals =
                transaction.GroupedTransactions;

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
                transaction);
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

        var transaction =
            _redoStack.Peek();

        transaction.Apply(
            project);

        _redoStack.Pop();

        if (
            transaction.IsHistoryJump &&
            transaction.GroupedTransactions
                is not null)
        {
            foreach (
                var original in
                transaction.GroupedTransactions)
            {
                _undoStack.Push(
                    original);
            }
        }
        else
        {
            _undoStack.Push(
                transaction);
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

        var transactionsNewestFirst =
            _undoStack
                .Take(actionCount)
                .ToList();

        var undoneTransactions =
            new List<PatchTransaction>(
                actionCount);

        try
        {
            foreach (
                var transaction in
                transactionsNewestFirst)
            {
                transaction.Undo(
                    project);

                undoneTransactions.Add(
                    transaction);
            }
        }
        catch
        {
            for (
                var i =
                    undoneTransactions.Count - 1;
                i >= 0;
                i--)
            {
                undoneTransactions[i]
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

        transactionsNewestFirst.Reverse();

        var groupedTransaction =
            PatchTransaction.CreateHistoryJump(
                transactionsNewestFirst);

        _redoStack.Push(
            groupedTransaction);

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

        var transactions =
            _redoStack
                .Take(actionCount)
                .ToList();

        var appliedTransactions =
            new List<PatchTransaction>(
                actionCount);

        try
        {
            foreach (
                var transaction in
                transactions)
            {
                transaction.Apply(
                    project);

                appliedTransactions.Add(
                    transaction);
            }
        }
        catch
        {
            for (
                var i =
                    appliedTransactions.Count - 1;
                i >= 0;
                i--)
            {
                appliedTransactions[i]
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

        var groupedTransaction =
            PatchTransaction.CreateHistoryJump(
                transactions);

        _undoStack.Push(
            groupedTransaction);

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
        if (
            !string.IsNullOrWhiteSpace(
                transaction.Label))
        {
            return transaction.Label;
        }

        return string.Join(
            " + ",
            transaction.Operations
                .Select(
                    operation =>
                        operation.Description));
    }
}