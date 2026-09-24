namespace UndoRedoSpike.Api.Services
{
    public class PatchSpikeService
    {
        public ProjectState Project { get; private set; }
        public PatchHistory History { get; } = new();

        public PatchSpikeService()
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
