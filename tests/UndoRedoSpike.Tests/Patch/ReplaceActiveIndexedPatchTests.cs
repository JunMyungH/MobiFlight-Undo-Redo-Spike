[TestClass]
public class ReplaceActiveIndexedPatchTests
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

        var itemIndex =
            project.ConfigItems.ToDictionary(
                item => item.Id);

        var patch =
            new ReplaceActiveIndexedPatch(
                itemIndex,
                item.Id,
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

        var itemIndex =
            project.ConfigItems.ToDictionary(
                item => item.Id);

        var patch =
            new ReplaceActiveIndexedPatch(
                itemIndex,
                item.Id,
                false,
                true);

        patch.Apply(project);
        patch.Undo(project);

        Assert.IsFalse(item.Active);
    }

    [TestMethod]
    public void Apply_ThrowsWhenItemIdDoesNotExist()
    {
        var project = new ProjectState();

        var itemIndex =
            new Dictionary<Guid, ConfigItem>();

        var patch =
            new ReplaceActiveIndexedPatch(
                itemIndex,
                Guid.NewGuid(),
                false,
                true);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => patch.Apply(project));
    }

    [TestMethod]
    public void Apply_UsesCurrentObjectMappedToId()
    {
        var itemId = Guid.NewGuid();

        var originalItem =
            new ConfigItem
            {
                Id = itemId,
                Active = false
            };

        var replacementItem =
            new ConfigItem
            {
                Id = itemId,
                Active = false
            };

        var itemIndex =
            new Dictionary<Guid, ConfigItem>
            {
                [itemId] = originalItem
            };

        var project = new ProjectState
        {
            ConfigItems = [replacementItem]
        };

        var patch =
            new ReplaceActiveIndexedPatch(
                itemIndex,
                itemId,
                false,
                true);

        itemIndex[itemId] =
            replacementItem;

        patch.Apply(project);

        Assert.IsFalse(
            originalItem.Active);

        Assert.IsTrue(
            replacementItem.Active);
    }
}