using System.Collections.Generic;
using UnityEngine.Pool;

[ManagedStateIgnore]
public class MultiplayBattleWorldManager : BaseWorldManager
{
    private static Debug Debug = new(nameof(MultiplayBattleWorldManager));

    private BattleWorld ServerWorld { get; set; }
    private NetworkManager NetworkManager { get; set; }
    private Dictionary<int, List<BattleWorldEventInfo>> ReceivedWorldEventInfos { get; set; } = new();
    protected Dictionary<int, List<BattleWorldEventInfo>> LocalWorldEventInfos { get; private set; } = new();
    public long GameStartUnixTimeMillis { get; private set; } = -1;
    public override int BattleTimeMillis => (int)(TimeUtils.UtcNowUnixTimeMillis - GameStartUnixTimeMillis);
    public int OpponentPlayerID { get; private set; } = -1;

    public MultiplayBattleWorldManager() : base()
    {
        ServerWorld = WorldPool.Get();
    }

    public override void Setup()
    {
        base.Setup();
        NetworkManager = CreateNetworkManager();
        NetworkManager.Connect("127.0.0.1", 7979);
    }

    public override bool IsSetupCompleted()
    {
        return base.IsSetupCompleted() &&
            NetworkManager.Status == NetworkStatus.CONNECTED;
    }

    public override void OnSetupCompleted()
    {
        base.OnSetupCompleted();
        NetworkManager.C2S_ENTER_WORLD(new C2S_MSG_ENTER_WORLD()
        {
            EnterUnixTimeMillis = TimeUtils.UtcNowUnixTimeMillis,
        });
    }

    public override void Initialize(BattleCamera camera)
    {
        base.Initialize(camera);
        ServerWorld.Initialize();
    }

    public override bool IsStarted()
    {
        return base.IsStarted() &&
            GameStartUnixTimeMillis > 0 &&
            TimeUtils.UtcNowUnixTimeMillis > GameStartUnixTimeMillis;
    }

    public override void AdvanceFrame(in BattleFrame frame)
    {
        AdvanceServerWorld(frame, ServerWorld.NextFrame, out var rewind);
        if (rewind)
        {
            RewindFuture(frame);
        }

        base.AdvanceFrame(frame);
    }

    private void AdvanceServerWorld(BattleFrame frame, int serverWorldNextFrame, out bool rewind)
    {
        rewind = false;

        while (TryPopServerWorldEventInfos(serverWorldNextFrame, out var serverWorldEventInfos))
        {
            ServerWorld.ExecuteWorldEventInfos(serverWorldEventInfos);
            ServerWorld.AdvanceFrame(frame);

            var localHash = GetWorldEventInfosHash(LocalWorldEventInfos[serverWorldNextFrame]);
            var serverHash = GetWorldEventInfosHash(serverWorldEventInfos);

            rewind |= localHash != serverHash;

            var msg = new C2S_MSG_FRAME_HASH()
            {
                Frame = ServerWorld.CurrentFrame,
                Hash = ServerWorld.GetWorldHash()
            };
            NetworkManager.C2S_FRAME_HASH(msg);

            ReleaseWorldEventInfos(serverWorldEventInfos);
            if (LocalWorldEventInfos.TryGetValue(serverWorldNextFrame, out var pastWorldEventInfos))
            {
                ReleaseWorldEventInfos(pastWorldEventInfos);
                LocalWorldEventInfos.Remove(serverWorldNextFrame);
            }
        }
    }

    private void RewindFuture(BattleFrame frame)
    {
        var savedFutureFrame = FutureWorld.NextFrame;
        FutureWorld.Release();
        FutureWorld = WorldPool.Get();
        FutureWorld.CopyFrom(ServerWorld);

        while (FutureWorld.NextFrame < savedFutureFrame)
        {
            FutureWorld.ExecuteWorldEventInfos(LocalWorldEventInfos[FutureWorld.NextFrame]);
            FutureWorld.AdvanceFrame(frame);
        }
    }

    protected override void ExecuteWorldEventInfos(int frame, List<BattleWorldEventInfo> worldEventInfos)
    {
        base.ExecuteWorldEventInfos(frame, worldEventInfos);

        var newWorldEventInfos = ListPool<BattleWorldEventInfo>.Get();
        newWorldEventInfos.AddRange(worldEventInfos);
        LocalWorldEventInfos.Add(frame, newWorldEventInfos);

        var serverFrameEvents = ListPool<C2S_MSG_FRAME_EVENT>.Get();

        foreach (var worldEventInfo in worldEventInfos)
        {
            var frameEventType = WorldEventToFrameEventType(worldEventInfo.WorldInputEventType);
            var msgFrameEvent = NetworkManager.C2S_FrameEventPool.Get();
            msgFrameEvent.EventType = frameEventType;
            msgFrameEvent.BattleTimeMillis = BattleTimeMillis;
            serverFrameEvents.Add(msgFrameEvent);
        }

        var frameEvents = NetworkManager.C2S_FrameEventsPool.Get();
        frameEvents.Frame = frame;
        frameEvents.Events = serverFrameEvents;
        NetworkManager.C2S_FRAME_EVENTS(frameEvents);

        // Release
        foreach (var serverFrameEvent in serverFrameEvents)
        {
            NetworkManager.C2S_FrameEventPool.Release(serverFrameEvent);
        }
        serverFrameEvents.Clear();

        ListPool<C2S_MSG_FRAME_EVENT>.Release(serverFrameEvents);
        NetworkManager.C2S_FrameEventsPool.Release(frameEvents);
    }

