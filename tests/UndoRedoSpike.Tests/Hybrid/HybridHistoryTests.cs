[TestClass]
public class HybridHistoryTests
{
    [TestMethod]
    public void Execute_AddsOneSemanticHistoryEntry()
    {
        var item = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems = [item]
        };

        var history = new HybridHistory();

        var entry = new HybridHistoryEntry(
            "Toggle Active",
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                    item.Id,
                    true,
                    false)
            ]));

        history.Execute(
            project,
            entry);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);

        Assert.IsTrue(history.CanUndo);
        Assert.IsFalse(history.CanRedo);

        Assert.HasCount(
            1,
            history.UndoEntryDetails);

        Assert.AreEqual(
            "Toggle Active [replace Active]",
            history.UndoEntryDetails[0]);
    }

    [TestMethod]
    public void UndoAndRedo_RestoreState()
    {
        var item = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems = [item]
        };

        var history = new HybridHistory();

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Toggle Active",
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        item.Id,
                        true,
                        false)
                ])));

        Assert.IsFalse(item.Active);

        history.Undo(project);

        Assert.IsTrue(item.Active);
        Assert.AreEqual(0, history.UndoCount);
        Assert.AreEqual(1, history.RedoCount);

        history.Redo(project);

        Assert.IsFalse(item.Active);
        Assert.AreEqual(1, history.UndoCount);
        Assert.AreEqual(0, history.RedoCount);
    }

    [TestMethod]
    public void Undo_FollowsLifoOrder()
    {
        var first = new ConfigItem
        {
            Name = "First",
            Active = false
        };

        var second = new ConfigItem
        {
            Name = "Second",
            Active = false
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second
            ]
        };

        var history = new HybridHistory();

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Toggle First",
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        first.Id,
                        false,
                        true)
                ])));

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Toggle Second",
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        second.Id,
                        false,
                        true)
                ])));

        Assert.IsTrue(first.Active);
        Assert.IsTrue(second.Active);

        history.Undo(project);

        Assert.IsTrue(first.Active);
        Assert.IsFalse(second.Active);

        history.Undo(project);

        Assert.IsFalse(first.Active);
        Assert.IsFalse(second.Active);
    }

    [TestMethod]
    public void NewActionAfterUndo_ClearsRedoStack()
    {
        var first = new ConfigItem
        {
            Active = false
        };

        var second = new ConfigItem
        {
            Active = false
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second
            ]
        };

        var history = new HybridHistory();

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Toggle First",
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        first.Id,
                        false,
                        true)
                ])));

        history.Undo(project);

        Assert.IsTrue(history.CanRedo);

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Toggle Second",
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        second.Id,
                        false,
                        true)
                ])));

        Assert.IsFalse(history.CanRedo);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void CompoundEdit_IsStoredAsSingleSemanticHistoryEntry()
    {
        var first = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var second = new ConfigItem
        {
            Name = "Gear Indicator",
            Active = false
        };

        var third = new ConfigItem
        {
            Name = "Flaps",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second,
                third
            ]
        };

        var history = new HybridHistory();

        var entry = new HybridHistoryEntry(
            "Compound Edit",
            new PatchTransaction(
            [
                new ReplaceNamePatch(
                    first.Id,
                    "Landing Light",
                    "Landing Light (Edited)"),

                new ReplaceActivePatch(
                    first.Id,
                    true,
                    false),

                new MoveConfigItemPatch(
                    first.Id,
                    0,
                    2)
            ]));

        history.Execute(
            project,
            entry);

        Assert.AreEqual(
            "Landing Light (Edited)",
            first.Name);

        Assert.IsFalse(first.Active);

        Assert.AreEqual(
            first.Id,
            project.ConfigItems[2].Id);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.HasCount(
            1,
            history.UndoEntryDetails);

        Assert.AreEqual(
            "Compound Edit [replace Name + replace Active + move ConfigItem]",
            history.UndoEntryDetails[0]);
    }

    [TestMethod]
    public void Undo_CompoundEdit_RestoresCompletePreviousState()
    {
        var first = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var second = new ConfigItem
        {
            Name = "Gear Indicator",
            Active = false
        };

        var third = new ConfigItem
        {
            Name = "Flaps",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second,
                third
            ]
        };

        var history = new HybridHistory();

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Compound Edit",
                new PatchTransaction(
                [
                    new ReplaceNamePatch(
                        first.Id,
                        "Landing Light",
                        "Landing Light (Edited)"),

                    new ReplaceActivePatch(
                        first.Id,
                        true,
                        false),

                    new MoveConfigItemPatch(
                        first.Id,
                        0,
                        2)
                ])));

        history.Undo(project);

        Assert.AreEqual(
            "Landing Light",
            first.Name);

        Assert.IsTrue(first.Active);

        Assert.AreEqual(
            first.Id,
            project.ConfigItems[0].Id);

        Assert.AreEqual(
            second.Id,
            project.ConfigItems[1].Id);

        Assert.AreEqual(
            third.Id,
            project.ConfigItems[2].Id);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            1,
            history.RedoCount);
    }

    [TestMethod]
    public void Redo_CompoundEdit_ReappliesCompleteResultingState()
    {
        var first = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var second = new ConfigItem
        {
            Name = "Gear Indicator",
            Active = false
        };

        var third = new ConfigItem
        {
            Name = "Flaps",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second,
                third
            ]
        };

        var history = new HybridHistory();

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Compound Edit",
                new PatchTransaction(
                [
                    new ReplaceNamePatch(
                        first.Id,
                        "Landing Light",
                        "Landing Light (Edited)"),

                    new ReplaceActivePatch(
                        first.Id,
                        true,
                        false),

                    new MoveConfigItemPatch(
                        first.Id,
                        0,
                        2)
                ])));

        history.Undo(project);
        history.Redo(project);

        Assert.AreEqual(
            "Landing Light (Edited)",
            first.Name);

        Assert.IsFalse(first.Active);

        Assert.AreEqual(
            second.Id,
            project.ConfigItems[0].Id);

        Assert.AreEqual(
            third.Id,
            project.ConfigItems[1].Id);

        Assert.AreEqual(
            first.Id,
            project.ConfigItems[2].Id);
    }

    [TestMethod]
    public void FailedExecute_RollsBackStateAndDoesNotAddHistoryEntry()
    {
        var item = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems = [item]
        };

        var history = new HybridHistory();

        var missingId =
            Guid.NewGuid();

        var entry = new HybridHistoryEntry(
            "Broken Compound Edit",
            new PatchTransaction(
            [
                new ReplaceNamePatch(
                    item.Id,
                    "Landing Light",
                    "Landing Light (Edited)"),

                new ReplaceActivePatch(
                    missingId,
                    true,
                    false)
            ]));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            history.Execute(
                project,
                entry));

        Assert.AreEqual(
            "Landing Light",
            item.Name);

        Assert.IsTrue(item.Active);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void FailedUndo_KeepsEntryInUndoStack()
    {
        var item = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems = [item]
        };

        var history = new HybridHistory();

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Toggle Active",
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        item.Id,
                        true,
                        false)
                ])));

        Assert.AreEqual(
            1,
            history.UndoCount);

        project.ConfigItems.Clear();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            history.Undo(project));

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void FailedRedo_KeepsEntryInRedoStack()
    {
        var item = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems = [item]
        };

        var history = new HybridHistory();

        history.Execute(
            project,
            new HybridHistoryEntry(
                "Toggle Active",
                new PatchTransaction(
                [
                    new ReplaceActivePatch(
                        item.Id,
                        true,
                        false)
                ])));

        history.Undo(project);

        Assert.AreEqual(
            1,
            history.RedoCount);

        project.ConfigItems.Clear();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            history.Redo(project));

        Assert.AreEqual(
            1,
            history.RedoCount);

        Assert.AreEqual(
            0,
            history.UndoCount);
    }
}