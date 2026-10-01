public class HybridHistoryEntry
{
    public string ActionName { get; }

    public IHybridHistoryOperation Operation { get; }

    public HybridHistoryEntry(
        string actionName,
        IHybridHistoryOperation operation)
    {
        ActionName = actionName;
        Operation = operation;
    }

    public HybridHistoryEntry(
        string actionName,
        PatchTransaction transaction)
        : this(
            actionName,
            new HybridPatchOperation(
                transaction))
    {
    }

    public void Apply(ProjectState project)
    {
        Operation.Apply(project);
    }

    public void Undo(ProjectState project)
    {
        Operation.Undo(project);
    }

    public string Describe()
    {
        return
            $"{ActionName} [{Operation.Describe()}]";
    }
}