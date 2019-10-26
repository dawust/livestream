namespace LiveStream
{
    public class SinkFactory
    {
        public ISink CreateSink(CmdArgs cmdArgs)
        {
            if (cmdArgs.IsSinkHttp)
            {
                return new HttpSink(cmdArgs.SinkHttpPort);
            }

            if (cmdArgs.IsSinkConsole)
            {
                return new ConsoleWriterSink();
            }

            if (cmdArgs.IsSinkM2Tcp)
            {
                return new M2TCPSink(cmdArgs.SinkM2TcpPort);
            }

            return new MTCPSink(
                uploadThreads: cmdArgs.SinkMtcpUploaders,
                destination: cmdArgs.SinkMtcpIp, 
                destinationPort: cmdArgs.SinkMtcpPort);
        }
    }
}