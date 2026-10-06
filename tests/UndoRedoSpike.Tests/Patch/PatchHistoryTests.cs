[TestClass]
public class PatchHistoryTests
{
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

        var history = new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                    first.Id,
                    false,
                    true)
            ]));

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                    second.Id,
                    false,
                    true)
            ]));

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

        var history = new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                    first.Id,
                    false,
                    true)
            ]));

        history.Undo(project);

        Assert.IsTrue(history.CanRedo);

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                    second.Id,
                    false,
                    true)
            ]));

        Assert.IsFalse(history.CanRedo);
        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void Execute_AddsOneHistoryEntry()
    {
        var item = new ConfigItem
        {
            Active = false
        };

        var project = new ProjectState
        {
            ConfigItems = [item]
        };

        var history = new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                    item.Id,
                    false,
                    true)
            ]));

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.IsTrue(history.CanUndo);
        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void FailedRedo_KeepsTransactionInRedoStack()
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

        var history = new PatchHistory();

        var transaction = new PatchTransaction(
        [
            new ReplaceActivePatch(
            item.Id,
            true,
            false)
        ]);

        history.Execute(
            project,
            transaction);

        history.Undo(project);

        Assert.AreEqual(1, history.RedoCount);

        project.ConfigItems.Clear();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            history.Redo(project));

        Assert.AreEqual(
            1,
            history.RedoCount);
    }

    [TestMethod]
    public void FailedUndo_KeepsTransactionInUndoStack()
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

        var history = new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                item.Id,
                true,
                false)
            ]));

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
    public void UndoTo_GroupsMultipleTransactionsAndRedoRestoresThem()
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
                Active = false
            };

        var third =
            new ConfigItem
            {
                Name = "Third",
                Active = false
            };

        var project =
            new ProjectState
            {
                ConfigItems =
                [
                    first,
                second,
                third
                ]
            };

        var history =
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                first.Id,
                false,
                true)
            ]));

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                second.Id,
                false,
                true)
            ]));

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                third.Id,
                false,
                true)
            ]));

        var success =
            history.UndoTo(
                project,
                2);

        Assert.IsTrue(success);

        Assert.IsTrue(first.Active);
        Assert.IsFalse(second.Active);
        Assert.IsFalse(third.Active);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            1,
            history.RedoCount);

        Assert.AreEqual(
            "History Jump (2 actions)",
            history.RedoEntryDetails[0]);

        history.Redo(project);

        Assert.IsTrue(first.Active);
        Assert.IsTrue(second.Active);
        Assert.IsTrue(third.Active);

        Assert.AreEqual(
            3,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void FailedUndoTo_RollsBackStateAndKeepsHistory()
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
                Active = false
            };

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
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                first.Id,
                false,
                true)
            ]));

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                second.Id,
                false,
                true)
            ]));

        project.ConfigItems.Remove(first);

        Assert.ThrowsExactly<InvalidOperationException>(
            () =>
                history.UndoTo(
                    project,
                    2));

        Assert.IsTrue(second.Active);

        Assert.AreEqual(
            2,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void FailedRedoTo_RollsBackStateAndKeepsHistory()
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
                Active = false
            };

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
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                first.Id,
                false,
                true)
            ]));

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                second.Id,
                false,
                true)
            ]));

        history.Undo(project);
        history.Undo(project);

        Assert.AreEqual(
            2,
            history.RedoCount);

        project.ConfigItems.Remove(second);

        Assert.ThrowsExactly<InvalidOperationException>(
            () =>
                history.RedoTo(
                    project,
                    2));

        Assert.IsFalse(first.Active);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            2,
            history.RedoCount);
    }

    [TestMethod]
    public void NestedHistoryJump_UsesSemanticActionCount()
    {
        var first =
            new ConfigItem
            {
                Active = false
            };

        var second =
            new ConfigItem
            {
                Active = false
            };

        var third =
            new ConfigItem
            {
                Active = false
            };

        var project =
            new ProjectState
            {
                ConfigItems =
                [
                    first,
                second,
                third
                ]
            };

        var history =
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                first.Id,
                false,
                true)
            ]));

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                second.Id,
                false,
                true)
            ]));

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                third.Id,
                false,
                true)
            ]));

        history.UndoTo(
            project,
            2);

        Assert.AreEqual(
            "History Jump (2 actions)",
            history.RedoEntryDetails[0]);

        history.Undo(
            project);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            2,
            history.RedoCount);

        history.RedoTo(
            project,
            2);

        Assert.IsTrue(first.Active);
        Assert.IsTrue(second.Active);
        Assert.IsTrue(third.Active);

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
    }

}