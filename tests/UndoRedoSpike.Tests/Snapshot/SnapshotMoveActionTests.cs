[TestClass]
public class SnapshotMoveActionTests
{
    [TestMethod]
    public void Move_ExecuteMovesFirstItemToLastAndAddsOneHistoryEntry()
    {
        var project = CreateProject();

        var firstId =
            project.ConfigItems[0].Id;

        var secondId =
            project.ConfigItems[1].Id;

        var thirdId =
            project.ConfigItems[2].Id;

        var history =
            new SnapshotHistory();

        ExecuteMove(
            history,
            project);

        Assert.AreEqual(
            secondId,
            project.ConfigItems[0].Id);

        Assert.AreEqual(
            thirdId,
            project.ConfigItems[1].Id);

        Assert.AreEqual(
            firstId,
            project.ConfigItems[2].Id);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);

        Assert.AreEqual(
            3,
            history.StoredConfigItemCopies);
    }

    [TestMethod]
    public void Move_UndoRestoresOriginalOrderButReplacesObjectReferences()
    {
        var project = CreateProject();

        var originalItems =
            project.ConfigItems.ToArray();

        var originalIds =
            originalItems
                .Select(item => item.Id)
                .ToArray();

        var history =
            new SnapshotHistory();

        ExecuteMove(
            history,
            project);

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
            Assert.AreNotSame(
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
    public void Move_RedoReappliesMovedOrderWithSameLogicalIdentity()
    {
        var project = CreateProject();

        var firstId =
            project.ConfigItems[0].Id;

        var secondId =
            project.ConfigItems[1].Id;

        var thirdId =
            project.ConfigItems[2].Id;

        var originalFirst =
            project.ConfigItems[0];

        var history =
            new SnapshotHistory();

        ExecuteMove(
            history,
            project);

        history.Undo(project);
        history.Redo(project);

        Assert.AreEqual(
            secondId,
            project.ConfigItems[0].Id);

        Assert.AreEqual(
            thirdId,
            project.ConfigItems[1].Id);

        Assert.AreEqual(
            firstId,
            project.ConfigItems[2].Id);

        Assert.AreNotSame(
            originalFirst,
            project.ConfigItems[2]);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    private static void ExecuteMove(
        SnapshotHistory history,
        ProjectState project)
    {
        history.Execute(
            project,
            state =>
            {
                var item =
                    state.ConfigItems[0];

                state.ConfigItems.RemoveAt(0);

                state.ConfigItems.Add(item);
            });
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