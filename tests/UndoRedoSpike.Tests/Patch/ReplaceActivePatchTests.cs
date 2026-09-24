[TestClass]
public class ReplaceActivePatchTests
{
    [TestMethod]
    public void Undo_RestoresPreviousActiveState()
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

        Assert.IsFalse(item.Active);

        history.Undo(project);

        Assert.IsTrue(item.Active);
    }

    [TestMethod]
    public void Redo_RestoresResultingActiveState()
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
        history.Redo(project);

        Assert.IsFalse(item.Active);
    }
}