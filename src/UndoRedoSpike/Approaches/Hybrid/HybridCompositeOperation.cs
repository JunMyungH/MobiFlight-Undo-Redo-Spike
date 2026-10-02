public sealed class HybridCompositeOperation
    : IHybridHistoryOperation
{
    private readonly List<IHybridHistoryOperation> _operations;

    public HybridCompositeOperation(
        IEnumerable<IHybridHistoryOperation> operations)
    {
        _operations =
            operations.ToList();
    }

    public void Apply(
        ProjectState project)
    {
        var appliedOperations =
            new Stack<IHybridHistoryOperation>();

        try
        {
            foreach (
                var operation in
                _operations)
            {
                operation.Apply(
                    project);

                appliedOperations.Push(
                    operation);
            }
        }
        catch
        {
            while (
                appliedOperations.Count >
                0)
            {
                appliedOperations
                    .Pop()
                    .Undo(project);
            }

            throw;
        }
    }

    public void Undo(
        ProjectState project)
    {
        var undoneOperations =
            new Stack<IHybridHistoryOperation>();

        try
        {
            for (
                var i =
                    _operations.Count - 1;
                i >= 0;
                i--)
            {
                var operation =
                    _operations[i];

                operation.Undo(
                    project);

                undoneOperations.Push(
                    operation);
            }
        }
        catch
        {
            while (
                undoneOperations.Count >
                0)
            {
                undoneOperations
                    .Pop()
                    .Apply(project);
            }

            throw;
        }
    }

    public string Describe()
    {
        return
            $"composite ({_operations.Count} history operations)";
    }
}