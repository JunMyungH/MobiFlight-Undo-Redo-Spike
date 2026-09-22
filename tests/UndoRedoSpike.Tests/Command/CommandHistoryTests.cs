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
}