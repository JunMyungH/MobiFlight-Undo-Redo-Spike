public class HybridPatchOperation
    : IHybridHistoryOperation
{
    public PatchTransaction Transaction { get; }

    public HybridPatchOperation(
        PatchTransaction transaction)
    {
        Transaction = transaction;
    }

    public void Apply(ProjectState project)
    {
        Transaction.Apply(project);
    }

    public void Undo(ProjectState project)
    {
        Transaction.Undo(project);
    }

    public string Describe()
    {
        return string.Join(
            " + ",
            Transaction.Operations
                .Select(operation =>
                    operation.Description));
    }
}