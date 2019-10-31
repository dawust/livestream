namespace LiveStream
{
    public class SourceFactory
    {
        public static ISource CreateSource(CmdArgs cmdArgs)
        {
            if (cmdArgs.IsSourceHttp)
            {
                return new HttpSource(cmdArgs.HttpUri);
            }

            if (cmdArgs.IsSourceM2tcp)
            {
                return new M2TCPSource(cmdArgs.M2TcpHost, cmdArgs.M2TcpPort, cmdArgs.M2TcpConnections);
            }
            
            return new UdpSource(cmdArgs.UdpPort);
        }
    }
}