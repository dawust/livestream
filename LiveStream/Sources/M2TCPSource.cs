using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using LiveStream.Sinks;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Sources;

public class M2TcpSource(string hostname, int port, int connections, bool sendResetPackets)
    : ISource
{
    private readonly Logger<M2TcpSource> logger = new();
    private readonly IDictionary<(int FileId, Guid Guid), IChunk> chunks = new Dictionary<(int FileId, Guid Guid), IChunk>();
    private readonly Guid connectionId = Guid.NewGuid();
    private AsyncBlockingQueue<IChunk> queue;
    private int currentFileId;
    private Guid currentSequence;

    public async Task SourceLoopAsync(AsyncBlockingQueue<IChunk> mediaQueue, CancellationToken cancellationToken)
    {
        queue = mediaQueue;
        logger.Info($"Start {connections} connections");
        
        var tasks = new List<Task>();
        
        for (var tid = 0; tid < (connections + 1) / 2; tid++)
        {
            tasks.Add(Task.Run(() => ReceiveAsync(shouldRequeue: true, cancellationToken: cancellationToken), cancellationToken));
            tasks.Add(Task.Run(() => ReceiveAsync(shouldRequeue: false, cancellationToken: cancellationToken), cancellationToken));
        }
        
        tasks.Add(Task.Run(() => ControlAsync(cancellationToken), cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task ControlAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            logger.Info("Start control thread");
            try
            {
                var tcpClient = new TcpClient(hostname, port);
                tcpClient.SendBufferSize = 256 * 1024;
                tcpClient.ReceiveBufferSize = 256 * 1024;
                tcpClient.NoDelay = true;
                var networkStream = tcpClient.GetStream();

                await networkStream.WriteInt32Async(M2TcpSink.ControlThreadMagicNumber);
                await networkStream.WriteGuidAsync(connectionId);

                while (true)
                {
                    IReadOnlyList<(int FileId, Guid Guid)> chunkIds;
                    int lastCheckedFileId;
                    Guid lastCheckedSequence;

                    lock (chunks)
                    {
                        chunkIds = chunks.Keys.ToList();
                        lastCheckedFileId = currentFileId;
                        lastCheckedSequence = currentSequence;
                    }

                    foreach (var (fileId, guid) in chunkIds)
                    {
                        await networkStream.WriteInt32Async(M2TcpSink.SingleIdMagicNumber);
                        await networkStream.WriteInt32Async(fileId);
                        await networkStream.WriteGuidAsync(guid);
                    }

                    await networkStream.WriteInt32Async(M2TcpSink.LastIdMagicNumber);
                    await networkStream.WriteInt32Async(lastCheckedFileId);
                    await networkStream.WriteGuidAsync(lastCheckedSequence);
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception e)
            {
                logger.Warning($"Control thread connection closed: {e.Message}");
            }

            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task ReceiveAsync(bool shouldRequeue, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var tcpClient = new TcpClient(hostname, port);
                tcpClient.SendBufferSize = 256 * 1024;
                tcpClient.ReceiveBufferSize = 256 * 1024;
                var networkStream = tcpClient.GetStream();

                var lastCheckedId = 0;
                var lastCheck = DateTime.UtcNow;

                await networkStream.WriteInt32Async(shouldRequeue
                    ? M2TcpSink.ReceiveRequeueThreadMagicNumber
                    : M2TcpSink.ReceiveOnlyThreadMagicNumber);
                await networkStream.WriteGuidAsync(connectionId);

                while (true)
                {
                    var fileId = await networkStream.ReadInt32Async();
                    var length = await networkStream.ReadInt32Async();
                    var sequence = await networkStream.ReadGuidAsync();
                    var buffer = await networkStream.ReadExactlyAsync(length);
            
                    var isStreamReset = fileId == 0 && currentSequence != sequence;
                    var chunk = new WorkChunk(
                        buffer: buffer, 
                        length: length, 
                        fileId: fileId, 
                        sequence: sequence, 
                        isStreamReset: isStreamReset && sendResetPackets);
                            
                    lock (chunks)
                    {
                        chunks[(chunk.FileId, chunk.Sequence)] = chunk;
                        if (isStreamReset)
                        {
                            currentSequence = chunk.Sequence;
                            currentFileId = 0;

                            logger.Info($"Got new sequence {currentSequence}");

                            foreach (var chunkId in chunks.Select(c => c.Key)
                                         .Where(kvp => kvp.Guid != currentSequence).ToList())
                            {
                                chunks.Remove(chunkId);
                            }
                        }

                        while (chunks.ContainsKey((currentFileId, currentSequence)))
                        {
                            var nextChunk = chunks[(currentFileId, currentSequence)];

                            queue.Enqueue(nextChunk);
                            currentFileId++;
                            lastCheckedId = currentFileId;
                            lastCheck = DateTime.UtcNow;
                        }
                                
                        foreach (var chunkId in chunks.Keys
                                     .Where(k => k.FileId < currentFileId && k.Guid == currentSequence).ToList())
                        {
                            chunks.Remove(chunkId);
                        }

                        if (lastCheckedId == currentFileId && lastCheck.AddSeconds(1) < DateTime.UtcNow)
                        {
                            logger.Warning($"Missing id {lastCheckedId}");
                            lastCheck = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Warning($"Connection closed: {e.Message}");
            }

            await Task.Delay(1000, cancellationToken);
        }
    }
}