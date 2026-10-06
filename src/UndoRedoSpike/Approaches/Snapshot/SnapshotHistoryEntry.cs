public sealed class SnapshotHistoryEntry
{
    public ProjectState Snapshot { get; }

    public IReadOnlyList<SnapshotHistoryEntry>?
        GroupedEntries
    { get; }

    public int ActionCount { get; }

    public bool IsHistoryJump =>
        GroupedEntries is not null;

    private SnapshotHistoryEntry(
        ProjectState snapshot,
        IReadOnlyList<SnapshotHistoryEntry>? groupedEntries,
        int actionCount)
    {
        Snapshot =
            snapshot;

        GroupedEntries =
            groupedEntries;

        ActionCount =
            actionCount;
    }

    public static SnapshotHistoryEntry Single(
        ProjectState snapshot)
    {
        return new SnapshotHistoryEntry(
            snapshot,
            null,
            1);
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
            actionCount);
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
            $"ProjectState snapshot ({Snapshot.ConfigItems.Count} ConfigItems)";
    }
}