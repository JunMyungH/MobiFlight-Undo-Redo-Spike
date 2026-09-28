[TestClass]
public class PatchBulkActionTests
{
    [TestMethod]
    public void BulkToggle_ExecuteTogglesAllItemsAndAddsOneHistoryEntry()
    {
        var project = CreateProject();
        var history = new PatchHistory();

        var transaction =
            CreateBulkToggleTransaction(
                project);

        Assert.AreEqual(
            5,
            transaction.Operations.Count);

        history.Execute(
            project,
            transaction);

        Assert.IsFalse(
            project.ConfigItems[0].Active);

        Assert.IsTrue(
            project.ConfigItems[1].Active);

        Assert.IsFalse(
            project.ConfigItems[2].Active);

        Assert.IsTrue(
            project.ConfigItems[3].Active);

        Assert.IsFalse(
            project.ConfigItems[4].Active);

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
            "replace Active + replace Active + replace Active + replace Active + replace Active",
            history.UndoEntryDetails[0]);
    }

    [TestMethod]
    public void BulkToggle_UndoRestoresPreviousState()
    {
        var project = CreateProject();
        var history = new PatchHistory();

        history.Execute(
            project,
            CreateBulkToggleTransaction(
                project));

        history.Undo(project);

        Assert.IsTrue(
            project.ConfigItems[0].Active);

        Assert.IsFalse(
            project.ConfigItems[1].Active);

        Assert.IsTrue(
            project.ConfigItems[2].Active);

        Assert.IsFalse(
            project.ConfigItems[3].Active);

        Assert.IsTrue(
            project.ConfigItems[4].Active);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            1,
            history.RedoCount);
    }

    [TestMethod]
    public void BulkToggle_RedoReappliesResultingState()
    {
        var project = CreateProject();
        var history = new PatchHistory();

        history.Execute(
            project,
            CreateBulkToggleTransaction(
                project));

        history.Undo(project);
        history.Redo(project);

        Assert.IsFalse(
            project.ConfigItems[0].Active);

        Assert.IsTrue(
            project.ConfigItems[1].Active);

        Assert.IsFalse(
            project.ConfigItems[2].Active);

        Assert.IsTrue(
            project.ConfigItems[3].Active);

        Assert.IsFalse(
            project.ConfigItems[4].Active);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    [TestMethod]
    public void BulkDelete_ExecuteRemovesOnlyActiveItemsAndAddsOneHistoryEntry()
    {
        var project = CreateProject();

        var second =
            project.ConfigItems[1];

        var fourth =
            project.ConfigItems[3];

        var history =
            new PatchHistory();

        var transaction =
            CreateBulkDeleteTransaction(
                project);

        Assert.AreEqual(
            3,
            transaction.Operations.Count);

        history.Execute(
            project,
            transaction);

        Assert.HasCount(
            2,
            project.ConfigItems);

        Assert.AreSame(
            second,
            project.ConfigItems[0]);

        Assert.AreSame(
            fourth,
            project.ConfigItems[1]);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);

        Assert.AreEqual(
            "remove ConfigItem + remove ConfigItem + remove ConfigItem",
            history.UndoEntryDetails[0]);
    }

    [TestMethod]
    public void BulkDelete_UndoRestoresOriginalOrderAndReferences()
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
            CreateBulkDeleteTransaction(
                project));

        history.Undo(project);

        Assert.HasCount(
            5,
            project.ConfigItems);

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

        Assert.IsTrue(
            project.ConfigItems[0].Active);

        Assert.IsFalse(
            project.ConfigItems[1].Active);

        Assert.IsTrue(
            project.ConfigItems[2].Active);

        Assert.IsFalse(
            project.ConfigItems[3].Active);

        Assert.IsTrue(
            project.ConfigItems[4].Active);

        Assert.AreEqual(
            0,
            history.UndoCount);

        Assert.AreEqual(
            1,
            history.RedoCount);
    }

    [TestMethod]
    public void BulkDelete_RedoRemovesSameItemsAgain()
    {
        var project = CreateProject();

        var expectedRemainingIds =
            project.ConfigItems
                .Where(item => !item.Active)
                .Select(item => item.Id)
                .ToArray();

        var history =
            new PatchHistory();

        history.Execute(
            project,
            CreateBulkDeleteTransaction(
                project));

        history.Undo(project);
        history.Redo(project);

        Assert.HasCount(
            2,
            project.ConfigItems);

        CollectionAssert.AreEqual(
            expectedRemainingIds,
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray());

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);
    }

    private static PatchTransaction
        CreateBulkToggleTransaction(
            ProjectState project)
    {
        var operations =
            project.ConfigItems
                .Select(item =>
                    (IPatchOperation)
                    new ReplaceActivePatch(
                        item.Id,
                        item.Active,
                        !item.Active))
                .ToList();

        return new PatchTransaction(
            operations);
    }

    private static PatchTransaction
        CreateBulkDeleteTransaction(
            ProjectState project)
    {
        var targets =
            project.ConfigItems
                .Select((item, index) => new
                {
                    Item = item,
                    Index = index
                })
                .Where(entry =>
                    entry.Item.Active)
                .OrderByDescending(entry =>
                    entry.Index)
                .ToList();

        var operations =
            targets
                .Select(entry =>
                    (IPatchOperation)
                    new RemoveConfigItemPatch(
                        entry.Item,
                        entry.Index))
                .ToList();

        return new PatchTransaction(
            operations);
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
                },
                new ConfigItem
                {
                    Name = "Fourth",
                    Active = false
                },
                new ConfigItem
                {
                    Name = "Fifth",
                    Active = true
                }
            ]
        };
    }
}