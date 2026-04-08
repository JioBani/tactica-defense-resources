# TACD-295: [시너지 > 정보 UI] - 활성 시너지의 아이콘/티어/진행률이 목록으로 표시된다

> 상위 이슈: TACD-252 [전장 - 시너지] - 시너지 정보 UI

## 요구사항

전장 화면 좌측 상단에 활성 시너지 목록 패널을 표시한다. 각 시너지는 아이콘, 현재 티어 진행도(도트), 진행률(카운트/임계치)을 보여주며, 소환수 배치/제거/판매 시 실시간으로 갱신된다. 시너지 아이콘을 클릭하면 상세 패널이 열리도록 이벤트를 전달한다(상세 패널 구현은 TACD-296 범위).

## 배경지식

### 시너지 시스템

시너지는 전장에 배치된 유니크 소환수 수에 따라 티어가 결정된다. 소환술사 효과(임계치 2/4/6/8, 4단계)와 소환수 특성(임계치 2/3/4, 3단계)이 있으며, UI에서는 두 종류를 구분 없이 동일하게 표시한다.

### 인디케이터 진행률 표기

- 분자: 현재 카운트 (`SynergyActivation.Count`)
- 분모: 다음 티어의 RequiredCount. 최고 티어 달성 시 최고 티어의 RequiredCount. 비활성 시 첫 번째 티어의 RequiredCount.
- 예: 카운트 3, 현재 활성 티어 임계치 2, 다음 티어 임계치 4 -> "3/4"

### 티어 도트 표기

시너지의 전체 티어 수만큼 도트를 가로로 나열한다. 활성 티어 이하의 도트는 활성 색상, 나머지는 비활성 색상으로 표시한다.

### 선행 설계

TACD-298에서 전체 설계가 완료되어 있다. 이 작업은 해당 설계의 구현이다.
- `Claude/develop/TACD-252/plan-1.md`: UI 계층 구조 + 스크립트 구조
- `Claude/develop/TACD-252/plan-2.md`: 데이터 흐름 + 갱신 설계

## 완료 정의

### 이벤트 인프라
- CD-1. SynergyController가 시너지 재계산 완료 후 GlobalEventBus로 재계산 완료 이벤트를 발행한다
  - 이벤트에는 재계산된 SynergyActivation이 포함된다

### SynergyIndicator (개별 시너지 슬롯)
- CD-2. 시너지 데이터가 바인딩되면 해당 시너지의 아이콘이 표시된다
- CD-3. 시너지의 전체 티어 수만큼 도트가 표시되고, 활성 티어까지는 활성 색상, 나머지는 비활성 색상으로 구분된다
- CD-4. 진행률이 "현재카운트/다음임계치" 형식의 텍스트로 표시된다
- CD-5. 시너지 재계산 이벤트 수신 시 해당 인디케이터의 티어 도트와 진행률 텍스트가 갱신된다
- CD-6. 인디케이터 클릭 시 상위 패널에 클릭 이벤트가 전달된다

### SynergyListPanel (목록 패널)
- CD-7. 초기화 시 SynergyManager의 시너지 목록을 읽어, 시너지 수만큼 사전 배치된 인디케이터를 활성화하고 나머지는 비활성화한다
- CD-8. 시너지 재계산 이벤트 수신 시 정렬 메서드를 호출하는 구조가 갖춰진다 (정렬 로직 자체는 TACD-297 범위)
- CD-9. 인디케이터 클릭 이벤트를 Action으로 상위(SynergyInfoPanel)에 전달한다

### SynergyInfoPanel (루트 조율자)
- CD-10. SynergyListPanel의 클릭 이벤트를 수신하여 SynergyDetailPanel에 전달하는 구조가 갖춰진다 (SynergyDetailPanel 구현은 TACD-296 범위)

### GameObject / 프리팹 구성
- CD-11. Canvas 하위에 SynergyInfoPanel > SynergyListPanel > SynergyIndicator 계층 구조가 구성된다
- CD-12. SynergyIndicator는 에디터에서 충분한 수로 사전 배치되어 있으며, 동적 Instantiate를 하지 않는다
- CD-13. SynergyDetailPanel GameObject가 비활성 상태로 존재한다 (내부 구현은 TACD-296 범위)

## 작업 단위

### [x] 단위 1: 이벤트 인프라 - OnSynergyRecalculatedEventDto

- CD-1. SynergyController가 시너지 재계산 완료 후 GlobalEventBus로 재계산 완료 이벤트를 발행한다

설계 참조: plan-2.md 2절 "GlobalEventBus 이벤트 추가"

작업 내용:
- OnSynergyRecalculatedEventDto 구조체 생성 (Events 폴더)
- SynergyController의 HandlePlacementChanged, HandleDefenderChanged에서 Recalculate 호출 후 GlobalEventBus.Publish 추가

### [x] 단위 2: SynergyIndicator 스크립트 구현

