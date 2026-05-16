# Plan TACD-297-1: 정렬 로직 구현

> 작업정의: TACD-297 단위 1
> 완료 정의: 활성/비활성 시너지 정렬, 티어 진행률·카운트 기반 세부 정렬, 안정 정렬, 자동 갱신

---

## 1. 현재 상태

- `SynergyListPanel.SortIndicators()`는 빈 메서드로 존재하며, `Start()`와 `HandleSynergyRecalculated()`에서 이미 호출되고 있다. 갱신 시점은 완료되어 있다.
- `SynergyIndicator.BoundActivation`으로 `SynergyActivation`에 접근 가능하다.
- `SynergyActivation`에서 제공하는 정렬 관련 데이터:
  - `ActiveTier.Value` — `SynergyTier?` (null이면 비활성)
  - `Count` — 현재 유니크 유닛 카운트
  - `Definition.Tiers` — `IReadOnlyList<SynergyTier>` (requiredCount 오름차순)
- 활성 인디케이터만 정렬 대상이다 (`gameObject.activeSelf == true`인 것만).
- `VerticalLayoutGroup`이 sibling 순서에 따라 자동 배치하므로, `SetSiblingIndex()`로 시각적 정렬을 제어한다.

## 2. 진행률 계산 로직

정렬에 필요한 "티어 진행률"을 계산해야 한다. `SynergyIndicatorService`에는 이미 도트별 비율을 계산하는 `CalculateRatios`가 있지만, 정렬용 단일 진행률 값과는 목적이 다르다. 정렬용 진행률은 SynergyListPanel 내 private 메서드로 구현한다.

**진행률 정의 (plan-2 5.1절 기준):**
- 활성 시너지: `Count / 다음 임계치의 RequiredCount`
  - 최고 티어 달성 시: 진행률 = 1.0
  - 그 외: `Count / NextTier.RequiredCount` (0.0 ~ 1.0 미만)
- 비활성 시너지: 진행률 계산 불필요 (카운트 기반 정렬)

**다음 임계치 결정:**
- 현재 활성 티어의 인덱스를 찾는다 (tiers에서 `tier.Tier == activeTier.Tier`인 위치).
- 다음 인덱스가 존재하면 해당 `RequiredCount`가 다음 임계치.
- 다음 인덱스가 없으면 최고 티어 달성이므로 진행률 1.0.

## 3. 정렬 기준 (우선순위)

1. **활성 상태**: 활성(`ActiveTier.Value.HasValue`) > 비활성
2. **활성끼리**: 티어 진행률 내림차순 (높을수록 상위)
3. **동일 진행률의 활성끼리**: 활성 티어 단계(`ActiveTier.Value.Value.Tier`) 내림차순
4. **비활성끼리**: 카운트 내림차순 (높을수록 상위)
5. **동일 카운트의 비활성끼리**: 기존 순서 유지 (안정 정렬)

## 4. 구현 계획

### 4.1 수정 파일

- `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyListPanel.cs`

### 4.2 SortIndicators() 구현

```
SortIndicators()
    ├── indicators 배열에서 활성(gameObject.activeSelf == true)인 것만 리스트로 수집
    ├── LINQ OrderBy 체인으로 안정 정렬 수행:
    │   ├── 1차: 활성 상태 (활성이 앞으로)
    │   ├── 2차: 활성이면 진행률 내림차순
    │   ├── 3차: 동일 진행률이면 활성 티어 단계 내림차순
    │   └── 4차: 비활성이면 카운트 내림차순
    └── 정렬 결과 순서대로 SetSiblingIndex(i) 호출
```

### 4.3 진행률 계산 private 메서드

```csharp
/// <summary>정렬에 사용할 활성 시너지의 티어 진행률을 계산한다.</summary>
private float CalculateTierProgress(SynergyActivation activation)
```

- 활성 티어의 인덱스를 tiers에서 찾는다.
- 다음 티어가 존재하면: `(float)count / nextTier.RequiredCount`
- 다음 티어가 없으면 (최고 티어): `1.0f`

### 4.4 using 추가

- `using System.Linq;` 추가 (OrderBy/ThenByDescending 사용)

## 5. 변경하지 않는 것

- `SynergyIndicatorService` — 정렬 로직은 SynergyListPanel 내부에서 완결. 기존 `CalculateRatios`는 도트 표시용이므로 변경하지 않는다.
- `SynergyIndicator` — 변경 없음.
- `SynergyActivation` — 변경 없음.
- 갱신 시점(Start, HandleSynergyRecalculated에서 호출) — 이미 완료.
