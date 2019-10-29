using System;
using System.Threading;

namespace LiveStream
{
    public class ConsoleWriterSink : ISink
    {
        public void SinkLoop(IConnectionManager connectionManager)
        {
            var connection = connectionManager.CreateConnection();
            
            var console = Console.OpenStandardOutput();
            
            while (true)
            {
                var chunk = connection.MediaQueue.ReadBlocking();
                console.Write(chunk.Buffer, 0, chunk.Length);
            }
        }
    }
}