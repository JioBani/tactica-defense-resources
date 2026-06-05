# 컨텍스트 문서 인덱스

> AI 에이전트용 네비게이션 진입점. 사용자 기능 계층(UR), 기능/인프라 시스템 문서, 제공 헬퍼를 한곳에서 찾는다.

## 레이어 진입점

- 사용자 기능 계층(UR): [ur/game-system.md](./ur/game-system.md)

## 기능 시스템 문서 (UR 매핑)

| 기능 | 대응 UR 노드 | 시스템 문서 |
|------|-------------|------------|
| 라운드와 페이즈 | 전장 > 라운드 & 페이즈 | [system](./system/라운드와페이즈.md) |
| 생명 크리스탈 | 전장 > 라운드 & 페이즈 (패배 조건) | [system](./system/생명크리스탈.md) |
| 소환 터미널 | 전장 > 소환 터미널 | [system](./system/소환터미널.md) |
| 소환수 | 전장 > 소환수 | [system](./system/소환수.md) |
| 대기석 | 전장 > 소환수 > 배치 & 라이프사이클 | [system](./system/대기석.md) |
| 배치 드롭존 | 전장 > 소환수 > 배치 & 라이프사이클 | [system](./system/배치드롭존.md) |
| 합성 | 전장 > 소환수 > 합성(승급) | [system](./system/합성.md) |
| 공격과 타겟팅 | 전장 > 소환수 > 소환수 동작 | [system](./system/공격과타겟팅.md) |
| 스킬 | 전장 > 소환수 > 소환수 동작 | [system](./system/스킬.md) |
| 행동 상태 | 전장 > 소환수 > 소환수 동작 | [system](./system/행동상태.md) |
| 유닛 공통(스탯) | 전장 > 유닛 스탯 & 데미지 > 능력치 & 스탯 구조 | [system](./system/유닛공통.md) |
| 데미지 계산 | 전장 > 유닛 스탯 & 데미지 > 데미지 계산 | [system](./system/데미지계산.md) |
| 시너지 | 전장 > 시너지 | [system](./system/시너지.md) |
| 소환수 특성 | 전장 > 시너지 > 소환수 특성 | [system](./system/소환수특성.md) |
| 침략자 | 전장 > 침략자 & 웨이브 | [system](./system/침략자.md) |
| 소환술사 | 전장 > 소환술사 | [system](./system/소환술사.md) |
| 전투 UI | 전장 (전투 UI) | [system](./system/전투UI.md) |

## 시스템 인프라 (UR 미매핑)

| 인프라 | 시스템 문서 |
|--------|------------|
| StatusEffect (상태이상 프레임워크) | [infra](./system/infra/StatusEffect.md) |
| 투사체 | [infra](./system/infra/투사체.md) |
| GlobalEventBus (전역 이벤트 버스) | [infra](./system/infra/GlobalEventBus.md) |
| StateBase (상태 머신 베이스) | [infra](./system/infra/StateBase.md) |
| ObjectPool (오브젝트 풀) | [infra](./system/infra/ObjectPool.md) |
| Rx (관찰 가능 값) | [infra](./system/infra/Rx.md) |
| 씬 수명과 접근 (SceneSingleton·SceneData) | [infra](./system/infra/씬수명과접근.md) |
| Draggable (드래그 앤 드롭/드롭존) | [infra](./system/infra/Draggable.md) |
| 비동기와 타이밍 (Timer·TaskQueue·Repeater 등) | [infra](./system/infra/비동기와타이밍.md) |
| 공용 유틸리티 (직렬화·인스펙터·확장 등 묶음) | [infra](./system/infra/공용유틸리티.md) |

## 헬퍼 인덱스

> "이 기능이 필요할 때 여기" — 전 문서의 제공 헬퍼 모음. 컴포넌트 단위로 묶고 주요 진입점을 사용 열에 적는다.

### 전장 기능