- CD-2. 시너지 데이터가 바인딩되면 해당 시너지의 아이콘이 표시된다
- CD-3. 시너지의 전체 티어 수만큼 도트가 표시되고, 활성 티어까지는 활성 색상, 나머지는 비활성 색상으로 구분된다
- CD-4. 진행률이 "현재카운트/다음임계치" 형식의 텍스트로 표시된다
- CD-5. 시너지 재계산 이벤트 수신 시 해당 인디케이터의 티어 도트와 진행률 텍스트가 갱신된다
- CD-6. 인디케이터 클릭 시 상위 패널에 클릭 이벤트가 전달된다

설계 참조: plan-1.md 3.3절 "SynergyIndicator", plan-2.md 3.2~3.4절

작업 내용:
- SynergyIndicator.cs 생성 (Ui/SynergyInfo/Scripts/)
- Bind(SynergyActivation): 아이콘 바인딩, 티어 도트 생성, RefreshDisplay 호출, 이벤트 구독
- RefreshDisplay(): 카운트 텍스트 갱신, 티어 도트 색상 갱신
- 다음 임계치 결정 로직 (private 메서드)
- OnDestroy에서 GlobalEventBus 구독 해제
- Button 클릭 → Action 이벤트 발행

### [x] 단위 3: SynergyListPanel + SynergyInfoPanel 스크립트 구현

- CD-7. 초기화 시 SynergyManager의 시너지 목록을 읽어, 시너지 수만큼 사전 배치된 인디케이터를 활성화하고 나머지는 비활성화한다
- CD-8. 시너지 재계산 이벤트 수신 시 정렬 메서드를 호출하는 구조가 갖춰진다
- CD-9. 인디케이터 클릭 이벤트를 Action으로 상위(SynergyInfoPanel)에 전달한다
- CD-10. SynergyListPanel의 클릭 이벤트를 수신하여 SynergyDetailPanel에 전달하는 구조가 갖춰진다

설계 참조: plan-1.md 3.1~3.2절, plan-2.md 3.1절

작업 내용:
- SynergyListPanel.cs 생성: SerializeField로 인디케이터 배열 참조, 초기화/바인딩/이벤트 전달
- SynergyInfoPanel.cs 생성: listPanel/detailPanel 참조, 클릭 이벤트 중계
- SynergyDetailPanel.cs는 빈 껍데기만 생성 (Show/Hide 시그니처 + SetActive 토글)
- SynergyListPanel에 SortIndicators() 빈 메서드 배치 (TACD-297에서 구현)

### [x] 단위 4: GameObject 계층 구성 및 연결

- CD-11. Canvas 하위에 SynergyInfoPanel > SynergyListPanel > SynergyIndicator 계층 구조가 구성된다
- CD-12. SynergyIndicator는 에디터에서 충분한 수로 사전 배치되어 있으며, 동적 Instantiate를 하지 않는다
- CD-13. SynergyDetailPanel GameObject가 비활성 상태로 존재한다

작업 내용:
- Unity 에디터에서 Battle 씬의 Canvas 하위에 GameObject 트리 구성 (plan-1.md 2절 참조)
- SynergyInfoPanel, SynergyListPanel, SynergyIndicator 컴포넌트 부착
- SynergyIndicator를 충분한 수(8~10개)로 사전 배치, 초기 비활성 상태
- VerticalLayoutGroup, Image, TMP_Text 등 UI 컴포넌트 구성
- SynergyDetailPanel GameObject 생성 후 비활성 상태로 배치
- SerializeField 참조 연결

## 관련 기획서

- `Document/기획서/시너지 기획 메모.md`
  - 시너지 종류, 카운트 규칙, 임계치, 시너지 정보 UI 필요 정보
- `Document/기획서/TFT 세트1 시너지 레퍼런스.md`
  - TFT 시너지 UI 레이아웃 참고 (좌측 상단 목록 배치)

## 기존 코드 참조

- `Assets/Scenes/Battle/Feature/Synergy/Scripts/SynergyActivation.cs` - 개별 시너지 카운트/티어 상태, RxValue 기반 변경 알림
- `Assets/Scenes/Battle/Feature/Synergy/Scripts/SynergyController.cs` - 시너지 효과 추상 클래스, HandlePlacementChanged/HandleDefenderChanged에서 Recalculate 호출 (이벤트 발행 추가 대상)
- `Assets/Scenes/Battle/Feature/Synergy/Scripts/SynergyManager.cs` - SynergyActivations 외부 조회 프로퍼티, Start()에서 초기화
- `Assets/Common/Data/Synergies/SynergyDefinitionData.cs` - 시너지 정의 (Icon, DisplayName, Tiers)
- `Assets/Common/Data/Synergies/SynergyTier.cs` - 티어 구조체 (Tier, RequiredCount)
- `Assets/Common/Scripts/GlobalEventBus/GlobalEventBus.cs` - 이벤트 버스 (IGameEvent 인터페이스 필수)
- `Assets/Scenes/Battle/Feature/Events/` - 기존 이벤트 DTO 위치 (신규 이벤트도 여기에 생성)
- `Assets/Scenes/Battle/Feature/Ui/Market/MarketUiManager.cs` - 기존 UI 패턴 참고 (OnEnable/OnDisable 구독/해제)

## 미결사항

- 없음
