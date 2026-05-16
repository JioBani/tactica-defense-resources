# Plan: TACD-295 단위 1 — 이벤트 인프라 (OnSynergyRecalculatedEventDto)

> 작업정의: TACD-295 단위 1
> 완료 정의: CD-1. SynergyController가 시너지 재계산 완료 후 GlobalEventBus로 재계산 완료 이벤트를 발행한다. 이벤트에는 재계산된 SynergyActivation이 포함된다.

---

## 1. 현재 상태 분석

### SynergyController의 Recalculate 호출 위치

`SynergyController`에는 `Activation.Recalculate()`를 호출하는 지점이 총 3곳이다.

| 메서드 | 호출 위치 | 시나리오 |
|---|---|---|
| `HandlePlacementChanged` | BattleArea 진입 시 (line 83) | Defender가 전장에 배치됨 |
| `HandlePlacementChanged` | BattleArea 이탈 시 (line 90) | Defender가 전장에서 빠짐 |
| `HandleDefenderChanged` | Despawn 시 (line 101) | Defender가 판매됨 |

### 기존 이벤트 DTO 패턴

- `Assets/Scenes/Battle/Feature/Events/` 폴더에 위치
- `readonly struct`가 아닌 `struct`로 선언하되, 프로퍼티는 `private set`으로 불변성 확보
- `IGameEvent` 인터페이스 구현 필수
- 네임스페이스: `Scenes.Battle.Feature.Events`

단, plan-2.md 설계에서는 `readonly struct`를 제안하고 있다. 기존 DTO 패턴(`OnDefenderChangedEventDto` 등)과 일관성을 위해 기존 패턴(struct + private set)을 따른다.

---

## 2. 변경 계획

### 2.1 신규 파일: OnSynergyRecalculatedEventDto.cs

- 경로: `Assets/Scenes/Battle/Feature/Events/OnSynergyRecalculatedEventDto.cs`
- 기존 DTO 패턴을 따라 `struct`, `IGameEvent` 구현
- `SynergyActivation Activation` 프로퍼티 (private set)
- 생성자에서 activation을 받아 설정

### 2.2 수정 파일: SynergyController.cs

- `Activation.Recalculate()` 호출 후마다 `GlobalEventBus.Publish(new OnSynergyRecalculatedEventDto(Activation))` 추가
- 대상: HandlePlacementChanged 내 2곳, HandleDefenderChanged 내 1곳 (총 3곳)
- using 추가 불필요: `Scenes.Battle.Feature.Events`와 `Common.Scripts.GlobalEventBus`는 이미 import되어 있음

---

## 3. 변경하지 않는 것

- `SynergyActivation` 자체는 수정하지 않는다
- 기존 이벤트(`OnTierActivated`, `OnTierChanged`, `OnTierDeactivated`, `ActiveTier.OnChange`)는 그대로 유지한다
