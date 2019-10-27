using System;
using System.Net;
using System.Threading;

namespace LiveStream
{
    class HttpSource : ISource
    {
        private const int ReceiveSize = 16384;
        private readonly string httpUri;
        private readonly MediaQueue queue = new MediaQueue();
        
        public HttpSource(string httpUri)
        {
            this.httpUri = httpUri;
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
                    Logger.Info<HttpSource>($"Read from {httpUri}");

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

                        var writer = new Chunk(buffer, receivedLength);

                        queue.Write(writer);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error<HttpSource>($"Lost connection to {httpUri}: {e.Message}");
                    Thread.Sleep(2000);
                }
            }
        }
    }
}