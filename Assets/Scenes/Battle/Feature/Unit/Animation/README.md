# 유닛 애니메이션 프레임워크

ActionStateController의 상태 변경에 연동하여 유닛별 셀 애니메이션을 자동 재생하는 프레임워크.

---

## 구조

```
UnitBase.controller (Base AnimatorController, 1개)
├── State: Idle, Move, Attack, Downed, Freeze, Waiting
├── 각 State에 placeholder 클립
└── Transition 없음 (코드에서 animator.Play()로 전환)

유닛별 OverrideController (유닛·성급마다 1개)
├── 기반: UnitBase.controller
└── placeholder 클립을 실제 .anim 클립으로 교체

UnitDefinitionData (SO)
└── animatorByStars[] → 성급별 OverrideController 참조

UnitAnimator (런타임 컴포넌트)
├── Unit.OnSpawnEvent 구독 → OverrideController를 Animator에 할당
└── IStateListener<ActionStateType> → 상태 변경 시 animator.Play()
```

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
| `UnitBase.controller` | Base AnimatorController (OverrideController의 원본 템플릿) |
| `placeholder_*.anim` | 각 상태의 placeholder 클립 (OverrideController 교체 목록용) |
| `UnitAnimator.cs` | 런타임 컴포넌트 (OnSpawnEvent 구독, IStateListener 구현) |
| `AnimationFrameworkSetup.cs` | Base Controller 자동 생성 에디터 스크립트 (Assets/Editor/) |
