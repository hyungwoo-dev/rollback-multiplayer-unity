using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

[ManagedStateIgnore]
public class MultiplayBattleWorldManager : BaseWorldManager
{
    private static Debug Debug = new(nameof(MultiplayBattleWorldManager));

    private object _serverUpdateLock = new();
    private BattleWorld ServerWorld { get; set; }

    private int LastCheckedServerWorldFrame { get; set; }
    private NetworkManager NetworkManager { get; set; }

    private object _futureUpdateLock = new();
    private Dictionary<int, List<BattleWorldEventInfo>> ReceivedIntermidiateWorldEventInfos { get; set; } = new();
    private Dictionary<int, List<BattleWorldEventInfo>> ReceivedServerWorldEventInfos { get; set; } = new();
    protected Dictionary<int, List<BattleWorldEventInfo>> LocalWorldEventInfos { get; private set; } = new();
    public long GameStartUnixTimeMillis { get; private set; } = -1;
    public override int BattleTimeMillis => (int)(TimeUtils.UtcNowUnixTimeMillis - GameStartUnixTimeMillis);
    public int OpponentPlayerID { get; private set; } = -1;

    private long _rewindFlag;
    private long _disposeFlag;
    private long _serverWorldRunningFlag;

    public Pool<BattleWorldEventInfo> IntermidiateWorldEventInfoPool { get; private set; }

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

    public override void Initialize(in BattleFrame frame)
    {
        base.Initialize(frame);

        if (InputManager is BattleWindowsInputManager windowsInputManager)
        {
            windowsInputManager.OnFrameEventImmediately += HandleOnFrameEventImmediately;
        }

        var thread = new Thread(static state =>
        {
            var parameters = state as object[];
            var worldManager = parameters[0] as MultiplayBattleWorldManager;
            var fixedFrame = (BattleFrame)parameters[1];
            Interlocked.Increment(ref worldManager._serverWorldRunningFlag);

            worldManager.ServerWorld.Initialize();
            while (Interlocked.Read(ref worldManager._disposeFlag) == 0)
            {
                worldManager.NetworkManager.ProcessPacket();

                bool rewind = false;
                lock (worldManager._serverUpdateLock)
                {
                    rewind = worldManager.AdvanceServerWorld(fixedFrame);
                }

                if (rewind)
                {
                    Interlocked.Increment(ref worldManager._rewindFlag);
                }

                Thread.Sleep(1);
            }

            Interlocked.Decrement(ref worldManager._serverWorldRunningFlag);
        });
        thread.Priority = ThreadPriority.Highest;
        thread.IsBackground = true;
        thread.Start(new object[]
        {
            this,
            frame
        });
    }

    public override bool IsStarted()
    {
        return base.IsStarted() &&
            GameStartUnixTimeMillis > 0 &&
            TimeUtils.UtcNowUnixTimeMillis > GameStartUnixTimeMillis;
    }

    public override void AdvanceFrame(in BattleFrame frame)
    {
        // 두 오브젝트에 대해 락을 걸지만, 두 오브젝트는 각각 다른 스레드에서 사용하기 때문에 데드락이 발생하지 않음
        lock (_futureUpdateLock)
        {
            int completeFrame;
            lock (_serverUpdateLock)
            {
                if (Interlocked.Read(ref _rewindFlag) > 0)
                {
                    Interlocked.Exchange(ref _rewindFlag, 0);
                    RewindFuture(frame);
                }
                completeFrame = ServerWorld.NextFrame;
            }

            if (completeFrame >= FutureWorld.CurrentFrame)
            {
                completeFrame = FutureWorld.CurrentFrame - 1;
            }

            if (LastCheckedServerWorldFrame != completeFrame)
            {
                LastCheckedServerWorldFrame = completeFrame;

                for (var i = LastCheckedServerWorldFrame; i <= completeFrame; ++i)
                {
                    if (LocalWorldEventInfos.TryGetValue(i, out var localWorldEventInfos))
                    {
                        FutureWorld.ReleaseWorldEventInfos(localWorldEventInfos);
                        LocalWorldEventInfos.Remove(i);
                    }

                    if (ReceivedIntermidiateWorldEventInfos.TryGetValue(i, out var intermidiateWorldEventInfo))
                    {
                        FutureWorld.ReleaseWorldEventInfos(intermidiateWorldEventInfo);
                        ReceivedIntermidiateWorldEventInfos.Remove(i);
                    }
                }
            }

            base.AdvanceFrame(frame);

            SendInputEvents(FutureWorld.CurrentFrame);
        }
    }

