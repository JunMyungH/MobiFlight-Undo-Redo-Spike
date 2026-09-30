public class RemoveConfigItemByIndexPatch
    : IPatchOperation
{
    private readonly ConfigItem _item;
    private readonly int _index;

    public string Description =>
        "remove ConfigItem (stored index)";

    public RemoveConfigItemByIndexPatch(
        ConfigItem item,
        int index)
    {
        _item = item;
        _index = index;
    }

    public void Apply(ProjectState project)
    {
        if (_index < 0 ||
            _index >= project.ConfigItems.Count ||
            project.ConfigItems[_index].Id != _item.Id)
        {
            throw new InvalidOperationException(
                $"ConfigItem {_item.Id} was not found at index {_index}.");
        }

        project.ConfigItems.RemoveAt(
            _index);
    }

    public void Undo(ProjectState project)
    {
        project.ConfigItems.Insert(
            _index,
            _item);
    }
}