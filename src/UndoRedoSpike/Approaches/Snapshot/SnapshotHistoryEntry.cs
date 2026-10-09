public sealed class SnapshotHistoryEntry
{
    public ProjectState Snapshot { get; }

    public IReadOnlyList<SnapshotHistoryEntry>?
        GroupedEntries
    { get; }

    public int ActionCount { get; }

    public string ActionName { get; }

    public bool IsHistoryJump =>
        GroupedEntries is not null;

    private SnapshotHistoryEntry(
        ProjectState snapshot,
        IReadOnlyList<SnapshotHistoryEntry>? groupedEntries,
        int actionCount,
        string actionName)
    {
        Snapshot =
            snapshot;

        GroupedEntries =
            groupedEntries;

        ActionCount =
            actionCount;

        ActionName =
            actionName;
    }

    public static SnapshotHistoryEntry Single(
        ProjectState snapshot,
        string actionName = "Project Change")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            actionName);

        return new SnapshotHistoryEntry(
            snapshot,
            null,
            1,
            actionName);
    }

    public static SnapshotHistoryEntry HistoryJump(
        ProjectState destination,
        IEnumerable<SnapshotHistoryEntry> originalEntries)
    {
        var entryList =
            originalEntries.ToList();

        var actionCount =
            entryList.Sum(
                entry =>
                    entry.ActionCount);

        return new SnapshotHistoryEntry(
            destination,
            entryList,
            actionCount,
            "History Jump");
    }

    public int StoredConfigItemCopies =>
        Snapshot.ConfigItems.Count +
        (
            GroupedEntries?.Sum(
                entry =>
                    entry.StoredConfigItemCopies)
            ?? 0
        );

    public string Describe()
    {
        if (IsHistoryJump)
        {
            return
                $"History Jump ({ActionCount} actions)";
        }

        return
            ActionName;
    }
}