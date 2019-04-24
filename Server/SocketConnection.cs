using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
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

        private object bufferLock = new object();

        internal void Send(byte[] data, int startIndex, int length)
        {
            if (startIndex < 0 || startIndex + length > data.Length) { throw new ArgumentOutOfRangeException(); }
         
            lock(this.sendLock)
            {
                this.Connection.Send(data, startIndex, length, SocketFlags.None);
            }
        }


        internal bool ReadPacket(out byte[] data)
        {
             data = null;
             lock(bufferLock)
             {
                 if(this.ReceiveBuffer!=null &&  this.ReceiveBuffer.Length>0)
                 {
                    ReceiveBuffer.Seek(0, SeekOrigin.Begin);
                    BinaryReader br = new BinaryReader(ReceiveBuffer);
                    int nextLength = br.ReadInt32();
                    if (nextLength <= 0)
                    { 
                        throw new Exception("Packet length invalid"); 
                    }
                    data = br.ReadBytes(nextLength);
                    int lengthLeft = (int)(ReceiveBuffer.Length - ReceiveBuffer.Position + 1);
                    if (lengthLeft > 0)
                    {
                        byte[] left = br.ReadBytes(lengthLeft);
                        ClearBuffer();
                        ReceiveBuffer.Write(left, 0, left.Length);
                    }
                    else
                    {
                        ClearBuffer();
                    }
                    return true;
                 }      
                 else
                 {
                    return false;                     
                 }           
             }
        }

        internal SocketConnection(Socket targetSocket)
        {
            this.Connection = targetSocket;
            this.ReceiveBuffer = new MemoryStream(512);
            this.NativeReceiveBuffer = new byte[512];
            this.LastReceiveTime = DateTime.Now;
        }

        private void ClearBuffer()
        {
            ReceiveBuffer.Seek(0, SeekOrigin.Begin);
            ReceiveBuffer.SetLength(0);
        }
    }
}