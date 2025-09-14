using System;
using System.IO;
using LiveStream.Sinks;
using System.Threading.Tasks;

namespace LiveStream;

public static class StreamExtensions
{
    public static async Task WriteWorkChunkAsync(this Stream stream, IWorkChunk chunk)
    {
        await stream.WriteInt32Async(chunk.FileId);
        await stream.WriteInt32Async(chunk.Length);
        await stream.WriteGuidAsync(chunk.Sequence);
        await stream.WriteChunkAsync(chunk);
    }

    public static async Task WriteChunkAsync(this Stream stream, IChunk chunk)
    {
        await stream.WriteAsync(chunk.Buffer, 0, chunk.Length);
    }
        
    public static async Task WriteInt32Async(this Stream stream, int number)
    {
        await stream.WriteAsync(BitConverter.GetBytes(Convert.ToInt32(number)), 0, 4);
    }

    public static async Task WriteGuidAsync(this Stream stream, Guid guid)
    {
        await stream.WriteAsync(guid.ToByteArray(), 0, 16);
    }
        
    public static async Task<int> ReadInt32Async(this Stream stream)
    {
        var buffer = await stream.ReadExactlyAsync(4);
            
        return BitConverter.ToInt32(buffer, 0);
    }
        
    public static async Task<Guid> ReadGuidAsync(this Stream stream)
    {
        var buffer = await stream.ReadExactlyAsync(16);
            
        return new Guid(buffer);
    }

    public static async Task<byte[]> ReadExactlyAsync(this Stream stream, int size)
    {
        var buffer = new byte[size];
        var receivedLength = 0;
        while (receivedLength < size)
        {
            var length = await stream.ReadAsync(buffer, receivedLength, size - receivedLength);
            if (length == 0)
            {
                throw new Exception("Socket was closed, returned 0 bytes");
            }
                
            receivedLength += length;
        }

        return buffer;
    }
}