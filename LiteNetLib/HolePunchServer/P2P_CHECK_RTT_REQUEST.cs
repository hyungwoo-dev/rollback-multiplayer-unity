using LiteNetLib.Utils;

namespace HolePunchServer
{
    internal struct P2P_CHECK_RTT_REQUEST : INetSerializable
    {
        public long RequestUnixTimeMillis;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestUnixTimeMillis);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestUnixTimeMillis = reader.GetLong();
        }
    }
}
