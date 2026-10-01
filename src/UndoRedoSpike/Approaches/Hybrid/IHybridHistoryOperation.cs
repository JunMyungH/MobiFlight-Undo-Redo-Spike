public interface IHybridHistoryOperation
{
    void Apply(ProjectState project);

    void Undo(ProjectState project);

    string Describe();
}