# Plan 2: 데이터 흐름 및 갱신 설계

> 작업정의: TACD-298 단위 2
> 완료 정의: 시너지 목록 패널의 데이터 흐름 정의 + 시너지 상세 패널의 데이터 흐름 정의 + 정렬 기준과 갱신 시점 정의

---

## 1. 설계 배경

### 기존 데이터 흐름 패턴 분석

코드베이스에서 UI가 데이터를 수신하는 방식은 두 가지로 나뉜다.

| 패턴 | 사용처 | 구독 방식 |
|---|---|---|
| RxValue.OnChange | MarketUiManager (Mana, Level, IsScanLocked) | 싱글톤 참조 → RxValue.OnChange += 핸들러 |
| GlobalEventBus | StatInfoPanel (OnObjectSelectedEvent) | GlobalEventBus.Subscribe<T>(핸들러) |

MarketUiManager는 OnEnable/OnDisable에서 RxValue.OnChange를 구독/해제하며, 핸들러에서 직접 UI 텍스트를 갱신한다. StatInfoPanel은 GlobalEventBus 이벤트를 받아 Show/Hide를 제어하고, 내부적으로 데이터를 직접 읽어 UI에 바인딩한다.

### SynergyActivation의 이벤트 구조

`SynergyActivation`이 제공하는 이벤트:

| 이벤트 | 발행 시점 | 시그니처 |
|---|---|---|
| `ActiveTier.OnChange` | 활성 티어 값이 변경될 때 (null ↔ SynergyTier, 또는 tier ↔ 다른 tier) | `Action<SynergyTier?>` |
| `OnTierActivated` | 비활성 → 활성 전환 시 | `Action<SynergyTier>` |
| `OnTierChanged` | 활성 상태에서 티어만 변경 시 | `Action<SynergyTier>` |
| `OnTierDeactivated` | 활성 → 비활성 전환 시 | `Action` |

**핵심 제약**: `SynergyActivation.Count`는 plain int이다. RxValue가 아니므로 카운트 변경만으로는 이벤트가 발행되지 않는다. 예를 들어 카운트가 2→3으로 증가해도 활성 티어가 동일하면(다음 임계치가 4일 때), `ActiveTier.OnChange`는 발행되지 않는다. UI에 "3/4" 형식의 카운트 텍스트를 표시하려면 카운트 변경도 감지해야 한다.

### 카운트 변경 감지 방안

`SynergyActivation.Recalculate()`는 `SynergyController.HandlePlacementChanged()`와 `HandleDefenderChanged()`에서 호출된다. 이 호출의 근본 트리거는 GlobalEventBus의 `OnDefenderPlacementChangedEventDto`와 `OnDefenderChangedEventDto`이다.

카운트 변경을 UI가 감지하는 방안 3가지:

| 방안 | 설명 | 장점 | 단점 |
|---|---|---|---|
| A. SynergyActivation에 OnRecalculated 이벤트 추가 | Recalculate() 호출 시마다 발행하는 콜백 추가 | 데이터 소스에서 직접 알림, UI가 정확한 타이밍에 갱신 | SynergyActivation 수정 필요 |
| B. UI가 GlobalEventBus 직접 구독 | UI가 OnDefenderPlacementChangedEventDto 등을 구독 | SynergyActivation 수정 불필요 | SynergyController와 동일 이벤트를 중복 구독, UI가 비즈니스 이벤트에 의존 |
| C. Count를 RxValue로 변경 | _count를 RxValue<int>로 교체 | 기존 패턴과 일관됨 | Recalculate 내부에서 ActiveTier보다 Count가 먼저 변경되어 순서 보장이 복잡 |

**채택: GlobalEventBus를 통한 재계산 완료 이벤트 발행**

`SynergyController`가 `Recalculate()` 호출 후 `GlobalEventBus.Publish(new OnSynergyRecalculatedEventDto(activation))`를 발행한다. UI는 이 이벤트를 구독하여 갱신한다.

근거:
- 프로젝트의 크로스 피처 통신 원칙(GlobalEventBus 사용)과 일관된다.
- SynergyActivation 자체를 수정하지 않아 기존 코드 영향이 없다.
- Recalculate 완료 후 1회만 발행하므로, UI가 Count와 ActiveTier를 함께 읽어 1회만 갱신할 수 있다.

---

## 2. GlobalEventBus 이벤트 추가

### 2.1 이벤트 DTO

```csharp
/// <summary>시너지 재계산 완료 시 발행된다.</summary>
public readonly struct OnSynergyRecalculatedEventDto
{
    public readonly SynergyActivation Activation;

    public OnSynergyRecalculatedEventDto(SynergyActivation activation)
    {
        Activation = activation;
    }
}
```

### 2.2 발행 위치

