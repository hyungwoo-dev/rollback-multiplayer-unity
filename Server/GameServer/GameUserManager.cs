using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace GameServer
{
    class GameUserManager
    {
        private readonly object _syncLock = new object();

        private readonly Dictionary<CUserToken, GameUser> _users;
        private readonly Dictionary<UUID, CUserToken> _tokens;

        public GameUserManager(int capacity)
        {
            _users = new Dictionary<CUserToken, GameUser>(capacity);
            _tokens = new Dictionary<UUID, CUserToken>(capacity, UUID.EqualityComparer.Instance);
        }

        public void AddUser(CUserToken token, GameUser user)
        {
            lock (_syncLock)
            {
                _users[token] = user;
                _tokens[user.ID] = token;
            }
        }

        public GameUser RemoveUser(CUserToken token)
        {

            lock (_syncLock)
            {
                if (_users.TryGetValue(token, out var value))
                {
                    _users.Remove(token);
                    _tokens.Remove(value.ID);
                    return value;
                }
            }

            return null;
        }

        public bool TryGetUser(UUID id, out GameUser user)
        {
            lock (_syncLock)
            {
                if (!_tokens.TryGetValue(id, out var token))
                {
                    user = null;
                    return false;
                }

                return _users.TryGetValue(token, out user);
            }
        }
    }
}
