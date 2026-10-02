public class CommandHistory
{
    private readonly Stack<IUndoableCommand> _undoStack =
        new();

    private readonly Stack<IUndoableCommand> _redoStack =
        new();

    public bool CanUndo =>
        _undoStack.Count > 0;

    public bool CanRedo =>
        _redoStack.Count > 0;

    public int UndoCount =>
        _undoStack.Count;

    public int RedoCount =>
        _redoStack.Count;

    public IReadOnlyList<string> UndoEntryTypes =>
        _undoStack
            .Select(Describe)
            .ToList();

    public IReadOnlyList<string> RedoEntryTypes =>
        _redoStack
            .Select(Describe)
            .ToList();

    public void Execute(
        IUndoableCommand command)
    {
        command.Execute();

        _undoStack.Push(command);
        _redoStack.Clear();
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        var command =
            _undoStack.Pop();

        command.Undo();

        _redoStack.Push(command);

        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0)
        {
            return false;
        }

        var command =
            _redoStack.Pop();

        command.Redo();

        _undoStack.Push(command);

        return true;
    }

    public bool UndoTo(
        int actionCount)
    {
        if (
            actionCount < 1 ||
            actionCount >
                _undoStack.Count)
        {
            return false;
        }

        var commandsNewestFirst =
            new List<IUndoableCommand>(
                actionCount);

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            var command =
                _undoStack.Pop();

            command.Undo();

            commandsNewestFirst.Add(
                command);
        }

        // Redo has to execute the actions
        // in their original chronological order.
        commandsNewestFirst.Reverse();

        var groupedCommand =
            new CompositeHistoryCommand(
                commandsNewestFirst,
                $"History Jump ({actionCount} actions)");

        _redoStack.Push(
            groupedCommand);

        return true;
    }

    public bool RedoTo(
        int actionCount)
    {
        if (
            actionCount < 1 ||
            actionCount >
                _redoStack.Count)
        {
            return false;
        }

        var commands =
            new List<IUndoableCommand>(
                actionCount);

        for (
            var i = 0;
            i < actionCount;
            i++)
        {
            var command =
                _redoStack.Pop();

            command.Redo();

            commands.Add(command);
        }

        var groupedCommand =
            new CompositeHistoryCommand(
                commands,
                $"History Jump ({actionCount} actions)");

        _undoStack.Push(
            groupedCommand);

        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private static string Describe(
        IUndoableCommand command)
    {
        if (
            command is
                CompositeHistoryCommand composite)
        {
            return composite.Description;
        }

        return command
            .GetType()
            .Name;
    }
}