# Plan 1: UI 계층 구조 및 스크립트 구조 설계

> 작업정의: TACD-298 단위 1
> 완료 정의: UI 계층 구조(GameObject 트리) 정의 + 스크립트 파일 구조(클래스 이름, 역할, 소속 경로) 정의

---

## 1. 설계 배경

### 기존 UI 패턴 분석

프로젝트의 기존 UI는 `Assets/Scenes/Battle/Feature/Ui/` 아래 서브 폴더로 관리된다.

| UI | 폴더 | 핵심 클래스 | 패턴 |
|---|---|---|---|
| 소환터미널 | `Ui/Market/` | `MarketUiManager` (MonoBehaviour) | RxValue.OnChange 구독으로 텍스트 갱신, MarketManager 싱글톤 참조 |
| 라운드 정보 | `Ui/RoundInfos/` | `RoundUiManager` (MonoBehaviour) | IStateListener로 페이즈 진입/퇴장 수신 |
| 유닛 스탯 패널 | `Ui/StatInfoPanel/` | `StatInfoPanel` (MonoBehaviour) | GlobalEventBus 구독 (OnObjectSelectedEvent), 프리팹 Instantiate로 셀 동적 생성 |
| 소환수 슬롯 | `Ui/Market/` | `DefenderSlot` (MonoBehaviour) | Action 이벤트 구독, 슬롯 데이터 바인딩 |

공통 특징:
- MonoBehaviour가 SerializeField로 하위 UI 요소 참조
- 데이터 갱신은 RxValue.OnChange 또는 GlobalEventBus 구독
- 비즈니스 로직은 별도 Manager/서비스에 위임 (develop.md 원칙)

### 시너지 데이터 구조 요약

- `SynergyManager.SynergyActivations`: `IReadOnlyDictionary<SynergyDefinitionData, SynergyActivation>`
- `SynergyActivation.ActiveTier`: `RxValue<SynergyTier?>` (OnChange 구독 가능)
- `SynergyActivation.Count`: 현재 유니크 유닛 수
- `SynergyDefinitionData`: DisplayName, Icon, Description, SynergyType, Tiers

---

## 2. UI 계층 구조 (GameObject 트리)

시너지 정보 UI는 화면 좌측 상단에 세로로 배치되는 **목록 패널**과, 시너지 클릭 시 열리는 **상세 패널**로 구성된다.

```
Canvas
└── SynergyInfoPanel                          [RectTransform, CanvasGroup]
    │                                          anchor: top-left, 세로 배치
    │
    ├── SynergyListPanel                       [RectTransform, VerticalLayoutGroup]
    │   │                                      시너지 아이콘 인디케이터를 세로로 나열
    │   │
    │   ├── SynergyIndicator (0)                    [RectTransform, Button] ← 프리팹 인스턴스
    │   │   ├── Icon                           [Image] 시너지 아이콘
    │   │   ├── TierIndicator                  [HorizontalLayoutGroup]
    │   │   │   └── TierDot (0..N)             [Image] 티어 도트 (활성/비활성 색상)
    │   │   └── CountText                      [TMP_Text] "2/4" 형식
    │   │
    │   ├── SynergyIndicator (1)                    [동일 구조]
    │   └── ...
    │
    └── SynergyDetailPanel                     [RectTransform, CanvasGroup]
        │                                      클릭 시 표시, 기본 비활성
        │
        ├── Header
        │   ├── DetailIcon                     [Image] 시너지 아이콘
        │   ├── DetailName                     [TMP_Text] 시너지 이름
        │   └── CloseButton                    [Button]
        │
        ├── TierList                           [VerticalLayoutGroup]
        │   └── TierRow (0..N)                 [HorizontalLayoutGroup] ← 프리팹 인스턴스
        │       ├── TierThreshold              [TMP_Text] "2" (필요 카운트)
        │       └── TierDescription            [TMP_Text] 효과 설명
        │
        └── UnitList                           [GridLayoutGroup]
            └── UnitSlot (0..N)                [RectTransform] ← 프리팹 인스턴스
                ├── UnitIcon                   [Image] 소환수 아이콘
                └── ActiveIndicator            [Image/Outline] 배치 여부 표시
```

### 설계 근거

