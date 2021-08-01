using GameGravity.PolyNetwork.Utility.Structure;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace GameGravity.PolyNetwork.Utility.MessagePostman
{
    interface IMessagePostman
    {
        void Send(byte[] msg);
        List<byte[]> Receive(ByteArray buff);
    }
}
