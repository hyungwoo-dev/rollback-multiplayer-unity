using FreeNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    internal class GameRoom
    {
        private const int MAX_USER_COUNT = 2;

        private readonly object _syncLock = new object();

        public readonly RoomID ID;

        private List<GameUser> _users = new List<GameUser>(MAX_USER_COUNT);

        public GameRoom(RoomID id)
        {
            ID = id;
        }

        public void AddUser(GameUser user)
        {
            lock (_syncLock)
            {
                _users.Add(user);
            }
        }

        public void RemoveUser(GameUser user)
        {
            lock (_syncLock)
            {
                _users.Remove(user);
            }
        }

        public bool IsFull()
        {
            lock (_syncLock)
            {
                return _users.Count == MAX_USER_COUNT;
            }
        }
    }
}
