using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[ManagedStateIgnore]
public class MultiplayBattleWorldManager : BattleWorldManager
{
    private static readonly object ServerWorldEventInfosLock = new();
    private static Debug Debug = new(nameof(MultiplayBattleWorldManager));

    private BattleWorld ServerWorld { get; set; }
    private NetworkManager NetworkManager { get; set; }
    private Dictionary<int, List<BattleWorldEventInfo>> ReceivedWorldEventInfos { get; set; } = new();
    private Dictionary<int, List<BattleWorldEventInfo>> LocalWorldEventInfos { get; set; } = new();


    public long GameStartUnixTimeMillis { get; private set; } = -1;
    public int PlayerID { get; private set; } = -1;
    public int OpponentPlayerID { get; private set; } = -1;


    public MultiplayBattleWorldManager() : base()
    {
        ServerWorld = WorldPool.Get();
    }

    public override void Prepare()
    {
        base.Prepare();
        var worldScene = new BattleWorldScene(this, BattleWorldSceneKind.VIEW, LayerMask.NameToLayer(BattleLayerMaskNames.Server));
        worldScene.Prepare();
        ServerWorld.Prepare(worldScene);

        NetworkManager = CreateNetworkManager();
        NetworkManager.Connect("127.0.0.1", 7979);
    }

