using System;
using System.Threading;

namespace LiveStream
{
    public class ConsoleWriterSink : ISink
    {
        private MediaQueue queue = new MediaQueue();
        
        public void StartSink(IConnectionPool connectionPool)
        {
            connectionPool.CreateConnection(queue);
            
            new Thread(WriteLoop).Start();
        }
        
        private void WriteLoop()
        {
            var console = Console.OpenStandardOutput();
            
            while (true)
            {
                var chunk = queue.ReadBlocking();
                console.Write(chunk.Buffer, 0, chunk.Length);
            }
        }
    }
}