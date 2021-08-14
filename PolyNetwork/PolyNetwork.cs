using Cemit.Awaiter;
using GameGravity.PolyNetwork.Clients;
using GameGravity.PolyNetwork.Servers;
using GameGravity.PolyNetwork.Utility.MessagePostman;
using GameGravity.PolyNetwork.Utility.Serializer;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace GameGravity.PolyNetwork
{
    public enum ConnectResult
    {
        error,
        success,
    }

    public class PolyNetwork
    {
        enum RunningMode
        {
            none,
            client,
            server,
        }

        public Action<Action> AsyncInvokeDelegate { get; set; } = (action) => new Thread(() => action?.Invoke()).Start();
        public Func<IPolyServer> PolyServerFactory { get; set; } = GetSelectServerFactory();
        public Func<IPolyClient> PolyClientFactory { get; set; } = GetAsynClient();
        public string Name { get; set; } = "PolyNetwork";
        public Action<string> Logger { get; set; }


        private RunningMode runningMode = RunningMode.none;

        private List<Func<dynamic, dynamic>> listeners = new List<Func<dynamic, dynamic>>();

        private IPolyClient client;
        private ushort clientMessageIndex = 0;
        private AsyncAction<dynamic>[] clientAsyncActions = new AsyncAction<dynamic>[65535];

        private IPolyServer server;

        private static Socket GetTcpSocket() => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private static Func<IPolyClient> GetAsynClient() => () =>
        {
            var socket = GetTcpSocket();
            return new PolyAsynClient(socket, new JsonSerializer(), new TcpMessagePostman(socket));
        };

        private static Func<IPolyServer> GetSelectServerFactory() => () =>
        {
            return new PolySelectServer(GetTcpSocket(), new JsonSerializer(), (socket) => new TcpMessagePostman(socket));
        };

        private void Log(string text)
        {
            if (Logger == null) return;
            Logger($"{Name} [ {text} ] [ {DateTime.Now.ToLongTimeString()} ]");
        }

        public PolyNetwork Listen(string address, int port)
        {
            if (runningMode != RunningMode.none)
            {
                throw new Exception("Listen() 或 Connect() 中有且仅有一个方法可被调用，请勿重复调用。");
            }

            runningMode = RunningMode.server;

            server = PolyServerFactory();

            Log($"启动监听：{address}:{port}");
            server.Listen(address, port);
            Log($"成功监听：{address}:{port}");

            server.OnReceive += (client, msg) =>
            {
                if (msg.i == -1) return; //心跳包

                Log($"收到消息：{msg.i} ({client.endPoint})");
                foreach (var listener in listeners)
                {
                    dynamic report = listener?.Invoke(msg.v);
                    if (report == null) continue;
                    dynamic temp = new { i = msg.i, v = report };
                    client.Send(temp); 
                }
            };

            return this;
        }

        public AsyncAction<ConnectResult> Connect(string address, int port)
        {
            if (runningMode != RunningMode.none)
            {
                throw new Exception("Listen() 或 Connect() 中有且仅有一个方法可被调用，请勿重复调用。");
            }

            runningMode = RunningMode.client;

            AsyncAction<ConnectResult> asyncAction = new AsyncAction<ConnectResult>();

            client = PolyClientFactory();

            client.OnReceive += (msg) =>
            {
                if (msg.i == -1) return; //心跳包
                
                clientAsyncActions[msg.i]?.ReportResult(msg.v, AsyncInvokeDelegate); 
            };

            client.OnConnectComplete += (error) => 
            {
                if (string.IsNullOrEmpty(error))
                {
                    Log($"连接服务器成功: {address}:{port}");
                    
                    asyncAction.ReportResult(ConnectResult.success, AsyncInvokeDelegate);

                    var timer = new System.Timers.Timer(1000) { AutoReset = true };
                    timer.Elapsed += (c, a) => client.Send(new { i = -1 });
                    timer.Start();
                }
                else
                {
                    Log($"连接错误: {error}");
                    asyncAction.ReportResult(ConnectResult.error, AsyncInvokeDelegate);
                }
            };

            Log($"连接服务器：{address}:{port}");
            client.Connect(address, port);

            return asyncAction;
        }

        public PolyNetwork AddListener(Func<dynamic, dynamic> listener)
        {
            listeners.Add(listener);
            return this;
        }

        public AsyncAction<dynamic> Send(dynamic msg)
        {
            AsyncAction<dynamic> action = new AsyncAction<dynamic>();

            if (runningMode != RunningMode.client)
            {
                throw new Exception("仅有客户端可以主动发送消息，调用 Connect() 进入客户端模式。");
            }

            if (client.IsConnect != true)
            {
                Log("发送消息出错：服务器已经断开，请重新连接");
                action.ReportResult(null, AsyncInvokeDelegate);
                return action;
            }
            
            dynamic temp = new { i = clientMessageIndex, v = msg };

            Log($"发送消息：{clientMessageIndex}");

            clientAsyncActions[clientMessageIndex] = action;
            clientMessageIndex++;
            clientMessageIndex %= ushort.MaxValue;
            
            client.Send(temp);
            
            return action;
        }
    }
}
