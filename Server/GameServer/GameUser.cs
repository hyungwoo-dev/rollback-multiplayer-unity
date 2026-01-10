using System;
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

        public RoomID RoomID { get; set; }

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
