using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace GameServer
{
    class GameService
    {
        private readonly GameUserManager _userManager;
        private readonly GameRoomManager _roomManager;

        public GameService(int capacity)
        {
            _userManager = new GameUserManager(capacity);
            _roomManager = new GameRoomManager(capacity / 2);
        }

        /// <summary>
        /// 클라이언트가 접속 완료 하였을 때 호출됩니다.
        /// n개의 워커 스레드에서 호출될 수 있으므로 공유 자원 접근시 동기화 처리를 해줘야 합니다.
        /// </summary>
        /// <returns></returns>
        public void OnSessionConnected(CUserToken token)
        {
            GameUser user = new GameUser(this, UUID.Generate(), token);
            _userManager.AddUser(token, user);
            _roomManager.AddUser(user);

            Console.WriteLine($"[GameService::OnSessionConnected] user connected. id: {user.ID}");
        }

        public void OnSessionDisconnected(CUserToken token)
        {
            var user = _userManager.RemoveUser(token);
            _roomManager.RemoveUser(user);
            Console.WriteLine($"[GameService::OnSessionDisconnected] user disconnected. id: {user.ID}");
        }

        public void ProcessPacket(GameUser user, CPacket msg)
        {
            Protocol protocol = (Protocol)msg.pop_protocol_id();
            Console.WriteLine($"[GameService::Process] protocol: {protocol.ToString()}");

            switch (protocol)
            {
                case Protocol.REQUEST:
                {
                    int number = msg.pop_int32();
                    CPacket packet = CPacket.create((short)Protocol.RESPONSE);
                    packet.push(number + 1);
                    user.send(packet);
                }
                break;
            }
        }
    }
}
