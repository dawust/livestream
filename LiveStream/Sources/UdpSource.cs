using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LiveStream
{
    class UdpSource : ISource
    {
        private readonly Logger<UdpSource> logger = new Logger<UdpSource>();
        private readonly int udpPort;
        public UdpSource(int udpPort)
        {
            this.udpPort = udpPort;
        }
        
        public void SourceLoop(MediaQueue mediaQueue)
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
                        mediaQueue.Write(chunk);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);
                }
            }
        }
    }
}