    private bool TryPopServerWorldEventInfos(int frame, out List<BattleWorldEventInfo> worldEventInfos)
    {
        lock (ReceivedWorldEventInfos)
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

        foreach (var worldEventInfos in LocalWorldEventInfos.Values)
        {
            ReleaseWorldEventInfos(worldEventInfos);
        }
        LocalWorldEventInfos.Clear();

        DisconnectNetworkManager();

        foreach (var worldEventInfos in ReceivedWorldEventInfos.Values)
        {
            ReleaseWorldEventInfos(worldEventInfos);
        }
        ReceivedWorldEventInfos.Clear();

        base.Dispose();
    }

    private NetworkManager CreateNetworkManager()
    {
        var networkManager = new NetworkManager();
        networkManager.OnFrameEvent += HandleOnFrameEvent;
        networkManager.OnFrameInvalidateHash += HandleOnFrameInvalidateHash;
        networkManager.OnGameStart += HandleOnGameStart;
        return networkManager;
    }

    private void HandleOnGameStart(S2C_MSG_GAME_START msgGameStart)
    {
        GameStartUnixTimeMillis = msgGameStart.GameStartUnixTimeMillis;
        PlayerID = msgGameStart.PlayerIndex;
        OpponentPlayerID = msgGameStart.OpponentPlayerIndex;
    }

    private void HandleOnFrameEvent(int frame, List<S2C_MSG_FRAME_EVENT> msgFrameEvents)
    {
        var worldEventInfos = ListPool<BattleWorldEventInfo>.Get();
        foreach (var msgFrameEvent in msgFrameEvents)
        {
            if (TryCreateWorldEvnetInfo(msgFrameEvent, frame, out var worldEventInfo))
            {
                worldEventInfos.Add(worldEventInfo);
            }
        }

        lock (ReceivedWorldEventInfos)
        {
            ReceivedWorldEventInfos.Add(frame, worldEventInfos);
        }
    }

    protected FrameEventType WorldEventToFrameEventType(BattleWorldInputEventType worldEventType)
    {
        switch (worldEventType)
        {
            case BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN:
            {
                return FrameEventType.RIGHT_ARROW_DOWN;
            }
            case BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP:
            {
                return FrameEventType.RIGHT_ARROW_UP;
            }
            case BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN:
            {
                return FrameEventType.LEFT_ARROW_DOWN;
            }
            case BattleWorldInputEventType.MOVE_LEFT_ARROW_UP:
            {
                return FrameEventType.LEFT_ARROW_UP;
            }
            case BattleWorldInputEventType.ATTACK1:
            {
                return FrameEventType.ATTACK1;
            }
            case BattleWorldInputEventType.ATTACK2:
            {
                return FrameEventType.ATTACK2;
            }
            case BattleWorldInputEventType.FIRE:
            {
                return FrameEventType.FIRE;
            }
            case BattleWorldInputEventType.JUMP:
            {
                return FrameEventType.JUMP;
            }
            case BattleWorldInputEventType.NONE:
            default:
            {
                return FrameEventType.NONE;
            }
        }
    }

    protected BattleWorldInputEventType FrameEventTypeToWorldEventType(FrameEventType frameEventType)
    {
        switch (frameEventType)
        {
            case FrameEventType.LEFT_ARROW_DOWN:
            {
                return BattleWorldInputEventType.MOVE_LEFT_ARROW_DOWN;
            }
            case FrameEventType.LEFT_ARROW_UP:
            {
                return BattleWorldInputEventType.MOVE_LEFT_ARROW_UP;
            }
            case FrameEventType.RIGHT_ARROW_DOWN:
            {
                return BattleWorldInputEventType.MOVE_RIGHT_ARROW_DOWN;
            }
            case FrameEventType.RIGHT_ARROW_UP:
            {
                return BattleWorldInputEventType.MOVE_RIGHT_ARROW_UP;
            }
            case FrameEventType.ATTACK1:
            {
                return BattleWorldInputEventType.ATTACK1;
            }
            case FrameEventType.ATTACK2:
            {
                return BattleWorldInputEventType.ATTACK2;
            }
            case FrameEventType.FIRE:
            {
                return BattleWorldInputEventType.FIRE;
            }
            case FrameEventType.JUMP:
            {
                return BattleWorldInputEventType.JUMP;
            }
            case FrameEventType.NONE:
            default:
            {
                return BattleWorldInputEventType.NONE;
            }
        }
    }

    private bool TryCreateWorldEvnetInfo(S2C_MSG_FRAME_EVENT msgFrameEvent, int frame, out BattleWorldEventInfo worldEventInfo)
    {
        var worldEventType = FrameEventTypeToWorldEventType(msgFrameEvent.EventType);
        worldEventInfo = WorldEventInfoPool.Get();
        worldEventInfo.TargetFrame = frame;
        worldEventInfo.WorldInputEventType = worldEventType;
        worldEventInfo.UnitID = msgFrameEvent.UserIndex;
        worldEventInfo.BattleTimeMillis = msgFrameEvent.BattleTimeMillis;
        return true;
    }

    private void HandleOnFrameInvalidateHash(S2C_MSG_INVALIDATE_HASH msgInvalidateHash)
    {
        Debug.LogError($"S2C_MSG_INVALIDATE_HASH, Frame: {msgInvalidateHash.Frame}, PlayerHash: {msgInvalidateHash.PlayerHash}, OpponentPlayerHash: {msgInvalidateHash.OpponentPlayerHash}");
    }

    private void DisconnectNetworkManager()
    {
        NetworkManager.OnFrameEvent -= HandleOnFrameEvent;
        NetworkManager.OnFrameInvalidateHash -= HandleOnFrameInvalidateHash;
        NetworkManager.OnGameStart -= HandleOnGameStart;
        NetworkManager.Dispose();
        NetworkManager = null;
    }
}
