namespace LiveStream
{
    public class SinkFactory
    {
        public static ISink CreateSink(CmdArgs cmdArgs)
        {
            if (cmdArgs.IsSinkHttp)
            {
                return new HttpSink(cmdArgs.SinkHttpPort, cmdArgs.SinkBufferSize);
            }

            if (cmdArgs.IsSinkConsole)
            {
                return new ConsoleWriterSink();
            }

            return new M2TCPSink(cmdArgs.SinkM2TcpPort);
        }
    }
}