using System;
using System.Collections.Generic;
using System.Net;
namespace TestService
{
    public interface IHandlerServer
    {
         object CreateNewUserData();
        bool OnNewClient(Client client, byte[] helloMessage);
        void OnClientEnabled(Client client);
        void OnClientDisconnected(Client client);
        bool OnDataReceived(Client client, byte[] data);
        
    }
    public class Server
    {
        public static SocketServer server;
        private IHandlerServer handler;
        private List<Client> clientList = new List<Client>();
        // private Func<Server, Client, byte[], byte[]>[] packetHandlers = new Func<Server, Client, byte[], byte[]>[65535];

        public static event EventHandler<ClientEventArgs> DeviceHeartBeat;

        public bool Started { get { return server.Started; } }

        public int ClientCount { get { return server.ClientCount; } }

		public static bool Run(IPAddress listenIP, int listenPort)
        {
            if (server != null && !server.Started) { server = null; }
            if (server == null)
            {
                server = new HakoServer(BloomServerHandler.GetInstance());
                RegisterPacketHandlers();
                bool result = server.Start(listenIP, listenPort);
                if (result)
                {
                    logger.Info("Run", "Server started.");
                }
                else
                {
                    server = null;
                }
                return result;
            }
            return true;
        }

        public void Stop()
        {
            server.Stop();
        }

        public Client SearchClient(object userdata)
        {
            ObjectExtensions.CheckNullArgs(new { userdata });
            SocketConnection sc = server.GetClientByUserData(userdata);
            if (sc != null)
            {
                Client c = sc.SystemData as Client;
                return c.ClientEnabled ? c : null;
            }
            return null;
        }


        private void server_ClientConnect(object sender, ClientConnectEventArgs e)
        {
            object userdata = this.handler.CreateNewUserData();
            if (userdata == null) { e.DropConnection = true; return; }
            e.AssignUserDataToClient(userdata);
            Client c = new Client() { Server = this, UserData = userdata, SocketClient = e.Client };
			if (e.Client != null && e.Client.Connection != null)
			{
                var ep = e.Client.Connection.RemoteEndPoint.ToString().Split(':');
                c.IP = ep[0];
                c.Port = Convert.ToInt32(ep[1]);
			}
            e.Client.SystemData = c;
        }

        public void KickClient(Client c)
        {
            this.server.DisconnectClient(c.SocketClient);
        }

        private void server_Receive(object sender, ReceiveEventArgs e)
        {
            e.Client.SystemData.CastAndDo<Client>((c) =>
            {
                if (c.ClientEnabled)
                {
                    if (this.handler.OnDataReceived(c, e.Data))
                    {
                        HakoPacket packet = new HakoPacket(e.Data);
                        HakoPacket response = this.DispatchServerPacket(c, packet);
                        if (response != null)
                        {
                            this.SendToClient(c, response);
                        }
                    }
                }
                else if (!c.HelloMessageReceived)
                {
                    c.HelloMessageReceived = true;
                    HakoPacket packet = new HakoPacket(e.Data);
                    if (packet.Id == HakoPacket.RESERVED_ID_VERSION_CHECK_V0)
                    {
                        HakoPacket helloMessage = packet.ReadPacket();
                        try
                        {
                            if (this.handler.OnNewClient(c, helloMessage))
                            {
                                lock (this.clientList)
                                {
                                    this.clientList.Add(c);
                                    c.ClientEnabled = true;
                                }
                                this.handler.OnClientEnabled(c);
                            }
                            else
                            {
                                this.KickClient(c);
                            }
                        }
                        catch (Exception) { this.KickClient(c); }
                    }
                    else
                    {
                        this.KickClient(c);
                    }
                }
            });
        }


        private void server_ClientDisconnect(object sender, ClientDisconnectEventArgs e)
        {
            e.Client.SystemData.CastAndDo<Client>((c) =>
            {
                c.Disconnected = true;
                lock(WaitingRequest.WaitingRequestTable)
                {
                    List<WaitingRequest> toRemove = new List<WaitingRequest>();
                    foreach (KeyValuePair<Guid, WaitingRequest> kvp in WaitingRequest.WaitingRequestTable)
                    {
                        WaitingRequest wr = kvp.Value;
                        if (wr.Client == c)
                        {
                            wr.ResponseAction(wr.Request, false, null);
                            toRemove.Add(wr);
                        }
                    }
                    foreach (WaitingRequest wr in toRemove)
                    {
                        WaitingRequest.WaitingRequestTable.Remove(wr.Request.Key);
                    }
                }
                if (c.ClientEnabled)
                {
                    lock (this.clientList)
                    {
                        this.clientList.Remove(c);
                    }
                    this.handler.OnClientDisconnected(c);
                }
            });
        }


        public Client GetClientByIndex(int index)
        {
            lock (this.clientList)
            {
                if (index >= 0 && index < this.clientList.Count)
                {
                    return this.clientList[index];
                }
                else
                {
                    return null;
                }
            }
        }

        public Server(IHandlerServer handler)
        {
            ObjectExtensions.CheckNullArgs(new { handler });
            this.handler = handler;
            this.server.ClientConnect += server_ClientConnect;
            this.server.ClientDisconnect += server_ClientDisconnect;
            this.server.Receive += server_Receive;
        }


        private static void RegisterPacketHandlers()
        {
            server.RegisterPacketHandler(7, new Func<HakoServer, HakoServer.Client, HakoPacket, HakoPacket>((s, c, p) =>
            {
                UserClient d = c.GetUserData<UserClient>();
                // d.CurrentDocument = p.ReadString();
                d.ClientId = p.ReadPrimitive<int>();
                if (DeviceHeartBeat != null)
                {
                    DeviceEventArgs args = new DeviceEventArgs() { Device = d };
                    DeviceHeartBeat(d, args);
                }
                return null;
            }));
        }


    }
}