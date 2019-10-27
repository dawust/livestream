using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace LiveStream
{
    public class MTCPSink : ISink
    {
        private readonly int uploadThreads;
        private readonly bool[] threadConnectionStatus;
        private readonly string destination;
        private readonly int destinationPort;

        private ConnectionWrapper connectionWrapper;
        
        public MTCPSink(int uploadThreads, string destination, int destinationPort)
        {
            this.uploadThreads = uploadThreads;
            this.threadConnectionStatus = new bool[uploadThreads];
            this.destination = destination;
            this.destinationPort = destinationPort;
        }

        public void StartSink(IConnectionManager connectionManager)
        {
            var connection = connectionManager.CreateConnection();
            connectionWrapper = new ConnectionWrapper(connection);

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
            NetworkStream networkStream = null;
            while (true)
            {
                try
                {
                    if (tcpClient == null || !tcpClient.Connected)
                    {
                        tcpClient = new TcpClient(destination, destinationPort);
                        tcpClient.SendBufferSize = 64 * 1024;
                        tcpClient.ReceiveBufferSize = 64 * 1024;
                        networkStream = tcpClient.GetStream();
                    }
                    SetThreadStatus(tcpClient.Connected, tid);

                    var workChunk = connectionWrapper.GetNextWorkChunk();
                    
                    var startTime = DateTime.Now;

                    networkStream.SendWorkChunk(workChunk);
                    connectionWrapper.FinishWorkChunk(workChunk);

                    if (workChunk.FileId % 50 == 0)
                    {
                        var processingTime = (DateTime.Now - startTime).Milliseconds;
                        Logger.Info<MTCPSink>($"Sent {workChunk.Length} Bytes; Block {workChunk.FileId}; Receiver Queue {connectionWrapper.SourceCount}; Work Queue {connectionWrapper.WorkCount}; Time {processingTime}");
                    }
                }
                catch (Exception e)
                {
                    SetThreadStatus(false, tid);
                    Logger.Error<MTCPSink>(e.Message);
                    Thread.Sleep(2000);
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
                    Logger.Info<MTCPSink>("Reset uploader");
                    connectionWrapper.Reset();
                }
            }
        }
    }
}