public interface IPatchOperation
{
    string Description { get; }

    void Apply(ProjectState project);
    void Undo(ProjectState project);
}