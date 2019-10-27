using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LiveStream
{
    public class MTCPSource : ISource
    {
        private readonly IDictionary<Tuple<int, int>, IChunk> chunks = new Dictionary<Tuple<int, int>, IChunk>();
        private readonly int port;

        private MediaQueue queue;
        private int lastId = 0;
        private int currentSeed = 0;

        public MTCPSource(int port)
        {
            this.port = port;
        }

        public MediaQueue StartSource()
        {
            queue = new MediaQueue();

            new Thread(ReceiveLoop).Start();

            return queue;
        }

        private void ReceiveLoop()
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            while (true)
            {
                var client = listener.AcceptTcpClient();
                var thread = new Thread(Listen);
                thread.Start(client);
            }
        }

        private void Listen(object o)
        {
            var tcpClient = (TcpClient) o;
            var networkStream = tcpClient.GetStream();
            var lastCheckedId = 0;
            var lastCheck = DateTime.Now;

            try
            {
                while (true)
                {
                    var fileId = networkStream.ReadInt32();
                    var length = networkStream.ReadInt32();
                    var seed = networkStream.ReadInt32();

                    var buffer = new byte[length];
                    networkStream.ReadExactly(buffer, length);

                    var chunk = new Chunk(buffer, length);

                    lock (chunks)
                    {
                        chunks[Tuple.Create(fileId, seed)] = chunk;
                        if (fileId == 0)
                        {
                            Logger.Info<MTCPSource>($"Got new seed {seed}");
                            currentSeed = seed;
                            lastId = 0;
                            foreach (var chunkId in chunks.Select(c => c.Key).Where(kvp => kvp.Item2 != currentSeed).ToList())
                            {
                                chunks.Remove(chunkId);
                            }
                        }

                        while (chunks.ContainsKey(Tuple.Create(lastId, currentSeed)))
                        {
                            var nextChunk = chunks[Tuple.Create(lastId, currentSeed)];
                            foreach (var chunkId in chunks.Select(c => c.Key).Where(kvp => kvp.Item1 < lastId && kvp.Item2 == currentSeed).ToList())
                            {
                                chunks.Remove(chunkId);
                            }

                            queue.Write(nextChunk);
                            lastId++;
                            lastCheckedId = lastId;
                            lastCheck = DateTime.Now;
                        }

                        if (lastCheckedId == lastId && lastCheck.AddSeconds(1) < DateTime.Now)
                        {
                            Logger.Warning<MTCPSource>($"Missing id {lastCheckedId}");
                            lastCheck = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warning<MTCPSource>($"Connection closed: {e.Message}");
            }
        }
    }
}