    private bool AdvanceServerWorld(BattleFrame frame)
    {
        var rewind = false;

        while (ReceivedServerWorldEventInfos.TryGetValue(ServerWorld.NextFrame, out var serverWorldEventInfos))
        {
            var serverWorldNextFrame = ServerWorld.NextFrame;
            ReceivedServerWorldEventInfos.Remove(serverWorldNextFrame);
            ServerWorld.ExecuteWorldEventInfos(serverWorldEventInfos);
            ServerWorld.AdvanceFrame(frame);

            foreach (var serverWorldEventInfo in serverWorldEventInfos)
            {
                if (serverWorldEventInfo.UnitID != PlayerID && serverWorldEventInfo.WorldInputEventType != BattleWorldInputEventType.NONE)
                {
                    rewind = true;
                    break;
                }
            }

            ServerWorld.ReleaseWorldEventInfos(serverWorldEventInfos);

            var msg = new C2S_MSG_FRAME_HASH()
            {
                Frame = ServerWorld.CurrentFrame,
                Hash = ServerWorld.GetWorldHash()
            };
            NetworkManager.C2S_FRAME_HASH(msg);
        }

        return rewind;
    }

    private void RewindFuture(BattleFrame frame)
    {
        var savedFutureFrame = FutureWorld.NextFrame;
        FutureWorld.Release();
        FutureWorld = WorldPool.Get();
        FutureWorld.CopyFrom(ServerWorld);

        while (FutureWorld.NextFrame < savedFutureFrame)
        {
            var nextFrame = FutureWorld.NextFrame;

            var list = FutureWorld.WorldEventInfoListPool.Get();
            if (LocalWorldEventInfos.TryGetValue(nextFrame, out var localWorldEventInfo))
            {
                list.AddRange(localWorldEventInfo);
            }
            if (ReceivedIntermidiateWorldEventInfos.TryGetValue(nextFrame, out var intermidiateWorldEventInfo))
            {
                list.AddRange(intermidiateWorldEventInfo);
            }
            list.Sort((lhs, rhs) =>
            {
                var compare = lhs.BattleTimeMillis.CompareTo(rhs.BattleTimeMillis);
                if (compare != 0)
                {
                    return compare;
                }
                else
                {
                    compare = lhs.UnitID.CompareTo(rhs.UnitID);
                    return compare;
                }
            });

            FutureWorld.ExecuteWorldEventInfos(LocalWorldEventInfos[FutureWorld.NextFrame]);
            FutureWorld.AdvanceFrame(frame);

            FutureWorld.WorldEventInfoListPool.Release(list);
        }
    }

    protected override void OnExecuteWorldEventInfos(int frame, List<BattleWorldEventInfo> worldEventInfos)
    {
        base.OnExecuteWorldEventInfos(frame, worldEventInfos);

        var newWorldEventInfos = FutureWorld.WorldEventInfoListPool.Get();
        newWorldEventInfos.AddRange(worldEventInfos);
        LocalWorldEventInfos.TryAdd(frame, newWorldEventInfos);
    }

    private void SendInputEvents(int frame)
    {
        var serverFrameEvents = NetworkManager.C2S_FrameEventListPool.Get();

        if (!LocalWorldEventInfos.TryGetValue(frame, out var worldEventInfos))
        {
            return;
        }

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
        NetworkManager.C2S_FrameEventListPool.Release(serverFrameEvents);
        NetworkManager.C2S_FrameEventsPool.Release(frameEvents);
    }

    public override void OnUpdate(in BattleFrame frame)
    {
        base.OnUpdate(frame);

        int serverNextFrame;
        lock (_serverUpdateLock)
        {
            serverNextFrame = ServerWorld.NextFrame;
        }

        var frameDrift = FutureWorld.NextFrame - serverNextFrame;
        var timeScale = AdjustSimulationSpeed(frameDrift);
        Time.timeScale = timeScale;
    }

