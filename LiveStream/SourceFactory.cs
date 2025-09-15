using LiveStream.Sources;

namespace LiveStream;

public static class SourceFactory
{
    public static ISource CreateSource(CmdArgs cmdArgs)
    {
        if (cmdArgs.IsSourceHttp)
        {
            return new HttpSource(cmdArgs.HttpUri);
        }
        
        if (cmdArgs.IsSourceHls)
        {
            return new HlsSource(cmdArgs.HttpUri);
        }
        
        if (cmdArgs.IsSourceQuic)
        {
            return new QuicSource(cmdArgs.SourceQuicHost, cmdArgs.SourceQuicPort);
        }

        if (cmdArgs.IsSourceM2Tcp)
        {
            return new M2TcpSource(
                hostname: cmdArgs.M2TcpHost, 
                port: cmdArgs.M2TcpPort, 
                connections: cmdArgs.M2TcpConnections, 
                sendResetPackets: cmdArgs.M2TcpResetPackets);
        }
            
        return new UdpSource(cmdArgs.UdpPort);
    }
}