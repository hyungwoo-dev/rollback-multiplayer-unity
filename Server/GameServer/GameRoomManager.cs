using FreeNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    internal class GameRoomManager
    {
        private readonly object _syncLock = new object();

        private readonly LinkedList<GameRoom> _roomList;
        private readonly Dictionary<RoomID, LinkedListNode<GameRoom>> _roomDictionary;

        public GameRoomManager(int capacity)
        {
            _roomList = new LinkedList<GameRoom>();
            _roomDictionary = new Dictionary<RoomID, LinkedListNode<GameRoom>>(capacity, RoomID.EqualityComparer.Instance);
        }

        public void AddUser(GameUser user)
        {
            lock (_syncLock)
            {
                if (_roomList.Count == 0 || _roomList.Last.Value.IsFull() || _roomList.Last.Value.IsAllUsersWorldEntered())
                {
                    var roomID = RoomID.Generate();
                    var room = new GameRoom(roomID);

                    var node = _roomList.AddLast(room);
                    _roomDictionary.Add(roomID, node);

                    user.RoomInfo = (roomID, 0);

                    room.AddUser(user);
                }
                else
                {
                    var roomID = _roomList.Last.Value.ID;
                    var room = _roomDictionary[roomID].Value;
                    user.RoomInfo = (roomID, 1);

                    room.AddUser(user);
                }
            }
        }

        public void RemoveUser(GameUser user)
        {
            lock (_syncLock)
            {
                var roomID = user.RoomInfo.RoomID;
                user.RoomInfo = (RoomID.Empty, -1);

                var node = _roomDictionary[roomID];
                if (node.Value.RemoveUser(user) == 0)
                {
                    Console.WriteLine($"[GameRoomManager::RemoveUser] Remove Room. RoomID: {roomID}");
                    _roomDictionary.Remove(roomID);
                    _roomList.Remove(node);
                }                
            }
        }

        public void OnReceiveEnterWorld(GameUser user, long enterUnixTimeMillis)
        {
            lock (_syncLock)
            {
                _roomDictionary[user.RoomInfo.RoomID].Value.OnEnterWorld(user.RoomInfo.Index);
            }
        }

        public void OnReceiveFrameEvent(GameUser user, int frame, List<GameRoomFrameEvent> frameEvents)
        {
            var gameRoom = _roomDictionary[user.RoomInfo.RoomID].Value;
            lock (_syncLock)
            {
                gameRoom.OnFrameEvent(user.RoomInfo.Index, frame, frameEvents);
            }

            // 경합이 없는 부분이라 Lock 걸지 않는다. 
            if (gameRoom.IsFullFrameEvents(frame))
            {
                gameRoom.BroadcastFrameEvents(frame);
            }
        }

        public void OnReceiveHash(GameUser user, int frame, int hash)
        {
            lock (_syncLock)
            {
                _roomDictionary[user.RoomInfo.RoomID].Value.OnReceiveHash(user.RoomInfo.Index, frame, hash);
            }
        }
    }
}
