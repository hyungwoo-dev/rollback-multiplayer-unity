using UnityEngine.Pool;

[ManagedStateIgnore]
public class BattleWorldManager
{
    private static Debug Debug = new(nameof(BattleWorldManager));

    protected BattleWorld LocalWorld { get; private set; }

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
        InputManager.OnUpdate(frame.Time, InputContext);
        LocalWorld.OnUpdate(frame);
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

    protected virtual void OnWorldInputEvent(BattleWorldInputEventType worldInputEventType)
    {
        var reservedFrame = LocalWorld.CurrentFrame + 1;

    }

    private void InitalizeInputManager()
    {
        InputManager.OnInputLeftDash += OnPlayerInputLeftDash;
        InputManager.OnInputRightDash += OnPlayerInputRightDash;
        InputManager.OnInputAttack1 += OnPlayerInputAttack1;
        InputManager.OnInputAttack2 += OnPlayerInputAttack2;
        InputManager.OnInputFire += OnPlayerInputFire;
        InputManager.OnInputJump += OnPlayerInputJump;
    }


    private void OnPlayerInputLeftDash()
    {
        var eventInfo = WorldEventInfoPool.Get();
        eventInfo.WorldInputEventType = BattleWorldInputEventType.LEFT_DASH;
        eventInfo.UnitID = 0;
        eventInfo.TargetFrame = LocalWorld.NextFrame;
        LocalWorld.AddWorldEventInfo(eventInfo);
    }


    private void OnPlayerInputRightDash()
    {
        var eventInfo = WorldEventInfoPool.Get();
        eventInfo.WorldInputEventType = BattleWorldInputEventType.RIGHT_DASH;
        eventInfo.UnitID = 0;
        eventInfo.TargetFrame = LocalWorld.NextFrame;
        LocalWorld.AddWorldEventInfo(eventInfo);
    }

    private void OnPlayerInputAttack1()
    {

    }

    private void OnPlayerInputAttack2()
    {

    }


    private void OnPlayerInputFire()
    {

    }


    private void OnPlayerInputJump()
    {
        var eventInfo = WorldEventInfoPool.Get();
        eventInfo.WorldInputEventType = BattleWorldInputEventType.JUMP;
        eventInfo.UnitID = 0;
        eventInfo.TargetFrame = LocalWorld.NextFrame;
        LocalWorld.AddWorldEventInfo(eventInfo);
    }

    private void DisposeInputManager()
    {
        InputManager.OnInputLeftDash -= OnPlayerInputLeftDash;
        InputManager.OnInputRightDash -= OnPlayerInputRightDash;
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
        WorldEventInfoPool = new ObjectPool<BattleWorldEventInfo>(createFunc: () => new BattleWorldEventInfo(this), defaultCapacity: 8);
    }

    private void DisposePool()
    {
        WorldPool.Dispose();
        WorldEventInfoPool.Dispose();
    }

    #endregion Pool
}
