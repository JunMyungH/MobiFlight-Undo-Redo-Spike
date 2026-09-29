[TestClass]
public class PatchCreateActionTests
{
    [TestMethod]
    public void Add_ExecuteAddsItemAtRequestedIndex()
    {
        var project = CreateProject();

        var copy =
            CreateCopy(
                project.ConfigItems[0]);

        var history =
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new AddConfigItemPatch(
                    copy,
                    1)
            ]));

        Assert.HasCount(
            4,
            project.ConfigItems);

        Assert.AreSame(
            copy,
            project.ConfigItems[1]);

        Assert.AreEqual(
            "First (Copy)",
            project.ConfigItems[1].Name);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.HasCount(
            1,
            history.UndoEntryDetails);

        Assert.AreEqual(
            "add ConfigItem",
            history.UndoEntryDetails[0]);
    }

    [TestMethod]
    public void Add_UndoRemovesCreatedItem()
    {
        var project = CreateProject();

        var originalIds =
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray();

        var copy =
            CreateCopy(
                project.ConfigItems[0]);

        var history =
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new AddConfigItemPatch(
                    copy,
                    1)
            ]));

        history.Undo(project);

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
    public void Add_RedoRestoresSameLogicalAndObjectIdentity()
    {
        var project = CreateProject();

        var copy =
            CreateCopy(
                project.ConfigItems[0]);

        var history =
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new AddConfigItemPatch(
                    copy,
                    1)
            ]));

        var createdId =
            copy.Id;

        history.Undo(project);
        history.Redo(project);

        var restoredItem =
            project.ConfigItems[1];

        Assert.AreEqual(
            createdId,
            restoredItem.Id);

        Assert.AreSame(
            copy,
            restoredItem);
    }

    private static ConfigItem CreateCopy(
        ConfigItem source)
    {
        return new ConfigItem
        {
            Name =
                $"{source.Name} (Copy)",

            Active =
                source.Active
        };
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