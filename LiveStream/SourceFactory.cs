using System;

namespace LiveStream
{
    public class SourceFactory
    {
        public ISource CreateSource(CmdArgs cmdArgs)
        {
            if (cmdArgs.IsSourceMtcp)
            {
                return new MTCPSource(cmdArgs.MtcpPort);
            }
            
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