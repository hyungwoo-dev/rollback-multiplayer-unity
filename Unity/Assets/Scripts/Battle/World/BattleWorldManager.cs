using System;
using System.Collections;
using UnityEngine;

[ManagedStateIgnore]
public class BattleWorldManager : MonoBehaviour
{
    private Debug Debug = new(nameof(BattleWorldManager));
    
    public BattleObjectManager ObjectManager { get; private set; }
    private BattleWorld ClientWorld { get; set; }
    private BattleWorld ServerWorld { get; set; }

    private bool IsStarted { get; set; }
    private float BattleTime { get; set; }

    private void Awake()
    {
        InitalizeInputManager();

        ObjectManager = new BattleObjectManager(this);
        ClientWorld = ObjectManager.BattleWorldPool.Get();
        ServerWorld = ObjectManager.BattleWorldPool.Get();
    }

    private IEnumerator Start()
    {
        yield return new WaitForFixedUpdate();
        IsStarted = true;

        Debug.Log($"Start - FixedTime: {Time.fixedTime}");
    }

    private void Update()
    {
        InputManager.OnUpdate(Time.unscaledTime, InputContext); 
    }

    private void FixedUpdate()
    {
        if (!IsStarted) return;
        Debug.Log($"FixedUpdate - FixedTime: {Time.fixedTime}");
    }

    private void OnDestroy()
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
        InputManager.OnInputAttack1 += OnPlayerInputAttack2;
        InputManager.OnInputAttack2 += OnPlayerInputAttack2;
        InputManager.OnInputFire += OnPlayerInputFire;
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

    private void DisposeInputManager()
    {
        InputManager.OnInputLeftDash -= OnPlayerInputLeftDash;
        InputManager.OnInputRightDash -= OnPlayerInputRightDash;
        InputManager.OnInputAttack1 -= OnPlayerInputAttack2;
        InputManager.OnInputAttack2 -= OnPlayerInputAttack2;
        InputManager.OnInputFire -= OnPlayerInputFire;

        InputManager = null;
        InputContext = null;
    }

    #endregion Input
}
