namespace LiveStream
{
    public interface IM2TCPConnection
    {
        WorkChunk GetNextWorkChunk();

        void FinishWorkChunk(WorkChunk workChunk);
        
        int SourceCount { get; }
        
        int WorkCount { get; }
    }
}