# StatusEffect — 시스템 관점

> 이 문서는 AI 에이전트가 코드 레이어에 맞게 이해하고 빠르게 코드를 찾아가도록 하는 시스템 관점 컨텍스트 문서다.
> 코드가 단일 진실이다. 이 문서는 코드를 재설명하지 않는다 — "인덱스 + α"에 한정한다.
>
> - **분류**: 인프라 — UR 미매핑 횡단 기반 (다수 시너지·유닛이 공통으로 사용하는 상태이상/효과 프레임워크). 본래 클래스 단위라 일반 SR 에는 안 들어갈 내용이나, 중요 공통 기반이라 별도 문서화한다.

## 1. 한 줄 정의

유닛에 부여되는 상태이상/효과(SE)를 표현·관리하고, 유닛 유형마다 다른 게임 이벤트(행동 상태 변화·공격 적중 등)를 관심 있는 SE에 전달하는 플러그인형 상태이상 프레임워크.

## 2. 도메인 ↔ 코드 매핑

| 도메인 개념 | 코드 식별자 | 위치(파일 경로) | 책임 (한 줄) |
|------------|------------|----------------|-------------|
| 상태이상/효과 한 개 | `StatusEffect` (abstract) | `Assets/Common/Scripts/StatusEffect/StatusEffect.cs` | SE 한 개의 생명주기 훅과 만료 상태를 가진 추상 단위 |
| 효과 메타데이터(ID·이름·아이콘) | `StatusEffectDefinitionData` | `Assets/Common/Data/StatusEffects/` (외부 모듈) | SE의 정적 정의. 본 모듈은 참조만 |
| 유닛이 가진 효과 묶음·적용/해제 | `StatusEffectController` | `Assets/Common/Scripts/StatusEffect/StatusEffectController.cs` | 유닛에 붙어 SE 컬렉션과 생명주기를 관리하는 MonoBehaviour |
| 효과 부여 시 전달되는 상황 데이터 | `StatusEffectContext` | `Assets/Common/Scripts/StatusEffect/StatusEffectContext.cs` | 게임별 Context가 상속해 데이터를 추가하는 빈 베이스 |
| "이 효과는 ~할 때 반응한다"의 ~ (반응 지점) | `IStatusEffectHook` 파생 인터페이스 | `Assets/Common/Scripts/StatusEffect/HookProvider/` | SE가 구현하면 특정 게임 이벤트를 수신 |

## 3. 구조 맵 (위치 중심)

진입점부터.

- **효과 정의**: `StatusEffect` 를 상속해 `OnApply/OnUpdate/OnRemove` 오버라이드 — `StatusEffect.cs`
- **효과 부여/해제**: `StatusEffectController.Apply` / `RemoveImmediate` — `StatusEffectController.cs`
- **반응 지점 추가**: `HookProvider/` 의 훅 인터페이스 + Provider + Binder 3종 세트
  - 마커: `IStatusEffectHook` — `HookProvider/IStatusEffectHook.cs`
  - 플러그인 계약: `IStatusEffectHookProvider` — `HookProvider/IStatusEffectHookProvider.cs`
  - 캐싱 베이스: `StatusEffectHookProvider<T>` — `HookProvider/StatusEffectHookProvider.cs`
  - 구체 반응 지점(행동 상태): `IOnActionStateChangedHook` / `OnActionStateChangedHookProvider` / `ActionStateHookBinder` — `HookProvider/ActionStateHookBinder.cs`
  - 구체 반응 지점(공격 적중): `IOnAttackHitHook` / `OnAttackHitHookProvider` / `AttackHitHookBinder` — `HookProvider/AttackHitHookBinder.cs`

### 3.1 제공 헬퍼 (다른 코드가 찾아오는 곳)

| 헬퍼 | 위치(파일 경로) | 이럴 때 쓴다 |
|------|----------------|-------------|
| `StatusEffect` (추상 베이스) | `Assets/Common/Scripts/StatusEffect/StatusEffect.cs` | 새 상태이상/효과를 만들 때 상속 |
| `StatusEffectController` | `Assets/Common/Scripts/StatusEffect/StatusEffectController.cs` | 유닛류 MonoBehaviour에 효과 관리 기능을 붙일 때 상속 |
| `StatusEffectHookProvider<T>` | `Assets/Common/Scripts/StatusEffect/HookProvider/StatusEffectHookProvider.cs` | 새로운 게임 이벤트를 효과에 연결하는 반응 지점을 추가할 때 상속 |
| `IStatusEffectHook` | `Assets/Common/Scripts/StatusEffect/HookProvider/IStatusEffectHook.cs` | 새 훅 인터페이스를 정의할 때 마커로 상속 |

### 3.2 의존 관계

- **밖으로 의존**: `Common.Data.StatusEffects`(`StatusEffectDefinitionData`), `Common.Scripts.SafeIterationList`(순회 중 안전 수정), `Common.Scripts.StateBase`(`IStateListener<T>`), `Scenes.Battle.Feature.Units.ActionStates`(`ActionStateController`·`ActionStateType`), `Scenes.Battle.Feature.Units.Attackers`/`Attackables`(`Attacker`·`AttackContext`)
- **밖에서 의존**: `Unit`(`Assets/Scenes/Battle/Feature/Unit/Unit/Scripts/Unit.cs`), 다수 `Synergy*Controller`·`Synergy*Effect`(`Assets/Scenes/Battle/Feature/Synergy/Scripts/`)가 SE를 정의·부여

## 4. +α — 코드만 봐선 모르는 것

- **분할 의도 (SE ↔ Controller)**: `StatusEffect` 를 MonoBehaviour에서 떼어내 순수 C#으로 둔 것은 효과 로직을 엔진 생명주기·씬에서 독립시켜 단위 테스트와 재사용을 쉽게 하기 위함이다. 엔진에 묶이는 책임(컬렉션·Update 루프·인스펙터 디버그)은 `StatusEffectController` 한쪽에 모았다.
- **분할 의도 (HookProvider 플러그인 구조)**: 컨트롤러 본체에 이벤트 종류를 하드코딩하지 않고 훅 인터페이스 + Provider로 개방-확장한 것은, 유닛 유형마다 관심 있는 게임 이벤트(행동 상태 변화/공격 적중/…)가 다르기 때문이다. 서브클래스가 자기 유형에 맞는 Provider 조합만 등록하면 된다.
- **분할 의도 (Binder를 MonoBehaviour로 분리)**: Provider는 순수 C#인데 Binder만 MonoBehaviour인 이유는, 프리팹 인스펙터에서 어떤 컨트롤러·소스(ActionStateController·Attacker)에 연결할지를 사람이 시각적으로 지정하게 하기 위함이다.
- **도메인적 의도**: "상태이상은 다양한 전투 사건에 반응한다"는 도메인 요구를 훅 인터페이스라는 개방 지점으로 인코딩했다. 새 반응 지점(예: 피격 시·턴 시작 시)은 3종 세트(훅 인터페이스/Provider/Binder)를 추가하는 것으로 확장한다.
- **비자명한 제약/관례**:
  - 만료는 즉시 제거가 아니라 `IsExpired` 표시 → Update 루프 끝 일괄 제거다 (순회 중 컬렉션 변경 회피). `SafeIterationList` 사용도 같은 이유.
  - `OnActionStateRun`(상태 유지 매 프레임 훅)은 성능을 이유로 의도적으로 비활성 상태다.

## 5. 레이어 링크

- 전역 네비게이션: [index.md](../../index.md)
- UR 미매핑(횡단 기반): [game-system.md](../../ur/game-system.md) §시스템 인프라
