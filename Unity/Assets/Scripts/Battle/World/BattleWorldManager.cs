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

    public virtual void Initialize()
    {
        var worldScene = new BattleWorldScene(this, BattleWorldSceneKind.GRAPHICS);
        worldScene.Initialize();
        LocalWorld.Initialize(worldScene);
    }

    public virtual void OnFixedUpdate(in BattleFrame frame)
    {
        LocalWorld.OnFixedUpdate(frame);
    }

    public virtual void OnUpdate(in BattleFrame frame)
    {
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
        return true;
    }

    #region Input

    private BattleInputManager InputManager { get; set; }
    private BattleInputContext InputContext { get; set; }

    protected virtual void OnWorldInputEvent(BattleWorldInputEventType worldInputEventType)
    {
        var reservedFrame = LocalWorld.CurrentFrame + 1;

    }

    private void InitalizeInputManager()
    {
        InputManager = new BattleInputManager();
        InputContext = new BattleInputContext();
        InputManager.OnInputLeftDash += OnPlayerInputLeftDash;
        InputManager.OnInputRightDash += OnPlayerInputRightDash;
        InputManager.OnInputAttack1 += OnPlayerInputAttack1;
        InputManager.OnInputAttack2 += OnPlayerInputAttack2;
        InputManager.OnInputFire += OnPlayerInputFire;
        InputManager.OnInputJump += OnPlayerInputJump;
    }

    private void OnPlayerInputFire()
    {
        
    }

    private void OnPlayerInputRightDash()
    {
        
    }

    private void OnPlayerInputLeftDash()
    {
        
    }

    private void OnPlayerInputAttack1()
    {

    }

    private void OnPlayerInputAttack2()
    {

    }

    private void OnPlayerInputJump()
    {

    }

    private void DisposeInputManager()
    {
        InputManager.OnInputLeftDash -= OnPlayerInputLeftDash;
        InputManager.OnInputRightDash -= OnPlayerInputRightDash;
        InputManager.OnInputAttack1 -= OnPlayerInputAttack2;
        InputManager.OnInputAttack2 -= OnPlayerInputAttack2;
        InputManager.OnInputFire -= OnPlayerInputFire;
        InputManager.OnInputJump -= OnPlayerInputJump;

        InputManager = null;
        InputContext = null;
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
