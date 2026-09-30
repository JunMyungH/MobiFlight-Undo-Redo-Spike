public class ReplaceActiveIndexedPatch
    : IPatchOperation
{
    private readonly IReadOnlyDictionary<Guid, ConfigItem> _itemIndex;
    private readonly Guid _itemId;
    private readonly bool _before;
    private readonly bool _after;

    public string Description =>
        "replace Active (indexed lookup)";

    public ReplaceActiveIndexedPatch(
        IReadOnlyDictionary<Guid, ConfigItem> itemIndex,
        Guid itemId,
        bool before,
        bool after)
    {
        _itemIndex = itemIndex;
        _itemId = itemId;
        _before = before;
        _after = after;
    }

    public void Apply(ProjectState project)
    {
        FindItem().Active = _after;
    }

    public void Undo(ProjectState project)
    {
        FindItem().Active = _before;
    }

    private ConfigItem FindItem()
    {
        if (!_itemIndex.TryGetValue(
                _itemId,
                out var item))
        {
            throw new InvalidOperationException(
                $"ConfigItem {_itemId} was not found.");
        }

        return item;
    }
}