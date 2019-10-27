using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LiveStream
{
    class UdpSource : ISource
    {
        private readonly int udpPort;
        private readonly MediaQueue queue = new MediaQueue();
        public UdpSource(int udpPort)
        {
            this.udpPort = udpPort;
        }
        
        public MediaQueue StartSource()
        {
            new Thread(ReceiveLoop).Start();
            
            return queue;
        }

        private void ReceiveLoop()
        {
            while (true)
            {
                try
                {
                    var endPoint = new IPEndPoint(IPAddress.Any, udpPort);
                    var udpClient = new UdpClient(endPoint) {Client = {ReceiveBufferSize = 1024 * 1024}};

                    while (true)
                    {
                        var buffer = udpClient.Receive(ref endPoint);

                        var chunk = new Chunk(buffer, buffer.Length);
                        queue.Write(chunk);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error<UdpSource>(e.Message);
                }
            }
        }
    }
}