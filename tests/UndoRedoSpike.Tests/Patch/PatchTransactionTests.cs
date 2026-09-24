[TestClass]
public class PatchTransactionTests
{
    [TestMethod]
    public void CompoundTransaction_AppliesAllOperations()
    {
        var first = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var second = new ConfigItem
        {
            Name = "Gear Indicator",
            Active = false
        };

        var third = new ConfigItem
        {
            Name = "Flaps",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second,
                third
            ]
        };

        var history = new PatchHistory();

        var transaction = new PatchTransaction(
        [
            new ReplaceNamePatch(
                first.Id,
                "Landing Light",
                "Landing Light (Edited)"),

            new ReplaceActivePatch(
                first.Id,
                true,
                false),

            new MoveConfigItemPatch(
                first.Id,
                0,
                2)
        ]);

        history.Execute(
            project,
            transaction);

        Assert.AreEqual(
            "Landing Light (Edited)",
            first.Name);

        Assert.IsFalse(first.Active);

        Assert.AreEqual(
            first.Id,
            project.ConfigItems[2].Id);

        Assert.AreEqual(
            1,
            history.UndoCount);
    }

    [TestMethod]
    public void Undo_CompoundTransaction_RestoresCompletePreviousState()
    {
        var first = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var second = new ConfigItem
        {
            Name = "Gear Indicator",
            Active = false
        };

        var third = new ConfigItem
        {
            Name = "Flaps",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second,
                third
            ]
        };

        var history = new PatchHistory();

        var transaction = new PatchTransaction(
        [
            new ReplaceNamePatch(
                first.Id,
                "Landing Light",
                "Landing Light (Edited)"),

            new ReplaceActivePatch(
                first.Id,
                true,
                false),

            new MoveConfigItemPatch(
                first.Id,
                0,
                2)
        ]);

        history.Execute(
            project,
            transaction);

        history.Undo(project);

        Assert.AreEqual(
            "Landing Light",
            first.Name);

        Assert.IsTrue(first.Active);

        Assert.AreEqual(
            first.Id,
            project.ConfigItems[0].Id);

        Assert.AreEqual(
            second.Id,
            project.ConfigItems[1].Id);

        Assert.AreEqual(
            third.Id,
            project.ConfigItems[2].Id);
    }

    [TestMethod]
    public void Redo_CompoundTransaction_ReappliesCompleteResultingState()
    {
        var first = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var second = new ConfigItem
        {
            Name = "Gear Indicator",
            Active = false
        };

        var third = new ConfigItem
        {
            Name = "Flaps",
            Active = true
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                first,
                second,
                third
            ]
        };

        var history = new PatchHistory();

        var transaction = new PatchTransaction(
        [
            new ReplaceNamePatch(
                first.Id,
                "Landing Light",
                "Landing Light (Edited)"),

            new ReplaceActivePatch(
                first.Id,
                true,
                false),

            new MoveConfigItemPatch(
                first.Id,
                0,
                2)
        ]);

        history.Execute(
            project,
            transaction);

        history.Undo(project);
        history.Redo(project);

        Assert.AreEqual(
            "Landing Light (Edited)",
            first.Name);

        Assert.IsFalse(first.Active);

        Assert.AreEqual(
            second.Id,
            project.ConfigItems[0].Id);

        Assert.AreEqual(
            third.Id,
            project.ConfigItems[1].Id);

        Assert.AreEqual(
            first.Id,
            project.ConfigItems[2].Id);
    }

    [TestMethod]
    public void CompoundTransaction_IsStoredAsSingleHistoryEntry()
    {
        var item = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var second = new ConfigItem
        {
            Name = "Second"
        };

        var project = new ProjectState
        {
            ConfigItems =
            [
                item,
                second
            ]
        };

        var history = new PatchHistory();

        history.Execute(
            project,
            new PatchTransaction(
            [
                new ReplaceNamePatch(
                    item.Id,
                    item.Name,
                    "Landing Light (Edited)"),

                new ReplaceActivePatch(
                    item.Id,
                    item.Active,
                    false),

                new MoveConfigItemPatch(
                    item.Id,
                    0,
                    1)
            ]));

        Assert.AreEqual(
            1,
            history.UndoCount);

        Assert.HasCount(
            1,
            history.UndoEntryDetails);

        Assert.AreEqual(
            "replace Name + replace Active + move ConfigItem",
            history.UndoEntryDetails[0]);
    }

    [TestMethod]
    public void FailedTransaction_RollsBackWhenLaterPatchCannotFindTarget()
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

        var missingId = Guid.NewGuid();

        var transaction = new PatchTransaction(
        [
            new ReplaceNamePatch(
            item.Id,
            "Landing Light",
            "Landing Light (Edited)"),

        new ReplaceActivePatch(
            missingId,
            true,
            false)
        ]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            history.Execute(
                project,
                transaction));

        Assert.AreEqual(
            "Landing Light",
            item.Name);

        Assert.IsTrue(item.Active);

        Assert.AreEqual(
            0,
            history.UndoCount);
    }
}