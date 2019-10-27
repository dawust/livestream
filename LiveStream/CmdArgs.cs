namespace LiveStream
{
    public struct CmdArgs
    {
        public int UdpPort { get; set; }
        
        public bool IsSourceHttp { get; set; }

        public string HttpUri { get; set; }
        
        public bool IsSourceMtcp { get; set; }
        
        public int MtcpPort { get; set; }
        
        public bool IsSourceM2tcp { get; set; }
        
        public string M2TcpHost { get; set; }
        
        public int M2TcpPort { get; set; }
        
        public int M2TcpConnections { get; set; }
        
        public string SinkMtcpIp { get; set; }
        
        public int SinkMtcpPort { get; set; }
        
        public int SinkMtcpUploaders { get; set; }

        public bool IsSinkHttp { get; set; }

        public int SinkHttpPort { get; set; }

        public bool IsSinkConsole { get; set; }

        public bool IsSinkM2Tcp { get; set; }

        public int SinkM2TcpPort { get; set; }
    }
}