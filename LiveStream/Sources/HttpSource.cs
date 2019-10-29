using System;
using System.Net;
using System.Threading;

namespace LiveStream
{
    class HttpSource : ISource
    {
        private const int ReceiveSize = 16384;
        
        private readonly Logger<HttpSource> logger = new Logger<HttpSource>();
        private readonly string httpUri;
        public HttpSource(string httpUri)
        {
            this.httpUri = httpUri;
        }
        
        public void SourceLoop(MediaQueue mediaQueue)
        {
            while (true)
            {
                try
                {
                    logger.Info($"Read from {httpUri}");

                    var request = (HttpWebRequest) WebRequest.Create(httpUri);
                    var responseStream = ((HttpWebResponse) request.GetResponse()).GetResponseStream();
                    while (true)
                    {
                        var buffer = new byte[ReceiveSize * 4];

                        var receivedLength = 0;
                        while (receivedLength < ReceiveSize * 2)
                        {
                            receivedLength += responseStream.Read(buffer, receivedLength, ReceiveSize);
                        }

                        var chunk = new Chunk(buffer, receivedLength);
                        mediaQueue.Write(chunk);
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Lost connection to {httpUri}: {e.Message}");
                    Thread.Sleep(2000);
                }
            }
        }
    }
}