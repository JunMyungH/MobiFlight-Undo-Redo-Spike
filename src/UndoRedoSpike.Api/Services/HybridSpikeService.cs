namespace UndoRedoSpike.Api.Services
{
    public class HybridSpikeService
    {
        public ProjectState Project { get; private set; }

        public HybridHistory History { get; } = new();

        public HybridSpikeService()
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
