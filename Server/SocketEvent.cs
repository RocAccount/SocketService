using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
namespace TestService
{
    public class ClientConnectEventArgs : EventArgs
    {
        public SocketConnection Client { get; set; }
        public bool DropConnection { get; set; }

        public void AssignUserDataToClient(object userdata)
        {
            this.Client.UserData = userdata;
        }

        public ClientConnectEventArgs(SocketConnection client)
            : base()
        {
            this.Client = client;
            this.DropConnection = false;
        }
    }

    public class ClientDisconnectEventArgs : EventArgs
    {
        public SocketConnection Client { get; private set; }

        public ClientDisconnectEventArgs(SocketConnection client)
            : base()
        {
            this.Client = client;
        }
    }

    public class ReceiveEventArgs : EventArgs
    {
        public SocketConnection Client { get; private set; }
        public byte[] Data { get; private set; }

        public ReceiveEventArgs(SocketConnection client, byte[] data)
            : base()
        {
            this.Client = client;
            this.Data = data;
        }
    }

}