`SynergyController`에서 `SynergyActivation.Recalculate()` 호출 후 `GlobalEventBus.Publish(new OnSynergyRecalculatedEventDto(activation))`를 발행한다.

SynergyActivation 자체는 변경하지 않는다. 기존 이벤트(`OnTierActivated`, `OnTierChanged`, `OnTierDeactivated`, `ActiveTier.OnChange`)도 그대로 유지한다.

---

## 3. 시너지 목록 패널 데이터 흐름

### 3.1 초기화

```
SynergyListPanel.Start()
    ├── SynergyManager.Instance.SynergyActivations 읽기
    │   → IReadOnlyDictionary<SynergyDefinitionData, SynergyActivation>
    │
    ├── 사전 배치된 인디케이터에 Bind(SynergyActivation) 호출
    │   → 시너지 수만큼 활성화 (SetActive(true))
    │   → 남은 인디케이터는 비활성 유지 (SetActive(false))
    │
    └── 초기 정렬 실행
```

**인디케이터 사전 배치**: SynergyIndicator는 동적 Instantiate하지 않는다. 충분한 수의 인디케이터를 **에디터에서 미리 자식으로 배치**해 두고, 필요한 만큼 SetActive(true)로 활성화한다. 사용하지 않는 인디케이터는 SetActive(false) 상태로 유지한다. SynergyListPanel은 `SerializeField`로 인디케이터 배열을 참조한다.

**초기화 시점**: `Start()`를 사용한다. `SynergyManager`는 `Start()`에서 초기화하므로, `SynergyListPanel`의 `Start()`가 이후에 호출되어야 한다. Unity의 Start() 호출 순서는 보장되지 않으므로, SynergyManager가 Start()에서 초기화하는 점을 고려하여 Script Execution Order 설정 또는 이벤트 기반 초기화가 필요할 수 있다. 구현 시점에서 결정한다.

### 3.2 인디케이터 바인딩 (SynergyIndicator.Bind)

```
SynergyIndicator.Bind(SynergyActivation activation)
    ├── 참조 저장: _activation = activation
    │
    ├── 정적 데이터 바인딩 (1회):
    │   ├── activation.Definition.Icon → icon.sprite
    │   └── activation.Definition.Tiers → 티어 도트 개수만큼 Image 생성
    │
    ├── 동적 데이터 초기 반영:
    │   └── RefreshDisplay() 호출
    │
    └── 이벤트 구독:
        └── GlobalEventBus.Subscribe<OnSynergyRecalculatedEventDto>(OnSynergyRecalculated)
```

### 3.3 인디케이터 갱신 (RefreshDisplay)

```
SynergyIndicator.RefreshDisplay()
    ├── 카운트 텍스트 갱신:
    │   └── countText.text = $"{_activation.Count}/{다음 임계치 또는 최대 임계치}"
    │
    ├── 티어 도트 갱신:
    │   └── 각 도트의 색상을 활성 티어까지 활성 색상, 나머지 비활성 색상으로 설정
    │
    └── 상위(SynergyListPanel)에 재정렬 요청 통지
```

**"다음 임계치" 결정 로직**: 카운트 텍스트에서 분모는 현재 활성 티어의 다음 티어의 RequiredCount를 사용한다. 최고 티어 달성 시에는 최고 티어의 RequiredCount를 분모로 사용한다. 비활성 시에는 첫 번째 티어의 RequiredCount를 분모로 사용한다. 이 로직은 서비스 클래스나 SynergyIndicator 내 private 메서드로 구현한다. (단순하므로 서비스 분리 불필요)

### 3.4 이벤트 구독 해제

```
SynergyIndicator.OnDestroy()
    └── GlobalEventBus.Unsubscribe<OnSynergyRecalculatedEventDto>(OnSynergyRecalculated)
```

### 3.5 인디케이터 클릭 전달

```
SynergyIndicator (Button.onClick)
    → SynergyListPanel.OnIndicatorClicked 이벤트 발행
        → SynergyInfoPanel이 수신하여 SynergyDetailPanel.Show(activation) 호출
```

### 3.6 데이터 흐름 요약 (목록 패널)

| 데이터 | 출처 | 읽기 시점 | 갱신 이벤트 |
|---|---|---|---|
| 시너지 아이콘 | `SynergyDefinitionData.Icon` | Bind() (1회) | 없음 (불변) |
| 티어 도트 개수 | `SynergyDefinitionData.Tiers.Count` | Bind() (1회) | 없음 (불변) |
| 현재 카운트 | `SynergyActivation.Count` | RefreshDisplay() | `OnSynergyRecalculatedEventDto` |
| 현재 활성 티어 | `SynergyActivation.ActiveTier.Value` | RefreshDisplay() | `OnSynergyRecalculatedEventDto` |
| 티어 임계치 목록 | `SynergyDefinitionData.Tiers` | RefreshDisplay() | 없음 (불변, 분모 계산에 사용) |

