using GameGravity.PolyNetwork.Utility.MessagePostman;
using GameGravity.PolyNetwork.Utility.Serializer;
using GameGravity.PolyNetwork.Utility.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace GameGravity.PolyNetwork.Servers
{
    class PolySelectServer : IPolyServer
    {
        class ClientState
        {
            public ByteArray byteArray;
            public IMessagePostman messagePostman;
            public DateTime lastActiveTime;
        }

        public Action<ClientContext, dynamic> OnReceive { get; set; }
        public Action<string> OnClientConnectError { get; set; }
        public Action<string> OnClientCommunicateError { get; set; }
        public int DisactiveTimeLimit { get; set; } = 8;

        private readonly Socket socket;
        private readonly Dictionary<Socket, ClientState> clientSockets = new Dictionary<Socket, ClientState>();
        private readonly Func<Socket, IMessagePostman> messagePostmanFatory;
        private readonly ISerializer serializer;

        public PolySelectServer(Socket socket, ISerializer serializer, Func<Socket, IMessagePostman> messagePostmanFatory)
        {
            this.socket = socket;
            this.serializer = serializer;
            this.messagePostmanFatory = messagePostmanFatory;
        }

        public void Listen(string address, int port)
        {
            socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
            socket.Listen(0);

            bool testActive = false;
            
            new Thread(StartLoop).Start();

            var timer = new System.Timers.Timer(1000) { AutoReset = true };
            timer.Elapsed += (c, a) => testActive = true;
            timer.Start();

            void StartLoop()
            {
                while (true)
                {
                    List<Socket> selectClient = clientSockets.Keys.ToList();
                    selectClient.Add(socket);
                    Socket.Select(selectClient, null, null, 1000);
                    foreach (var client in selectClient)
                    {
                        if (client == socket) AcceptConnect();
                        else ReceiveMessage(client);
                    }

                    if (testActive)
                    {
                        testActive = false;
                        TestActiveClient();
                    }
                }

                void AcceptConnect()
                {
                    try
                    {
                        Socket client = socket.Accept();

                        ClientState state = new ClientState 
                        {
                            byteArray = new ByteArray(), 
                            lastActiveTime = DateTime.Now,
                            messagePostman = messagePostmanFatory(client)
                        };

                        clientSockets.Add(client, state);
                    }
                    catch (Exception e)
                    {
                        OnClientConnectError?.Invoke(e.Message);
                    }
                }

                void ReceiveMessage(Socket socket)
                {
                    if (!clientSockets.ContainsKey(socket)) return;

                    ClientState state = clientSockets[socket];
                    state.lastActiveTime = DateTime.Now;

                    if (state.byteArray.Remain <= 0)
                    {
                        clientSockets.Remove(socket);
                        socket.Close();
                        OnClientCommunicateError?.Invoke($"缓冲区溢出! 单条协议最长长度为 {ByteArray.DEFAULT_SIZE} byte。已强制关闭连接 {socket.LocalEndPoint} ！");
                        return;
                    }

                    int count = 0;
                    try 
                    {
                        count = socket.Receive(state.byteArray.bytes, state.byteArray.writeIndex, state.byteArray.Remain, 0); 
                    }
                    catch (Exception e) 
                    {
                        OnClientCommunicateError?.Invoke($"接收数据错误：{e}。已强制关闭连接 {socket.LocalEndPoint} !"); 
                    }

                    if (count == 0)
                    {
                        clientSockets.Remove(socket);
                        socket.Close();
                        return;
                    }

                    state.byteArray.writeIndex += count;

                    var msgs = state.messagePostman.Receive(state.byteArray);
                    
                    state.byteArray.CheckAndMoveBytes();

                    if (msgs == null) return;

                    foreach (var msg in msgs)
                    {
                        OnReceive?.Invoke(GetClientContext(), serializer.Deserialize(msg));
                    }

                    ClientContext GetClientContext() => new ClientContext
                    {
                        endPoint = socket.RemoteEndPoint,
                        Send = (data) => state.messagePostman.Send(serializer.Serialize(data)) 
                    };
                }
            }

            void TestActiveClient()
            {
                var now = DateTime.Now;

                foreach (var item in clientSockets.ToArray())
                {
                    if ((now - item.Value.lastActiveTime).TotalSeconds > DisactiveTimeLimit)
                    {
                        item.Key.Close();
                        clientSockets.Remove(item.Key);
                    }
                }
            }
        }
    }
}