    private float AdjustSimulationSpeed(int frameDrift)
    {
        // 서버와 SLOW_LEVEL1_FRAME_THRESHOLD 프레임을 초과해서 차이가 나면 슬로우가 시작되고, LOCK_FRAME_THRESHOLD에 도달하면 Lock에 걸린다.
        // Slow는 1단계 (낮은 슬로우)와 2단계 (프레임 차이가 벌어질수록 느려지는)가 있다.
        const int SLOW_LEVEL1_FRAME_THRESHOLD = 2;
        const int SLOW_LEVEL2_FRAME_THRESHOLD = 3;
        const int LOCK_FRAME_THRESHOLD = 10;
        const float SLOW_LEVEL1_TIME_SCALE = 0.9f;

        if (frameDrift <= SLOW_LEVEL1_FRAME_THRESHOLD)
        {
            // Unlock
            return 1.0f;
        }
        else if (frameDrift >= LOCK_FRAME_THRESHOLD)
        {
            // Lock
            return 0.0f;
        }
        else
        {
            // Slow
            if (frameDrift <= SLOW_LEVEL2_FRAME_THRESHOLD)
            {
                // Slow Level1
                return SLOW_LEVEL1_TIME_SCALE;
            }
            else
            {
                // Slow Level2
                var t = (frameDrift - SLOW_LEVEL2_FRAME_THRESHOLD) / (float)(LOCK_FRAME_THRESHOLD - SLOW_LEVEL2_FRAME_THRESHOLD);
                var timeScale = Mathf.Lerp(SLOW_LEVEL1_TIME_SCALE, 0.0f, t);
                return timeScale;
            }
        }
    }

    public override void Dispose()
    {
        Interlocked.Increment(ref _disposeFlag);

        while (Interlocked.Read(ref _serverWorldRunningFlag) > 0)
        {
            // 서버 월드 스레드가 종료될 떄 까지 대기한다.
        }

        if (InputManager is BattleWindowsInputManager windowsInputManager)
        {
            windowsInputManager.OnFrameEventImmediately -= HandleOnFrameEventImmediately;
        }

        foreach (var worldEventInfos in ReceivedIntermidiateWorldEventInfos.Values)
        {
            FutureWorld.ReleaseWorldEventInfos(worldEventInfos);
        }
        ReceivedIntermidiateWorldEventInfos.Clear();

        foreach (var worldEventInfos in LocalWorldEventInfos.Values)
        {
            FutureWorld.ReleaseWorldEventInfos(worldEventInfos);
        }
        LocalWorldEventInfos.Clear();

        foreach (var worldEventInfos in ReceivedServerWorldEventInfos.Values)
        {
            ServerWorld.ReleaseWorldEventInfos(worldEventInfos);
        }
        ReceivedServerWorldEventInfos.Clear();

        ServerWorld.Release();
        ServerWorld = null;

        DisconnectNetworkManager();

        base.Dispose();
    }

