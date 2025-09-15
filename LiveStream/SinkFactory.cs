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

        if (cmdArgs.IsSinkQuic)
        {
            return new QuicSink(cmdArgs.SinkQuicPort);
        }
        
        if (cmdArgs.IsSinkConsole)
        {
            return new ConsoleWriterSink();
        }

        return new M2TcpSink(cmdArgs.SinkM2TcpPort);
    }
}