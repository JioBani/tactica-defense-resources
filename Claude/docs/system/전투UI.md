# 전투UI — 시스템 관점

> 이 문서는 AI 에이전트가 코드 레이어에 맞게 이해하고 빠르게 코드를 찾아가도록 하는 시스템 관점 컨텍스트 문서다.
> 코드가 단일 진실이다. 이 문서는 코드를 재설명하지 않는다 — "인덱스 + α"에 한정한다.
>
> - **분류**: 기능(단순 구현) — 대응 UR 노드: `전장 (인게임)` 의 전투 화면 UI 표면(시너지 정보·유닛 스탯 패널·라운드 정보·소환 터미널 UI). 개별 도메인 노드(시너지·소환 터미널·유닛 스탯)의 *표시(view)* 측면을 한데 묶은 뷰 레이어 문서다.

## 1. 한 줄 정의

전장 도메인 매니저(시너지·마켓·라운드·유닛)가 노출하는 상태를 구독해 전투 화면에 시각화하고, 플레이어 입력(클릭)을 해당 매니저의 행동으로 위임하는 인게임 UI 뷰 레이어 — 비즈니스 로직은 보유하지 않는다.

## 2. 도메인 ↔ 코드 매핑

| 도메인 개념 | 코드 식별자 | 위치(파일 경로) | 책임 (한 줄) |
|------------|------------|----------------|-------------|
| 시너지 정보 UI 루트 | `SynergyInfoPanel` | `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyInfoPanel.cs` | 목록 패널·상세 패널 조율, 인디케이터 클릭 → 상세 표시 |
| 현재 전장 시너지 목록 표시 | `SynergyListPanel` / `SynergyIndicator` | `Ui/SynergyInfo/Scripts/SynergyListPanel.cs`, `SynergyIndicator.cs` | `SynergyManager.SynergyActivations` 바인딩, 티어 도트 표시·클릭 전달 |
| 시너지 상세(효과·보유 소환수) | `SynergyDetailPanel` + 섹션 뷰 | `Ui/SynergyInfo/Scripts/SynergyDetailPanel.cs`, `SynergyDetailEffectSection.cs`, `SynergyDetailMemberList.cs` | 단일 표시 슬롯 라이프사이클, 효과 진행도·보유 소환수 섹션 위임 |
| 유닛 스탯 패널 | `StatInfoPanel` / `StatCell` | `Ui/StatInfoPanel/Scripts/StatInfoPanel.cs`, `StatCell.cs` | 선택된 유닛의 능력치를 셀로 렌더, 합성 시 갱신 |
| 스탯 표기(이름·서식) | `UnitStatKindExtensions` | `Ui/StatInfoPanel/Scripts/UnitStatKindExtensions.cs` | `UnitStatKind` → 한국어 표시명·값 서식(% / 배수 등) |
| 라운드/페이즈 정보 표시 | `RoundUiManager` / `RoundFailCountUiManager` | `Ui/RoundInfos/RoundUiManager.cs`, `RoundFailCountUiManager.cs` | 페이즈 전환 배너, 남은 패배 횟수 표시 |
| 전장 승리·패배 화면 | `BattleWinUiManager` / `BattleLoseUiManager` | `Ui/RoundInfos/BattleWinUiManager.cs`, `BattleLoseUiManager.cs` | 승/패 이벤트 수신 → 결과 패널 표시·확인 대기 |
| 소환 터미널 UI | `MarketUiManager` | `Ui/Market/MarketUiManager.cs` | 마나·배치 상한·재스캔·스캔 잠금·터미널 열기/닫기 표시·위임 |
| 소환수 구매 슬롯 | `DefenderSlot` | `Ui/Market/DefenderSlot.cs` | 리롤된 마켓 슬롯(유닛·성급·역할군) 카드 표시 |
| 성급별 등장 확률 패널 | `StarRatesPanel` | `Ui/Market/StarRatesPanel.cs` | 레벨별 `StarProbabilityConfig` 확률 표시 |
| 배치 상한 표시 | `DefenderPlacementLimitText` | `Ui/Market/DefenderPlacementLimitText.cs` | 현재 배치 수 / 상한 표시(배치·라운드 변경 구독) |
| 카메라 전환·미리보기 | `SwitchViewManager` / `ButtonHandler` | `Ui/SwitchViewManager.cs`, `Ui/ButtonHandler.cs` | 아군/적군 진영 카메라 전환 버튼, 페이즈별 활성 제어 |