    private NetworkManager CreateNetworkManager()
    {
        var networkManager = new NetworkManager();
        networkManager.OnGameStart += HandleOnGameStart;
        networkManager.OnFrameEvent += HandleOnFrameEvent;
        networkManager.OnFrameInvalidateHash += HandleOnFrameInvalidateHash;
        networkManager.OnIntermidiateFrameEvent += HandleOnIntermidiateFrameEvent;
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
        var worldEventInfos = ServerWorld.WorldEventInfoListPool.Get();
        foreach (var msgFrameEvent in msgFrameEvents)
        {
            var worldEventInfo = CreateServerWorldEvnetInfo(msgFrameEvent, frame);
            worldEventInfos.Add(worldEventInfo);
        }

        ReceivedServerWorldEventInfos.TryAdd(frame, worldEventInfos);
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
            case FrameEventType.NONE:
            default:
            {
                return BattleWorldInputEventType.NONE;
            }
        }
    }

    private BattleWorldEventInfo CreateServerWorldEvnetInfo(S2C_MSG_FRAME_EVENT msgFrameEvent, int frame)
    {
        var worldEventType = FrameEventTypeToWorldEventType(msgFrameEvent.EventType);
        var worldEventInfo = ServerWorld.WorldEventInfoPool.Get();
        worldEventInfo.TargetFrame = frame;
        worldEventInfo.WorldInputEventType = worldEventType;
        worldEventInfo.UnitID = msgFrameEvent.UserIndex;
        worldEventInfo.BattleTimeMillis = msgFrameEvent.BattleTimeMillis;
        return worldEventInfo;
    }

    private BattleWorldEventInfo CreateIntermidiateWorldEventInfo(FrameEventType eventType, int frame, int userIndex, int battleTimeMillis)
    {
        var worldEventType = FrameEventTypeToWorldEventType(eventType);
        var worldEventInfo = FutureWorld.WorldEventInfoPool.Get();
        worldEventInfo.TargetFrame = frame;
        worldEventInfo.WorldInputEventType = worldEventType;
        worldEventInfo.UnitID = userIndex;
        worldEventInfo.BattleTimeMillis = battleTimeMillis;
        return worldEventInfo;
    }

    private void HandleOnFrameInvalidateHash(S2C_MSG_INVALIDATE_HASH msgInvalidateHash)
    {
        Debug.LogError($"S2C_MSG_INVALIDATE_HASH, Frame: {msgInvalidateHash.Frame}, PlayerHash: {msgInvalidateHash.PlayerHash}, OpponentPlayerHash: {msgInvalidateHash.OpponentPlayerHash}");
    }

    private void HandleOnIntermidiateFrameEvent(S2C_MSG_INTERMIDIATE_FRAME_EVENT msgIntermidiateFrameEvent)
    {
        lock (_futureUpdateLock)
        {
            lock (_serverUpdateLock)
            {
                if (msgIntermidiateFrameEvent.Frame < ServerWorld.NextFrame)
                {
                    return;
                }
            }

            var worldEventInfo = CreateIntermidiateWorldEventInfo(
                msgIntermidiateFrameEvent.FrameEvent.EventType,
                msgIntermidiateFrameEvent.Frame,
                msgIntermidiateFrameEvent.FrameEvent.UserIndex,
                msgIntermidiateFrameEvent.FrameEvent.BattleTimeMillis
            );

            if (ReceivedIntermidiateWorldEventInfos.TryGetValue(msgIntermidiateFrameEvent.Frame, out var list))
            {
                list.Add(worldEventInfo);
            }
            else
            {
                list = FutureWorld.WorldEventInfoListPool.Get();
                list.Add(worldEventInfo);
                ReceivedIntermidiateWorldEventInfos.Add(msgIntermidiateFrameEvent.Frame, list);
            }
        }

        Interlocked.Increment(ref _rewindFlag);
    }


    private void DisconnectNetworkManager()
    {
        NetworkManager.OnGameStart -= HandleOnGameStart;
        NetworkManager.OnIntermidiateFrameEvent -= HandleOnIntermidiateFrameEvent;
        NetworkManager.OnFrameEvent -= HandleOnFrameEvent;
        NetworkManager.OnFrameInvalidateHash -= HandleOnFrameInvalidateHash;

        NetworkManager.Dispose();
        NetworkManager = null;
    }

    private void HandleOnFrameEventImmediately(FrameEventType frameEventType)
    {
        var intermidiateFrameEvent = NetworkManager.C2S_IntermidiateFrameEventsPool.Get();
        var frameEvent = NetworkManager.C2S_FrameEventPool.Get();
        frameEvent.EventType = frameEventType;
        frameEvent.BattleTimeMillis = BattleTimeMillis;

        intermidiateFrameEvent.Frame = FutureWorld.NextFrame;
        intermidiateFrameEvent.Event = frameEvent;
        NetworkManager.C2S_INTERMIDIATE_FRAME_EVENT(intermidiateFrameEvent);

        NetworkManager.C2S_FrameEventPool.Release(frameEvent);
        NetworkManager.C2S_IntermidiateFrameEventsPool.Release(intermidiateFrameEvent);

        lock (_futureUpdateLock)
        {
            var worldEventInfo = CreateIntermidiateWorldEventInfo(frameEventType, FutureWorld.NextFrame, PlayerID, BattleTimeMillis);
            FutureWorld.ExecuteWorldEventInfo(worldEventInfo);

            if (LocalWorldEventInfos.TryGetValue(worldEventInfo.TargetFrame, out var list))
            {
                list.Add(worldEventInfo);
            }
            else
            {
                list = FutureWorld.WorldEventInfoListPool.Get();
                list.Add(worldEventInfo);
                LocalWorldEventInfos.Add(worldEventInfo.TargetFrame, list);
            }
        }
    }
}