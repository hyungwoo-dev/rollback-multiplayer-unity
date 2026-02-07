using UnityEngine;

[ManagedStateIgnore]
public class MultiplayBattleWorldManager : BattleWorldManager
{
    private static Debug Debug = new(nameof(MultiplayBattleWorldManager));

    private BattleWorld ServerWorld { get; set; }
    private NetworkManager NetworkManager { get; set; }

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
    }

    public override void Initialize(BattleCamera camera)
    {
        base.Initialize(camera);
        ServerWorld.Initialize();
    }

    public override bool IsReady()
    {
        return base.IsReady() && ServerWorld.IsReady();
    }

    public override void AdvanceFrame(in BattleFrame frame)
    {
        base.AdvanceFrame(frame);
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

        base.Dispose();
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

    }

    private void HandleOnFrameEvent(S2C_MSG_FRAME_EVENT msgFrameEvent)
    {

    }

    private void HandleOnFrameInvalidateHash(S2C_MSG_INVALIDATE_HASH msgInvalidateHash)
    {

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
