[TestClass]
public class HybridCreateActionTests
{
    [TestMethod]
    public void Duplicate_ExecuteCreatesItemAndAddsOneSemanticHistoryEntry()
    {
        var project = CreateProject();

        var copy =
            CreateCopy(
                project.ConfigItems[0]);

        var history =
            new HybridHistory();

        history.Execute(
            project,
            CreateDuplicateEntry(
                copy));

        Assert.HasCount(
            4,
            project.ConfigItems);

        Assert.AreSame(
            copy,
            project.ConfigItems[1]);

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.HasCount(
            1,
            history.UndoEntryDetails);

        Assert.AreEqual(
            "Duplicate Config Item [add ConfigItem]",
            history.UndoEntryDetails[0]);
    }

    [TestMethod]
    public void Duplicate_UndoRestoresOriginalState()
    {
        var project = CreateProject();

        var originalIds =
            project.ConfigItems
                .Select(item => item.Id)
                .ToArray();

        var copy =
            CreateCopy(
                project.ConfigItems[0]);

        var history =
            new HybridHistory();

        history.Execute(
            project,
            CreateDuplicateEntry(
                copy));

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
    public void Duplicate_RedoRestoresSameLogicalAndObjectIdentity()
    {
        var project = CreateProject();

        var copy =
            CreateCopy(
                project.ConfigItems[0]);

        var history =
            new HybridHistory();

        history.Execute(
            project,
            CreateDuplicateEntry(
                copy));

        var createdId =
            copy.Id;

        history.Undo(project);
        history.Redo(project);

        var restoredItem =
            project.ConfigItems[1];

        Assert.AreEqual(
            createdId,
            restoredItem.Id);

        Assert.AreSame(
            copy,
            restoredItem);

        Assert.AreEqual(
            "Duplicate Config Item [add ConfigItem]",
            history.UndoEntryDetails[0]);
    }

    private static HybridHistoryEntry
        CreateDuplicateEntry(
            ConfigItem copy)
    {
        return new HybridHistoryEntry(
            "Duplicate Config Item",
            new PatchTransaction(
            [
                new AddConfigItemPatch(
                    copy,
                    1)
            ]));
    }

    private static ConfigItem CreateCopy(
        ConfigItem source)
    {
        return new ConfigItem
        {
            Name =
                $"{source.Name} (Copy)",

            Active =
                source.Active
        };
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