    public override void Initialize(BattleCamera camera)
    {
        base.Initialize(camera);
        ServerWorld.Initialize();

        NetworkManager.C2S_ENTER_WORLD(new C2S_MSG_ENTER_WORLD()
        {
            EnterUnixTimeMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
    }

    public override bool IsReady()
    {
        return base.IsReady() &&
            ServerWorld.IsReady() &&
            NetworkManager.Status == NetworkStatus.CONNECTED;
    }

    public override bool IsStarted()
    {
        return base.IsStarted() &&
            GameStartUnixTimeMillis > 0 &&
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > GameStartUnixTimeMillis;
    }

    public override void AdvanceFrame(in BattleFrame frame)
    {
        var futureNextFrame = FutureWorld.NextFrame;
        var serverWorldNextFrame = ServerWorld.NextFrame;
        if (FutureWorld.NextFrame > serverWorldNextFrame && TryGetServerWorldEventInfos(serverWorldNextFrame, out var serverWorldEventInfos))
        {
            ServerWorld.ExecuteWorldEventInfos(serverWorldEventInfos);
            ServerWorld.AdvanceFrame(frame);

            var localHash = GetWorldEventInfosHash(LocalWorldEventInfos[serverWorldNextFrame]);
            var serverHash = GetWorldEventInfosHash(serverWorldEventInfos);

            var rewind = localHash != serverHash;
            if (rewind)
            {
                FutureWorld.CopyFrom(ServerWorld);
                while (FutureWorld.NextFrame < futureNextFrame)
                {
                    if (LocalWorldEventInfos.TryGetValue(FutureWorld.NextFrame, out var futureWorldEventInfos))
                    {
                        FutureWorld.ExecuteWorldEventInfos(futureWorldEventInfos);
                    }
                    FutureWorld.AdvanceFrame(frame);
                }
            }

            ReleaseWorldEventInfos(serverWorldEventInfos);
            if (LocalWorldEventInfos.TryGetValue(serverWorldNextFrame, out var pastWorldEventInfos))
            {
                ReleaseWorldEventInfos(pastWorldEventInfos);
                LocalWorldEventInfos.Remove(serverWorldNextFrame);
            }
        }

        base.AdvanceFrame(frame);
    }

    protected override void ExecuteWorldEventInfos(int frame, List<BattleWorldEventInfo> worldEventInfos)
    {
        base.ExecuteWorldEventInfos(frame, worldEventInfos);

        var newWorldEventInfos = ListPool<BattleWorldEventInfo>.Get();
        newWorldEventInfos.AddRange(worldEventInfos);
        LocalWorldEventInfos.Add(frame, newWorldEventInfos);

        var serverFrameEvents = new List<C2S_MSG_FRAME_EVENT>();
        foreach (var worldEventInfo in worldEventInfos)
        {
            var message = CreateFrameEventMessage(worldEventInfo);
            serverFrameEvents.Add(message);
        }

        NetworkManager.C2S_FRAME_EVENT(serverFrameEvents);
    }

    private int GetWorldEventInfosHash(List<BattleWorldEventInfo> worldEventInfos)
    {
        var hash = int.MaxValue;
        foreach (var worldEventInfo in worldEventInfos)
        {
            hash ^= worldEventInfo.GetHashCode();
        }
        return hash;
    }

    private bool TryGetServerWorldEventInfos(int frame, out List<BattleWorldEventInfo> worldEventInfos)
    {
        lock (ServerWorldEventInfosLock)
        {
            if (ReceivedWorldEventInfos.TryGetValue(frame, out worldEventInfos))
            {
                ReceivedWorldEventInfos.Remove(frame);
                return true;
            }
            return false;
        }
    }

    public override void OnUpdate(in BattleFrame frame)
    {
        base.OnUpdate(frame);
    }

    public override void Dispose()
    {
        ServerWorld.Release();
        ServerWorld = null;

        DisposeNetworkManager();

        foreach (var worldEventInfos in ReceivedWorldEventInfos.Values)
        {
            ReleaseWorldEventInfos(worldEventInfos);
        }
        ReceivedWorldEventInfos.Clear();

        foreach (var worldEventInfos in LocalWorldEventInfos.Values)
        {
            ReleaseWorldEventInfos(worldEventInfos);
        }
        LocalWorldEventInfos.Clear();

        base.Dispose();
    }


    private void ReleaseWorldEventInfos(List<BattleWorldEventInfo> worldEventInfos)
    {
        foreach (var worldEventInfo in worldEventInfos)
        {
            worldEventInfo.Release(this);
        }
        ListPool<BattleWorldEventInfo>.Release(worldEventInfos);
    }


    private NetworkManager CreateNetworkManager()
    {
        var networkManager = new NetworkManager();
        NetworkManager.OnFrameEvent += HandleOnFrameEvent;
        NetworkManager.OnFrameInvalidateHash += HandleOnFrameInvalidateHash;
        NetworkManager.OnGameStart += HandleOnGameStart;
        return networkManager;
    }

    private void HandleOnGameStart(S2C_MSG_GAME_START msgGameStart)
    {
        GameStartUnixTimeMillis = msgGameStart.GameStartUnixTimeMillis;
        PlayerID = msgGameStart.PlayerIndex;
        OpponentPlayerID = msgGameStart.OpponentPlayerIndex;
    }

    private void HandleOnFrameEvent(List<S2C_MSG_FRAME_EVENT> msgFrameEvents)
    {
        var frame = -1;
        var worldEventInfos = ListPool<BattleWorldEventInfo>.Get();
        foreach (var msgFrameEvent in msgFrameEvents)
        {
            frame = msgFrameEvent.Frame;
            if (TryCreateWorldEvnetInfo(msgFrameEvent, out var worldEventInfo))
            {
                worldEventInfos.Add(worldEventInfo);
            }
        }

        lock (ServerWorldEventInfosLock)
        {
            ReceivedWorldEventInfos.Add(frame, worldEventInfos);
        }
    }

    private C2S_MSG_FRAME_EVENT CreateFrameEventMessage(BattleWorldEventInfo worldEventInfo)
    {
        switch (worldEventInfo.WorldInputEventType)
        {
            case BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN:
            {
                return new C2S_MSG_FRAME_EVENT()
                {
                    Frame = worldEventInfo.TargetFrame,
                    UserIndex = worldEventInfo.UnitID,
                    EventType = FrameEventType.RIGHT_ARROW_DOWN,
                };
            }
            case BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP:
            {
                return new C2S_MSG_FRAME_EVENT()
                {
                    Frame = worldEventInfo.TargetFrame,
                    UserIndex = worldEventInfo.UnitID,
                    EventType = FrameEventType.RIGHT_ARROW_UP,
                };
            }
            case BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN:
            {
                return new C2S_MSG_FRAME_EVENT()
                {
                    Frame = worldEventInfo.TargetFrame,
                    UserIndex = worldEventInfo.UnitID,
                    EventType = FrameEventType.LEFT_ARROW_DOWN,
                };
            }
            case BattleWorldInputEventType.MOVE_LEFT_ARROW_UP:
            {
                return new C2S_MSG_FRAME_EVENT()
                {
                    Frame = worldEventInfo.TargetFrame,
                    UserIndex = worldEventInfo.UnitID,
                    EventType = FrameEventType.LEFT_ARROW_UP,
                };
            }
            case BattleWorldInputEventType.ATTACK1:
            {
                return new C2S_MSG_FRAME_EVENT()
                {
                    Frame = worldEventInfo.TargetFrame,
                    UserIndex = worldEventInfo.UnitID,
                    EventType = FrameEventType.ATTACK1,
                };
            }
            case BattleWorldInputEventType.ATTACK2:
            {
                return new C2S_MSG_FRAME_EVENT()
                {
                    Frame = worldEventInfo.TargetFrame,
                    UserIndex = worldEventInfo.UnitID,
                    EventType = FrameEventType.ATTACK2,
                };
            }
            case BattleWorldInputEventType.FIRE:
            {
                return new C2S_MSG_FRAME_EVENT()
                {
                    Frame = worldEventInfo.TargetFrame,
                    UserIndex = worldEventInfo.UnitID,
                    EventType = FrameEventType.FIRE,
                };
            }
            case BattleWorldInputEventType.JUMP:
            {
                return new C2S_MSG_FRAME_EVENT()
                {
                    Frame = worldEventInfo.TargetFrame,
                    UserIndex = worldEventInfo.UnitID,
                    EventType = FrameEventType.JUMP,
                };
            }
            case BattleWorldInputEventType.NONE:
            default:
            {
                return new C2S_MSG_FRAME_EVENT()
                {
                    Frame = worldEventInfo.TargetFrame,
                    UserIndex = worldEventInfo.UnitID,
                    EventType = FrameEventType.NONE,
                };
            }
        }
    }

    private bool TryCreateWorldEvnetInfo(S2C_MSG_FRAME_EVENT msgFrameEvent, out BattleWorldEventInfo worldEventInfo)
    {
        switch (msgFrameEvent.EventType)
        {
            case FrameEventType.LEFT_ARROW_DOWN:
            {
                worldEventInfo = WorldEventInfoPool.Get();
                worldEventInfo.WorldInputEventType = BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN;
                worldEventInfo.TargetFrame = msgFrameEvent.Frame;
                worldEventInfo.UnitID = msgFrameEvent.UserIndex;
                return true;
            }
            case FrameEventType.LEFT_ARROW_UP:
            {
                worldEventInfo = WorldEventInfoPool.Get();
                worldEventInfo.WorldInputEventType = BattleWorldInputEventType.MOVE_LEFT_ARROW_UP;
                worldEventInfo.TargetFrame = msgFrameEvent.Frame;
                worldEventInfo.UnitID = msgFrameEvent.UserIndex;
                return true;
            }
            case FrameEventType.RIGHT_ARROW_DOWN:
            {
                worldEventInfo = WorldEventInfoPool.Get();
                worldEventInfo.WorldInputEventType = BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN;
                worldEventInfo.TargetFrame = msgFrameEvent.Frame;
                worldEventInfo.UnitID = msgFrameEvent.UserIndex;
                return true;
            }
            case FrameEventType.RIGHT_ARROW_UP:
            {
                worldEventInfo = WorldEventInfoPool.Get();
                worldEventInfo.WorldInputEventType = BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP;
                worldEventInfo.TargetFrame = msgFrameEvent.Frame;
                worldEventInfo.UnitID = msgFrameEvent.UserIndex;
                return true;
            }
            case FrameEventType.ATTACK1:
            {
                worldEventInfo = WorldEventInfoPool.Get();
                worldEventInfo.WorldInputEventType = BattleWorldInputEventType.ATTACK1;
                worldEventInfo.TargetFrame = msgFrameEvent.Frame;
                worldEventInfo.UnitID = msgFrameEvent.UserIndex;
                return true;
            }
            case FrameEventType.ATTACK2:
            {
                worldEventInfo = WorldEventInfoPool.Get();
                worldEventInfo.WorldInputEventType = BattleWorldInputEventType.ATTACK2;
                worldEventInfo.TargetFrame = msgFrameEvent.Frame;
                worldEventInfo.UnitID = msgFrameEvent.UserIndex;
                return true;
            }
            case FrameEventType.FIRE:
            {
                worldEventInfo = WorldEventInfoPool.Get();
                worldEventInfo.WorldInputEventType = BattleWorldInputEventType.FIRE;
                worldEventInfo.TargetFrame = msgFrameEvent.Frame;
                worldEventInfo.UnitID = msgFrameEvent.UserIndex;
                return true;
            }
            case FrameEventType.JUMP:
            {
                worldEventInfo = WorldEventInfoPool.Get();
                worldEventInfo.WorldInputEventType = BattleWorldInputEventType.JUMP;
                worldEventInfo.TargetFrame = msgFrameEvent.Frame;
                worldEventInfo.UnitID = msgFrameEvent.UserIndex;
                return true;
            }
            default:
            {
                worldEventInfo = null;
                return false;
            }
        }
    }

    private void HandleOnFrameInvalidateHash(S2C_MSG_INVALIDATE_HASH msgInvalidateHash)
    {
        Debug.LogError($"S2C_MSG_INVALIDATE_HASH, Frame: {msgInvalidateHash.Frame}, PlayerHash: {msgInvalidateHash.PlayerHash}, OpponentPlayerHash: {msgInvalidateHash.OpponentPlayerHash}");
    }

    private void DisposeNetworkManager()
    {
        NetworkManager.OnFrameEvent -= HandleOnFrameEvent;
        NetworkManager.OnFrameInvalidateHash -= HandleOnFrameInvalidateHash;
        NetworkManager.OnGameStart -= HandleOnGameStart;
        NetworkManager.Dispose();
        NetworkManager = null;
    }
}
