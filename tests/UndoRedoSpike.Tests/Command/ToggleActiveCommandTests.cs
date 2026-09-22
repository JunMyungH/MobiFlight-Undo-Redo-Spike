[TestClass]
public class ToggleActiveCommandTests
{
    [TestMethod]
    public void Undo_RestoresPreviousActiveState()
    {
        var item = new ConfigItem
        {
            Name = "Test Item",
            Active = true
        };

        var history = new CommandHistory();

        history.Execute(new ToggleActiveCommand(item));

        Assert.IsFalse(item.Active);

        history.Undo();

        Assert.IsTrue(item.Active);
    }

    [TestMethod]
    public void Redo_RestoresResultingActiveState()
    {
        var item = new ConfigItem
        {
            Name = "Test Item",
            Active = true
        };

        var history = new CommandHistory();

        history.Execute(new ToggleActiveCommand(item));

        history.Undo();
        history.Redo();

        Assert.IsFalse(item.Active);
    }
}