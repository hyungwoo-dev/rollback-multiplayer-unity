using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
using ThreadPriority = System.Threading.ThreadPriority;

[ManagedStateIgnore]
public class MultiplayBattleWorldManager : BaseWorldManager
{
    private static Debug Debug = new(nameof(MultiplayBattleWorldManager));

    private object _serverUpdateLock = new();
    private BattleWorld ServerWorld { get; set; }
    private Dictionary<int, int> PlayerHashes = new();
    private Dictionary<int, int> OpponentPlayerHashes = new();

    private int LastCheckedServerWorldFrame { get; set; }
    private int LastHashCheckedServerWorldFrame { get; set; }

    private NetworkManager NetworkManager { get; set; }

    private object _futureUpdateLock = new();
    private Dictionary<int, List<BattleWorldEventInfo>> ReceivedIntermidiateWorldEventInfos { get; set; } = new();
    private Dictionary<int, List<BattleWorldEventInfo>> ReceivedServerWorldEventInfos { get; set; } = new();

    public long? PlayerGameStartUnixTimeMillis { get; private set; } = null;
    public int? PlayerRandomValue { get; private set; } = null;
    public long? OpponentPlayerGameStartUnixTimeMillis { get; private set; } = null;
    public int? OpponentPlayerRandomValue { get; private set; } = null;

    public long? GameStartUnixTimeMillis
    {
        get
        {
            if (PlayerGameStartUnixTimeMillis == null || OpponentPlayerGameStartUnixTimeMillis == null)
            {
                return null;
            }

            return MathUtils.Max(PlayerGameStartUnixTimeMillis.Value, OpponentPlayerGameStartUnixTimeMillis.Value) + 1000;
        }
    }
    public override int BattleTimeMillis => (int)(TimeUtils.UtcNowUnixTimeMillis - GameStartUnixTimeMillis);
    public int OpponentPlayerID { get; private set; } = -1;

    private long _rewindFlag;
    private long _disposeFlag;
    private long _serverWorldRunningFlag;

    private List<BattleWorldEventInfo> _serverWorldEventInfos = new();

    public MultiplayBattleWorldManager() : base()
    {
        ServerWorld = WorldPool.Get();
    }

    public override void Setup()
    {
        base.Setup();
        NetworkManager = CreateNetworkManager();
        NetworkManager.Connect("localhost", 50010);
    }

    public override bool IsSetupCompleted()
    {
        return base.IsSetupCompleted() &&
            NetworkManager.State == LiteNetState.P2P_CONNECTED;
    }

    public override void OnSetupCompleted()
    {
        base.OnSetupCompleted();
        var nowUnixTimeMillis = TimeUtils.UtcNowUnixTimeMillis;
        var randomValue = Random.Range(int.MinValue, int.MaxValue);
        var message = new P2P_ENTER_WORLD()
        {
            EnterUnixTimeMillis = nowUnixTimeMillis,
            RandomValue = randomValue,
        };
        PlayerGameStartUnixTimeMillis = nowUnixTimeMillis;
        NetworkManager.SendEnterWorld(ref message);
    }

