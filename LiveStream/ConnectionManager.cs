using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveStream;

public class ConnectionManager : IConnectionManager
{
    private readonly Logger<ConnectionManager> logger = new();
    private readonly List<Connection> connections = new();
    private volatile IReadOnlyList<IConnection> connectionsClone = new List<IConnection>();

    public IReadOnlyConnection CreateConnection()
    {            
        var connection = new Connection(destructorAction: CloseDeadConnections);
            
        lock (connections)
        {
            connections.Add(connection);
            connectionsClone = connections.ToList();
        }
            
        var connectionCount = GetConnections().Count;
        logger.Info($"Connection established, {connectionCount} connections");

        return connection;
    }

    private void CloseDeadConnections()
    {
        lock (connections)
        {
            connections.RemoveAll(c => !c.IsAlive);
            connectionsClone = connections.ToList();
        }
            
        var connectionCount = GetConnections().Count;
        logger.Info($"Connection removed, {connectionCount} connections");
    }

    public IReadOnlyList<IConnection> GetConnections()
    {
        return connectionsClone;
    }
        
    private class Connection(Action destructorAction) : IConnection
    {
        public bool IsAlive { get; private set; } = true;
        public bool HasWrites { get; private set; }
        private readonly AsyncBlockingQueue<IChunk> queue = new();

        public void Write(IChunk chunk)
        {
            HasWrites = true;
            queue.Enqueue(chunk);
        } 

        public async Task<IChunk> ReadBlockingAsync() => await queue.DequeueAsync();
            
        public async Task<IChunk> ReadBlockingOrNullAsync(int millisecondsTimeout) => await queue.DequeueAsync(TimeSpan.FromMilliseconds(millisecondsTimeout));

        public int Size => queue.Count;

        public void Dispose()
        {
            IsAlive = false;
            destructorAction();
            queue.Clear();
        }
    }
}