| 헬퍼 | 소속 | 위치(파일 경로) | 이럴 때 쓴다 |
|------|------|----------------|-------------|
| `RoundManager` | 라운드와 페이즈 | `Assets/Scenes/Battle/Feature/Round/RoundManager.cs` | 라운드 루프 접근·페이즈 전이 구독(`SetReady`·`GetCurrentRoundData`·`RoundIndex`·`RemainingFailCount`·`IsMissionFailed`) |
| `RoundAggressorManager.IsAllAggressorsCompleted` | 라운드와 페이즈 | `Assets/Scenes/Battle/Feature/Round/RoundAggressorManager.cs` | 현재 라운드 침략자 완료 여부 조회 |
| `MarketManager` | 소환 터미널 | `Assets/Scenes/Battle/Feature/Market/Scripts/MarketManager.cs` | 소환 터미널 접근·액션(`Mana`·`BuyDefender`·`Sell`·`Reroll`·`LevelUp`·`ToggleScanLock`·`DefenderPlacementLimit`) |
| `MarketUnitRoller` | 소환 터미널 | `Assets/Scenes/Battle/Feature/Market/Scripts/MarketUnitRoller.cs` | 등장 확률·추첨 상태 조회 |
| `DefenderManager` | 소환수 | `Assets/Scenes/Battle/Feature/Unit/Defender/DefenderManager.cs` | 전장 소환수 조회/생성/제거(`Defenders`·`GetBattleAreaDefenders`·`GetPlacementCount`·`GenerateDefender`·`IsAllDefenderDowned`) |
| `Defender.AddSynergy/HasSynergy/Synergies` | 소환수 | `Assets/Scenes/Battle/Feature/Unit/Defender/Defender.cs` | 개별 소환수 시너지 부여·조회 |
| `WaitingAreaReferences.Instance.waitingAreas` | 대기석 | `Assets/Scenes/Battle/Feature/WaitingArea/Scripts/WaitingAreaReferences.cs` | 대기석 슬롯 순회·빈 슬롯 탐색 |
| `DefenderSideSell.WouldIncreaseBattleCount` | 배치 드롭존 | `Assets/Scenes/Battle/Feature/Sell/Scripts/DefenderSideSell.cs` | 배치가 전장 인원을 늘리는지(신규 vs 이동) 판별 |
| `AggressorSideSellManager.GetEmptySell` | 배치 드롭존 | `Assets/Scenes/Battle/Feature/Sell/Scripts/AggressorSideSellManager.cs` | 침략자 측 빈 배치 칸 조회 |
| `DefenderFusionManager` | 합성 | `Assets/Scenes/Battle/Feature/Fusion/Scripts/DefenderFusionManager.cs` | 강화 합성 시도·강화량 조회(`TryEnhanceFusion`·`GetReinforcementAmount`) |
| `FusionGroupDetector` / `EnhancementFusionDetector` | 합성 | `Assets/Scenes/Battle/Feature/Fusion/Scripts/` | Unity 의존 없이 합성/강화 그룹 판정(테스트 용이) |
| `Attacker` (`OnAttackHitEvent`·`OnTargetEnter/Exit`) | 공격과 타겟팅 | `Assets/Scenes/Battle/Feature/Unit/Attacker/Attacker.cs` | 공격 적중/타겟 획득 구독 |
| `Victim.Hit` | 공격과 타겟팅 | `Assets/Scenes/Battle/Feature/Unit/Attacker/Victim.cs` | 피해 적용 + 적중 통지 |
| `DamageCalculator.Calculate` | 데미지 계산 | `Assets/Scenes/Battle/Feature/Unit/Damage/DamageCalculator.cs` | 공격력/기본데미지에서 최종 피해 산출 |
| `SkillCoefficientCalculator.Calculate` | 데미지 계산 | `Assets/Scenes/Battle/Feature/Unit/Damage/SkillCoefficientCalculator.cs` | 스킬 계수+시전자 스탯으로 기본 데미지 산출 |
| `Castable` / `Executable` / `SkillCast` | 스킬 | `Assets/Scenes/Battle/Feature/Unit/Casting/`, `.../Skills/Scripts/Skills/` | 새 능동 행동·효과·스킬 추가 시 상속 |
| `SkillFactory.CreateSkill` / `SkillCaster.ResetCooldown` | 스킬 | `Assets/Scenes/Battle/Feature/Unit/Skills/Scripts/` | 스킬 인스턴스 생성·쿨다운 초기화 |
| `ActionStateTransitionService` | 행동 상태 | `Assets/Scenes/Battle/Feature/Unit/ActionState/ActionStateTransitionService.cs` | 체력/타겟/페이즈 입력으로 다음 행동 상태 판정(MonoBehaviour 없이) |
| `UnitStat` / `UnitStatSheet` | 유닛 공통 | `Assets/Scenes/Battle/Feature/Unit/Unit/UnitStats/` | 출처 귀속 수정자 부여/회수, 능력치 조회, 체력·성급·강화(`AddModifier`·`Get`·`RecoverFullHealth`·`UpgradeStar`) |
| `UnitGenerator.Generate*` | 유닛 공통 | `Assets/Scenes/Battle/Feature/Unit/Unit/UnitGenerator.cs` | 종류별 유닛 인스턴스 풀 스폰 |
| `SynergyController` (추상) | 시너지 | `Assets/Scenes/Battle/Feature/Synergy/Scripts/SynergyController.cs` | 새 시너지 효과 적용 행동 추가 시 상속(+Factory 등록) |
| `SynergyStatusEffect<T>` / `TierLinkedStatusEffect<T>` | 시너지 | `Assets/Scenes/Battle/Feature/Synergy/Scripts/` | 시너지 소속/비소속 효과 작성 시 상속 |
| `SynergyManager.SynergyActivations/SynergyMembers` | 시너지 | `Assets/Scenes/Battle/Feature/Synergy/Scripts/SynergyManager.cs` | UI에서 시너지 티어·소속 조회 |
| `SummonTraitService.Distribute` | 소환수 특성 | `Assets/Scenes/Battle/Feature/SummonTrait/SummonTraitService.cs` | 소환수 풀에 특성 균등 랜덤 배정 |
| `SummonerManager` | 소환술사 | `Assets/Scenes/Battle/Feature/Unit/Summoner/SummonerManager.cs` | 편성 주입·소환술사 목록 조회(`SetFormation`·`Summoners`) |
| `AggressorPreview` / `AggressorSample` | 침략자 | `Assets/Scenes/Battle/Feature/Unit/Aggressor/Scripts/` | 웨이브 프리뷰 정지·등장 수 표시 |
| `SynergyIndicatorService` / `StatInfoPanel` / `SynergyDetailPanel` | 전투 UI | `Assets/Scenes/Battle/Feature/Ui/` | 시너지 도트 비율 계산·스탯/시너지 패널 표시 |

