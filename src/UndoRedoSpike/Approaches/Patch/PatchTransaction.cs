public class PatchTransaction
{
    private readonly List<IPatchOperation> _operations;

    public IReadOnlyList<IPatchOperation> Operations =>
        _operations;

    public PatchTransaction(
        IEnumerable<IPatchOperation> operations)
    {
        _operations = operations.ToList();
    }

    public void Apply(ProjectState project)
    {
        var appliedOperations =
            new Stack<IPatchOperation>();

        try
        {
            foreach (var operation in _operations)
            {
                operation.Apply(project);

                appliedOperations.Push(operation);
            }
        }
        catch
        {
            while (appliedOperations.Count > 0)
            {
                var operation =
                    appliedOperations.Pop();

                operation.Undo(project);
            }

            throw;
        }
    }

    public void Undo(ProjectState project)
    {
        var undoneOperations =
            new Stack<IPatchOperation>();

        try
        {
            for (var i = _operations.Count - 1; i >= 0; i--)
            {
                var operation =
                    _operations[i];

                operation.Undo(project);

                undoneOperations.Push(operation);
            }
        }
        catch
        {
            while (undoneOperations.Count > 0)
            {
                var operation =
                    undoneOperations.Pop();

                operation.Apply(project);
            }

            throw;
        }
    }
}