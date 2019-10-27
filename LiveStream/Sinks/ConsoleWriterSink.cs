using System;
using System.Threading;

namespace LiveStream
{
    public class ConsoleWriterSink : ISink
    {
        private IConnection connection;
        
        public void StartSink(IConnectionPool connectionPool)
        {
            connection = connectionPool.CreateConnection();
            
            new Thread(WriteLoop).Start();
        }
        
        private void WriteLoop()
        {
            var console = Console.OpenStandardOutput();
            
            while (true)
            {
                var chunk = connection.MediaQueue.ReadBlocking();
                console.Write(chunk.Buffer, 0, chunk.Length);
            }
        }
    }
}