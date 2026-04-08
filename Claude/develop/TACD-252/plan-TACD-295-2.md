# Plan TACD-295-2: SynergyIndicator 스크립트 구현

> 작업정의: TACD-295 단위 2
> 완료 정의:
> - CD-2. 시너지 데이터가 바인딩되면 해당 시너지의 아이콘이 표시된다
> - CD-3. 시너지의 전체 티어 수만큼 도트가 표시되고, 활성 티어까지는 활성 색상, 나머지는 비활성 색상으로 구분된다
> - CD-4. 진행률이 "현재카운트/다음임계치" 형식의 텍스트로 표시된다
> - CD-5. 시너지 재계산 이벤트 수신 시 해당 인디케이터의 티어 도트와 진행률 텍스트가 갱신된다
> - CD-6. 인디케이터 클릭 시 상위 패널에 클릭 이벤트가 전달된다

---

## 1. 탐색 결과 요약

### 기존 코드 확인

- `SynergyActivation` (단위 1에서 변경 없음): `Count` (int), `ActiveTier` (RxValue<SynergyTier?>), `Definition` (SynergyDefinitionData)
- `SynergyDefinitionData`: `Icon` (Sprite), `Tiers` (IReadOnlyList<SynergyTier>), `DisplayName` (string)
- `SynergyTier`: `Tier` (int), `RequiredCount` (int)
- `OnSynergyRecalculatedEventDto` (단위 1에서 생성됨): `Activation` (SynergyActivation)
- `GlobalEventBus`: `Subscribe<T>`, `Unsubscribe<T>`, `Publish<T>` — T는 `struct, IGameEvent`

### UI 패턴 참고

- `MarketUiManager`: OnEnable/OnDisable에서 RxValue.OnChange 구독/해제
- `DefenderSlot`: 데이터 바인딩 메서드(SetSlotData)로 UI 요소 갱신, 클릭 시 상위 매니저 호출
- 네임스페이스 규칙: `Scenes.Battle.Feature.Ui.{서브폴더}`

### 선행 설계 확인

- plan-1.md 3.3절: SynergyIndicator의 역할, SerializeField 정의
- plan-2.md 3.2~3.4절: Bind → RefreshDisplay → OnDestroy 흐름, 이벤트 구독/해제 패턴

### 설계 변경사항

plan-1.md에서는 `tierDotPrefab`을 Instantiate하는 동적 생성 방식을 제안했으나, 작업정의에서 **"티어 도트는 에디터에서 사전 배치 (SerializeField Image 배열)"** 로 지시하고 있다. 따라서 `Image[] tierDots`를 SerializeField로 선언하고, Bind 시 Definition.Tiers 개수만큼 SetActive(true), 나머지는 SetActive(false)하는 방식으로 변경한다. 동적 Instantiate는 사용하지 않는다.

---

## 2. 구현 계획

### 2.1 파일 생성

- 경로: `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyIndicator.cs`
- 네임스페이스: `Scenes.Battle.Feature.Ui.SynergyInfo`
- 베이스: `MonoBehaviour`

### 2.2 SerializeField

| 필드 | 타입 | 설명 |
|---|---|---|
| `icon` | `Image` | 시너지 아이콘 표시 |
| `countText` | `TMP_Text` | "현재카운트/다음임계치" 텍스트 |
| `tierDots` | `Image[]` | 에디터에서 사전 배치된 티어 도트 이미지 배열 |
| `activeDotColor` | `Color` | 활성 도트 색상 |
| `inactiveDotColor` | `Color` | 비활성 도트 색상 |
| `button` | `Button` | 인디케이터 클릭 이벤트 수신 |

### 2.3 public 멤버