    public override void Initialize(in BattleFrame frame)
    {
        base.Initialize(frame);

        var thread = new Thread(static state =>
        {
            var parameters = state as object[];
            var worldManager = parameters[0] as MultiplayBattleWorldManager;
            var fixedFrame = (BattleFrame)parameters[1];
            Interlocked.Increment(ref worldManager._serverWorldRunningFlag);

            worldManager.ServerWorld.Initialize();
            while (Interlocked.Read(ref worldManager._disposeFlag) == 0)
            {
                worldManager.NetworkManager.PollEvents();

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
            GameStartUnixTimeMillis != null &&
            TimeUtils.UtcNowUnixTimeMillis > GameStartUnixTimeMillis;
    }

    public override void OnStart()
    {
        base.OnStart();
        PlayerID = GetPlayerID();
        OpponentPlayerID = PlayerID == 0 ? 1 : 0;

        int GetPlayerID()
        {
            if (PlayerGameStartUnixTimeMillis == OpponentPlayerGameStartUnixTimeMillis)
            {
                return PlayerRandomValue < OpponentPlayerRandomValue ? 0 : 1;
            }

            return PlayerGameStartUnixTimeMillis < OpponentPlayerGameStartUnixTimeMillis ? 0 : 1;
        }
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

                    // 롤백을 시도할 때는 두 월드를 모두 락 걸고 시도해야 한다.
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

            while (PlayerHashes.TryGetValue(LastHashCheckedServerWorldFrame, out var playerHash) &&
                OpponentPlayerHashes.TryGetValue(LastHashCheckedServerWorldFrame, out var opponentPlayerHash))
            {
                if (playerHash != opponentPlayerHash)
                {
                    Debug.LogError($"InvalidHash - Frame: {LastHashCheckedServerWorldFrame}, PlayerHash: {playerHash}, OpponentPlayerHash: {opponentPlayerHash}");
                }

                PlayerHashes.Remove(LastHashCheckedServerWorldFrame);
                OpponentPlayerHashes.Remove(LastHashCheckedServerWorldFrame);
                LastHashCheckedServerWorldFrame += 1;
            }

            base.AdvanceFrame(frame);

            SendInputEvents(FutureWorld.CurrentFrame);
        }
    }

    private bool AdvanceServerWorld(BattleFrame frame)
    {
        var rewind = false;

        while (ReceivedServerWorldEventInfos.TryGetValue(ServerWorld.NextFrame, out var serverWorldEventInfos) && FutureWorld.NextFrame > ServerWorld.NextFrame)
        {
            var serverWorldNextFrame = ServerWorld.NextFrame;

            _serverWorldEventInfos.AddRange(serverWorldEventInfos);
            ReceivedServerWorldEventInfos.Remove(serverWorldNextFrame);

            if (LocalWorldEventInfos.TryGetValue(ServerWorld.NextFrame, out var localWorldEventInfos))
            {
                _serverWorldEventInfos.AddRange(localWorldEventInfos);
            }

            _serverWorldEventInfos.Sort((lhs, rhs) =>
            {
                var compare = lhs.BattleTimeMillis.CompareTo(rhs.BattleTimeMillis);
                return compare != 0 ? compare : lhs.UnitID.CompareTo(rhs.UnitID);
            });

            ServerWorld.ExecuteWorldEventInfos(_serverWorldEventInfos);
            ServerWorld.AdvanceFrame(frame);

            foreach (var serverWorldEventInfo in serverWorldEventInfos)
            {
                if (serverWorldEventInfo.WorldInputEventType != BattleWorldInputEventType.NONE)
                {
                    rewind = true;
                    break;
                }
            }

            _serverWorldEventInfos.Clear();
            ServerWorld.ReleaseWorldEventInfos(serverWorldEventInfos);

            var serverCurrentFrame = ServerWorld.CurrentFrame;
            var hash = ServerWorld.GetWorldHash();
            var message = new P2P_FRAME_HASH()
            {
                Frame = serverCurrentFrame,
                Hash = hash
            };

            NetworkManager.SendFrameHash(ref message);

            PlayerHashes.Add(serverCurrentFrame, hash);
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
            var futureWorldNextFrame = FutureWorld.NextFrame;

            var list = FutureWorld.WorldEventInfoListPool.Get();

            if (LocalWorldEventInfos.TryGetValue(futureWorldNextFrame, out var localWorldEventInfo))
            {
                list.AddRange(localWorldEventInfo);
            }
            if (ReceivedIntermidiateWorldEventInfos.TryGetValue(futureWorldNextFrame, out var intermidiateWorldEventInfo))
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

            FutureWorld.ExecuteWorldEventInfos(list);
            FutureWorld.AdvanceFrame(frame);
            FutureWorld.WorldEventInfoListPool.Release(list);
        }
    }

    private void SendInputEvents(int frame)
    {
        using var _ = ListPool<P2P_FRAME_EVENT>.Get(out var serverFrameEvents);
        var frameEvents = new P2P_FRAME_EVENTS();

        if (LocalWorldEventInfos.TryGetValue(frame, out var worldEventInfos))
        {
            foreach (var worldEventInfo in worldEventInfos)
            {
                var frameEventType = WorldEventToFrameEventType(worldEventInfo.WorldInputEventType);
                var msgFrameEvent = new P2P_FRAME_EVENT();
                msgFrameEvent.EventType = frameEventType;
                msgFrameEvent.BattleTimeMillis = BattleTimeMillis;
                serverFrameEvents.Add(msgFrameEvent);
            }

            frameEvents.Frame = frame;
            frameEvents.Events = serverFrameEvents;
        }
        else
        {
            var msgFrameEvent = new P2P_FRAME_EVENT();
            msgFrameEvent.EventType = FrameEventType.NONE;
            serverFrameEvents.Add(msgFrameEvent);

            frameEvents.Frame = frame;
            frameEvents.Events = serverFrameEvents;
        }

        NetworkManager.SendFrameEvents(ref frameEvents);
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
        networkManager.OnEnterWorld += HandleOnGameStart;
        networkManager.OnFrameEvents += HandleOnFrameEvents;
        networkManager.OnIntermidiateFrameEvent += HandleOnIntermidiateFrameEvent;
        networkManager.OnFrameHash += HandleOnFrameHash;
        return networkManager;
    }

    private void HandleOnGameStart(in P2P_ENTER_WORLD message)
    {
        OpponentPlayerGameStartUnixTimeMillis = message.EnterUnixTimeMillis;
    }

    private void HandleOnFrameEvents(in P2P_FRAME_EVENTS message)
    {
        if (ReceivedServerWorldEventInfos.ContainsKey(message.Frame))
        {
            return;
        }

        var worldEventInfos = ServerWorld.WorldEventInfoListPool.Get();
        foreach (var frameEvent in message.Events)
        {
            var worldEventInfo = CreateServerWorldEventInfo(message.Frame, frameEvent);
            worldEventInfos.Add(worldEventInfo);
        }

        ReceivedServerWorldEventInfos.Add(message.Frame, worldEventInfos);
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

    private BattleWorldEventInfo CreateServerWorldEventInfo(int frame, in P2P_FRAME_EVENT frameEvent)
    {
        var worldEventType = FrameEventTypeToWorldEventType(frameEvent.EventType);
        var worldEventInfo = ServerWorld.WorldEventInfoPool.Get();
        worldEventInfo.TargetFrame = frame;
        worldEventInfo.WorldInputEventType = worldEventType;
        worldEventInfo.UnitID = OpponentPlayerID;
        worldEventInfo.BattleTimeMillis = frameEvent.BattleTimeMillis;
        return worldEventInfo;
    }

    private void HandleOnFrameHash(in P2P_FRAME_HASH message)
    {
        OpponentPlayerHashes.Add(message.Frame, message.Hash);
    }

    private void HandleOnIntermidiateFrameEvent(in P2P_INTERMIDIATE_FRAME_EVENT message)
    {
        lock (_futureUpdateLock)
        {
            lock (_serverUpdateLock)
            {
                if (message.Frame < ServerWorld.NextFrame)
                {
                    return;
                }
            }

            var worldInputEventType = FrameEventTypeToWorldEventType(message.FrameEvent.EventType);
            var worldEventInfo = CreateIntermidiateWorldEventInfo(
                worldInputEventType,
                message.Frame,
                OpponentPlayerID,
                message.FrameEvent.BattleTimeMillis
            );

            if (ReceivedIntermidiateWorldEventInfos.TryGetValue(message.Frame, out var list))
            {
                list.Add(worldEventInfo);
            }
            else
            {
                list = FutureWorld.WorldEventInfoListPool.Get();
                list.Add(worldEventInfo);
                ReceivedIntermidiateWorldEventInfos.Add(message.Frame, list);
            }
        }

        Interlocked.Increment(ref _rewindFlag);
    }


    private void DisconnectNetworkManager()
    {
        NetworkManager.OnEnterWorld -= HandleOnGameStart;
        NetworkManager.OnIntermidiateFrameEvent -= HandleOnIntermidiateFrameEvent;
        NetworkManager.OnFrameEvents -= HandleOnFrameEvents;
        NetworkManager.OnFrameHash -= HandleOnFrameHash;

        NetworkManager.Dispose();
        NetworkManager = null;
    }

    protected override void HandleOnFrameEventImmediately(BattleWorldInputEventType worldInputEventType)
    {
        var frameEventType = WorldEventToFrameEventType(worldInputEventType);
        var intermidiateFrameEvent = new P2P_INTERMIDIATE_FRAME_EVENT();
        var frameEvent = new P2P_FRAME_EVENT();
        frameEvent.EventType = frameEventType;
        frameEvent.BattleTimeMillis = BattleTimeMillis;

        intermidiateFrameEvent.Frame = FutureWorld.NextFrame;
        intermidiateFrameEvent.FrameEvent = frameEvent;
        NetworkManager.SendIntermidiateFrameEvent(ref intermidiateFrameEvent);

        lock (_futureUpdateLock)
        {
            base.HandleOnFrameEventImmediately(worldInputEventType);
        }
    }
}