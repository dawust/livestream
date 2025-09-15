namespace LiveStream
{
    public struct CmdArgs
    {
        public int UdpPort { get; set; }
        
        public bool IsSourceHttp { get; set; }

        public string HttpUri { get; set; }
        
        public bool IsSourceQuic { get; set; }
        
        public string SourceQuicHost { get; set; }
        
        public int SourceQuicPort { get; set; }
        
        public bool IsSourceM2Tcp { get; set; }
        
        public bool IsSourceHls { get; set; }
        
        public string M2TcpHost { get; set; }
        
        public int M2TcpPort { get; set; }
        
        public int M2TcpConnections { get; set; }
        
        public bool M2TcpResetPackets { get; set; }

        public bool IsSinkHttp { get; set; }
        
        public bool IsSinkQuic { get; set; }
        
        public int SinkQuicPort { get; set; }

        public int SinkHttpPort { get; set; }

        public int SinkBufferSize { get; set; }
        
        public bool IsSinkConsole { get; set; }

        public bool IsSinkM2Tcp { get; set; }

        public int SinkM2TcpPort { get; set; }
    }
}