| 멤버 | 시그니처 | 설명 |
|---|---|---|
| `OnClicked` | `event Action<SynergyActivation>` | 클릭 시 바인딩된 SynergyActivation을 전달. 상위(SynergyListPanel)에서 구독한다. |
| `BoundActivation` | `SynergyActivation` (읽기 전용 프로퍼티) | 현재 바인딩된 SynergyActivation. SynergyListPanel의 정렬 등에서 참조한다. |
| `Bind` | `void Bind(SynergyActivation activation)` | 데이터를 바인딩하고 UI를 초기화한다. |

### 2.4 Bind(SynergyActivation) 메서드

1. `_activation` 필드에 참조 저장
2. 아이콘 바인딩: `icon.sprite = activation.Definition.Icon` (CD-2)
3. 티어 도트 초기화: `Definition.Tiers.Count`만큼 도트를 SetActive(true), 나머지는 SetActive(false) (CD-3)
4. `RefreshDisplay()` 호출하여 동적 데이터 초기 반영 (CD-3, CD-4)
5. `GlobalEventBus.Subscribe<OnSynergyRecalculatedEventDto>(HandleSynergyRecalculated)` (CD-5)

### 2.5 RefreshDisplay() 메서드 (private)

1. **카운트 텍스트 갱신** (CD-4):
   - `countText.text = $"{_activation.Count}/{GetNextThreshold()}"`
2. **티어 도트 색상 갱신** (CD-3):
   - 활성 도트 수 결정: `_activation.ActiveTier.Value`가 null이면 0, 있으면 해당 티어의 인덱스 + 1
   - 활성 인덱스 이하의 도트는 `activeDotColor`, 나머지는 `inactiveDotColor`

### 2.6 GetNextThreshold() 메서드 (private)

"다음 임계치" 결정 로직 (작업정의 배경지식 참고):
- `Definition.Tiers`를 순회하여 현재 `ActiveTier`보다 한 단계 위 티어의 `RequiredCount`를 반환
- 최고 티어 달성 시: 최고 티어의 `RequiredCount` 반환
- 비활성 시(ActiveTier == null): 첫 번째 티어의 `RequiredCount` 반환

### 2.7 GetActiveDotCount() 메서드 (private)

활성 도트 수 결정 로직:
- `ActiveTier.Value`가 null이면 0 반환
- `Definition.Tiers`를 순회하여 ActiveTier.Value.Value.Tier 이하인 티어의 개수를 반환

### 2.8 HandleSynergyRecalculated(OnSynergyRecalculatedEventDto) 메서드 (private)

- `dto.Activation == _activation` 참조 비교
- 일치하면 `RefreshDisplay()` 호출 (CD-5)

### 2.9 OnDestroy()

- `GlobalEventBus.Unsubscribe<OnSynergyRecalculatedEventDto>(HandleSynergyRecalculated)` (구독 해제)

### 2.10 Awake()

- `button.onClick.AddListener(HandleClick)` (CD-6)

### 2.11 HandleClick() 메서드 (private)

- `OnClicked?.Invoke(_activation)` (CD-6)

---

## 3. 완료 정의 대응

| CD | 구현 위치 | 검증 방법 |
|---|---|---|
| CD-2 | Bind() → icon.sprite 할당 | Bind 호출 후 아이콘 sprite가 Definition.Icon과 일치 |
| CD-3 | Bind() → 도트 활성/비활성, RefreshDisplay() → 도트 색상 | Definition.Tiers.Count만큼 도트 활성, 활성 티어까지 activeDotColor |
| CD-4 | RefreshDisplay() → countText.text 갱신 | "카운트/다음임계치" 형식 문자열 |
| CD-5 | HandleSynergyRecalculated() → RefreshDisplay() | GlobalEventBus 이벤트 수신 시 도트와 텍스트 갱신 |
| CD-6 | Awake() → button.onClick, HandleClick() → OnClicked | 클릭 시 Action 발행 |

---

## 4. 범위 밖 사항

- SynergyListPanel, SynergyInfoPanel (단위 3에서 구현)
- GameObject 계층 구성 및 프리팹 (단위 4에서 구현)
- 정렬 로직 (TACD-297 범위)
