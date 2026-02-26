using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace GameServer
{
    /// <summary>
    /// 하나의 session객체를 나타낸다.
    /// </summary>
    class GameUser : IPeer
    {
        private readonly GameService _service;
        private readonly CUserToken _token;
        public readonly UUID ID;

        public (RoomID RoomID, int Index) RoomInfo = (RoomID.Empty, -1);
        
        public GameUser(GameService service, UUID uuid, CUserToken token)
        {
            _service = service;
            _token = token;
            _token.set_peer(this);
            ID = uuid;
        }

        public void Ban()
        {
            _token.ban();
        }

        public void send(S2C_MSG_GAME_START msg)
        {
            var packet = new CPacket();
            packet.set_protocol((short)S2C_MSG.S2C_START_GAME);
            packet.push(msg.GameStartUnixTimeMillis);
            packet.push(msg.PlayerIndex);
            packet.push(msg.OpponentPlayerIndex);
            send(packet);
        }

        public void send(S2C_MSG_INTERMIDIATE_FRAME_EVENT msg)
        {
            var packet = new CPacket();
            packet.set_protocol((short)S2C_MSG.S2C_INTERMIDIATE_FRAME_EVENT);
            packet.push(msg.Frame);
            packet.push((byte)msg.FrameEvent.EventType);
            packet.push(msg.FrameEvent.UserIndex);
            packet.push(msg.FrameEvent.BattleTimeMillis);
            send(packet);
        }

        public void send(S2C_MSG_FRAME_EVENTS msg)
        {
            var packet = new CPacket();
            packet.set_protocol((short)S2C_MSG.S2C_FRAME_EVENT);
            packet.push(msg.Frame);
            packet.push(msg.FrameEvents.Count);

            foreach (var item in msg.FrameEvents)
            {
                var eventType = item.EventType;
                packet.push((byte)eventType);
                if (eventType != FrameEventType.NONE)
                {
                    packet.push(item.UserIndex);
                    packet.push(item.BattleTimeMillis);
                }
            }
            send(packet);
        }

        public void send(S2C_MSG_INVALIDATE_HASH msg)
        {
            var packet = new CPacket();
            packet.set_protocol((short)S2C_MSG.S2C_INVALIDATE_HASH);
            packet.push(msg.Frame);
            packet.push(msg.PlayerHash);
            packet.push(msg.OpponentPlayerHash);
            send(packet);
        }

        public void send(CPacket msg)
        {
            msg.record_size();
            _token.send(new ArraySegment<byte>(msg.buffer, 0, msg.position));
        }

        void IPeer.on_removed()
        {
            _service.OnSessionDisconnected(_token);
        }

        void IPeer.disconnect()
        {
            _token.ban();
        }

        void IPeer.on_message(CPacket msg)
        {
            _service.ProcessPacket(this, msg);
        }
    }
}
