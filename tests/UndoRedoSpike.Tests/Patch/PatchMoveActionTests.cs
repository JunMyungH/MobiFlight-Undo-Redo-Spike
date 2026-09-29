[TestClass]
public class PatchMoveActionTests
{
    [TestMethod]
    public void Move_ExecuteMovesFirstItemToLastAndAddsOneHistoryEntry()
    {
        var project = CreateProject();

        var first = project.ConfigItems[0];
        var second = project.ConfigItems[1];
        var third = project.ConfigItems[2];

        var history =
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new MoveConfigItemPatch(
                    first.Id,
                    0,
                    2)
            ]));

        Assert.AreSame(
            second,
            project.ConfigItems[0]);

        Assert.AreSame(
            third,
            project.ConfigItems[1]);

        Assert.AreSame(
            first,
            project.ConfigItems[2]);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);

        Assert.HasCount(
            1,
            history.UndoEntryDetails);

        Assert.AreEqual(
            "move ConfigItem",
            history.UndoEntryDetails[0]);
    }

    [TestMethod]
    public void Move_UndoRestoresOriginalOrderAndReferences()
    {
        var project = CreateProject();

        var originalItems =
            project.ConfigItems.ToArray();

        var originalIds =
            originalItems
                .Select(item => item.Id)
                .ToArray();

        var history =
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new MoveConfigItemPatch(
                    originalItems[0].Id,
                    0,
                    2)
            ]));

        history.Undo(project);

        CollectionAssert.AreEqual(
            originalIds,
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray());

        for (var i = 0;
             i < originalItems.Length;
             i++)
        {
            Assert.AreSame(
                originalItems[i],
                project.ConfigItems[i]);
        }

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            1,
            history.RedoCount);
    }

    [TestMethod]
    public void Move_RedoReappliesMovedOrderAndReferences()
    {
        var project = CreateProject();

        var first = project.ConfigItems[0];
        var second = project.ConfigItems[1];
        var third = project.ConfigItems[2];

        var history =
            new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new MoveConfigItemPatch(
                    first.Id,
                    0,
                    2)
            ]));

        history.Undo(project);
        history.Redo(project);

        Assert.AreSame(
            second,
            project.ConfigItems[0]);

        Assert.AreSame(
            third,
            project.ConfigItems[1]);

        Assert.AreSame(
            first,
            project.ConfigItems[2]);

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