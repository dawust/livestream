using System;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sinks
{
    public class ConsoleWriterSink : ISink
    {
        public async Task SinkLoopAsync(IConnectionManager connectionManager, CancellationToken cancellationToken)
        {
            var connection = connectionManager.CreateConnection();
            
            var console = Console.OpenStandardOutput();
            
            while (true)
            {
                var chunk = await connection.ReadBlockingAsync();
                await console.WriteChunkAsync(chunk);
            }
        }
    }
}