- **SynergyListPanel**: TFT 좌측 상단 시너지 목록과 동일한 레이아웃. VerticalLayoutGroup으로 인디케이터를 세로 정렬.
- **SynergyIndicator**: 아이콘 + 티어 진행도 + 카운트를 한 슬롯에 표시. Button 컴포넌트로 클릭 이벤트 수신.
- **TierDot**: 임계치 단계만큼 도트를 생성하고 활성 티어까지 색상을 변경하여 진행률을 시각적으로 표현.
- **SynergyDetailPanel**: 클릭 시 열리는 상세 패널. 기획서 초록색 영역에 해당. CanvasGroup으로 alpha 전환.
- **TierRow**: 시너지의 모든 티어를 나열하여 "2/4/6/8" 임계치와 각 티어 효과를 한 번에 표시.
- **UnitList**: TACD-296에서 구현할 소환수 목록 영역. 현재는 구조만 정의하고 구현은 해당 작업에서 진행.

---

## 3. 스크립트 파일 구조

### 폴더 구조

```
Assets/Scenes/Battle/Feature/Ui/SynergyInfo/
├── Scripts/
│   ├── SynergyInfoPanel.cs         # 전체 패널 루트 (목록+상세 조율)
│   ├── SynergyListPanel.cs         # 목록 패널 (인디케이터 생성/정렬/갱신)
│   ├── SynergyIndicator.cs              # 개별 시너지 슬롯 (아이콘, 티어 도트, 카운트)
│   ├── SynergyDetailPanel.cs       # 상세 패널 (열기/닫기, 정보 바인딩)
│   ├── SynergyTierRow.cs           # 상세 패널 내 티어 행
│   └── SynergyUnitSlot.cs          # 상세 패널 내 소환수 슬롯
└── Prefabs/
    ├── SynergyIndicator.prefab
    ├── SynergyTierRow.prefab
    └── SynergyUnitSlot.prefab
```

### 클래스 정의

#### 3.1 SynergyInfoPanel

| 항목 | 내용 |
|---|---|
| 경로 | `Ui/SynergyInfo/Scripts/SynergyInfoPanel.cs` |
| 네임스페이스 | `Scenes.Battle.Feature.Ui.SynergyInfo` |
| 베이스 | `MonoBehaviour` |
| 역할 | 시너지 정보 UI의 루트 컴포넌트. SynergyListPanel과 SynergyDetailPanel을 조율한다. |
| 책임 | - SynergyListPanel에서 인디케이터 클릭 이벤트를 수신하여 SynergyDetailPanel을 열고 닫는다 |
| SerializeField | `SynergyListPanel listPanel`, `SynergyDetailPanel detailPanel` |

#### 3.2 SynergyListPanel

| 항목 | 내용 |
|---|---|
| 경로 | `Ui/SynergyInfo/Scripts/SynergyListPanel.cs` |
| 네임스페이스 | `Scenes.Battle.Feature.Ui.SynergyInfo` |
| 베이스 | `MonoBehaviour` |
| 역할 | 시너지 목록 패널. SynergyManager의 SynergyActivations를 읽어 SynergyIndicator을 생성·정렬·갱신한다. |
| 책임 | - 초기화 시 SynergyManager.SynergyActivations를 순회하여 인디케이터 생성 |
|       | - SynergyActivation.ActiveTier.OnChange 구독으로 인디케이터 갱신 |
|       | - 정렬 로직 호출 (TACD-297에서 구현, 현 단계에서는 호출 구조만 정의) |
|       | - 인디케이터 클릭 시 Action 이벤트로 상위(SynergyInfoPanel)에 통지 |
| SerializeField | `SynergyIndicator[] indicators` (에디터에서 사전 배치) |
| 이벤트 | `Action<SynergyActivation> OnIndicatorClicked` |

#### 3.3 SynergyIndicator

| 항목 | 내용 |
|---|---|
| 경로 | `Ui/SynergyInfo/Scripts/SynergyIndicator.cs` |
| 네임스페이스 | `Scenes.Battle.Feature.Ui.SynergyInfo` |
| 베이스 | `MonoBehaviour` |
| 역할 | 목록 패널 내 개별 시너지 슬롯. 시너지 1개의 아이콘, 티어 진행도, 카운트를 표시한다. |
| 책임 | - Bind(SynergyActivation)으로 데이터 바인딩 |
|       | - SynergyActivation.ActiveTier.OnChange 구독으로 UI 자동 갱신 |
|       | - 티어 도트 생성 및 활성/비활성 색상 업데이트 |
|       | - Button 클릭 이벤트 → 상위(SynergyListPanel)에 통지 |
| SerializeField | `Image icon`, `TMP_Text countText`, `Transform tierDotContainer`, `Image tierDotPrefab` |

#### 3.4 SynergyDetailPanel

