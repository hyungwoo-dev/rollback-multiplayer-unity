using FreeNet;
using System.Collections.Generic;

public partial class NetworkManager
{
    public void C2S_ENTER_WORLD(C2S_MSG_ENTER_WORLD msgEnterWorld)
    {
        CPacket packet = new CPacket();
        packet.set_protocol((short)C2S_MSG.ENTER_WORLD);
        packet.push(msgEnterWorld.EnterUnixTimeMillis);

        Send(packet);
    }

    public void C2S_FRAME_EVENT(List<C2S_MSG_FRAME_EVENT> msgFrameEvent)
    {
        CPacket packet = new CPacket();
        packet.set_protocol((short)C2S_MSG.FRAME_EVENT);
        packet.push(msgFrameEvent.Count);
        for (int i = 0; i < msgFrameEvent.Count; i++)
        {
            packet.push((byte)msgFrameEvent[i].EventType);
            packet.push(msgFrameEvent[i].Frame);
        }

        Send(packet);
    }

    public void C2S_FRAME_HASH(C2S_MSG_FRAME_HASH msgFrameHash)
    {
        CPacket packet = new CPacket();
        packet.set_protocol((short)C2S_MSG.FRAME_HASH);
        packet.push(msgFrameHash.Frame);
        packet.push(msgFrameHash.Hash);

        Send(packet);
    }
}