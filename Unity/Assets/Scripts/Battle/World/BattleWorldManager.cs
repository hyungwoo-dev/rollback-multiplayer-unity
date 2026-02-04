using UnityEngine;
using UnityEngine.Pool;

[ManagedStateIgnore]
public class BattleWorldManager
{
    private static Debug Debug = new(nameof(BattleWorldManager));

    public BattleWorld LocalWorld { get; private set; }
    protected int PlayerID { get; private set; } = 0;

    public BattleWorldManager()
    {
        InitalizeInputManager();
        InitializePool();
        LocalWorld = WorldPool.Get();
    }

    public virtual void Prepare()
    {
        var worldScene = new BattleWorldScene(this, BattleWorldSceneKind.GRAPHICS);
        worldScene.Prepare();
        LocalWorld.Prepare(worldScene);
    }

    public virtual void Initialize()
    {
        LocalWorld.Initialize();
    }

    public virtual void OnFixedUpdate(in BattleFrame frame)
    {
        LocalWorld.OnFixedUpdate(frame);
    }

    public virtual void OnUpdate(in BattleFrame frame)
    {
        InputManager.OnUpdate(InputContext);
        LocalWorld.Interpolate(frame);
    }

    public virtual void Dispose()
    {
        DisposeInputManager();

        LocalWorld.Release();
        LocalWorld = null;

        DisposePool();
    }

    public virtual bool IsReady()
    {
        return LocalWorld.IsReady();
    }

    #region Input

    private BattleInputManager InputManager { get; } = new BattleInputManager();
    private BattleInputContext InputContext { get; } = new BattleInputContext();

    private void InitalizeInputManager()
    {
        InputManager.OnInputMoveBackDown += OnPlayerInputMoveBackDown;
        InputManager.OnInputMoveBackUp += OnPlayerInputMoveBackUp;
        InputManager.OnInputMoveForwardDown += OnPlayerInputMoveForwardDown;
        InputManager.OnInputMoveForwardUp += OnPlayerInputMoveForwardUp;
        InputManager.OnInputAttack1 += OnPlayerInputAttack1;
        InputManager.OnInputAttack2 += OnPlayerInputAttack2;
        InputManager.OnInputFire += OnPlayerInputFire;
        InputManager.OnInputJump += OnPlayerInputJump;
    }

    private void OnPlayerInputMoveForwardDown()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.MOVE_FORWARD_DOWN, PlayerID);
    }

    private void OnPlayerInputMoveForwardUp()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.MOVE_FORWARD_UP, PlayerID);
    }

    private void OnPlayerInputMoveBackDown()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.MOVE_BACK_DOWN, PlayerID);
    }

    private void OnPlayerInputMoveBackUp()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.MOVE_BACK_UP, PlayerID);
    }

    private void OnPlayerInputAttack1()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.ATTACK1, PlayerID);
    }

    private void OnPlayerInputAttack2()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.ATTACK2, PlayerID);
    }

    private void OnPlayerInputFire()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.FIRE, PlayerID);
    }

    private void OnPlayerInputJump()
    {
        PerformWorldEventInfo(BattleWorldInputEventType.JUMP, PlayerID);
    }

    protected virtual BattleWorldEventInfo PerformWorldEventInfo(BattleWorldInputEventType inputEventType, int unitId)
    {
        var eventInfo = WorldEventInfoPool.Get();
        eventInfo.WorldInputEventType = inputEventType;
        eventInfo.UnitID = 0;
        eventInfo.TargetFrame = LocalWorld.NextFrame;
        LocalWorld.AddWorldEventInfo(eventInfo);
        return eventInfo;
    }

    private void DisposeInputManager()
    {
        InputManager.OnInputMoveBackDown -= OnPlayerInputMoveBackDown;
        InputManager.OnInputMoveBackUp -= OnPlayerInputMoveBackUp;
        InputManager.OnInputMoveForwardDown -= OnPlayerInputMoveForwardDown;
        InputManager.OnInputMoveForwardUp -= OnPlayerInputMoveForwardUp;
        InputManager.OnInputAttack1 -= OnPlayerInputAttack2;
        InputManager.OnInputAttack2 -= OnPlayerInputAttack2;
        InputManager.OnInputFire -= OnPlayerInputFire;
        InputManager.OnInputJump -= OnPlayerInputJump;
        InputManager.Dispose();
    }

    #endregion Input

    #region Pool

    public ObjectPool<BattleWorld> WorldPool { get; private set; }
    public ObjectPool<BattleWorldEventInfo> WorldEventInfoPool { get; private set; }

    private void InitializePool()
    {
        WorldPool = new ObjectPool<BattleWorld>(createFunc: () => new BattleWorld(this), defaultCapacity: 2);
        WorldEventInfoPool = new ObjectPool<BattleWorldEventInfo>(createFunc: () => new BattleWorldEventInfo(this), defaultCapacity: 32);
    }

    private void DisposePool()
    {
        WorldPool.Dispose();
        WorldEventInfoPool.Dispose();
    }

    #endregion Pool
}
