# 유닛 애니메이션 프레임워크

ActionStateController의 상태 변경에 연동하여 유닛별 셀 애니메이션을 자동 재생하는 프레임워크.
특정 상태의 애니메이션을 지정된 시간에 맞추는 속도 조절 메커니즘을 포함한다.

---

## 구조

```
StateAnimator<T> (제네릭 베이스, Common/Scripts/StateBase/)
├── StateBaseController<T> 구독 (IStateListener<T>)
├── OnStateEnter → animator.Play(state.ToString())
├── targetDurations: 상태별 목표 시간 등록 → 자동 속도 계산
└── clipLengthCache: Initialize 시 전체 클립 길이 캐싱

UnitAnimator : StateAnimator<ActionStateType> (유닛 특화)
├── Unit.OnSpawnEvent 구독 → OverrideController 할당
├── AttackSpeed.OnChange 구독
└── Attack의 targetDuration = 1/attackSpeed 로 자동 갱신

UnitBase.controller (Base AnimatorController, 1개)
├── State: Idle, Move, Attack, Downed, Freeze, Waiting
├── 각 State에 placeholder 클립
└── Transition 없음 (코드에서 animator.Play()로 전환)

유닛별 OverrideController (유닛·성급마다 1개)
├── 기반: UnitBase.controller
└── placeholder 클립을 실제 .anim 클립으로 교체

UnitDefinitionData (SO)
└── animatorByStars[] → 성급별 OverrideController 참조
```

---

## 속도 조절 메커니즘

특정 상태의 애니메이션 1루프를 지정된 시간에 정확히 맞춘다.

```
animator.speed = clipLength / targetDuration
```

- Attack 상태: targetDuration = 1/attackSpeed → 공격 사이클과 애니메이션이 정확히 일치
- 미등록 상태: animator.speed = 1.0f (기본 속도)

속도 조절이 필요한 상태가 추가되면 `SetTargetDuration(state, duration)`으로 등록한다.

---

## 새 유닛 애니메이션 추가 방법

### 1. 셀 애니메이션 클립 생성

1. Project 창에서 해당 상태의 스프라이트들을 **모두 선택**
2. Hierarchy에 **드래그** → Unity가 `.anim` 클립을 자동 생성
3. 상태별로 반복 (idle, move, attack, downed, freeze, waiting)

> 자동 생성된 AnimatorController는 사용하지 않으므로 삭제해도 됨. `.anim` 클립만 사용.

### 2. AnimatorOverrideController 생성

1. Project 창 우클릭 → **Create > Animator Override Controller**
2. Inspector에서 **Controller** 필드에 `UnitBase.controller`를 할당
3. Override 목록이 표시됨 — 각 placeholder를 실제 클립으로 교체:

| Original Clip | Override Clip |
|--------------|---------------|
| placeholder_idle | (유닛)_idle.anim |
| placeholder_move | (유닛)_move.anim |
| placeholder_attack | (유닛)_attack.anim |
| placeholder_downed | (유닛)_downed.anim |
| placeholder_freeze | (유닛)_freeze.anim |
| placeholder_waiting | (유닛)_waiting.anim |

> 모든 상태를 채우지 않아도 됨. 비어 있으면 placeholder(빈 클립)가 재생됨.

### 3. 성급별로 반복

성급별 비주얼이 다른 경우, 성급마다 별도의 OverrideController를 생성한다.

### 4. UnitDefinitionData SO에 연결

해당 유닛의 UnitDefinitionData SO를 열고, **애니메이션** 섹션의 `animatorByStars`에 할당:

```
[0] — (미사용, 비워둠)
[1] — 1성 OverrideController
[2] — 2성 OverrideController
[3] — 3성 OverrideController
```

---

## 새 ActionState 추가 시

1. `ActionStateType` enum에 새 값 추가 (예: `Stun`)
2. `UnitBase.controller`를 열고 **같은 이름의 State 추가** + placeholder 클립 할당
3. 각 유닛 OverrideController에서 새 placeholder를 실제 클립으로 교체
4. **코드 수정 없음** — `animator.Play("Stun")`이 자동으로 동작

---

## 파일 목록

| 파일 | 역할 |
|------|------|
| `StateAnimator.cs` | 제네릭 베이스 클래스 (Common/Scripts/StateBase/) |
| `UnitAnimator.cs` | Unit 특화 애니메이터 (StateAnimator 상속, AttackSpeed 연동) |
| `UnitBase.controller` | Base AnimatorController (OverrideController의 원본 템플릿) |
| `placeholder_*.anim` | 각 상태의 placeholder 클립 (OverrideController 교체 목록용) |
| `AnimationFrameworkSetup.cs` | Base Controller 자동 생성 에디터 스크립트 (Assets/Editor/) |
