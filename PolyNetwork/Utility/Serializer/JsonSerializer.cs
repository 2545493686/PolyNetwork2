using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameGravity.PolyNetwork.Utility.Serializer
{
    public class JsonSerializer : ISerializer
    {
        public dynamic Deserialize(byte[] data)
        {
           return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data));
        }

        public byte[] Serialize(dynamic data)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        }
    }
}
