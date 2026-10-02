public sealed class CompositeHistoryCommand
    : IUndoableCommand
{
    private readonly List<IUndoableCommand> _commands;

    public IReadOnlyList<IUndoableCommand> Commands =>
        _commands;

    public string Description { get; }

    public int ActionCount =>
        _commands.Count;

    public CompositeHistoryCommand(
        IEnumerable<IUndoableCommand> commands,
        string description)
    {
        _commands =
            commands.ToList();

        Description =
            description;
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