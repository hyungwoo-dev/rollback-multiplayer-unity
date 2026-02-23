using FreeNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace GameServer
{
    internal class GameRoom
    {
        private const int MAX_USER_COUNT = 2;
        public readonly RoomID ID;

        private List<GameUser> _users = new List<GameUser>(MAX_USER_COUNT);
        private Dictionary<int, List<GameRoomFrameEvent>[]> _frameEvents = new Dictionary<int, List<GameRoomFrameEvent>[]>();
        private Dictionary<int, int?[]> _frameHashes = new Dictionary<int, int?[]>();
        private bool[] _enterWorldFlags = new bool[MAX_USER_COUNT];

        public GameRoom(RoomID id)
        {
            ID = id;
        }

        public void AddUser(GameUser user)
        {
            lock (_users)
            {
                _users.Add(user);
                Console.WriteLine($"[GameRoom::AddUser] Room: {ID} User: {user.ID}, Full: {IsFull()}");
            }
        }

        public int RemoveUser(GameUser user)
        {
            lock (_users)
            {
                _users.Remove(user);
                Console.WriteLine($"[GameRoom::RemoveUser] Room: {ID} User: {user.ID}");
                return _users.Count;
            }
        }

        public void OnEnterWorld(int userIndex)
        {
            lock (_enterWorldFlags)
            {
                _enterWorldFlags[userIndex] = true;
            }
            
            if (IsAllUsersWorldEntered())
            {
                const long START_DELAY = 1500;
                var startUnixTimeMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + START_DELAY;
                lock (_users)
                {
                    foreach (var user in _users)
                    {
                        var playerIndex = (byte)user.RoomInfo.Index;
                        var opponentPlayerIndex = (byte)(user.RoomInfo.Index == 0 ? 1 : 0);

                        Console.WriteLine($"[GameRoom::OnEnterWorld] Send Message Game Start. PlayerIndex: {playerIndex}, OpponentPlayerIndex: {opponentPlayerIndex}, GameStartUnixTimeMillis: {startUnixTimeMillis}");

                        user.send(new S2C_MSG_GAME_START()
                        {
                            PlayerIndex = playerIndex,
                            OpponentPlayerIndex = opponentPlayerIndex,
                            GameStartUnixTimeMillis = startUnixTimeMillis,
                        });
                    }
                }
            }
        }

        public void OnFrameEvent(int userIndex, int frame, List<GameRoomFrameEvent> frameEvents)
        {
            lock (_frameEvents)
            {
                if (_frameEvents.TryGetValue(frame, out var events) && events[userIndex] == null)
                {
                    events[userIndex] = frameEvents;
                }
                else
                {
                    var newEvents = new List<GameRoomFrameEvent>[MAX_USER_COUNT];
                    newEvents[userIndex] = frameEvents;
                    _frameEvents.Add(frame, newEvents);
                }
            }
        }

        public bool IsFullFrameEvents(int frame)
        {
            lock (_frameEvents)
            {
                if (!_frameEvents.TryGetValue(frame, out var events))
                {
                    return false;
                }

                foreach (var @event in events)
                {
                    if (@event == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void BroadcastInvalidateHash(int frame, int?[] hashes)
        {
            lock (_users)
            {
                foreach (var user in _users)
                {
                    var userIndex = user.RoomInfo.Index;
                    var opponentUserIndex = userIndex == 0 ? 1 : 0;
                    var playerHash = hashes[userIndex].Value;
                    var opponentPlayerHash = hashes[opponentUserIndex].Value;
                    Console.WriteLine($"[GameRoom::BroadcastInvalidateHash] Send Message Game Start To {userIndex}. Frame: {frame}, PlayerHash: {playerHash}, OpponentPlayerHash: {opponentPlayerHash}");
                    user.send(new S2C_MSG_INVALIDATE_HASH()
                    {
                        Frame = frame,
                        PlayerHash = playerHash,
                        OpponentPlayerHash = opponentPlayerHash,
                    });
                }
            }
        }

        public void BroadcastFrameEvents(int frame)
        {
            var frameEvents = GetFrameEvents(frame);

            var msg = new S2C_MSG_FRAME_EVENTS()
            {
                Frame = frame,
                FrameEvents = frameEvents,
            };

            lock (_users)
            {
                foreach (var user in _users)
                {
                    Console.WriteLine($"[GameRoom::BroadcastFrameEvents] UserIndex: {user.RoomInfo.Index}, Frame: {frame}, Events: {string.Join(", ", frameEvents.Select(f => f.ToString()))}");
                    user.send(msg);
                }
            }
        }

        private List<S2C_MSG_FRAME_EVENT> GetFrameEvents(int frame)
        {
            lock (_frameEvents)
            {
                return _frameEvents[frame]
                    .SelectMany(v => v)
                    .OrderBy(frameEvent => frameEvent.BattleTimeMillis)
                    .ThenBy(frameEvent => frameEvent.UserIndex)
                    .Select(gameRoomEvent => new S2C_MSG_FRAME_EVENT()
                    {
                        EventType = gameRoomEvent.EventType,
                        UserIndex = (byte)gameRoomEvent.UserIndex,
                        BattleTimeMillis = gameRoomEvent.BattleTimeMillis,
                    })
                    .ToList();
            }
        }

        public void OnReceiveHash(int userIndex, int frame, int hash)
        {
            lock (_frameHashes)
            {
                if (_frameHashes.TryGetValue(frame, out var hashes))
                {
                    hashes[userIndex] = hash;
                }
                else
                {
                    var newHashes = new int?[MAX_USER_COUNT];
                    newHashes[userIndex] = hash;
                    _frameHashes.Add(frame, newHashes);
                }
            }

            if (IsFullHashes(frame, out var hashCheck))
            {
                lock (_frameEvents)
                {
                    _frameEvents.Remove(frame);
                }

                if (!hashCheck)
                {
                    BroadcastInvalidateHash(frame, _frameHashes[frame]);
                }
            }
        }

        private bool IsFullHashes(int frame, out bool hashCheck)
        {
            hashCheck = true;
            int? tempHash = null;
            lock (_frameHashes)
            {
                if (_frameHashes.TryGetValue(frame, out var hashes))
                {
                    foreach (var hash in hashes)
                    {
                        if (hash == null)
                        {
                            return false;
                        }

                        if (tempHash == null)
                        {
                            tempHash = hash;
                        }
                        else
                        {
                            if (tempHash != hash)
                            {
                                hashCheck = false;
                            }
                        }
                    }
                }

                return true;
            }
        }

        public bool IsFull()
        {
            lock (_users)
            {
                return _users.Count == MAX_USER_COUNT;
            }
        }

        public bool IsAllUsersWorldEntered()
        {
            lock (_enterWorldFlags)
            {
                foreach (var enterWorldFlag in _enterWorldFlags)
                {
                    if (!enterWorldFlag)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