---

## 4. 시너지 상세 패널 데이터 흐름

### 4.1 상세 패널 열기 (Show)

```
SynergyDetailPanel.Show(SynergyActivation activation)
    ├── 참조 저장: _currentActivation = activation
    │
    ├── 이전 참조 해제 (열려있던 다른 시너지가 있을 경우)
    │
    ├── 헤더 바인딩:
    │   ├── detailIcon.sprite = activation.Definition.Icon
    │   └── detailName.text = activation.Definition.DisplayName
    │
    ├── 시너지 설명 바인딩:
    │   └── descriptionText.text = activation.Definition.Description
    │       (플레이스홀더 치환은 별도 작업으로 분리됨. 원본 텍스트 그대로 표시)
    │
    ├── 티어 행 생성:
    │   ├── 기존 TierRow 인스턴스 제거 (ClearTierRows)
    │   └── activation.Definition.Tiers 순회:
    │       ├── SynergyTierRow 프리팹 Instantiate
    │       └── tierRow.Bind(tier, isActive)
    │           ├── thresholdText.text = tier.RequiredCount.ToString()
    │           ├── descriptionText.text = tier 효과 설명 (플레이스홀더 치환 없이 원본)
    │           └── 활성 여부에 따라 색상/굵기 구분
    │
    ├── 소환수 슬롯 영역 (TACD-296에서 구현):
    │   └── (현재 비워둠)
    │
    ├── 이벤트 구독 (아직 구독하지 않은 경우):
    │   └── GlobalEventBus.Subscribe<OnSynergyRecalculatedEventDto>(OnSynergyRecalculated)
    │
    └── 패널 표시 (gameObject.SetActive(true) 또는 CanvasGroup.alpha = 1)
```

### 4.2 상세 패널 갱신 (RefreshDetail)

상세 패널이 열려 있는 동안 시너지 상태가 변경되면 갱신한다.

```
SynergyDetailPanel.RefreshDetail()
    ├── 티어 행의 활성 상태 갱신:
    │   └── 각 TierRow에 현재 활성 티어와 비교하여 isActive 업데이트
    │
    └── 소환수 슬롯 갱신 (TACD-296에서 구현):
        └── (현재 비워둠)
```

RefreshDetail은 티어 행을 다시 생성하지 않고, 기존 TierRow의 활성 상태만 업데이트한다. 티어 목록 자체는 `SynergyDefinitionData`의 불변 데이터이므로 변경되지 않는다.

### 4.3 상세 패널 닫기 (Hide)

```
SynergyDetailPanel.Hide()
    ├── 참조 해제: _currentActivation = null
    │
    └── 패널 숨기기 (gameObject.SetActive(false) 또는 CanvasGroup.alpha = 0)
```

### 4.4 닫기 트리거

- CloseButton 클릭 → Hide() 호출
- 같은 인디케이터 재클릭 → SynergyInfoPanel에서 토글 로직으로 Hide() 호출
- 다른 인디케이터 클릭 → Show(다른 activation) 호출 (내부에서 이전 구독 해제 후 새 데이터 바인딩)

### 4.5 티어 행의 활성 판정

`SynergyTierRow.SetActive(bool isActive)`로 활성 상태를 외부에서 제어한다.

판정 기준: `activation.ActiveTier.Value.HasValue && tier.Tier <= activation.ActiveTier.Value.Value.Tier`

- 활성 티어가 null이면 모든 행이 비활성
- 활성 티어가 있으면 해당 티어 이하의 모든 행이 활성

### 4.6 데이터 흐름 요약 (상세 패널)

| 데이터 | 출처 | 읽기 시점 | 갱신 이벤트 |
|---|---|---|---|
| 시너지 아이콘 | `SynergyDefinitionData.Icon` | Show() (클릭 시) | 없음 (불변) |
| 시너지 이름 | `SynergyDefinitionData.DisplayName` | Show() (클릭 시) | 없음 (불변) |
| 시너지 설명 | `SynergyDefinitionData.Description` | Show() (클릭 시) | 없음 (불변, 플레이스홀더 치환 미구현) |
| 티어 목록 | `SynergyDefinitionData.Tiers` | Show() (TierRow 생성 시) | 없음 (불변) |
| 각 티어의 활성 여부 | `SynergyActivation.ActiveTier.Value`와 비교 | Show() + RefreshDetail() | `OnSynergyRecalculatedEventDto` |
| 소환수 목록 | (TACD-296에서 결정) | - | - |
| 소환수 배치 여부 | (TACD-296에서 결정) | - | - |

---

## 5. 정렬 기준과 갱신 시점

### 5.1 정렬 기준

TFT 시너지 목록의 정렬 규칙을 참고하여 다음과 같이 정렬한다.

