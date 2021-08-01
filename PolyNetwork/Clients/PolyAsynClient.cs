using GameGravity.PolyNetwork.Utility.MessagePostman;
using GameGravity.PolyNetwork.Utility.Serializer;
using GameGravity.PolyNetwork.Utility.Structure;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameGravity.PolyNetwork.Clients
{
    class PolyAsynClient : IPolyClient
    {
        public Action<dynamic> OnReceive { get; set; }
        public ClientConnectCallback OnConnectComplete { get; set; }

        public bool IsConnect => socket.Connected;

        private readonly Socket socket;
        private readonly IMessagePostman messagePostman;
        private readonly ISerializer serializer;

        private ByteArray byteArray;

        public PolyAsynClient(Socket socket, ISerializer serializer, IMessagePostman messagePostman)
        {
            this.socket = socket;
            this.messagePostman = messagePostman;
            this.serializer = serializer;
        }

        /// <summary>
        /// 建立连接，成功建立后会自动开始接收消息
        /// </summary>
        public void Connect(string address, int port)
        {
            socket.BeginConnect(address, port, OnConnectComplete, null);
            
            void OnConnectComplete(IAsyncResult asyncResult)
            {
                try
                {
                    socket.EndConnect(asyncResult);
                    this.OnConnectComplete?.Invoke(string.Empty);

                    byteArray = new ByteArray();
                    socket.BeginReceive(byteArray.bytes, byteArray.writeIndex, byteArray.Remain, 0, ReceiveCallback, null);
                }
                catch (Exception e)
                {
                    this.OnConnectComplete?.Invoke(e.Message);
                }
                
            }
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            int count = socket.EndReceive(asyncResult);
            if (count == 0) socket.Close();

            byteArray.writeIndex += count;

            var msgs = messagePostman.Receive(byteArray);

            if (msgs == null) return;

            foreach (var msg in msgs)
            {
                OnReceive?.Invoke(serializer.Deserialize(msg));
            }

            if (byteArray.Remain < 64)
            {
                byteArray.MoveBytes();
                byteArray.ReSize(byteArray.Capacity * 2);
            }

            socket.BeginReceive(byteArray.bytes, byteArray.readIndex, byteArray.Remain, 0, ReceiveCallback, null);
        }

        public void Send(dynamic msg)
        {
            messagePostman.Send(serializer.Serialize(msg));
        }
    }
}
