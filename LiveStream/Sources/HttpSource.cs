using System.Net;
using System.Threading;

namespace LiveStream
{
    class HttpSource : ISource
    {
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
            Logger.Info<HttpSource>($"Read from {httpUri}");
            
            var request = (HttpWebRequest) WebRequest.Create(httpUri);
            var responseStream = ((HttpWebResponse) request.GetResponse()).GetResponseStream();
            while (true)
            {
                var buffer = new byte[16384];
                var count = responseStream.Read(buffer, 0, buffer.Length);

                var writer = new Chunk(buffer, count);

                queue.Write(writer);
            }
        }
    }
}