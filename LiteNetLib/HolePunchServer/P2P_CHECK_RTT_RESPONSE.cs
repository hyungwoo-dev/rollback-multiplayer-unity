using LiteNetLib.Utils;

namespace HolePunchServer
{
    internal struct P2P_CHECK_RTT_RESPONSE : INetSerializable
    {
        public long RequestUnixTimeMillis;
        public long ResponseUnixTimeMillis;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestUnixTimeMillis);
            writer.Put(ResponseUnixTimeMillis);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestUnixTimeMillis = reader.GetLong();
            ResponseUnixTimeMillis = reader.GetLong();
        }
    }
}
