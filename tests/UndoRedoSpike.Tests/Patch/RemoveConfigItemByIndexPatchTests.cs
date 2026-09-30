[TestClass]
public class RemoveConfigItemByIndexPatchTests
{
    [TestMethod]
    public void Apply_RemovesItemAtStoredIndex()
    {
        var first = new ConfigItem();
        var second = new ConfigItem();
        var third = new ConfigItem();

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second,
                third
            ]
        };

        var patch =
            new RemoveConfigItemByIndexPatch(
                second,
                1);

        patch.Apply(project);

        Assert.HasCount(
            2,
            project.ConfigItems);

        Assert.AreSame(
            first,
            project.ConfigItems[0]);

        Assert.AreSame(
            third,
            project.ConfigItems[1]);
    }

    [TestMethod]
    public void Undo_RestoresItemAtStoredIndex()
    {
        var first = new ConfigItem();
        var second = new ConfigItem();
        var third = new ConfigItem();

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second,
                third
            ]
        };

        var patch =
            new RemoveConfigItemByIndexPatch(
                second,
                1);

        patch.Apply(project);
        patch.Undo(project);

        Assert.AreSame(
            second,
            project.ConfigItems[1]);
    }

    [TestMethod]
    public void Apply_ThrowsWhenStoredIndexNoLongerMatchesItem()
    {
        var first = new ConfigItem();
        var second = new ConfigItem();

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second
            ]
        };

        var patch =
            new RemoveConfigItemByIndexPatch(
                second,
                0);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => patch.Apply(project));
    }
}