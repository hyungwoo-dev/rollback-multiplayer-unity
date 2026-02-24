# rollback-multiplayer-unity

Translated by DeepL (https://www.deepl.com/)

This is a rollback multiplayer project built using the Unity engine.

This project focuses on implementing rollback architecture. There may be exceptions not handled on the server, and the client's gameplay logic is simple.

## Architecture

The server acts as a relay server, transmitting input from each client and forwarding events received from the client side to other players.

The client runs two games simultaneously.

### ServerWorld

This is a game that progresses through events received from the server.

By synchronizing state through deterministic game logic, both players share the same state within the same frame.

### FutureWorld

This game applies player input in real time.

FutureWorld maintains a state where player input is applied instantly in ServerWorld, while also holding a state that predicts the future based on past data.

                              +------------------------+
                              |         Server         |
                              |      (Input Relay)     |
                              +------------------------+
                                     ↑           ↑                     
                                     |           |
        +-----------------------------+         +-----------------------------+
        |           Client1           |         |           Client2           |
        |                             |         |                             |
        |  +-----------------------+  |         |  +-----------------------+  |
        |  |      ServerWorld      |  |         |  |      ServerWorld      |  |
        |  | (Deterministic State) |  |         |  | (Deterministic State) |  |
        |  +-----------------------+  |         |  +-----------------------+  |
        |              ↑              |         |              ↑              |
        |  +-----------------------+  |         |  +-----------------------+  |
        |  |      FutureWorld      |  |         |  |      FutureWorld      |  |
        |  | (Predicted State)     |  |         |  | (Predicted State)     |  |
        |  +-----------------------+  |         |  +-----------------------+  |
        +-----------------------------+         +-----------------------------+
        
## Rollback
Each client's ServerWorld receives events from the server and progresses deterministically, with each player maintaining the same state within the same frame.

However, the prediction-based FutureWorld experiences inconsistencies when the opponent's input events are transmitted. This is because the event conflicts with both the past state of the FutureWorld and the current state of the ServerWorld.

To resolve these inconsistencies, FutureWorld copies the ServerWorld state at the point where the inconsistency occurred. It then applies the player's input events to this copied ServerWorld state to synchronize it, subsequently generating a new predicted state.

```
Time →
Frame:   F10    F11    F12    F13    F14

ServerWorld:
          S10 -> S11 -> S12 -> S13

FutureWorld:
          S10 -> S11 -> S12 -> P13 -> P14
                              ↑
                       (Local input applied immediately)

Opponent Input Received (At F12)
            ↓
FutureWorld Rollback:
 1. ServerWorld S12 Copy
 2. Local Input Reapply
 3. Recalculate P13 and P14 again
```

##  Rendering Interpolation

FutureWorld is actually one frame ahead of what the player sees on screen.

This is to interpolate the state between the fixed frame update loop and the render frame update loop.

Therefore, what the player actually sees on screen is a frame interpolated from the previous frame's state in FutureWorld.

```
Frame: F12 -> F13
┌──────────────────────────┐
│        Engine Loop       │
└──────────────────────────┘
            │
            ▼
┌──────────────────────────────────────────────┐
│  FixedUpdate Loop                            │
│                                              │
│    FutureWorld.ApplyTo(LocalWorldScene)      │
│    FutureWorld.ApplyEvents()                 │
│    FutureWorld.AdvanceFrame()                │
│  [F12] → [F13]                               │
└──────────────────────────────────────────────┘
            │
            ▼
┌──────────────────────────────────────────────┐
│  Update Loop                                 │
│                                              │
│    Interpolate(                              │
│      PreviousState[F12],                     │
│      CurrentState[F13],                      │
│      t (InterpolatingTime / FixedDeltaTime)  │
│    )                                         │
│  [F12] ~ [F13]                               │
└──────────────────────────────────────────────┘
```

## Delay Compensation

Gameplay speed may vary depending on the player's network environment or device specifications.

When gameplay speeds differ, the system adjusts the speed of the faster player to compensate for the gap between the two players.

Below is a portion of the game speed compensation code.

```
    private float AdjustSimulationSpeed(int frameDrift)
    {
        ...

        if (frameDrift <= SLOW_LEVEL1_FRAME_THRESHOLD)
        {
            return 1.0f;
        }
        else if (frameDrift >= LOCK_FRAME_THRESHOLD)
        {
            return 0.0f;
        }
        else
        {
            if (frameDrift <= SLOW_LEVEL2_FRAME_THRESHOLD)
            {
                return SLOW_LEVEL1_TIME_SCALE;
            }
            else
            {
                var t = (frameDrift - SLOW_LEVEL2_FRAME_THRESHOLD) / (float)(LOCK_FRAME_THRESHOLD - SLOW_LEVEL2_FRAME_THRESHOLD);
                var timeScale = Mathf.Lerp(SLOW_LEVEL1_TIME_SCALE, 0.0f, t);
                return timeScale;
            }
        }
    }
```

## Parallelism

The BattleWorld class is a pure logic object that does not depend on Unity.

The game progression cycle of ServerWorld is independent of Unity's Update/FixedUpdate routines.
Therefore, it runs the game in the background by creating a separate thread.

This structure is designed to perform stable simulations unaffected by render frame fluctuations.
It also serves as an optimization strategy to enhance input responsiveness and leverage multi-core environments.

## Others

### Mathematics Library

Floating-point results may vary depending on the hardware environment.

To address this, we utilized the deterministic fixed-point math library *FixedMathSharp-Unity*.

https://github.com/mrdav30/FixedMathSharp-Unity

### Root Motion

To deterministically calculate the position and rotation values of Unity engine's root motion, separate animation data was created.

Animation progresses according to the FixedDeltaTime cycle, and the amount of movement and rotation per frame is converted into data to implement movement within the game logic.

### Build execution

The Builds/ folder in the repository contains the Server and Unity build programs. (Windows environment)

You can run Builds/Server/GameServer.exe first, then run the two Builds/Unity/ActionGame.exe programs to test.

### Unity Test

For single-player mode, run it in the Scenes/BattleTest.unity scene.

Starting from the Scenes/BattleStart.unity scene will attempt to connect to the server.

This can be run alongside the build program for debugging and testing purposes.
