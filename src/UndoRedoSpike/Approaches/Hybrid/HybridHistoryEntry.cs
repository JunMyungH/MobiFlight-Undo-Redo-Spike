public class HybridHistoryEntry
{
    public string ActionName { get; }

    public IHybridHistoryOperation Operation { get; }

    public IReadOnlyList<HybridHistoryEntry>?
        GroupedEntries
    { get; }

    public int ActionCount { get; }

    public bool IsHistoryJump =>
        GroupedEntries is not null;

    public HybridHistoryEntry(
        string actionName,
        IHybridHistoryOperation operation)
    {
        ActionName =
            actionName;

        Operation =
            operation;

        ActionCount = 1;
    }

    private HybridHistoryEntry(
        string actionName,
        IHybridHistoryOperation operation,
        IEnumerable<HybridHistoryEntry> groupedEntries)
    {
        ActionName =
            actionName;

        Operation =
            operation;

        GroupedEntries =
            groupedEntries.ToList();

        ActionCount =
            GroupedEntries.Sum(
                entry =>
                    entry.ActionCount);
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

    public static HybridHistoryEntry CreateHistoryJump(
        IEnumerable<HybridHistoryEntry> entries,
        string actionName)
    {
        var entryList =
            entries.ToList();

        return new HybridHistoryEntry(
            actionName,
            new HybridCompositeOperation(
                entryList.Select(
                    entry =>
                        entry.Operation)),
            entryList);
    }

    public void Apply(
        ProjectState project)
    {
        Operation.Apply(
            project);
    }

    public void Undo(
        ProjectState project)
    {
        Operation.Undo(
            project);
    }

    public string Describe()
    {
        if (IsHistoryJump)
        {
            return ActionName;
        }

        return
            $"{ActionName} [{Operation.Describe()}]";
    }
}