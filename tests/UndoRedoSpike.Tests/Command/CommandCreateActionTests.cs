[TestClass]
public class CommandCreateActionTests
{
    [TestMethod]
    public void Duplicate_ExecuteCreatesCopyAfterSource()
    {
        var project = CreateProject();
        var source = project.ConfigItems[0];

        var history = new CommandHistory();

        history.Execute(
            new DuplicateConfigItemCommand(
                project,
                source.Id));

        Assert.HasCount(
            4,
            project.ConfigItems);

        var copy =
            project.ConfigItems[1];

        Assert.AreNotEqual(
            source.Id,
            copy.Id);

        Assert.AreEqual(
            "First (Copy)",
            copy.Name);

        Assert.AreEqual(
            source.Active,
            copy.Active);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void Duplicate_UndoRemovesCreatedItem()
    {
        var project = CreateProject();

        var originalIds =
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray();

        var history = new CommandHistory();

        history.Execute(
            new DuplicateConfigItemCommand(
                project,
                project.ConfigItems[0].Id));

        history.Undo();

        Assert.HasCount(
            3,
            project.ConfigItems);

        CollectionAssert.AreEqual(
            originalIds,
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray());

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            1,
            history.RedoCount);
    }

    [TestMethod]
    public void Duplicate_RedoRestoresSameLogicalAndObjectIdentity()
    {
        var project = CreateProject();
        var history = new CommandHistory();

        history.Execute(
            new DuplicateConfigItemCommand(
                project,
                project.ConfigItems[0].Id));

        var createdItem =
            project.ConfigItems[1];

        var createdId =
            createdItem.Id;

        history.Undo();
        history.Redo();

        var restoredItem =
            project.ConfigItems[1];

        Assert.AreEqual(
            createdId,
            restoredItem.Id);

        Assert.AreSame(
            createdItem,
            restoredItem);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    private static ProjectState CreateProject()
    {
        return new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
                {
                    Name = "First",
                    Active = true
                },
                new ConfigItem
                {
                    Name = "Second",
                    Active = false
                },
                new ConfigItem
                {
                    Name = "Third",
                    Active = true
                }
            ]
        };
    }
}