> 순수 기술 구성요소(`SynergyIndicatorService` — 비율 계산, `AlertManager` — 알림 표시 stub)는 도메인 개념이 없어 §3 구조 맵에만 둔다.

## 3. 구조 맵 (위치 중심)

진입점부터. 대부분 `MonoBehaviour` 로, 도메인 매니저의 `RxValue.OnChange` 또는 `GlobalEventBus` / `IStateListener<PhaseType>` 를 구독하는 수동 뷰다.

- **시너지 정보 UI** (`Ui/SynergyInfo/`): 루트 `SynergyInfoPanel` → `SynergyListPanel`(인디케이터 바인딩) → 클릭 → `SynergyDetailPanel.Show(SynergyActivation)`. 상세는 UIToolkit(UXML/USS, `SynergyInfo/Uxml/`) 기반이며 `SynergyDetailEffectSection`(효과·티어 진행도) + `SynergyDetailMemberList`(보유 소환수) 두 일반 클래스 섹션 뷰로 분할.
- **유닛 스탯 패널** (`Ui/StatInfoPanel/`): 진입점 `StatInfoPanel.Show(Unit)` — `OnObjectSelectedEvent` 수신으로 표시, `OnDefenderFusedEventDto` 로 갱신. `MainStats`/서브 스탯을 `StatCell` 프리팹으로 인스턴스화.
- **라운드 정보** (`Ui/RoundInfos/`): `RoundUiManager`(페이즈 배너, `IStateListener<PhaseType>`), `RoundFailCountUiManager`(남은 패배 횟수), `BattleWinUiManager`·`BattleLoseUiManager`(전장 결과, `OnBattleWin/LoseEventDto` 구독).
- **소환 터미널 UI** (`Ui/Market/`): `MarketUiManager`(마나·레벨업·리롤·스캔잠금·슬라이드 토글), `DefenderSlot`(구매 카드), `StarRatesPanel`(확률), `DefenderPlacementLimitText`(배치 수), `ShopUiEventHandler`(구매 버튼 위임). 프리팹은 `Ui/Market/Prefebs/`.
- **카메라/뷰 전환** (`Ui/`): `SwitchViewManager`(진영 전환·라운드 정보, `IStateListener<PhaseType>`), `ButtonHandler`(적 진영 미리보기 카메라 트윈).
- **알림 stub**: `AlertManager`(`SceneSingleton`) — `Ui/AlertManager.cs`. 현재 `Debug.Log` 만 하는 placeholder.

### 3.1 제공 헬퍼 (다른 코드가 찾아오는 곳)

| 헬퍼 | 위치(파일 경로) | 이럴 때 쓴다 |
|------|----------------|-------------|
| `SynergyIndicatorService.CalculateRatios` | `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyIndicatorService.cs` | 시너지 티어 도트의 fill 비율을 카운트 기준으로 계산할 때(테스트 가능하도록 MonoBehaviour에서 분리) |
| `UnitStatKindExtensions.GetDisplayName` / `FormatStatValue` | `Assets/Scenes/Battle/Feature/Ui/StatInfoPanel/Scripts/UnitStatKindExtensions.cs` | `UnitStatKind` 를 한국어 표시명·UI 서식 문자열로 변환할 때 |
| `StatInfoPanel.Show(Unit)` / `Hide()` | `Assets/Scenes/Battle/Feature/Ui/StatInfoPanel/Scripts/StatInfoPanel.cs` | 특정 유닛의 스탯 패널을 외부에서 띄우거나 닫을 때 |
| `SynergyDetailPanel.Show(SynergyActivation)` / `Hide()` / `IsVisible` | `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyDetailPanel.cs` | 시너지 상세 패널을 외부에서 표시·제어할 때 |

### 3.2 의존 관계

