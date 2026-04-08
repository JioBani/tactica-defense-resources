# Plan: 단위 3 — SynergyListPanel + SynergyInfoPanel 스크립트 구현

> 작업정의: TACD-295 단위 3
> 완료 정의: CD-7, CD-8, CD-9, CD-10

---

## 1. 대상 완료 정의

- CD-7. 초기화 시 SynergyManager의 시너지 목록을 읽어, 시너지 수만큼 사전 배치된 인디케이터를 활성화하고 나머지는 비활성화한다
- CD-8. 시너지 재계산 이벤트 수신 시 정렬 메서드를 호출하는 구조가 갖춰진다 (정렬 로직 자체는 TACD-297 범위)
- CD-9. 인디케이터 클릭 이벤트를 Action으로 상위(SynergyInfoPanel)에 전달한다
- CD-10. SynergyListPanel의 클릭 이벤트를 수신하여 SynergyDetailPanel에 전달하는 구조가 갖춰진다 (SynergyDetailPanel 구현은 TACD-296 범위)

---

## 2. 코드베이스 분석

### SynergyManager 초기화 시점
- `SynergyManager.Start()`에서 `SynergyActivations` 딕셔너리를 채운다.
- `SynergyListPanel`도 `Start()`에서 초기화해야 하므로, 호출 순서가 보장되지 않는다.
- **해결**: `SynergyListPanel`은 `[DefaultExecutionOrder(100)]`을 지정하여 SynergyManager(기본 0)보다 나중에 실행되도록 한다.

### SynergyIndicator 기존 API
- `Bind(SynergyActivation)`: 데이터 바인딩, 이벤트 구독, UI 초기화
- `OnClicked`: `Action<SynergyActivation>` 이벤트
- `BoundActivation`: 바인딩된 activation 반환

### GlobalEventBus 패턴
- `Subscribe<T>(Action<T>)` / `Unsubscribe<T>(Action<T>)` — OnEnable/OnDisable에서 구독/해제

---

## 3. 구현 계획

### 3.1 SynergyListPanel.cs (신규)

**경로**: `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyListPanel.cs`
**네임스페이스**: `Scenes.Battle.Feature.Ui.SynergyInfo`
**베이스**: `MonoBehaviour`

- `[DefaultExecutionOrder(100)]` 어트리뷰트 부여
- `[SerializeField] private SynergyIndicator[] indicators` — 에디터에서 사전 배치된 인디케이터 배열
- `Action<SynergyActivation> OnIndicatorClicked` — 상위 패널에 클릭 통지용 이벤트

**Start()**:
1. `SynergyManager.Instance.SynergyActivations`를 읽는다
2. 시너지 수만큼 인디케이터에 `Bind()` 호출 + `SetActive(true)`
3. 나머지 인디케이터는 `SetActive(false)`
4. 각 활성 인디케이터의 `OnClicked`에 클릭 핸들러 구독
5. 초기 정렬 호출 (`SortIndicators()`)

**OnEnable()**:
- `GlobalEventBus.Subscribe<OnSynergyRecalculatedEventDto>(HandleSynergyRecalculated)`

**OnDisable()**:
- `GlobalEventBus.Unsubscribe<OnSynergyRecalculatedEventDto>(HandleSynergyRecalculated)`

**HandleSynergyRecalculated(dto)**:
- `SortIndicators()` 호출

**SortIndicators()**:
- 빈 메서드로 선언 (TACD-297에서 구현)

**HandleIndicatorClicked(SynergyActivation)**:
- `OnIndicatorClicked?.Invoke(activation)` (CD-9)

### 3.2 SynergyInfoPanel.cs (신규)

**경로**: `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyInfoPanel.cs`
**네임스페이스**: `Scenes.Battle.Feature.Ui.SynergyInfo`
**베이스**: `MonoBehaviour`

- `[SerializeField] private SynergyListPanel listPanel`
- `[SerializeField] private SynergyDetailPanel detailPanel`

**OnEnable()**:
- `listPanel.OnIndicatorClicked += HandleIndicatorClicked`

**OnDisable()**:
- `listPanel.OnIndicatorClicked -= HandleIndicatorClicked`

**HandleIndicatorClicked(SynergyActivation)**:
- `detailPanel.Show(activation)` 호출 (CD-10)

### 3.3 SynergyDetailPanel.cs (신규 — 빈 껍데기)

**경로**: `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyDetailPanel.cs`
**네임스페이스**: `Scenes.Battle.Feature.Ui.SynergyInfo`
**베이스**: `MonoBehaviour`

- `Show(SynergyActivation activation)`: `gameObject.SetActive(true)`
- `Hide()`: `gameObject.SetActive(false)`

---

## 4. 변경 파일 요약

| 파일 | 변경 유형 |
|---|---|
| `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyListPanel.cs` | 신규 |
| `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyInfoPanel.cs` | 신규 |
| `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyDetailPanel.cs` | 신규 |
