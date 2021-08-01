using GameGravity.PolyNetwork.Utility.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace GameGravity.PolyNetwork.Utility.MessagePostman
{
    class TcpMessagePostman : IMessagePostman
    {
        private readonly Socket socket;
        private readonly ByteArray sendBuff;

        private bool isSending = false;

        public TcpMessagePostman(Socket socket)
        {
            this.socket = socket;
            this.sendBuff = new ByteArray();
        }

        public List<byte[]> Receive(ByteArray buff)
        {
            List<byte[]> temps = new List<byte[]>();
            
            while (true)
            {
                int count = buff.ReadInt16();
                
                if (count == 0 || buff.Length < count) break;

                buff.readIndex += 2;

                var temp = new byte[count];
                Array.Copy(buff.bytes, buff.readIndex, temp, 0, count);
                temps.Add(temp);

                buff.readIndex += count;
            }

            return temps.Count == 0 ? null : temps;
        }

        public void Send(byte[] msg)
        {
            lock (sendBuff)
            {
                if (sendBuff.Remain < 64) sendBuff.ReSize(sendBuff.Capacity * 2);

                sendBuff.bytes[sendBuff.writeIndex++] = (byte)(msg.Length % 256);
                sendBuff.bytes[sendBuff.writeIndex++] = (byte)(msg.Length / 256);

                Array.Copy(msg, 0, sendBuff.bytes, sendBuff.writeIndex, msg.Length);
                sendBuff.writeIndex += msg.Length;
            }

            if (isSending) return;

            socket.BeginSend(sendBuff.bytes, sendBuff.readIndex, sendBuff.Length, SocketFlags.None, SendCallback, null);

            void SendCallback(IAsyncResult ar)
            {
                lock (sendBuff)
                {
                    int count = socket.EndSend(ar);
                    sendBuff.readIndex += count;
                    if (sendBuff.Length == 0)
                    {
                        isSending = false;
                        return;
                    }
                    sendBuff.CheckAndMoveBytes();
                }
                socket.BeginSend(sendBuff.bytes, sendBuff.readIndex, sendBuff.Length, SocketFlags.None, SendCallback, null);
            }
        }
    }
}
