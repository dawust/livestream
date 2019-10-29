using System;
using System.Threading;

namespace LiveStream
{
    class Program
    {
        public static void Main(string[] args)
        {
            var logger = new Logger<Program>();
        
            System.Net.ServicePointManager.DefaultConnectionLimit = 50;
            System.Net.ServicePointManager.MaxServicePoints = 50;

            var cmdArgs = new CmdArgs
            {
                SinkM2TcpPort = 13999,
                UdpPort = 1236, 
                M2TcpConnections = 30
            };

            var optionSet = new OptionSet()
            {
                {"http", v => cmdArgs.IsSourceHttp = v != null},
                {"port=", (int v) => cmdArgs.UdpPort = v},
                {"url=", v => cmdArgs.HttpUri = v},
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
                logger.Error(e.Message);
                Console.WriteLine(e.Message);
                return;
            }

            if (!cmdArgs.IsSinkConsole)
            {
                DisplayHelp(cmdArgs);
            }
            
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            var mediaQueue = new MediaQueue();
            var connectionManager = new ConnectionManager();
            var source = new SourceFactory().CreateSource(cmdArgs);
            var sink = new SinkFactory().CreateSink(cmdArgs);    
                
            new Thread(() => source.SourceLoop(mediaQueue)).Start();
            new Thread(() => sink.SinkLoop(connectionManager)).Start();
            
            new Distributor().DistributionLoop(mediaQueue, connectionManager);            
        }

        private static void DisplayHelp(CmdArgs cmdArgs)
        {
            Console.WriteLine("Streaming Magic TCP 0.62");
            Console.WriteLine("Sources");
            Console.WriteLine("--udp          | UDP Source (default) : ");
            Console.WriteLine("--port         | UDP Port             : " + cmdArgs.UdpPort);
            Console.WriteLine("");
            Console.WriteLine("--http         | HTTP Source          : " + cmdArgs.IsSourceHttp);
            Console.WriteLine("--url          | HTTP URL             : " + cmdArgs.HttpUri);
            Console.WriteLine("");
            Console.WriteLine("--m2tcp        | M2TCP Source         : " + cmdArgs.IsSourceM2tcp);
            Console.WriteLine("--m2tcphost    | M2TCP Host           : " + cmdArgs.M2TcpHost);
            Console.WriteLine("--m2tcpport    | M2TCP Port           : " + cmdArgs.M2TcpPort);
            Console.WriteLine("--m2tcpconn    | M2TCP Connections    : " + cmdArgs.M2TcpConnections);
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Sinks");
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