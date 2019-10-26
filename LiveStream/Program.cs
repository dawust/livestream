using System;
using System.Threading;

namespace LiveStream
{
    class Program
    {
        public static void Main(string[] args)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 50;
            System.Net.ServicePointManager.MaxServicePoints = 50;

            var cmdArgs = new CmdArgs
            {
                UdpPort = 1236, 
                SinkMtcpIp = "127.0.0.1", 
                SinkMtcpUploaders = 16, 
                SinkMtcpPort = 9345,
                M2TcpConnections = 16
            };

            var optionSet = new OptionSet()
            {
                {"mtcp", v => cmdArgs.IsSourceMtcp = v != null},
                {"http", v => cmdArgs.IsSourceHttp = v != null},
                {"port=", (int v) => cmdArgs.UdpPort = v},
                {"url=", v => cmdArgs.HttpUri = v},
                {"conn=", (int v) => cmdArgs.SinkMtcpUploaders = v},
                {"dest=", v => cmdArgs.SinkMtcpIp = v},
                {"dport=", (int v) => cmdArgs.SinkMtcpPort = v},
                {"mtcpport=", (int v) => cmdArgs.MtcpPort = v},
                {"sinkhttp", v => cmdArgs.IsSinkHttp = v != null},
                {"sinkhttpport=", (int v) => cmdArgs.SinkHttpPort = v},
                {"sinkconsole", v => cmdArgs.IsSinkConsole = v != null},
                {"sinkm2tcp", v => cmdArgs.IsSinkM2Tcp = v != null},
                {"sinkm2tcpport=", (int v) => cmdArgs.SinkM2TcpPort = v},
                {"m2tcp", v => cmdArgs.IsSourceM2tcp = v != null},
                {"m2tcphost=", v => cmdArgs.M2TcpHost = v},
                {"m2tcpport=", (int v) => cmdArgs.M2TcpPort = v},
                {"m2tcpconn=", (int v) => cmdArgs.M2TcpConnections = v}
            };

            try
            {
                optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Logger.Error<Program>(e.Message);
                Console.WriteLine(e.Message);
                return;
            }

            if (!cmdArgs.IsSinkConsole)
            {
                DisplayHelp(cmdArgs);
            }
            
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            var connectionPool = new ConnectionPool();
            
            var sink = new SinkFactory().CreateSink(cmdArgs);
            sink.StartSink(connectionPool);
            
            var source = new SourceFactory().CreateSource(cmdArgs);
            var queue = source.StartSource();
            
            var distributor = new Distributor();
            distributor.DistributionLoop(queue, connectionPool);            
        }

        private static void DisplayHelp(CmdArgs cmdArgs)
        {
            Console.WriteLine("Streaming Magic TCP 0.3");
            Console.WriteLine("Sources");
            Console.WriteLine("--udp          | UDP Source (default) : ");
            Console.WriteLine("--port         | UDP Port             : " + cmdArgs.UdpPort);
            Console.WriteLine("");
            Console.WriteLine("--http         | HTTP Source          : " + cmdArgs.IsSourceHttp);
            Console.WriteLine("--url          | HTTP URL             : " + cmdArgs.HttpUri);
            Console.WriteLine("");
            Console.WriteLine("--mtcp         | MTCP Source          : " + cmdArgs.IsSourceMtcp);
            Console.WriteLine("--mtcpport     | MTCP Port            : " + cmdArgs.MtcpPort);
            Console.WriteLine("");
            Console.WriteLine("--m2tcp        | M2TCP Source         : " + cmdArgs.IsSourceM2tcp);
            Console.WriteLine("--m2tcphost    | M2TCP Host           : " + cmdArgs.M2TcpHost);
            Console.WriteLine("--m2tcpport    | M2TCP Port           : " + cmdArgs.M2TcpPort);
            Console.WriteLine("--m2tcpconn    | M2TCP Connections    : " + cmdArgs.M2TcpConnections);
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Sinks");
            Console.WriteLine("--sinkmtcp     | MTCP Sink (default)  : ");
            Console.WriteLine("--conn         | Max Connections      : " + cmdArgs.SinkMtcpUploaders);
            Console.WriteLine("--dest         | Destination          : " + cmdArgs.SinkMtcpIp);
            Console.WriteLine("--dport        | Destination Port     : " + cmdArgs.SinkMtcpPort);
            Console.WriteLine("");
            Console.WriteLine("--sinkhttp     | HTTP Sink            : " + cmdArgs.IsSinkHttp);
            Console.WriteLine("--sinkhttpport | HTTP Port            : " + cmdArgs.SinkHttpPort);
            Console.WriteLine("");
            Console.WriteLine("--sinkconsole  | Output to console    : " + cmdArgs.IsSinkConsole);
            Console.WriteLine("");
            Console.WriteLine("--sinkm2tcp    | M2TCP Sink           : " + cmdArgs.IsSinkM2Tcp);
            Console.WriteLine("--sinkm2tcpport| M2TCP Port           : " + cmdArgs.SinkM2TcpPort);
            Console.WriteLine(new string('=', 80));
        }
    }
}