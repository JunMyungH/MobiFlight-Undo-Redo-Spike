[TestClass]
public class SnapshotBulkActionTests
{
    [TestMethod]
    public void BulkToggle_ExecuteTogglesAllItemsAndAddsOneHistoryEntry()
    {
        var project = CreateProject();
        var history = new SnapshotHistory();

        ExecuteBulkToggle(
            history,
            project);

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

        // Proxy for stored snapshot size,
        // not a byte-level memory measurement.
        Assert.AreEqual(
            5,
            history.StoredConfigItemCopies);
    }

    [TestMethod]
    public void BulkToggle_UndoRestoresPreviousState()
    {
        var project = CreateProject();
        var history = new SnapshotHistory();

        ExecuteBulkToggle(
            history,
            project);

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
        var history = new SnapshotHistory();

        ExecuteBulkToggle(
            history,
            project);

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
            new SnapshotHistory();

        ExecuteBulkDelete(
            history,
            project);

        Assert.HasCount(
            2,
            project.ConfigItems);

        Assert.AreEqual(
            second.Id,
            project.ConfigItems[0].Id);

        Assert.AreEqual(
            fourth.Id,
            project.ConfigItems[1].Id);

        Assert.IsFalse(
            project.ConfigItems[0].Active);

        Assert.IsFalse(
            project.ConfigItems[1].Active);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.AreEqual(
            0,
            history.RedoCount);

        Assert.AreEqual(
            5,
            history.StoredConfigItemCopies);
    }

    [TestMethod]
    public void BulkDelete_UndoRestoresOriginalOrderAndState()
    {
        var project = CreateProject();

        var originalIds =
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray();

        var history =
            new SnapshotHistory();

        ExecuteBulkDelete(
            history,
            project);

        history.Undo(project);

        Assert.HasCount(
            5,
            project.ConfigItems);

        CollectionAssert.AreEqual(
            originalIds,
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray());

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
    public void BulkDelete_UndoPreservesLogicalIdentityButNotReferenceIdentity()
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

        ExecuteBulkDelete(
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
            new SnapshotHistory();

        ExecuteBulkDelete(
            history,
            project);

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

    private static void ExecuteBulkToggle(
        SnapshotHistory history,
        ProjectState project)
    {
        history.Execute(
            project,
            state =>
            {
                foreach (var item in state.ConfigItems)
                {
                    item.Active =
                        !item.Active;
                }
            });
    }

    private static void ExecuteBulkDelete(
        SnapshotHistory history,
        ProjectState project)
    {
        var selectedIds =
            project.ConfigItems
                .Where(item => item.Active)
                .Select(item => item.Id)
                .ToHashSet();

        history.Execute(
            project,
            state =>
            {
                state.ConfigItems.RemoveAll(
                    item =>
                        selectedIds.Contains(
                            item.Id));
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