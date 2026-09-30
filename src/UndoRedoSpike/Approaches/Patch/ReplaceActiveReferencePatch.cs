public class ReplaceActiveReferencePatch
    : IPatchOperation
{
    private readonly ConfigItem _item;
    private readonly bool _before;
    private readonly bool _after;

    public string Description =>
        "replace Active (direct reference)";

    public ReplaceActiveReferencePatch(
        ConfigItem item,
        bool before,
        bool after)
    {
        _item = item;
        _before = before;
        _after = after;
    }

    public void Apply(ProjectState project)
    {
        _item.Active = _after;
    }

    public void Undo(ProjectState project)
    {
        _item.Active = _before;
    }
}