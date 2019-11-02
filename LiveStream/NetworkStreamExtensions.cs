using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using LiveStream.Sinks;

namespace LiveStream
{
    public static class StreamExtensions
    {
        public static void WriteWorkChunk(this Stream stream, IWorkChunk chunk)
        {
            stream.WriteInt32(chunk.FileId);
            stream.WriteInt32(chunk.Length);
            stream.WriteGuid(chunk.Sequence);
            stream.WriteChunk(chunk);
        }

        public static void WriteChunk(this Stream stream, IChunk chunk)
        {
            stream.Write(chunk.Buffer, 0, chunk.Length);
        }
        
        public static void WriteInt32(this Stream stream, int number)
        {
            stream.Write(BitConverter.GetBytes(Convert.ToInt32(number)), 0, 4);
        }

        public static void WriteGuid(this Stream stream, Guid guid)
        {
            stream.Write(guid.ToByteArray(), 0, 16);
        }
        
        public static int ReadInt32(this Stream stream)
        {
            var buffer = stream.ReadExactly(4);
            
            return BitConverter.ToInt32(buffer, 0);
        }
        
        public static Guid ReadGuid(this Stream stream)
        {
            var buffer = stream.ReadExactly(16);
            
            return new Guid(buffer);
        }

        public static byte[] ReadExactly(this Stream stream, int size)
        {
            var buffer = new byte[size];
            var receivedLength = 0;
            while (receivedLength < size)
            {
                var length = stream.Read(buffer, receivedLength, size - receivedLength);
                if (length == 0)
                {
                    throw new Exception("Socket was closed, returned 0 bytes");
                }
                
                receivedLength += length;
            }

            return buffer;
        }
    }
}