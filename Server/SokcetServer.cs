using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;
namespace TestService
{

    public class SocketServer
    {
        public int ClientCount { get { return clients.Count; } }
        public bool Started { get; private set; }
        public bool DisablePing { get; set; }
        public event EventHandler<ClientConnectEventArgs> ClientConnect;
        public event EventHandler<ClientDisconnectEventArgs> ClientDisconnect;
        public event EventHandler<ReceiveEventArgs> Receive;

        private Socket innerSocket;
        private List<SocketConnection> clients;
        private Dictionary<object, SocketConnection> clientMap;

        private object clientListLock = new object(), eventQueueLock = new object();
        private bool buzy = false;

        ~SocketServer()
        {
            Stop();
        }

        public bool Listen(IPAddress ip, int port)
        {
            if (buzy || Started || clients.Count > 0) { return false; }
            try
            {
                buzy = true;
                innerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                innerSocket.Bind(new IPEndPoint(ip, port));
                innerSocket.Listen(100);
                innerSocket.BeginAccept(this.acceptCallBack, innerSocket);
            }
            catch (Exception) { buzy = false; return false; }
            Started = true;
            buzy = false;
            return true;
        }

        public void Stop()
        {
            if (!Started) { return; }
            innerSocket.Close();
            lock (clientListLock)
            {
                foreach (SocketConnection sc in clients)
                {
                    sc.Connection.Close();
                }
            }
            innerSocket = null;
            Started = false;
        }

        public SocketConnection GetClientByIndex(int index)
        {
            lock (clientListLock)
            {
                if (index < 0 || index >= clients.Count) { return null; }
                return clients[index];
            }
        }

        public SocketConnection GetClientByUserData(object userdata)
        {
            if (userdata == null) { return null; }
            lock (clientListLock)
            {
                SocketConnection result = null;
                this.clientMap.TryGetValue(userdata, out result);
                return result;
            }
        }

        public bool SendToClient(SocketConnection client, byte[] data, int startIndex, int length)
        {
            if (!Started || buzy) { return false; }
            try
            {
                client.Send(data, startIndex, length);
                return true;
            }
            catch (Exception) { return false; }
        }

        public int BroadcastToClient(byte[] data, int startIndex, int length)
        {
            if (!Started || buzy) { return -1; }
            int failedCount = 0;
            lock (clientListLock)
            {
                foreach (SocketConnection sc in clients)
                {
                    if (!SendToClient(sc, data, startIndex, length))
                    {
                        failedCount++;
                    }
                }
            }
            return failedCount;
        }

        public void DisconnectClient(SocketConnection client)
        {
            client.Connection.Close();
        }

        private void acceptCallBack(IAsyncResult ar)
        {
            if (innerSocket == null) { return; }
            try
            {
                Socket callBackSocket = ar.AsyncState as Socket;
                if (callBackSocket != null)
                {
                    Socket newClient = callBackSocket.EndAccept(ar);
                    SocketConnection sc = new SocketConnection(innerSocket);
                    ClientConnectEventArgs arg = new ClientConnectEventArgs(sc);
                    if (ClientConnect != null) { ClientConnect(this, arg); }
                    if (arg.DropConnection)
                    {
                        newClient.Close();
                    }
                    else
                    {
                        lock (clientListLock)
                        {
                            clients.Add(sc);
                            if (sc.UserData != null) { clientMap.Add(sc.UserData, sc); }
                        }
                        sc.Connection.BeginReceive(sc.NativeReceiveBuffer, 0, sc.NativeReceiveBuffer.Length, SocketFlags.None, this.receiveCallBack, sc);
                    }
                }
            }
            catch (Exception) { }
            if (innerSocket != null)
            {
                try
                {
                    innerSocket.BeginAccept(this.acceptCallBack, innerSocket);
                }
                catch (Exception) { }
            }
        }

        private void receiveCallBack(IAsyncResult ar)
        {
            SocketConnection sc = ar.AsyncState as SocketConnection;
            if (sc == null) { return; }
            int bytesRead = 0;
            try
            {
                bytesRead = sc.Connection.EndReceive(ar);
                if (bytesRead == 0) { throw new Exception(); }
                //sc.WriteBuffer(sc.NativeReceiveBuffer, bytesRead);
                byte[] data;
                while (sc.ReadPacket(out data))
                {
                    if (data != null)
                    {
                        if (Receive != null)
                        {
                            ReceiveEventArgs arg = new ReceiveEventArgs(sc, data);
                            Receive(this, arg);
                        }
                    }
                }
                sc.Connection.BeginReceive(sc.NativeReceiveBuffer, 0, sc.NativeReceiveBuffer.Length, SocketFlags.None, this.receiveCallBack, sc);
            }
            catch (Exception)
            {
                ClientDisconnectEventArgs arg = new ClientDisconnectEventArgs(sc);
                if (ClientDisconnect != null) { ClientDisconnect(this, arg); }
                lock (clientListLock)
                {
                    clients.Remove(sc);
                    if (sc.UserData != null) { clientMap.Remove(sc.UserData); }
                }
                this.DisconnectClient(sc);
                return;
            }
        }


        public SocketServer()
        {
            clients = new List<SocketConnection>();
            clientMap = new Dictionary<object, SocketConnection>();
        }

    }
}