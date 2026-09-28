[TestClass]
public class CommandBulkActionTests
{
    [TestMethod]
    public void BulkToggle_ExecuteTogglesAllItemsAndAddsOneHistoryEntry()
    {
        var project = CreateProject();
        var history = new CommandHistory();

        history.Execute(
            new BulkToggleCommand(
                project.ConfigItems));

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
    public void BulkToggle_UndoRestoresPreviousState()
    {
        var project = CreateProject();
        var history = new CommandHistory();

        history.Execute(
            new BulkToggleCommand(
                project.ConfigItems));

        history.Undo();

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
        var history = new CommandHistory();

        history.Execute(
            new BulkToggleCommand(
                project.ConfigItems));

        history.Undo();
        history.Redo();

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
    public void BulkDelete_ExecuteRemovesOnlySelectedItemsAndAddsOneHistoryEntry()
    {
        var project = CreateProject();

        var second =
            project.ConfigItems[1];

        var fourth =
            project.ConfigItems[3];

        var selectedItems =
            project.ConfigItems
                .Where(item => item.Active)
                .ToList();

        var history = new CommandHistory();

        history.Execute(
            new BulkDeleteCommand(
                project,
                selectedItems));

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
    }

    [TestMethod]
    public void BulkDelete_UndoRestoresOriginalOrderAndState()
    {
        var project = CreateProject();

        var originalIds =
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray();

        var selectedItems =
            project.ConfigItems
                .Where(item => item.Active)
                .ToList();

        var history = new CommandHistory();

        history.Execute(
            new BulkDeleteCommand(
                project,
                selectedItems));

        history.Undo();

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
    public void BulkDelete_UndoPreservesOriginalObjectReferences()
    {
        var project = CreateProject();

        var originalItems =
            project.ConfigItems.ToArray();

        var selectedItems =
            project.ConfigItems
                .Where(item => item.Active)
                .ToList();

        var history = new CommandHistory();

        history.Execute(
            new BulkDeleteCommand(
                project,
                selectedItems));

        history.Undo();

        for (var i = 0;
             i < originalItems.Length;
             i++)
        {
            Assert.AreSame(
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

        var selectedItems =
            project.ConfigItems
                .Where(item => item.Active)
                .ToList();

        var history = new CommandHistory();

        history.Execute(
            new BulkDeleteCommand(
                project,
                selectedItems));

        history.Undo();
        history.Redo();

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