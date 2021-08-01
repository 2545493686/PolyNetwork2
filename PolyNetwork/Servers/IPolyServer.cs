using GameGravity.PolyNetwork.Clients;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GameGravity.PolyNetwork.Servers
{
    public class ClientContext
    {
        public EndPoint endPoint;
        public Action<dynamic> Send;
    }

    public interface IPolyServer
    {
        Action<ClientContext, dynamic> OnReceive { get; set; }
        Action<string> OnClientConnectError { get; set; }
        Action<string> OnClientCommunicateError { get; set; }

        void Listen(string address, int port);
    }
}
