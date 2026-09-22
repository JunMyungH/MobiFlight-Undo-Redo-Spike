public interface IUndoableCommand
{ 
    void Execute();
    void Undo();
    void Redo();
}