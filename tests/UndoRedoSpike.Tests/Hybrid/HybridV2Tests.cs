[TestClass]
public class HybridV2Tests
{
    [TestMethod]
    public void PatchAndSnapshotEntries_WorkInSameHistory()
    {
        var first =
            new ConfigItem
            {
                Name = "First",
                Active = false
            };

        var second =
            new ConfigItem
            {
                Name = "Second",
                Active = true
            };

        var firstId = first.Id;
        var secondId = second.Id;

        var project =
            new ProjectState
            {
                ConfigItems =
                [
                    first,
                    second
                ]
            };

        var history =
            new HybridHistory();

        // Patch-backed action
        history.Execute(
            project,
            new HybridHistoryEntry(
                "Toggle First",
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        firstId,
                        false,
                        true)
                ])));

        // Snapshot-backed action
        history.Execute(
            project,
            new HybridHistoryEntry(
                "Delete Second",
                new HybridSnapshotOperation(
                    state =>
                    {
                        state.ConfigItems.RemoveAll(
                            item =>
                                item.Id == secondId);
                    })));

        Assert.HasCount(
            1,
            project.ConfigItems);

        Assert.AreEqual(
            2,
            history.UndoCount);

        Assert.AreEqual(
            "Delete Second [snapshot]",
            history.UndoEntryDetails[0]);

        Assert.AreEqual(
            "Toggle First [replace Active]",
            history.UndoEntryDetails[1]);

        // Undo Snapshot
        history.Undo(project);

        Assert.HasCount(
            2,
            project.ConfigItems);

        Assert.IsTrue(
            project.ConfigItems
                .Single(item =>
                    item.Id == firstId)
                .Active);

        // Undo Patch
        history.Undo(project);

        Assert.IsFalse(
            project.ConfigItems
                .Single(item =>
                    item.Id == firstId)
                .Active);

        // Redo Patch
        history.Redo(project);

        Assert.IsTrue(
            project.ConfigItems
                .Single(item =>
                    item.Id == firstId)
                .Active);

        // Redo Snapshot
        history.Redo(project);

        Assert.HasCount(
            1,
            project.ConfigItems);

        Assert.AreEqual(
            firstId,
            project.ConfigItems[0].Id);
    }
}