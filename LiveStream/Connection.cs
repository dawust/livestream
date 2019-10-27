using System;

namespace LiveStream
{
    public class Connection : IConnection
    {
        private readonly Action destructorAction;
        public bool IsAlive { get; private set; }
        public MediaQueue MediaQueue { get; }
        
        public Connection(MediaQueue mediaQueue, Action destructorAction)
        {
            MediaQueue = mediaQueue;
            IsAlive = true;
            this.destructorAction = destructorAction;
        }

        public void Dispose()
        {
            IsAlive = false;
            destructorAction();
        }
    }
}