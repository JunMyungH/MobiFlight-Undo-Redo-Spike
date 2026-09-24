public class RemoveConfigItemPatch : IPatchOperation
{
    private readonly ConfigItem _item;
    private readonly int _index;

    public string Description =>
        "remove ConfigItem";

    public RemoveConfigItemPatch(
        ConfigItem item,
        int index)
    {
        _item = item;
        _index = index;
    }

    public void Apply(ProjectState project)
    {
        var index = project.ConfigItems.FindIndex(
            item => item.Id == _item.Id);

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"ConfigItem {_item.Id} was not found.");
        }

        project.ConfigItems.RemoveAt(index);
    }

    public void Undo(ProjectState project)
    {
        project.ConfigItems.Insert(
            _index,
            _item);
    }
}