using FreeNet;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

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
            var user = new GameUser(this, UUID.Generate(), token);
            Console.WriteLine($"[GameService::OnSessionConnected] user connected. id: {user.ID}");
            _userManager.AddUser(token, user);
            _roomManager.AddUser(user);
        }

        public void OnSessionDisconnected(CUserToken token)
        {
            var user = _userManager.RemoveUser(token);
            _roomManager.RemoveUser(user);
            Console.WriteLine($"[GameService::OnSessionDisconnected] user disconnected. id: {user.ID}");
        }

        public void ProcessPacket(GameUser user, CPacket msg)
        {
            C2S_MSG protocol = (C2S_MSG)msg.pop_protocol_id();
            Console.WriteLine($"[GameService::Process] protocol: {protocol.ToString()}");

            switch (protocol)
            {
                case C2S_MSG.ENTER_WORLD:
                {
                    var enterUnixTimeMillis = msg.pop_int64();
                    Console.WriteLine($"[GameService::ProcessPacket] MSG: {protocol}, enterUnixTimeMillis: {enterUnixTimeMillis}");
                    _roomManager.OnReceiveEnterWorld(user, enterUnixTimeMillis);
                    break;
                }
                case C2S_MSG.FRAME_EVENT:
                {
                    var frame = msg.pop_int32();
                    var count = msg.pop_int32();
                    var frameEvents = new List<GameRoomFrameEvent>();
                    for (int i = 0; i < count; i++)
                    {
                        var eventType = (FrameEventType)msg.pop_byte();
                        var battleTimeMillis = msg.pop_int32();
                        frameEvents.Add(new GameRoomFrameEvent()
                        {
                            EventType = eventType,
                            BattleTimeMillis = battleTimeMillis,
                        });

                        Console.WriteLine($"[GameService::ProcessPacket] MSG: {protocol}, EventType: {eventType}, Frame: {frame}, BattleTimeMillis: {battleTimeMillis}");
                    }

                    _roomManager.OnReceiveFrameEvent(user, frame, frameEvents);
                    break;
                }
                case C2S_MSG.FRAME_HASH:
                {
                    var frame = msg.pop_int32();
                    var hash = msg.pop_int32();
                    Console.WriteLine($"[GameService::ProcessPacket] MSG: {protocol}, Frame: {frame}, Hash: {hash}");
                    _roomManager.OnReceiveHash(user, frame, hash);
                    break;
                }
            }
        }
    }
}
