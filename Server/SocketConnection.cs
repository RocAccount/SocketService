using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using 
namespace TestService
{
    public class SocketConnection
    {
         public  object UserData { set; get; }
        
        internal object SystemData { get; set; }
        internal Socket Connection { get; set; }
        internal MemoryStream ReceiveBuffer { get; set; }
        internal byte[] NativeReceiveBuffer { get; set; }
        internal DateTime LastReceiveTime { get; set; }

        private object sendLock = new object();
        private byte[] headerBuffer = new byte[3];

        internal void Send(byte[] data, int startIndex, int length)
        {
            if (startIndex < 0 || startIndex + length > data.Length) { throw new ArgumentOutOfRangeException(); }
         
            lock(this.sendLock)
            {
                // this.headerBuffer[0] = ConnectionData.PACKET_HEAD;
                // fixed (byte* p = &this.headerBuffer[1])
                // {
                //     *(ushort*)p = (ushort)length;
                // }
                this.Connection.Send(this.headerBuffer);
                this.Connection.Send(data, startIndex, length, SocketFlags.None);
            }
        }

        internal SocketConnection(Socket targetSocket)
        {
            this.Connection = targetSocket;
            this.ReceiveBuffer = new MemoryStream(512);
            this.NativeReceiveBuffer = new byte[512];
            this.LastReceiveTime = DateTime.Now;
        }
    }
}