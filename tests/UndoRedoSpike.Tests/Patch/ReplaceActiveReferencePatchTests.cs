[TestClass]
public class ReplaceActiveReferencePatchTests
{
    [TestMethod]
    public void Apply_ChangesActiveState()
    {
        var item = new ConfigItem
        {
            Active = false
        };

        var project = new ProjectState
        {
            ConfigItems = [item]
        };

        var patch =
            new ReplaceActiveReferencePatch(
                item,
                false,
                true);

        patch.Apply(project);

        Assert.IsTrue(item.Active);
    }

    [TestMethod]
    public void Undo_RestoresPreviousActiveState()
    {
        var item = new ConfigItem
        {
            Active = false
        };

        var project = new ProjectState
        {
            ConfigItems = [item]
        };

        var patch =
            new ReplaceActiveReferencePatch(
                item,
                false,
                true);

        patch.Apply(project);
        patch.Undo(project);

        Assert.IsFalse(item.Active);
    }
}