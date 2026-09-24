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
}