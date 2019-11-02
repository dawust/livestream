using System;

namespace LiveStream.Sinks
{
    public class ConsoleWriterSink : ISink
    {
        public void SinkLoop(IConnectionManager connectionManager)
        {
            var connection = connectionManager.CreateConnection();
            
            var console = Console.OpenStandardOutput();
            
            while (true)
            {
                var chunk = connection.ReadBlocking();
                console.WriteChunk(chunk);
            }
        }
    }
}