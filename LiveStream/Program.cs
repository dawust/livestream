using System;
using System.Threading;
using System.Threading.Tasks;
using Buffer = LiveStream.Distributor.Buffer;

namespace LiveStream
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var logger = new Logger<Program>();

            var cmdArgs = new CmdArgs
            {
                SinkM2TcpPort = 13999,
                UdpPort = 1236,
                M2TcpConnections = 30
            };

            var optionSet = new OptionSet()
            {
                {"http", v => cmdArgs.IsSourceHttp = v != null},
                {"hls", v => cmdArgs.IsSourceHls = v != null},
                {"port=", (int v) => cmdArgs.UdpPort = v},
                {"url=", v => cmdArgs.HttpUri = v},
                {"sinkhttp", v => cmdArgs.IsSinkHttp = v != null},
                {"sinkhttpport=", (int v) => cmdArgs.SinkHttpPort = v},
                {"sinkbuffer=", (int v) => cmdArgs.SinkBufferSize = v},
                {"sinkconsole", v => cmdArgs.IsSinkConsole = v != null},
                {"sinkm2tcp", v => cmdArgs.IsSinkM2Tcp = v != null},
                {"sinkm2tcpport=", (int v) => cmdArgs.SinkM2TcpPort = v},
                {"m2tcp", v => cmdArgs.IsSourceM2Tcp = v != null},
                {"m2tcphost=", v => cmdArgs.M2TcpHost = v},
                {"m2tcpport=", (int v) => cmdArgs.M2TcpPort = v},
                {"m2tcpconn=", (int v) => cmdArgs.M2TcpConnections = v},
                {"m2tcpreset", v => cmdArgs.M2TcpResetPackets = v != null}
            };

            try
            {
                optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                logger.Error(e.Message);
                return;
            }

            if (!cmdArgs.IsSinkConsole)
            {
                DisplayHelp(cmdArgs);
            }

            var mediaQueue = new AsyncBlockingQueue<IChunk>();
            var connectionManager = new ConnectionManager();

            var source = SourceFactory.CreateSource(cmdArgs);
            var sink = SinkFactory.CreateSink(cmdArgs);
            var buffer = new Buffer(cmdArgs.SinkBufferSize);

            _ = Task.Run(() => source.SourceLoopAsync(mediaQueue, CancellationToken.None));
            _ = Task.Run(() => sink.SinkLoopAsync(connectionManager, CancellationToken.None));

            await new Distributor.Distributor().DistributionLoopAsync(mediaQueue, connectionManager, buffer);
        }

        private static void DisplayHelp(CmdArgs cmdArgs)
        {
            Console.WriteLine("Streaming Magic TCP 0.80");
            Console.WriteLine("Sources");
            Console.WriteLine("--udp          | UDP Source (default) : ");
            Console.WriteLine("--port         | UDP Port             : " + cmdArgs.UdpPort);
            Console.WriteLine("");
            Console.WriteLine("--http         | HTTP Source          : " + cmdArgs.IsSourceHttp);
            Console.WriteLine("--hls          | HTTP IsHLS           : " + cmdArgs.IsSourceHls);
            Console.WriteLine("--url          | HTTP URL             : " + cmdArgs.HttpUri);
            Console.WriteLine("");
            Console.WriteLine("--m2tcp        | M2TCP Source         : " + cmdArgs.IsSourceM2Tcp);
            Console.WriteLine("--m2tcphost    | M2TCP Host           : " + cmdArgs.M2TcpHost);
            Console.WriteLine("--m2tcpport    | M2TCP Port           : " + cmdArgs.M2TcpPort);
            Console.WriteLine("--m2tcpconn    | M2TCP Connections    : " + cmdArgs.M2TcpConnections);
            Console.WriteLine("--m2tcpreset   | M2TCP Send reset pkt.: " + cmdArgs.M2TcpResetPackets);
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Sinks");
            Console.WriteLine("--sinkbuffer   | Packet buffer        : " + cmdArgs.SinkBufferSize);
            Console.WriteLine("");
            Console.WriteLine("--sinkhttp     | HTTP Sink            : " + cmdArgs.IsSinkHttp);
            Console.WriteLine("--sinkhttpport | HTTP Port            : " + cmdArgs.SinkHttpPort);
            Console.WriteLine("");
            Console.WriteLine("--sinkconsole  | Output to console    : " + cmdArgs.IsSinkConsole);
            Console.WriteLine("");
            Console.WriteLine("--sinkm2tcp    | M2TCP Sink (default) : " + cmdArgs.IsSinkM2Tcp);
            Console.WriteLine("--sinkm2tcpport| M2TCP Port           : " + cmdArgs.SinkM2TcpPort);
            Console.WriteLine(new string('=', 80));
        }
    }
}