| 항목 | 내용 |
|---|---|
| 경로 | `Ui/SynergyInfo/Scripts/SynergyDetailPanel.cs` |
| 네임스페이스 | `Scenes.Battle.Feature.Ui.SynergyInfo` |
| 베이스 | `MonoBehaviour` |
| 역할 | 시너지 상세 패널. 시너지 클릭 시 효과 설명, 티어 목록, 소환수 목록을 표시한다. |
| 책임 | - Show(SynergyActivation)으로 패널을 열고 데이터를 바인딩한다 |
|       | - Hide()로 패널을 닫는다 |
|       | - 티어 행(SynergyTierRow) 동적 생성 |
|       | - 소환수 슬롯(SynergyUnitSlot) 영역은 TACD-296에서 구현 |
| SerializeField | `Image detailIcon`, `TMP_Text detailName`, `Button closeButton`, `SynergyTierRow tierRowPrefab`, `Transform tierListContainer`, `Transform unitListContainer`, `SynergyUnitSlot unitSlotPrefab` |

#### 3.5 SynergyTierRow

| 항목 | 내용 |
|---|---|
| 경로 | `Ui/SynergyInfo/Scripts/SynergyTierRow.cs` |
| 네임스페이스 | `Scenes.Battle.Feature.Ui.SynergyInfo` |
| 베이스 | `MonoBehaviour` |
| 역할 | 상세 패널 내 티어 1행. 임계치 수치와 효과 설명을 표시한다. |
| 책임 | - Bind(SynergyTier, bool isActive)로 데이터 바인딩 |
|       | - 활성 티어 여부에 따라 시각적 구분 (색상/굵기) |
| SerializeField | `TMP_Text thresholdText`, `TMP_Text descriptionText` |

#### 3.6 SynergyUnitSlot

| 항목 | 내용 |
|---|---|
| 경로 | `Ui/SynergyInfo/Scripts/SynergyUnitSlot.cs` |
| 네임스페이스 | `Scenes.Battle.Feature.Ui.SynergyInfo` |
| 베이스 | `MonoBehaviour` |
| 역할 | 상세 패널 내 소환수 아이콘 슬롯. 해당 시너지에 속하는 소환수를 표시한다. |
| 책임 | - Bind(UnitLoadOutData, bool isActive)로 데이터 바인딩 |
|       | - 배치 여부에 따라 활성/비활성 시각 표시 |
| SerializeField | `Image unitIcon`, `Image activeIndicator` |
| 비고 | TACD-296에서 구현. 현 단계에서는 클래스 구조만 정의. |

---

## 4. 네임스페이스 규칙

모든 시너지 정보 UI 스크립트는 `Scenes.Battle.Feature.Ui.SynergyInfo` 네임스페이스를 사용한다.
- 기존 패턴: `Scenes.Battle.Feature.Ui.Markets`, `Scenes.Battle.Feature.Ui.StatInfoPanel` 등

---

## 5. 하위 스토리 대응 매핑

| 하위 스토리 | 주 담당 클래스 | 설명 |
|---|---|---|
| TACD-295 (시너지 목록 패널) | `SynergyListPanel`, `SynergyIndicator` | 인디케이터 생성, 아이콘/티어/카운트 표시, 갱신 로직 |
| TACD-296 (시너지 상세 패널) | `SynergyDetailPanel`, `SynergyTierRow`, `SynergyUnitSlot` | 클릭 시 상세 정보, 소환수 목록 |
| TACD-297 (정렬) | `SynergyListPanel` 내 정렬 메서드 | 시너지 종류·활성 상태에 따른 정렬 |

---

## 6. 설계 원칙 준수 사항

- **Feature-based organization**: `Ui/SynergyInfo/` 서브 폴더에 스크립트와 프리팹을 자체 포함
- **MonoBehaviour와 비즈니스 로직 분리**: MonoBehaviour는 UI 바인딩과 이벤트 수신만 담당. 정렬 로직 등 비즈니스 로직이 복잡해지면 서비스 클래스로 분리 (TACD-297 시점에 판단)
- **데이터 갱신은 RxValue.OnChange 구독**: SynergyActivation.ActiveTier.OnChange 활용
- **크로스 피처 통신은 GlobalEventBus**: 단, SynergyManager는 싱글톤이므로 직접 참조가 가능. UI → SynergyManager 접근은 싱글톤 참조 사용
- **인디케이터 사전 배치**: SynergyIndicator는 동적 Instantiate하지 않고, 에디터에서 충분한 수를 미리 배치하여 SetActive로 켜고 끄는 방식 사용
- **프리팹 동적 생성**: SynergyTierRow, SynergyUnitSlot은 프리팹으로 만들어 Instantiate. (현재 ObjectPooler 대상 아님 - 소수 개체이므로)
