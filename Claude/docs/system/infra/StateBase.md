# StateBase — 시스템 관점

> 이 문서는 **AI 에이전트가 코드 레이어에 맞게 이해하고 빠르게 코드를 찾아가도록** 하는 시스템 관점 컨텍스트 문서다.
> 코드가 단일 진실(single source of truth)이다. 이 문서는 코드를 재설명하지 않는다 — **"인덱스 + α"** 에 한정한다.
>
> - **분류**: 인프라 — UR 미매핑 횡단 기반 (열거형 기반 상태 머신을 여러 기능이 공통으로 올라타 사용한다)
> - **이 문서가 다루는 것**: ① 이 대상이 시스템에서 무엇인가(한 줄) ② 주요 추상 ↔ 코드(파일 경로)의 매핑 ③ 무엇이 어디에 있는가(구조 맵) ④ 다른 코드가 가져다 쓸 헬퍼(상속 진입점·IStateListener) ⑤ 코드만 봐선 모르는 의도(+α).
> - **이 문서가 다루지 않는 것**: 상태 전환 알고리즘·메서드 본문 요약. 그건 코드를 보면 된다.

---

## 1. 한 줄 정의

`StateBase` 는 열거형(Enum)으로 정의된 상태의 라이프사이클(Enter → Run → Exit)을 매 프레임 자동으로 구동하고, 상태 변화를 구독자(`IStateListener`)에게 통지할 책임을 지는 제네릭 상태 머신 베이스다.

## 2. 주요 추상 ↔ 코드 매핑

이 인프라는 사용자 도메인 개념에 직접 매핑되지 않는다. 아래는 상태 머신을 구성하는 주요 추상과 그 책임이다. 설명 열은 SR 수준(책임), 위치 열은 네비게이션(경로·타입).

| 주요 추상 | 코드 식별자 | 위치(파일 경로) | 책임 (한 줄) |
|----------|------------|----------------|-------------|
| 상태 머신 베이스 | `StateBaseController<T>` (abstract) | `Assets/Common/Scripts/StateBase/StateBaseController.cs` | 열거형 상태의 라이프사이클을 매 프레임 구동하고 리스너에게 통지한다 |
| 상태 변화 구독 표면 | `IStateListener<T>` | `Assets/Common/Scripts/StateBase/IStateListener.cs` | 상태 진입/실행/종료 콜백 계약을 정의한다 (하나의 클래스가 여러 머신을 구독 가능하도록 제네릭) |
| 상태-애니메이션 연동 베이스 | `StateAnimator<T>` | `Assets/Common/Scripts/StateBase/StateAnimator.cs` | 상태 머신을 구독해 상태별 애니메이션을 자동 전환하고 1루프를 목표 시간에 맞춘다 |
| 전환 조건 확장점 | `CheckStateTransition(T)` (virtual) | `Assets/Common/Scripts/StateBase/StateBaseController.cs` | 파생 클래스가 상태별 전환 규칙을 구현하는 오버라이드 지점 |

> 상태 머신 자체에는 게임 도메인 개념이 없다. 도메인 의미는 파생 클래스의 상태 열거형(예: `ActionStateType`, `PhaseType`)에서 부여된다.

## 3. 구조 맵 (위치 중심)

무엇이 어디에 있는가. 진입점부터.

- **상속 진입점**: `StateBaseController<T>` 를 상속 — `Assets/Common/Scripts/StateBase/StateBaseController.cs`
- **초기 상태 설정**: `StartStateBase(T)` — 라이프사이클 시작 시 1회 호출
- **전환 규칙 구현**: `CheckStateTransition(T)` 오버라이드 — 매 프레임 호출되어 다음 상태를 반환
- **외부 강제 전환**: `RequestStateChange(T)` — 외부에서 상태를 직접 바꿀 때
- **현재 상태 조회**: `CurrentState` (프로퍼티) / `showState` (인스펙터 노출용 SerializeField)
- **구독 표면**: `IStateListener<T>` — `Assets/Common/Scripts/StateBase/IStateListener.cs`
- **애니메이션 연동 베이스**: `StateAnimator<T>` — `Assets/Common/Scripts/StateBase/StateAnimator.cs` (상태별 애니메이션 자동 재생 + `SetTargetDuration` 으로 1루프 시간 맞춤)

