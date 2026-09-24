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
        foreach (var operation in _operations)
        {
            operation.Apply(project);
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