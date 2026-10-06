public sealed class CompositeHistoryCommand
    : IUndoableCommand
{
    private readonly List<IUndoableCommand> _commands;

    public IReadOnlyList<IUndoableCommand> Commands =>
        _commands;

    public string Description { get; }

    public int ActionCount =>
        _commands.Sum(
            command =>
                command is
                    CompositeHistoryCommand composite
                    ? composite.ActionCount
                    : 1);

    public CompositeHistoryCommand(
        IEnumerable<IUndoableCommand> commands,
        string description)
    {
        _commands =
            commands.ToList();

        Description =
            description;
    }

    public static CompositeHistoryCommand CreateHistoryJump(
        IEnumerable<IUndoableCommand> commands)
    {
        var commandList =
            commands.ToList();

        var actionCount =
            commandList.Sum(
                command =>
                    command is
                        CompositeHistoryCommand composite
                        ? composite.ActionCount
                        : 1);

        return new CompositeHistoryCommand(
            commandList,
            $"History Jump ({actionCount} actions)");
    }

    public void Execute()
    {
        Redo();
    }

    public void Undo()
    {
        for (
            var i =
                _commands.Count - 1;
            i >= 0;
            i--)
        {
            _commands[i].Undo();
        }
    }

    public void Redo()
    {
        foreach (
            var command in
            _commands)
        {
            command.Redo();
        }
    }
}