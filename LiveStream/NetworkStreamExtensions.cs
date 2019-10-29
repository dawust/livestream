using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LiveStream
{
    public static class NetworkStreamExtensions
    {
        public static void SendWorkChunk(this NetworkStream networkStream, WorkChunk chunk)
        {
            networkStream.SendInt32(chunk.FileId);
            networkStream.SendInt32(chunk.Length);
            networkStream.SendGuid(chunk.Sequence);
            networkStream.Write(chunk.Buffer, 0, chunk.Length);
        }
        
        public static void SendInt32(this NetworkStream networkStream, int number)
        {
            networkStream.Write(BitConverter.GetBytes(Convert.ToInt32(number)), 0, 4);
        }

        public static void SendGuid(this NetworkStream networkStream, Guid guid)
        {
            networkStream.Write(guid.ToByteArray(), 0, 16);
        }
        
        public static int ReadInt32(this NetworkStream networkStream)
        {
            var buffer = networkStream.ReadExactly(4);
            
            return BitConverter.ToInt32(buffer, 0);
        }
        
        public static Guid ReadGuid(this NetworkStream networkStream)
        {
            var buffer = networkStream.ReadExactly( 16);
            return new Guid(buffer);
        }

        public static byte[] ReadExactly(this NetworkStream networkStream, int size)
        {
            var buffer = new byte[size];
            var receivedLength = 0;
            while (receivedLength < size)
            {
                var length = networkStream.Read(buffer, receivedLength, size - receivedLength);
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