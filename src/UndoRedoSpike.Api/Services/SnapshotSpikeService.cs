namespace UndoRedoSpike.Api.Services
{ 
    public class SnapshotSpikeService
    {
        public ProjectState Project { get; private set; }
        public SnapshotHistory History { get; } = new();

        public SnapshotSpikeService()
        {
            Project = CreateProject.Create(3);
        }

        public void Reset(int itemCount)
        {
            Project = CreateProject.Create(itemCount);
            History.Clear();
        }
    }
}