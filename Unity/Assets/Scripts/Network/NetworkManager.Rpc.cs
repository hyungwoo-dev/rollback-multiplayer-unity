using FreeNet;

public partial class NetworkManager
{
    public void RPC_REQUEST(int number)
    {
        CPacket packet = CPacket.create((short)Protocol.REQUEST);
        packet.push(number);
        Send(packet);
    }
}