using LiveStream.Sinks;

namespace LiveStream;

public static class SinkFactory
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

        return new M2TcpSink(cmdArgs.SinkM2TcpPort);
    }
}