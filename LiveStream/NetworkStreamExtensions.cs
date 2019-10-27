using System;
using System.Net.Sockets;

namespace LiveStream
{
    public static class NetworkStreamExtensions
    {
        public static void SendWorkChunk(this NetworkStream networkStream, WorkChunk chunk)
        {
            networkStream.Write(BitConverter.GetBytes(Convert.ToUInt32(chunk.FileId)), 0, 4);
            networkStream.Write(BitConverter.GetBytes(Convert.ToUInt32(chunk.Length)), 0, 4);
            networkStream.Write(BitConverter.GetBytes(Convert.ToInt32(chunk.Seed)), 0, 4);
            networkStream.Write(chunk.Buffer, 0, chunk.Length);
        }
        
        public static void SendInt32(this NetworkStream networkStream, int number)
        {
            networkStream.Write(BitConverter.GetBytes(Convert.ToInt32(number)), 0, 4);
        }
        
        public static int ReadInt32(this NetworkStream networkStream)
        {
            var buffer = new byte[4];
            var length = networkStream.ReadExactly(buffer, 4);
            
            if (length != 4)
            {
                throw new Exception("Did not receive 4 bytes");
            }
            
            return BitConverter.ToInt32(buffer, 0);
        }

        public static int ReadExactly(this NetworkStream networkStream, byte[] buffer, int size)
        {
            var receivedLength = 0;
            while (receivedLength < size)
            {
                var length = networkStream.Read(buffer, receivedLength, size - receivedLength);
                if (length == 0)
                {
                    return receivedLength;
                }
                
                receivedLength += length;
            }

            return receivedLength;
        }
    }
}