[TestClass]
public class HybridHistoryEntryTests
{
    [TestMethod]
    public void Describe_IncludesSemanticActionAndPatchOperations()
    {
        var item = new ConfigItem
        {
            Name = "Landing Light",
            Active = true
        };

        var entry = new HybridHistoryEntry(
            "Compound Edit",
            new PatchTransaction(
            [
                new ReplaceNamePatch(
                    item.Id,
                    "Landing Light",
                    "Landing Light (Edited)"),

                new ReplaceActivePatch(
                    item.Id,
                    true,
                    false)
            ]));

        Assert.AreEqual(
            "Compound Edit [replace Name + replace Active]",
            entry.Describe());
    }

    [TestMethod]
    public void Apply_ExecutesUnderlyingPatchTransaction()
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

        var entry = new HybridHistoryEntry(
            "Toggle Active",
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                    item.Id,
                    true,
                    false)
            ]));

        entry.Apply(project);

        Assert.IsFalse(item.Active);
    }

    [TestMethod]
    public void Undo_ReversesUnderlyingPatchTransaction()
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

        var entry = new HybridHistoryEntry(
            "Toggle Active",
            new PatchTransaction(
            [
                new ReplaceActivePatch(
                    item.Id,
                    true,
                    false)
            ]));

        entry.Apply(project);

        Assert.IsFalse(item.Active);

        entry.Undo(project);

        Assert.IsTrue(item.Active);
    }
}