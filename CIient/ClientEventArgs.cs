using System;
using System.Net;

namespace TestService
{
        public class ClientEventArgs : EventArgs
        {
            public UserClient TestCilent { get; internal set; }
        }

        public class UserClient
        {
            public string Name { get; internal set; }
            public string AppVersion { get; internal set; }
            public Guid ClientId { get; internal set; }
            public bool IsActive { get; internal set; }
            public string IP { get; set; }
            public int Port { get; set; }
        } 
}