using System;
using System.Collections.Generic;
using System.Text;

namespace GameGravity.PolyNetwork.Utility.Serializer
{
    public interface ISerializer
    {
        dynamic Deserialize(byte[] data);
        byte[] Serialize(dynamic data);
    }
}
