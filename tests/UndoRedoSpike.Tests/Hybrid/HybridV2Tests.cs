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

    [TestMethod]
    public void HistoryJump_WithPatchAndSnapshot_UndoToAndRedoRestoreState()
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

        var firstId =
            first.Id;

        var secondId =
            second.Id;

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

        // 1. Patch-backed action
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

        // 2. Snapshot-backed action
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

        // 3. Patch-backed action
        history.Execute(
            project,
            new HybridHistoryEntry(
                "Rename First",
                new PatchTransaction(
                [
                    new ReplaceNamePatch(
                    firstId,
                    "First",
                    "First Edited")
                ])));

        Assert.HasCount(
            1,
            project.ConfigItems);

        var currentFirst =
            project.ConfigItems.Single(
                item =>
                    item.Id == firstId);

        Assert.IsTrue(
            currentFirst.Active);

        Assert.AreEqual(
            "First Edited",
            currentFirst.Name);

        // Jump back through all three actions.
        var success =
            history.UndoTo(
                project,
                3);

        Assert.IsTrue(success);

        Assert.HasCount(
            2,
            project.ConfigItems);

        var restoredFirst =
            project.ConfigItems.Single(
                item =>
                    item.Id == firstId);

        var restoredSecond =
            project.ConfigItems.Single(
                item =>
                    item.Id == secondId);

        Assert.IsFalse(
            restoredFirst.Active);

        Assert.AreEqual(
            "First",
            restoredFirst.Name);

        Assert.IsTrue(
            restoredSecond.Active);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            1,
            history.RedoCount);

        Assert.AreEqual(
            3,
            history.NextRedoActionCount);

        Assert.AreEqual(
            "History Jump (3 actions)",
            history.RedoEntryDetails[0]);

        // One Redo replays the complete jump.
        history.Redo(
            project);

        Assert.HasCount(
            1,
            project.ConfigItems);

        var redoneFirst =
            project.ConfigItems.Single(
                item =>
                    item.Id == firstId);

        Assert.IsTrue(
            redoneFirst.Active);

        Assert.AreEqual(
            "First Edited",
            redoneFirst.Name);

        Assert.AreEqual(
            3,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void HistoryJump_WithPatchAndSnapshot_RedoToAndUndoRestoreState()
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

        var firstId =
            first.Id;

        var secondId =
            second.Id;

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

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Rename First",
                new PatchTransaction(
                [
                    new ReplaceNamePatch(
                    firstId,
                    "First",
                    "First Edited")
                ])));

        // Move all actions to the Redo stack individually.
        history.Undo(project);
        history.Undo(project);
        history.Undo(project);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            3,
            history.RedoCount);

        Assert.HasCount(
            2,
            project.ConfigItems);

        var initialFirst =
            project.ConfigItems.Single(
                item =>
                    item.Id == firstId);

        Assert.IsFalse(
            initialFirst.Active);

        Assert.AreEqual(
            "First",
            initialFirst.Name);

        // Jump forward through all three actions.
        var success =
            history.RedoTo(
                project,
                3);

        Assert.IsTrue(success);

        Assert.HasCount(
            1,
            project.ConfigItems);

        var redoneFirst =
            project.ConfigItems.Single(
                item =>
                    item.Id == firstId);

        Assert.IsTrue(
            redoneFirst.Active);

        Assert.AreEqual(
            "First Edited",
            redoneFirst.Name);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);

        Assert.AreEqual(
            3,
            history.NextUndoActionCount);

        Assert.AreEqual(
            "History Jump (3 actions)",
            history.UndoEntryDetails[0]);

        // The complete RedoTo jump itself can be undone once.
        history.Undo(
            project);

        Assert.HasCount(
            2,
            project.ConfigItems);

        var restoredFirst =
            project.ConfigItems.Single(
                item =>
                    item.Id == firstId);

        var restoredSecond =
            project.ConfigItems.Single(
                item =>
                    item.Id == secondId);

        Assert.IsFalse(
            restoredFirst.Active);

        Assert.AreEqual(
            "First",
            restoredFirst.Name);

        Assert.IsTrue(
            restoredSecond.Active);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            3,
            history.RedoCount);
    }
}