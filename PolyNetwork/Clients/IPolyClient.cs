using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GameGravity.PolyNetwork.Clients
{
    /// <summary>
    /// 连接完成后的回调
    /// </summary>
    /// <param name="error">错误报告，为Empty表示正常连接</param>
    public delegate void ClientConnectCallback(string error);
    
    public interface IPolyClient
    {
        bool IsConnect { get; }

        ClientConnectCallback OnConnectComplete { get; set; }
        Action<dynamic> OnReceive { get; set; }

        void Connect(string address, int ip);

        void Send(dynamic msg);
    }
}
