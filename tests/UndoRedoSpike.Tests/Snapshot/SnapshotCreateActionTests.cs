[TestClass]
public class SnapshotCreateActionTests
{
    [TestMethod]
    public void Duplicate_ExecuteCreatesCopyAfterSource()
    {
        var project = CreateProject();
        var source = project.ConfigItems[0];

        var history =
            new SnapshotHistory();

        ExecuteDuplicate(
            history,
            project);

        Assert.HasCount(
            4,
            project.ConfigItems);

        var copy =
            project.ConfigItems[1];

        Assert.AreNotEqual(
            source.Id,
            copy.Id);

        Assert.AreEqual(
            "First (Copy)",
            copy.Name);

        Assert.AreEqual(
            source.Active,
            copy.Active);

        Assert.AreEqual(
            1,
            history.UndoCount);
    }

    [TestMethod]
    public void Duplicate_UndoRestoresOriginalState()
    {
        var project = CreateProject();

        var originalIds =
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray();

        var history =
            new SnapshotHistory();

        ExecuteDuplicate(
            history,
            project);

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
    public void Duplicate_RedoRestoresSameLogicalIdentityButNotObjectReference()
    {
        var project = CreateProject();

        var history =
            new SnapshotHistory();

        ExecuteDuplicate(
            history,
            project);

        var createdItem =
            project.ConfigItems[1];

        var createdId =
            createdItem.Id;

        history.Undo(project);
        history.Redo(project);

        var restoredItem =
            project.ConfigItems[1];

        Assert.AreEqual(
            createdId,
            restoredItem.Id);

        Assert.AreNotSame(
            createdItem,
            restoredItem);

        Assert.AreEqual(
            "First (Copy)",
            restoredItem.Name);
    }

    private static void ExecuteDuplicate(
        SnapshotHistory history,
        ProjectState project)
    {
        history.Execute(
            project,
            state =>
            {
                var source =
                    state.ConfigItems[0];

                var copy =
                    new ConfigItem
                    {
                        Name =
                            $"{source.Name} (Copy)",

                        Active =
                            source.Active
                    };

                state.ConfigItems.Insert(
                    1,
                    copy);
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