
namespace TestService
{

    public sealed class Client
    {
        public T GetUserData<T>() { return (T)this.UserData; }
        public bool Disconnected { get; internal set; }

        internal Server Server { get; set; }
        internal object UserData { get; set; }
        internal SocketConnection SocketClient { get; set; }
        internal bool HelloMessageReceived { get; set; }
        internal bool ClientEnabled { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
    }


}