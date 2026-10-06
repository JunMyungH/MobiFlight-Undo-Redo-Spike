[TestClass]
public class CommandHistoryTests
{

    [TestMethod]
    public void Undo_FollowsLifoOrder()
    {
        var first = new ConfigItem { Active = false };
        var second = new ConfigItem { Active = false };

        var history = new CommandHistory();

        history.Execute(new ToggleActiveCommand(first));
        history.Execute(new ToggleActiveCommand(second));

        Assert.IsTrue(first.Active);
        Assert.IsTrue(second.Active);

        history.Undo();

        Assert.IsTrue(first.Active);
        Assert.IsFalse(second.Active);

        history.Undo();

        Assert.IsFalse(first.Active);
        Assert.IsFalse(second.Active);
    }

    [TestMethod]
    public void NewActionAfterUndo_ClearsRedoStack()
    {
        var first = new ConfigItem { Active = false };
        var second = new ConfigItem { Active = false };

        var history = new CommandHistory();

        history.Execute(new ToggleActiveCommand(first));

        history.Undo();

        Assert.IsTrue(history.CanRedo);

        history.Execute(new ToggleActiveCommand(second));

        Assert.IsFalse(history.CanRedo);
        Assert.AreEqual(0, history.RedoCount);
    }

    [TestMethod]
    public void UndoTo_GroupsMultipleActionsAndRedoRestoresThem()
    {
        var first =
            new ConfigItem { Active = false };

        var second =
            new ConfigItem { Active = false };

        var third =
            new ConfigItem { Active = false };

        var history =
            new CommandHistory();

        history.Execute(
            new ToggleActiveCommand(first));

        history.Execute(
            new ToggleActiveCommand(second));

        history.Execute(
            new ToggleActiveCommand(third));

        Assert.IsTrue(first.Active);
        Assert.IsTrue(second.Active);
        Assert.IsTrue(third.Active);

        var success =
            history.UndoTo(2);

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
            history.RedoEntryTypes[0]);

        history.Redo();

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

        var history =
            new CommandHistory();

        history.Execute(
            new ToggleActiveCommand(
                first));

        history.Execute(
            new ToggleActiveCommand(
                second));

        history.Execute(
            new ToggleActiveCommand(
                third));

        history.UndoTo(2);

        Assert.AreEqual(
            "History Jump (2 actions)",
            history.RedoEntryTypes[0]);

        history.Undo();

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            2,
            history.RedoCount);

        history.RedoTo(2);

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
            history.UndoEntryTypes[0]);
    }
}