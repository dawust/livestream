using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace LiveStream
{
    public class MTCPSink : ISink
    {
        private int fileId;
        private int seed;

        private readonly int uploadThreads;
        private readonly bool[] threadConnectionStatus;
        private readonly string destination;
        private readonly int destinationPort;
        private readonly MediaQueue queue = new MediaQueue();
        private readonly List<WorkChunk> workItems = new List<WorkChunk>();

        public MTCPSink(int uploadThreads, string destination, int destinationPort)
        {
            this.uploadThreads = uploadThreads;
            this.threadConnectionStatus = new bool[uploadThreads];
            this.destination = destination;
            this.destinationPort = destinationPort;

            Reset();
        }

        public void StartSink(IConnectionPool connectionPool)
        {
            connectionPool.CreateConnection(queue);
            
            for (var tid = 0; tid < uploadThreads; tid++)
            {
                var thread = new Thread(UploadThread);
                thread.Priority = ThreadPriority.Lowest;
                thread.Start(tid);
            }
        }
        
        private void UploadThread(object par)
        {
            var tid = (int) par;
            Logger.Info<MTCPSink>("Thread started " + tid);

            TcpClient tcpClient = null;
            NetworkStream netStream = null;
            while (true)
            {
                try
                {
                    if (tcpClient == null || !tcpClient.Connected)
                    {
                        tcpClient = new TcpClient(destination, destinationPort);
                        tcpClient.SendBufferSize = 64 * 1024;
                        tcpClient.ReceiveBufferSize = 64 * 1024;
                        netStream = tcpClient.GetStream();
                    }
                    SetThreadStatus(tcpClient.Connected, tid);

                    var workChunk = GetNextWorkChunk();
                    
                    var startTime = DateTime.Now;

                    netStream.SendWorkChunk(workChunk);
                    lock (workItems)
                    {
                        workChunk.Processed = true;
                    }

                    if (workChunk.FileId % 50 == 0)
                    {
                        var processingTime = (DateTime.Now - startTime).Milliseconds;
                        Logger.Info<MTCPSink>($"Sent {workChunk.Length} Bytes; Block {workChunk.FileId}; Receiver Queue {queue.Count}; Work Queue {workItems.Count}; Time {processingTime}");
                    }
                }
                catch (Exception e)
                {
                    SetThreadStatus(false, tid);
                    Logger.Error<MTCPSink>(e.Message);
                }
            }
        }

        private void SetThreadStatus(bool status, int tid)
        {
            lock (threadConnectionStatus)
            {
                threadConnectionStatus[tid] = status;

                if (threadConnectionStatus.All(tcs => tcs == false))
                {
                    Reset();
                }
            }
        }

        private void Reset()
        {
            Logger.Info<MTCPSink>("Reset uploader");
            lock (workItems)
            {
                workItems.Clear();
                fileId = 0;
                seed = DateTime.Now.GetHashCode() / 100;
            }
        }

        private WorkChunk GetNextWorkChunk()
        {
            var workChunk = GetWorkChunkToRetryOrNull();
            
            if (workChunk == null)
            {
                var receiverChunk = queue.ReadBlocking();
                lock (workItems)
                {
                    workChunk = new WorkChunk(
                        buffer: receiverChunk.Buffer, 
                        length: receiverChunk.Length, 
                        fileId: fileId,
                        seed: seed,
                        processed: false,
                        retryAt: DateTime.Now.AddSeconds(1));

                    fileId++;
                    
                    workItems.Add(workChunk);
                }
            }

            return workChunk;
        }

        private WorkChunk GetWorkChunkToRetryOrNull()
        {
            lock (workItems)
            {
                workItems.RemoveAll(wi => wi.Processed);
                var workChunk = workItems.FirstOrDefault(wi => wi.RetryAt < DateTime.Now);

                if (workChunk == null)
                {
                    return null;
                }
                
                workChunk.RetryAt = DateTime.Now.AddMilliseconds(500);
                return workChunk;
            }
        }
    }
}