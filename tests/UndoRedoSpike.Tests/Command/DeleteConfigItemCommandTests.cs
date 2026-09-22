[TestClass]
public class DeleteConfigItemCommandTests
{
    [TestMethod]
    public void Undo_RestoresDeletedItemAtOriginalIndex()
    {
        var first = new ConfigItem { Name = "First" };
        var deleted = new ConfigItem { Name = "Delete Me" };
        var third = new ConfigItem { Name = "Third" };

        var project = new ProjectState
        {
            ConfigItems = new List<ConfigItem>
            {
                first,
                deleted,
                third
            }
        };

        var history = new CommandHistory();

        history.Execute(
            new DeleteConfigItemCommand(project, deleted.Id));

        Assert.AreEqual(2, project.ConfigItems.Count);

        history.Undo();

        Assert.AreEqual(3, project.ConfigItems.Count);
        Assert.AreEqual(deleted.Id, project.ConfigItems[1].Id);
    }

    [TestMethod]
    public void Redo_RemovesRestoredItemAgain()
    {
        var first = new ConfigItem { Name = "First" };
        var deleted = new ConfigItem { Name = "Delete Me" };

        var project = new ProjectState
        {
            ConfigItems = new List<ConfigItem>
            {
                first,
                deleted
            }
        };

        var history = new CommandHistory();

        history.Execute(
            new DeleteConfigItemCommand(project, deleted.Id));

        history.Undo();
        history.Redo();

        Assert.AreEqual(1, project.ConfigItems.Count);
        Assert.AreEqual(first.Id, project.ConfigItems[0].Id);
    }
}