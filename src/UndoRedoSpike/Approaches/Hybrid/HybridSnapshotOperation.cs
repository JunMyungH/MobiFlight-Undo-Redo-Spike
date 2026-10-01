public class HybridSnapshotOperation
    : IHybridHistoryOperation
{
    private readonly Action<ProjectState> _mutation;

    private ProjectState? _snapshot;

    private bool _executed;

    public HybridSnapshotOperation(
        Action<ProjectState> mutation)
    {
        _mutation = mutation;
    }

    public void Apply(ProjectState project)
    {
        if (!_executed)
        {
            var before =
                project.Clone();

            try
            {
                _mutation(project);
            }
            catch
            {
                Restore(
                    project,
                    before);

                throw;
            }

            _snapshot = before;
            _executed = true;

            return;
        }

        Swap(project);
    }

    public void Undo(ProjectState project)
    {
        if (!_executed ||
            _snapshot is null)
        {
            throw new InvalidOperationException(
                "Snapshot operation has not been executed.");
        }

        Swap(project);
    }

    public string Describe()
    {
        return "snapshot";
    }

    private void Swap(
        ProjectState project)
    {
        if (_snapshot is null)
        {
            throw new InvalidOperationException(
                "Snapshot was not stored.");
        }

        var current =
            project.Clone();

        Restore(
            project,
            _snapshot);

        _snapshot = current;
    }

    private static void Restore(
        ProjectState target,
        ProjectState snapshot)
    {
        target.ConfigItems =
            snapshot.ConfigItems
                .Select(item =>
                    item.Clone())
                .ToList();
    }
}