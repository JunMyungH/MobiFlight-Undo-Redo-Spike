[TestClass]
public class SnapshotHistoryTests
{
    [TestMethod]
    public void Undo_RestoresPreviousToggleState()
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

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems[0].Active = false;
        });

        Assert.IsFalse(project.ConfigItems[0].Active);

        history.Undo(project);

        Assert.IsTrue(project.ConfigItems[0].Active);
    }

    [TestMethod]
    public void Redo_RestoresResultingToggleState()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
                {
                    Name = "Landing Light",
                    Active = true
                }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems[0].Active = false;
        });

        history.Undo(project);
        history.Redo(project);

        Assert.IsFalse(project.ConfigItems[0].Active);
    }

    [TestMethod]
    public void Undo_RestoresDeletedItemAtOriginalIndex()
    {
        var first = new ConfigItem { Name = "First" };
        var deleted = new ConfigItem { Name = "Delete Me" };
        var third = new ConfigItem { Name = "Third" };

        var deletedId = deleted.Id;

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                deleted,
                third
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems.RemoveAll(
                item => item.Id == deletedId);
        });

        Assert.HasCount(2, project.ConfigItems);

        history.Undo(project);

        Assert.HasCount(3, project.ConfigItems);
        Assert.AreEqual(
            deletedId,
            project.ConfigItems[1].Id);
    }

    [TestMethod]
    public void Undo_FollowsLifoOrder()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem
                {
                    Name = "First",
                    Active = false
                },
                new ConfigItem
                {
                    Name = "Second",
                    Active = false
                }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems[0].Active = true;
        });

        history.Execute(project, state =>
        {
            state.ConfigItems[1].Active = true;
        });

        history.Undo(project);

        Assert.IsTrue(project.ConfigItems[0].Active);
        Assert.IsFalse(project.ConfigItems[1].Active);

        history.Undo(project);

        Assert.IsFalse(project.ConfigItems[0].Active);
        Assert.IsFalse(project.ConfigItems[1].Active);
    }

    [TestMethod]
    public void NewActionAfterUndo_ClearsRedoStack()
    {
        var project = new ProjectState
        {
            ConfigItems =
            [
                new ConfigItem { Active = false },
                new ConfigItem { Active = false }
            ]
        };

        var history = new SnapshotHistory();

        history.Execute(project, state =>
        {
            state.ConfigItems[0].Active = true;
        });

        history.Undo(project);

        Assert.IsTrue(history.CanRedo);

        history.Execute(project, state =>
        {
            state.ConfigItems[1].Active = true;
        });

        Assert.IsFalse(history.CanRedo);
        Assert.AreEqual(0, history.RedoCount);
    }
}