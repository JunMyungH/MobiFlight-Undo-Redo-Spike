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
        for (var i = _operations.Count - 1; i >= 0; i--)
        {
            _operations[i].Undo(project);
        }
    }
}