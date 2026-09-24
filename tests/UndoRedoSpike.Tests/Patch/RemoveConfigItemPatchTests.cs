[TestClass]
public class RemoveConfigItemPatchTests
{
    [TestMethod]
    public void Undo_RestoresDeletedItemAtOriginalIndex()
    {
        var first = new ConfigItem
        {
            Name = "First"
        };

        var deleted = new ConfigItem
        {
            Name = "Delete Me"
        };

        var third = new ConfigItem
        {
            Name = "Third"
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                deleted,
                third
            ]
        };

        var history = new PatchHistory();

        var transaction = new PatchTransaction(
        [
            new RemoveConfigItemPatch(
                deleted,
                1)
        ]);

        history.Execute(
            project,
            transaction);

        Assert.HasCount(
            2,
            project.ConfigItems);

        history.Undo(project);

        Assert.HasCount(
            3,
            project.ConfigItems);

        Assert.AreEqual(
            deleted.Id,
            project.ConfigItems[1].Id);
    }

    [TestMethod]
    public void Undo_RestoresSameObjectReference()
    {
        var deleted = new ConfigItem
        {
            Name = "Delete Me"
        };

        var project = new ProjectState
        {
            ConfigItems = [deleted]
        };

        var history = new PatchHistory();

        var transaction = new PatchTransaction(
        [
            new RemoveConfigItemPatch(
                deleted,
                0)
        ]);

        history.Execute(
            project,
            transaction);

        history.Undo(project);

        Assert.AreSame(
            deleted,
            project.ConfigItems[0]);
    }

    [TestMethod]
    public void Redo_RemovesRestoredItemAgain()
    {
        var first = new ConfigItem
        {
            Name = "First"
        };

        var deleted = new ConfigItem
        {
            Name = "Delete Me"
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                deleted
            ]
        };

        var history = new PatchHistory();

        var transaction = new PatchTransaction(
        [
            new RemoveConfigItemPatch(
                deleted,
                1)
        ]);

        history.Execute(
            project,
            transaction);

        history.Undo(project);
        history.Redo(project);

        Assert.HasCount(
            1,
            project.ConfigItems);

        Assert.AreEqual(
            first.Id,
            project.ConfigItems[0].Id);
    }
}