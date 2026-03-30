# rollback-multiplayer-unity

Unity 엔진으로 제작한 롤백 멀티플레이어 프로젝트입니다.

롤백 아키텍처 구현에 집중한 프로젝트로, 서버에서 처리되지 않은 예외 처리가 있을 수 있고, 클라이언트의 게임 플레이 로직은 단순합니다.

## 아키텍처

서버는 각 클라이언트의 인풋을 전달하는 릴레이 서버이고, 클라이언트 측에서 전달받은 이벤트를 다른 플레이어에게 전달하는 역할입니다.

클라이언트는 두 개의 게임을 동시에 진행시킵니다.

### ServerWorld

서버로부터 받은 이벤트를 통해 진행시키는 게임입니다.

결정론적인 게임 로직을 통해 상태를 동기화함으로써, 두 플레이어는 같은 프레임에 같은 상태를 공유합니다.

### FutureWorld

플레이어의 인풋을 즉시 반영한 게임입니다.

FutureWorld는 ServerWorld 상태에 플레이어 입력을 즉시 반영한 상태로, 과거 데이터를 기반으로 미래를 예측한 상태를 가지고 있습니다.

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
        
## 롤백
각 클라이언트의 ServerWorld는 서버로부터 이벤트를 전달받아 결정론적으로 진행하기 때문에, 프레임 별로 같은 상태를 가집니다.

하지만 예측이 반영된 FutureWorld는 상대방의 인풋 이벤트가 전달되면 FutureWolrd의 과거 상태와 ServerWorld의 상태에 불일치가 발생하게 됩니다.

FutureWorld는 이런 불일치를 해결하기 위해 불일치가 발생한 시점의 ServerWorld 상태를 복사하고, 복사된 ServerWorld의 상태로부터 플레이어의 인풋 이벤트를 반영하여 동기화를 맞춘 상태에서 다시 예측된 상태를 만듭니다

```
시간 →
Frame:   F10    F11    F12    F13    F14

ServerWorld:
          S10 -> S11 -> S12 -> S13

FutureWorld:
          S10 -> S11 -> S12 -> P13 -> P14
                              ↑
                       (Local Input 즉시 반영)

상대 Input 도착 (F12 시점)
            ↓
FutureWorld 롤백:
 1. ServerWorld S12 복사
 2. Local Input 재적용
 3. 다시 P13, P14 재계산
```

## 렌더링 보간

FutureWorld는 실제로 플레이어가 보는 화면보다 한 프레임 더 진행되어 있습니다.

이는 고정 프레임 업데이트 루프와 렌더 프레임 업데이트 루프 간 상태를 보간하기 위함입니다.

따라서, 실제로 플레이어가 보는 화면은 한 프레임 이전 상태로부터 FutureWorld 상태를 보간한 화면입니다.

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

## P2P 홀펀칭

상대방과 통신할 때, 최소한의 지연으로 통신하기 위해 P2P 홀펀칭을 선택했습니다.

호스트 없는 P2P 통신으로, 플레이어는 각자 자신의 정보를 상대방에게 전달하고 상태를 결정론적으로 동기화합니다.

서버 라이브러리는 LiteNetLib을 사용했습니다.

https://github.com/RevenantX/LiteNetLib

## 지연 보정

플레이어는 네트워크 환경이나 기기 스펙에 따라 게임 진행 속도에 차이가 날 수 있습니다.

게임 진행에 차이가 나는 경우, 더 앞선 플레이어의 진행 속도를 조절하는 방식으로 두 플레이어 간 진행 격차를 보정합니다.

아래는 게임 속도 보정 코드의 일부입니다.

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

## 병렬성

BattleWorld 클래스는 Unity에 의존하지 않는 순수 로직 객체입니다.

ServerWorld의 게임 진행 주기는 Unity의 Update/FixedUpdate 루틴과 무관합니다.
따라서 별도의 스레드를 생성하여 백그라운드에서 게임을 진행합니다.

이 구조는 렌더 프레임 변동의 영향을 받지 않고 안정적으로 시뮬레이션을 수행하기 위한 설계입니다.
또한 입력 반응성을 높이고, 멀티 코어 환경을 활용하기 위한 최적화 전략이기도 합니다.

## 인풋 스레드

유니티의 인풋 시스템을 통해 유저의 입력을 확인하게 되면 메인 스레드의 로직 업데이트 타이밍에 입력을 확인하게 됩니다.

더 빠른 입력 전송을 위해 인풋 스레드를 생성해서 로직 업데이트 루틴과 상관없이 입력을 서버로 전송할 수 있도록 구현했습니다.

## 그 외

### 수학 라이브러리

부동소수점은 하드웨어 환경에 따라 다른 결과를 낼 수 있습니다.

이를 해결하기 위해 결정론적 고정소수점 수학 라이브러리 *FixedMathSharp-Unity* 를 활용했습니다.

https://github.com/mrdav30/FixedMathSharp-Unity

### 루트 모션

Unity 엔진의 루트 모션의 위치, 회전 값을 결정론적으로 계산하기 위해 별도의 애니메이션 데이터를 제작했습니다.

FixedDeltaTime 주기에 맞춰 애니메이션을 진행하고, 진행한 프레임의 이동량과 회전량을 데이터로 전환하여 게임 로직에서 움직임을 수행하도록 구현했습니다.

### 빌드 실행

저장소 Builds/ 폴더에 Server와 Unity 빌드 프로그램이 포함되어 있습니다. (Windows 환경)

Builds/Server/HolePunchServer.exe를 먼저 실행하고, 두 개의 Builds/Unity/ActionGame.exe 프로그램을 실행해서 테스트할 수 있습니다.

서버 프로그램과 클라이언트 프로그램 실행 시, 네트워크 허용 팝업에서 허용을 선택하세요.

### Unity 테스트

싱글 플레이의 경우, Scenes/BattleTest.unity 씬에서 실행합니다.

Scenes/BattleStart.unity 씬에서 시작하면 서버에 연결을 시도합니다.

이는 디버깅 및 테스트를 위해 빌드 프로그램과 함께 실행할 수 있습니다.