### 인프라

| 헬퍼 | 소속 | 위치(파일 경로) | 이럴 때 쓴다 |
|------|------|----------------|-------------|
| `StatusEffect` / `StatusEffectController` / `StatusEffectHookProvider<T>` / `IStatusEffectHook` | StatusEffect | `Assets/Common/Scripts/StatusEffect/` | 새 상태이상·효과 관리·반응 지점 추가 시 상속 |
| `ProjectileGenerator.Generate` / `Projectile.Shot`·`OnHit` | 투사체 | `Assets/Scenes/Battle/Feature/Projectiles/Scripts/` | 투사체 발급·발사·명중 구독 |
| `GlobalEventBus.Subscribe/Publish` / `IGameEvent` | GlobalEventBus | `Assets/Common/Scripts/GlobalEventBus/GlobalEventBus.cs` | 전역 이벤트 수신/발행, 새 이벤트 정의 |
| `StateBaseController<T>` / `IStateListener<T>` / `StateAnimator<T>` | StateBase | `Assets/Common/Scripts/StateBase/` | 열거형 상태 머신 작성·상태 반응 구독·상태 애니 연동 |
| `ObjectPooler.Spawn/SpawnUI/DeSpawn` / `Poolable` | ObjectPool | `Assets/Common/Scripts/ObjectPool/` | 오브젝트 풀 대여/반납 |
| `RxValue<T>` (`OnChange`·`Value`) | Rx | `Assets/Common/Scripts/Rx/RxValue.cs` | 상태 값 보유·변경 통지(반응형) |
| `SceneSingleton<T>` (`Instance`·`OnAwakeSingleton`) / `SceneData<T>` | 씬 수명과 접근 | `Assets/Common/Scripts/SceneSingleton/`, `.../SceneDataManager/` | 씬 단일 매니저 작성·전역 접근·씬 간 데이터 전달 |
| `Draggable2D` / `DropZone2D` / `ExclusiveDropZone2D` / `IDropRule` | Draggable | `Assets/Common/Scripts/Draggable/` | 드래그 이동·드롭존·단일 점유 슬롯·수용 규칙 주입 |
| `TimerManager.Make` / `GlobalTaskQueue.Enqueue` / `DynamicRepeater` / `RepeatX` / `CallbackLifetimeBinder` | 비동기와 타이밍 | `Assets/Common/Scripts/{Timer,TaskQueue,DynamicRepeater,RepeatX,CallbackLifetimeBinder}/` | 타이머·순차 비동기 큐·가변 주기 반복·N회 실행·수명 바인딩 |
| `SafeIterationList<T>` / `SerializableDictionary` / `BubbleMessageSpawner` / `Selectable2D` / `Fraction` 외 | 공용 유틸리티 | `Assets/Common/Scripts/` | 순회 중 수정 안전 리스트·직렬화·말풍선·선택·진영 enum 등 (전체는 문서 참조) |