- **밖으로 의존**:
  - 도메인 매니저(읽기/위임): `SynergyManager`·`SynergyActivation`(시너지 — [구현](./시너지.md)), `MarketManager`(소환 터미널, `Mana`/`Level`/`IsScanLocked`/`DefenderPlacementLimit` 등 `RxValue`), `RoundManager`(라운드·페이즈), `DefenderManager`·`UnitGenerator`(소환수 배치)
  - 횡단 인프라: `GlobalEventBus`(선택·합성·승패·배치 변경 이벤트), `StateBase`/`IStateListener<PhaseType>`(페이즈 전환), `SceneSingleton`, `Common.Data`(`SynergyDefinitionData`·`UnitStatKind`·`StarProbabilityConfig`·`UnitLoadOutData`)
  - 외부 패키지: UnityEngine.UI / TMPro / UIToolkit(UIElements), DG.Tweening(연출), Cysharp UniTask(승패 확인 대기)
- **밖에서 의존**: 거의 없음 — 뷰 종단점이라 다른 시스템이 이 디렉토리를 참조하지 않는다. 다른 UI가 위 §3.1 헬퍼(스탯 서식·상세 패널 Show)를 가져다 쓰는 정도.

## 4. +α — 코드만 봐선 모르는 것

- **분할 의도 (뷰 ↔ 도메인)**: 이 디렉토리는 일관되게 *얇은 뷰* 다. 마나·티어·확률·배치 상한 등 어떤 수치/판정도 여기서 계산하지 않고 도메인 매니저의 `RxValue.OnChange` / 이벤트를 구독해 표시만 한다(각 파일 주석에 "비즈니스 로직은 …Manager가 담당" 명시). 클릭 핸들러(`OnClickLevelUp`, `OnClickReroll` 등)는 곧장 매니저 메서드로 위임한다.
- **분할 의도 (Service 분리)**: `SynergyIndicatorService` 는 비율 계산만 떼어내 MonoBehaviour 밖으로 빼서 유닛 테스트를 가능하게 한 의도적 분리(주석 명시). UI 본체는 테스트 어려운 표시만 남긴다.
- **분할 의도 (시너지 상세 UIToolkit)**: 시너지 상세만 UGUI가 아닌 UIToolkit(UXML/USS)로 구현되어, 진행도 노드/connector 의 시각 상태를 USS 클래스(`progress-node--passed/active/next` 등) 토글로 표현한다. 나머지 전투 UI는 UGUI(TMP/Image) 기반 — 혼용 상태.
- **도메인적 의도 (시너지 표시 규칙)**: 인디케이터는 카운트 1 이상인 시너지만 노출하고, 카운트는 매니저 측 유니크 기준을 그대로 따른다. 상세의 보유 소환수 목록은 시너지 종류(소환술사 효과/소환수 특성) 분기 없이 역참조 통로만 읽어, 누적 정보 없는 시너지는 자연히 빈 목록이 된다(`SynergyDetailMemberList` 주석).
- **비자명한 제약/관례**:
  - `AlertManager` 는 현재 `Debug.Log` 만 하는 placeholder — 실제 토스트/알림 UI는 미구현.
  - `ShopUiEventHandler` 는 임시 데이터(`tempUnitLoadOutData`)로 소환수를 생성하며 대기석 확인 위치에 대한 TODO 가 남아 있다(실험/임시 경로).
  - `Ui/Market/Prefebs/` 폴더명은 오타(Prefabs)지만 실제 경로이므로 그대로 사용한다.
  - 다수 패널이 `[DefaultExecutionOrder(100)]` 로 도메인 매니저(`SynergyManager` 등) 초기화 이후 `Start`/`OnEnable` 가 돌도록 실행 순서를 강제한다.

## 5. 레이어 링크

- 전역 네비게이션: [index.md](../index.md)
- 대응 사용자 기능: [game-system.md](../ur/game-system.md) 의 `전장 (인게임)` — 특히 `시너지`(시너지 카운트·UI), `소환 터미널`, `유닛 스탯 & 데미지`, `라운드 & 페이즈` 노드의 화면 표시 측면
