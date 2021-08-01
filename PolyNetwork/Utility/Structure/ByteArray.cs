using System;
using System.Collections.Generic;
using System.Text;

namespace GameGravity.PolyNetwork.Utility.Structure
{
    public class ByteArray
    {
        public byte[] bytes;
        public int readIndex;
        public int writeIndex;

        public int Remain => Capacity - writeIndex;
        public int Length => writeIndex - readIndex;
        public int Capacity { get; private set; }

        public const int DEFAULT_SIZE = 2048;
        const int MOVE_BYTES_CEILING = 8;

        readonly int m_IninSize;

        public ByteArray(int size = DEFAULT_SIZE)
        {
            bytes = new byte[size];
            Capacity = size;
            m_IninSize = size;
            readIndex = 0;
            writeIndex = 0;
        }

        public ByteArray(byte[] defaultBytes)
        {
            bytes = defaultBytes;
            Capacity = defaultBytes.Length;
            m_IninSize = defaultBytes.Length;
            readIndex = 0;
            writeIndex = defaultBytes.Length;
        }

        public void ReSize(int size)
        {
            if (size < Length || size < m_IninSize)
            {
                return;
            }

            Capacity = GetNewSize(size);
            byte[] newBytes = new byte[Capacity];
            Array.Copy(bytes, readIndex, newBytes, 0, Length);
            bytes = newBytes;

            writeIndex = Length; //读取下标设置到原数据之后
            readIndex = 0;


            int GetNewSize(int oldSize)
            {
                int n = 1;
                while (n < oldSize)
                {
                    n *= 2;
                }

                return n;
            }
        }

        public void CheckAndMoveBytes()
        {
            if (Length < MOVE_BYTES_CEILING)
            {
                MoveBytes();
            }
        }

        public void MoveBytes()
        {
            Array.Copy(bytes, readIndex, bytes, 0, Length);
            writeIndex = Length;
            readIndex = 0;
        }

        /// <summary>
        /// 写入字符，返回成功写入的字符个数
        /// </summary>
        public int Write(byte[] bytes, int offset, int count)
        {
            if (Remain < count)
            {
                ReSize(Length + count);
            }
            Array.Copy(bytes, offset, this.bytes, writeIndex, count);
            writeIndex += count;
            return count;
        }

        /// <summary>
        /// 读取字符，返回成功读取的字符个数
        /// </summary>
        public int Read(byte[] bytes, int offset, int count)
        {
            count = Math.Min(count, Length);
            Array.Copy(this.bytes, 0, bytes, offset, count);
            //readIndex += count;
            //CheckAndMoveBytes();
            return count;
        }

        /// <summary>
        /// 以Int16的形式返回字符数组中前两个字符，失败返回0
        /// </summary>
        public short ReadInt16()
        {
            if (Length < 2) return 0;
            return (short)((bytes[readIndex + 1] << 8) | bytes[readIndex]);
        }

        /// <summary>
        /// 以Int32的形式返回字符数组中前四个字符，失败返回0
        /// </summary>
        public int ReadInt32()
        {
            if (Length < 4) return 0;

            return
                bytes[readIndex + 3] << 24 |
                bytes[readIndex + 2] << 16 |
                bytes[readIndex + 1] << 8 |
                bytes[readIndex];

            //readIndex += 4;
            //CheckAndMoveBytes();
        }

    }
}
