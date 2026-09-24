public class HybridHistoryEntry
{
    public string ActionName { get; }

    public PatchTransaction Transaction { get; }

    public HybridHistoryEntry(
        string actionName,
        PatchTransaction transaction)
    {
        ActionName = actionName;
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
        var operations = string.Join(
            " + ",
            Transaction.Operations
                .Select(operation =>
                    operation.Description));

        return $"{ActionName} [{operations}]";
    }
}