### 3.1 제공 헬퍼 (다른 코드가 찾아오는 곳)

다른 AI 에이전트가 "이 기능이 필요할 때 여기"라고 찾아올 대상.

| 헬퍼 | 위치(파일 경로) | 이럴 때 쓴다 |
|------|----------------|-------------|
| `StateBaseController<T>` (상속) | `Assets/Common/Scripts/StateBase/StateBaseController.cs` | 새 열거형 상태 머신이 필요할 때 — 상속 후 `CheckStateTransition` 오버라이드, `StartStateBase` 로 시작 |
| `StartStateBase(T)` | `Assets/Common/Scripts/StateBase/StateBaseController.cs` | 머신의 초기 상태를 1회 설정·진입시킬 때 |
| `RequestStateChange(T)` | `Assets/Common/Scripts/StateBase/StateBaseController.cs` | 전환 조건 밖에서 상태를 강제로 바꿔야 할 때 |
| `RegisterListener` / `UnregisterListener` | `Assets/Common/Scripts/StateBase/StateBaseController.cs` | 상태 변화 콜백을 구독/해제할 때 |
| `IStateListener<T>` (구현) | `Assets/Common/Scripts/StateBase/IStateListener.cs` | 상태 진입/실행/종료에 반응하는 동작(HP 회복·외형 변경 등)을 머신 밖에서 붙일 때 |
| `StateAnimator<T>` (상속) | `Assets/Common/Scripts/StateBase/StateAnimator.cs` | 상태 머신에 애니메이션을 자동 연동하고 클립 속도를 목표 시간에 맞출 때 |

### 3.2 의존 관계

- **밖으로 의존**: `Common.Scripts.SafeIterationList.SafeIterationList<T>` (순회 중 리스너 add/remove 안전 처리), UnityEngine (`MonoBehaviour`, `Animator`, `AnimatorOverrideController`).
- **밖에서 의존**: 유닛 행동·페이즈·라운드·마켓·카메라·UI 등 다수의 컨트롤러가 상속해 사용한다. 대표 소비처 — `Assets/Scenes/Battle/Feature/Unit/ActionState/ActionStateController.cs` (`StateBaseController<ActionStateType>`), `Assets/Scenes/Battle/Feature/Round/RoundManager.cs`, `Assets/Scenes/Battle/Feature/Unit/Animation/UnitAnimator.cs` (`StateAnimator<T>`). `StatusEffect` 인프라도 `Assets/Common/Scripts/StatusEffect/HookProvider/ActionStateHookBinder.cs` 에서 `IStateListener` 로 연동한다.

## 4. +α — 코드만 봐선 모르는 것

- **분할 의도**: 상태 머신(전환·라이프사이클)과 그에 반응하는 동작을 분리했다. 머신은 "지금 어떤 상태인가"만 책임지고, HP 회복·외형 변경·애니메이션 같은 부수 동작은 `IStateListener` 구독자가 머신 밖에서 붙인다 — 한 머신을 여러 관심사가 비침투적으로 확장하기 위함이다.
- **도메인적 의도**: 상태를 열거형으로 강제해 타입 안전성을 얻고, 같은 베이스를 유닛 행동·전투 페이즈·UI 전환 등 도메인이 다른 영역에 공통으로 재사용한다. 도메인 의미는 베이스가 아니라 파생 측 열거형에 산다.
- **비자명한 제약/관례**: `StateAnimator<T>` 는 상태 이름(`Enum.ToString()`)을 애니메이터 스테이트명으로, `placeholder_<상태소문자>` 를 OverrideController 클립 키로 쓰는 네이밍 관례에 의존한다 — 상태 열거형 값과 애니메이션 에셋 명명이 합의돼 있어야 한다. 리스너 순회 중 등록/해제가 가능하도록 `SafeIterationList` 를 쓴다.

## 5. 레이어 링크

- 전역 네비게이션: [index.md](../../index.md)
- UR 미매핑(횡단 기반): [game-system.md](../../ur/game-system.md) §시스템 인프라
