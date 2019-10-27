using System;

namespace LiveStream
{
    public interface IConnection : IDisposable
    {
        MediaQueue MediaQueue { get; }
    }
}