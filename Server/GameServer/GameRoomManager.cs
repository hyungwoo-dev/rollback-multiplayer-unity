using FreeNet;
using System;
using System.Collections.Generic;
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
                if(_roomList.Count == 0 || _roomList.Last.Value.IsFull())
                {
                    var roomID = RoomID.Generate();
                    var newRoom = new GameRoom(roomID);
                    
                    var node = _roomList.AddLast(newRoom);
                    _roomDictionary.Add(roomID, node);

                    user.RoomID = roomID;
                    newRoom.AddUser(user);
                }
                else
                {
                    var roomID = _roomList.Last.Value.ID;
                    user.RoomID = roomID;
                    _roomDictionary[roomID].Value.AddUser(user);
                }
            }
        }

        public void RemoveUser(GameUser user)
        {
            lock (_syncLock)
            {
                var roomID = user.RoomID;
                user.RoomID = RoomID.Empty;

                var node = _roomDictionary[roomID];
                _roomDictionary.Remove(roomID);
                _roomList.Remove(node);
            }
        }
    }
}
