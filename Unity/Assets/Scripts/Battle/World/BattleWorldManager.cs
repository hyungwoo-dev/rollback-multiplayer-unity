using System;
using System.Collections;
using UnityEngine;

[ManagedStateIgnore]
public class BattleWorldManager
{
    private Debug Debug = new(nameof(BattleWorldManager));
    
    public BattleObjectManager ObjectManager { get; private set; }
    private BattleWorld ClientWorld { get; set; }
    private BattleWorld ServerWorld { get; set; }

    private bool IsStarted { get; set; }
    private float BattleTime { get; set; }

    public BattleWorldManager()
    {
        InitalizeInputManager();

        ObjectManager = new BattleObjectManager(this);
        ClientWorld = ObjectManager.BattleWorldPool.Get();
        ServerWorld = ObjectManager.BattleWorldPool.Get();
    }


    public void Initialize()
    {

    }

    public void AdvanceFrame(BattleFrame frame)
    {

    }

    public void OnUpdate(BattleFrame frame)
    {
        InputManager.OnUpdate(frame.Time, InputContext);
    }

    public void Dispose()
    {
        DisposeInputManager();

        ClientWorld.Release();
        ClientWorld = null;
        ServerWorld.Release();
        ClientWorld = null;

        ObjectManager.Dispose();
    }

    #region Input

    private BattleInputManager InputManager { get; set; }
    private BattleInputContext InputContext { get; set; }

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
}