**1차 정렬: 활성 상태** (활성 > 비활성)
- 활성: `ActiveTier.Value.HasValue == true`
- 비활성: `ActiveTier.Value.HasValue == false`

**2차 정렬 (활성 시너지 내부): 티어 진행률 내림차순**
- 진행률 = `Count / 다음 임계치의 RequiredCount`
- 최고 티어 달성 시 진행률 = 1.0 (최상위 배치)
- 동일 진행률인 경우: 현재 활성 티어 단계(Tier) 내림차순

**3차 정렬 (비활성 시너지 내부): 카운트 내림차순**
- 카운트가 높을수록 상위 배치 (곧 활성화될 가능성이 높으므로)
- 동일 카운트인 경우: SynergyDefinitionData의 참조 순서 유지 (안정 정렬)

**시너지 종류(소환술사 효과 / 소환수 특성) 구분은 하지 않는다.** (결정사항 1에 따라 UI에서 동일하게 표시)

### 5.2 정렬 갱신 시점

정렬은 다음 시점에 실행한다:

| 시점 | 트리거 | 설명 |
|---|---|---|
| 초기화 | SynergyListPanel.Start() | 인디케이터 전체 생성 후 1회 |
| 시너지 재계산 | SynergyActivation.OnRecalculated | 카운트 또는 티어가 변경될 때마다 |

`OnSynergyRecalculatedEventDto`는 GlobalEventBus로 발행된다. SynergyListPanel은 이 이벤트를 구독하여, 어떤 시너지든 재계산되면 전체 목록을 재정렬한다.

### 5.3 정렬 구현 위치

`SynergyListPanel` 내 private 메서드로 구현한다.

```
SynergyListPanel.SortIndicators()
    ├── _indicators 리스트를 정렬 기준에 따라 정렬
    └── 정렬 결과에 따라 각 인디케이터의 sibling index 설정
        └── indicator.transform.SetSiblingIndex(i)
```

VerticalLayoutGroup이 sibling 순서에 따라 자동 배치하므로, SetSiblingIndex만으로 시각적 정렬이 완료된다.

비즈니스 로직이 단순(비교 함수 1개)하므로 별도 서비스 클래스 분리 없이 MonoBehaviour 내에 구현한다. 복잡해지면 TACD-297 구현 시점에 서비스 분리를 검토한다.

### 5.4 정렬 성능

시너지 개수는 최대 4~8개(소환술사 4명 × 소환술사 효과 1개 + 소환수 특성 최대 8개)이므로 매 재계산마다 전체 정렬해도 성능 문제가 없다.

---

## 6. 전체 데이터 흐름 다이어그램

```
[Defender 배치/제거/판매]
    │
    ├── GlobalEventBus: OnDefenderPlacementChangedEventDto
    │   └── SynergyController.HandlePlacementChanged()
    │       ├── SynergyActivation.Recalculate()
    │       │   ├── Count 갱신
    │       │   ├── ActiveTier.Value 갱신 → RxValue.OnChange 발행 (티어 변경 시)
    │       │   └── OnTierActivated / OnTierChanged / OnTierDeactivated 발행 (해당 시)
    │       └── GlobalEventBus.Publish(OnSynergyRecalculatedEventDto) ← [신규]
    │
    └── GlobalEventBus: OnDefenderChangedEventDto (Despawn)
        └── SynergyController.HandleDefenderChanged()
            ├── SynergyActivation.Recalculate() → (동일 흐름)
            └── GlobalEventBus.Publish(OnSynergyRecalculatedEventDto) ← [신규]

[GlobalEventBus: OnSynergyRecalculatedEventDto]
    │
    ├── SynergyIndicator.OnSynergyRecalculated(dto)
    │   └── dto.Activation == _activation이면 RefreshDisplay()
    │       ├── 카운트 텍스트 갱신
    │       └── 티어 도트 색상 갱신
    │
    ├── SynergyListPanel.OnSynergyRecalculated(dto)
    │   └── SortIndicators()
    │       └── sibling index 재설정 → VerticalLayoutGroup 자동 재배치
    │
    └── SynergyDetailPanel.OnSynergyRecalculated(dto)
        └── dto.Activation == _currentActivation이면 RefreshDetail()
            └── TierRow 활성 상태 갱신
```

---

## 7. 변경 최소 범위

이 설계에서 필요한 코드 변경:

1. **OnSynergyRecalculatedEventDto** (신규): 이벤트 DTO 구조체
2. **SynergyController** (수정): Recalculate() 호출 후 GlobalEventBus.Publish 추가

SynergyActivation 자체는 변경하지 않는다. 기존 이벤트(`OnTierActivated`, `OnTierChanged`, `OnTierDeactivated`, `ActiveTier.OnChange`)도 그대로 유지한다.
