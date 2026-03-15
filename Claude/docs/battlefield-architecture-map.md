# 전장(인게임) 클래스 맵

> 07 게임 시스템 기획서의 기능 범례에 따라 전장 관련 클래스를 정리한 문서.
> 각 클래스의 내부 로직은 다루지 않으며, "이 기능을 수정하려면 어떤 클래스를 봐야 하는가"를 빠르게 파악하기 위한 용도.

---

## 1. 유닛

유닛 공통(소환수/침략자 모두 해당)의 능력치, 스탯, 데미지, 공격, 스킬, 행동 상태 관련 클래스.

### 1.1 유닛 기본

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Unit | 모든 유닛의 기본 클래스. 스탯 시트, 상태머신, 체력바를 포함 | Unit/Scripts/Unit.cs |
| UnitGenerator | UnitLoadOutData로부터 소환수/침략자 프리팹을 오브젝트 풀에서 스폰 및 초기화 | Unit/UnitGenerator.cs |

### 1.2 스탯 시스템

| 클래스 | 역할 | 경로 |
|--------|------|------|
| UnitStatSheet | 유닛의 모든 스탯 시트. 성급/강화 변경 시 스탯 업데이트 | Unit/UnitStats/UnitStatSheet.cs |
| UnitStat | 개별 스탯의 값 및 계산 모드 관리 | Unit/UnitStats/UnitStats.cs |
| StatModifier | 스탯 수정자 데이터 구조 (Flat/Percent 타입) | Unit/UnitStats/StatModifier.cs |

### 1.3 데미지 계산

| 클래스 | 역할 | 경로 |
|--------|------|------|
| DamageCalculator | 기획서 기반 데미지 공식 실행 | Unit/Damage/DamageCalculator.cs |
| SkillCoefficientCalculator | 스킬 계수와 스탯으로부터 최종 계수값 계산 | Unit/Damage/SkillCoefficientCalculator.cs |
| DamageType | 데미지 종류 enum (Physical/Magical) | Common/Data/Damage/DamageType.cs |

### 1.4 공격 시스템

공격은 캐스팅 시스템(Castable/Executable)을 통해 처리된다.
공격 적중 시 `Victim.Hit` → `Attacker.NotifyAttackHit` → `OnAttackHitHookProvider` → SE 훅 순으로 이벤트가 전파된다.

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Attacker | 대상 감지 및 공격 실행. DynamicRepeater로 공격 루프 관리. OnAttackHitEvent로 적중 이벤트 발행 | Unit/Attacker/Attacker.cs |
| Victim | 피해를 받는 대상 컴포넌트. Hit 시 Attacker에 적중 알림 | Unit/Attacker/Victim.cs |
| Castable | 캐스트 가능한 능력의 추상 기본 클래스 | Unit/Casting/Castable/Castable.cs |
| AttackCast | 일반 공격을 Castable로 래핑. 발사체 생성 및 Executor 연결 | Unit/Casting/Castable/AttackCast.cs |
| Executable | 캐스트 결과로 실행되는 액션의 추상 기본 클래스 | Unit/Casting/Executable/Executable.cs |
| RangeAttackExecutor | 원거리 공격 실행. 데미지 계산 후 Victim.Hit 호출 | Unit/Casting/Executable/RangeAttackExecutor.cs |
| SkillCaster | 스킬 쿨다운 관리 및 스킬 캐스트 실행 | Unit/Casting/Caster/SkillCaster.cs |

### 1.6 스킬 시스템

| 클래스 | 역할 | 경로 |
|--------|------|------|
| SkillFactory | SkillDefinitionData 타입에 따라 SkillCast 인스턴스 생성 | Unit/Skills/Scripts/SkillFactory.cs |
| SkillCast | 스킬 캐스트의 추상 기본 클래스 (Castable 상속) | Unit/Skills/Scripts/Skills/SkillCast.cs |
| SkillCreateContext | 스킬 생성에 필요한 DTO | Unit/Skills/Scripts/SkillCreateContext.cs |
| InitializeContext | 스킬 초기화 시 전달 정보 | Unit/Skills/Scripts/Contexts/InitializeContext.cs |
| CanExecuteContext | 스킬 실행 가능 여부 판정 시 전달 정보 | Unit/Skills/Scripts/Contexts/CanExecuteContext.cs |
| ExecuteContext | 스킬 실행 시 전달 정보 | Unit/Skills/Scripts/Contexts/ExecuteContext.cs |
| FireArrow | 화살 발사 스킬 구현 (SkillCast 상속) | Unit/Skills/Scripts/Skills/FireArrow/FireArrow.cs |
| FireArrowExecutor | 화살 발사 스킬의 실행 로직 (Executable 상속) | Unit/Skills/Scripts/Skills/FireArrow/FireArrowExecutor.cs |

### 1.7 행동 상태

| 클래스 | 역할 | 경로 |
|--------|------|------|
| ActionStateType | 유닛 행동 상태 enum (Idle/Move/Attack/Downed/Freeze/Waiting) | Unit/ActionState/ActionStateType.cs |
| ActionStateController | 유닛 행동 상태 전환 관리 (StateBase 패턴) | Unit/ActionState/ActionStateController.cs |
| ActionStateTransitionService | 행동 상태 전환 판정 로직 (순수 C# 클래스) | Unit/ActionState/ActionStateTransitionService.cs |

### 1.8 이동

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Mover | 유닛의 이동 로직 및 Rigidbody2D 속도 제어 | Unit/Mover/Mover.cs |

### 1.9 발사체

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Projectile | 목표를 추적하며 충돌 판정 후 풀에 반환 | Projectiles/Scripts/Projectile.cs |
| ProjectileGenerator | ObjectPooler를 통해 발사체 인스턴스를 생성하는 싱글톤 | Projectiles/Scripts/ProjectileGenerator.cs |

### 1.10 체력바

| 클래스 | 역할 | 경로 |
|--------|------|------|
| HealthBar | HP 바 UI 렌더링. 성급에 따른 테두리 색 변경 | Unit/HealthBar/Scripts/HealthBar.cs |

### 1.11 유닛 데이터 (ScriptableObject)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| UnitDefinitionData | 유닛 식별 정보 (ID, 이름, 비용, 아이콘, 시너지 정의) | Common/Data/Units/UnitDefinitions/UnitDefinitionData.cs |
| UnitStatsByLevelData | 성급별 능력치 정의 (체력, 공격력, 방어력 등 13가지) | Common/Data/Units/UnitStatsByLevel/UnitStatsByLevelData.cs |
| UnitLoadOutData | 유닛 완전 설정 번들 (정의 + 스킬 + 스탯 + 성급별 비용) | Common/Data/Units/UnitLoadOuts/UnitLoadOutData.cs |
| SkillDefinitionData | 스킬 정의 ScriptableObject | Common/Data/Skills/SkillDefinitions/SkillDefinitionData.cs |
| SkillCoefficient | 스킬 계수 데이터 | Common/Data/Skills/SkillCoefficient.cs |
| StatScaling | 스탯 스케일링 정의 | Common/Data/Skills/StatScaling.cs |

---

## 2. 소환수

소환수(플레이어 유닛)의 라이프사이클, 합성(승급), 배치 관련 클래스.

### 2.1 소환수 관리

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Defender | 소환수 개별 개체. 드래그 기능 및 배치 상태 관리 | Unit/Defender/Defender.cs |
| DefenderManager | 소환수 생성/제거/조회 및 배치 변경 감지. 싱글톤 | Unit/Defender/DefenderManager.cs |
| Placement | 소환수 배치 위치 enum (WaitingArea/BattleArea) | Unit/Defender/Placement.cs |

### 2.2 합성 (승급)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| DefenderFusionManager | 기본 합성(3개 승급) 자동 연쇄 및 강화 합성(드래그) 실행 관리. 싱글톤 | Fusion/Scripts/DefenderFusionManager.cs |
| FusionGroupDetector | 동일 유닛 3개 이상의 기본 합성 그룹 탐지 (순수 로직) | Fusion/Scripts/FusionGroupDetector.cs |
| EnhancementFusionDetector | 3성+ 타겟과 동일 종류 2성/3성 재료 쌍의 강화 합성 탐지 (순수 로직) | Fusion/Scripts/EnhancementFusionDetector.cs |
| FusionCandidate | 합성 후보 데이터 (struct) | Fusion/Scripts/FusionCandidate.cs |
| EnhancementFusionResult | 강화 합성 결과 (struct) | Fusion/Scripts/EnhancementFusionResult.cs |

### 2.3 대기석 (배치 대기 영역)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| WaitingAreaReferences | 대기석 슬롯 목록 관리. 싱글톤 | WaitingArea/Scripts/WaitingAreaReferences.cs |
| WaitingArea | 개별 대기석 슬롯. 드롭 규칙 (정비 페이즈만 이동 가능) | WaitingArea/Scripts/WaitingArea.cs |

---

## 3. 소환 터미널

소환(구매), 스캔(리롤), 잠금, 환원(판매), 배치상한 증가, 마나 충전 관련 클래스.

### 3.1 상점 로직

| 클래스 | 역할 | 경로 |
|--------|------|------|
| MarketManager | 소환 터미널의 경제 로직 총괄 (마나, 스캔, 소환수 구매, 배치상한 증가, 판매) | Market/Scripts/MarketManager.cs |
| MarketDefenderSlot | 상점 슬롯에 등장한 소환수 데이터 (유닛, 성급, 판매 여부) | Market/Scripts/MarketDefenderSlot.cs |
| MarketUnitRoller | 리롤 시 소환수 성급과 코스트를 확률에 따라 결정 | Market/Scripts/MarketUnitRoller.cs |

### 3.2 판매

| 클래스 | 역할 | 경로 |
|--------|------|------|
| DefenderSellZone | 드래그 끝 지점이 판매 영역인지 판정하고 판매 처리 | Market/Scripts/DefenderSellZone.cs |
| DefenderSideSell | 아군 배치 영역에서 배치 룰 검사 및 강화 합성 처리 | Sell/Scripts/DefenderSideSell.cs |
| AggressorSideSell | 적군 판매 슬롯에 점유하는 유닛 관리 | Sell/Scripts/AggressorSideSell.cs |
| AggressorSideSellManager | 적군 판매 슬롯 목록 관리 및 빈 슬롯 조회 | Sell/Scripts/AggressorSideSellManager.cs |

### 3.3 설정 데이터 (ScriptableObject)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| ManaIncomeConfig | 라운드별 기본 마나, 이자, 연승/연패 보너스 설정 | Common/Data/Configs/ManaIncomeConfig.cs |
| PlacementConfig | 초기 배치상한 및 레벨업 테이블 (배치상한과 비용 정의) | Common/Data/Configs/PlacementConfig.cs |
| StarProbabilityConfig | 배치상한 레벨별 소환수 성급(1/2/3성) 등장 확률 설정 | Common/Data/Configs/StarProbabilityConfig.cs |

### 3.4 상점 UI

| 클래스 | 역할 | 경로 |
|--------|------|------|
| MarketUiManager | 마나 표시, 배치 증가/재스캔/스캔 잠금 버튼, 터미널 열기/닫기 관리 | Ui/Scripts/MarketUiManager.cs |
| DefenderSlot | 상점 슬롯의 소환수 이미지, 성급, 마나 표시 및 구매 처리 | Ui/Scripts/DefenderSlot.cs |
| ShopUiEventHandler | 상점의 소환수 구매 버튼 클릭 처리 및 배치 제약 검증 | Ui/Scripts/ShopUiEventHandler.cs |
| StarRatesPanel | 마켓 레벨에 따른 성급별 등장 확률 표시 UI | Ui/Scripts/StarRatesPanel.cs |
| DefenderPlacementLimitText | "현재 배치/최대 배치" 표시 텍스트 갱신 | Ui/Scripts/DefenderPlacementLimitText.cs |

---

## 4. 라운드

라운드 진행, 페이즈(정비/전투), 승패 조건 관련 클래스.

### 4.1 라운드 관리

| 클래스 | 역할 | 경로 |
|--------|------|------|
| RoundManager | StateBase 패턴 기반 라운드 상태 관리 및 페이즈 전환. 싱글톤 | Round/RoundManager.cs |
| PhaseType | 라운드 페이즈 enum (Maintenance/Ready/Combat/End/RoundLose/BattleWin/BattleLose) | Round/Phase/PhaseType.cs |
| WaveManager | 웨이브 관리 (현재 미구현) | Round/Wave/WaveManager.cs |

### 4.2 라운드 UI

| 클래스 | 역할 | 경로 |
|--------|------|------|
| RoundUiManager | 라운드/페이즈 전환 시 패널 애니메이션 표시 | Ui/Scripts/RoundUiManager.cs |
| RoundInfoViewer | 정비 페이즈에서 다음 라운드 침략자 프리뷰 표시 | Round/RoundInfoViewer.cs |
| SwitchViewManager | 아군/적군 진영 전환 버튼과 라운드 정보 표시 관리 | Ui/Scripts/SwitchViewManager.cs |
| RoundFailCountUiManager | 남은 라운드 실패 횟수 표시 | Ui/Scripts/RoundFailCountUiManager.cs |
| BattleWinUiManager | 전투 승리 패널 표시 및 씬 전환 | Ui/Scripts/BattleWinUiManager.cs |
| BattleLoseUiManager | 전투 패배 패널 표시 및 씬 전환 | Ui/Scripts/BattleLoseUiManager.cs |

---

## 5. 침략자

침략자(적 유닛) 스폰, 웨이브, 전장 데이터 관련 클래스.

### 5.1 침략자 유닛

| 클래스 | 역할 | 경로 |
|--------|------|------|
| Aggressor | 침략자 유닛. 다운 시 오브젝트 풀 반납, 방어선 진입 시 즉시 제거 | Unit/Aggressor/Scripts/Aggressor.cs |
| AggressorPreview | 프리뷰 모드에서 침략자를 Freeze 상태로 고정하고 등장 숫자 표시 | Unit/Aggressor/Scripts/AggressorPreview.cs |
| AggressorSample | 침략자 그룹의 등장 숫자를 텍스트로 표시하는 UI 컴포넌트 | Unit/Aggressor/Scripts/AggressorSample.cs |

### 5.2 스폰 관리

| 클래스 | 역할 | 경로 |
|--------|------|------|
| RoundAggressorManager | Combat 진입 시 SpawnEntry를 순회하며 침략자를 시간차 비동기 스폰 | Round/RoundAggressorManager.cs |

### 5.3 전장 데이터 (ScriptableObject)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| BattlefieldData | 전장 메타데이터 (이름, 타입, 라운드 목록) | Common/Data/Battlefields/BattlefieldData.cs |
| BattlefieldData.RoundData | 라운드의 모든 침략자 스폰 정보 (내부 클래스) | (위와 동일) |
| BattlefieldData.SpawnEntry | 침략자 유닛, 수량, 스폰 시간, 성급 정의 (내부 클래스) | (위와 동일) |
| BattlefieldData.BattlefieldType | 전장 타입 enum (일반/정예/보스) | (위와 동일) |
| BattlefieldData.RewardData | 전장 클리어 시 보상 정의 (내부 클래스) | (위와 동일) |

### 5.4 방어선 (라운드 실패 판정)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| LifeCrystalAttacker | 침략자 방어선 통과 시 라운드 실패 이벤트 발행 | LifeCrystal/Scripts/LifeCrystalAttacker.cs |
| LifeCrystalManager | 생명력 크리스탈 관리 (현재 미구현) | LifeCrystal/Scripts/LifeCrystalManager.cs |

---

## 6. 시너지

시너지 데이터 정의 및 런타임 효과 적용 시스템.

### 6.1 시너지 데이터

| 클래스 | 역할 | 경로 |
|--------|------|------|
| SynergyDefinitionData | 시너지 정의 ScriptableObject (id, displayName, icon, synergyType, tiers) | Common/Data/Synergies/SynergyDefinitionData.cs |
| SynergyTier | 시너지 단계별 정의 struct (requiredCount, constants) | Common/Data/Synergies/SynergyTier.cs |
| SynergyType | 시너지 종류 enum (SummonerEffect / SummonTrait) | Common/Data/Synergies/SynergyType.cs |
| SynergyId | 시너지 고유 식별자 enum | Common/Data/Synergies/SynergyId.cs |

### 6.2 시너지 프레임워크

| 클래스 | 역할 | 경로 |
|--------|------|------|
| SynergyManager | 배치 변경 시 시너지 카운트 재계산 및 Controller 활성화/비활성화. 싱글톤 | Synergy/Scripts/SynergyManager.cs |
| SynergyActivation | 시너지별 카운트·티어 상태 관리. ActiveTier(RxValue) 제공 | Synergy/Scripts/SynergyActivation.cs |
| SynergyController | Template Method 패턴. SSE 부여를 보장하고 자식이 추가 효과를 확장 | Synergy/Scripts/SynergyController.cs |
| SynergyControllerFactory | SynergyId별 Controller 생성. 싱글톤 | Synergy/Scripts/SynergyControllerFactory.cs |
| SynergyStatusEffect\<T\> | 시너지 전용 SE 추상 클래스. ActiveTier 구독으로 티어 변경/비활성화 자체 처리 | Synergy/Scripts/SynergyStatusEffect.cs |
| TierLinkedStatusEffect\<T\> | 비보유 유닛용 부수 SE. SSE와 동일한 티어 구독 생명주기 | Synergy/Scripts/TierLinkedStatusEffect.cs |

### 6.3 SE HookProvider (공격 적중 훅)

| 클래스 | 역할 | 경로 |
|--------|------|------|
| IOnAttackHitHook | 공격 적중 시 SE에 전달되는 훅 인터페이스 | Common/Scripts/StatusEffect/HookProvider/IOnAttackHitHook.cs |
| OnAttackHitHookProvider | Attacker 이벤트를 구독하여 IOnAttackHitHook SE에 전달 | Common/Scripts/StatusEffect/HookProvider/OnAttackHitHookProvider.cs |
| DefenderStatusEffectController | Defender용 SE Controller. OnAttackHitHookProvider 등록 | Unit/Unit/Scripts/DefenderStatusEffectController.cs |

---

## 보조: 이벤트 (GlobalEventBus DTO)

피처 간 통신에 사용되는 이벤트 DTO 목록.

| 클래스 | 발행 시점 | 경로 |
|--------|----------|------|
| OnDefenderDragEventDto | 소환수 드래그 시작/종료 | Events/OnDefenderDragEventDto.cs |
| OnDefenderPlacementChangedEventDto | 소환수 배치 위치 변경 (대기석/전장) | Events/OnDefenderPlacementChangedEventDto.cs |
| OnDefenderFusedEventDto | 소환수 합성 완료 시 | Events/OnDefenderFusedEventDto.cs |
| OnManaNotEnoughDto | 마나 부족 시 | Market/Scripts/OnManaNotEnoughDto.cs |
| RoundAggressorCompletedEventDto | 라운드의 모든 침략자 처치 시 | Events/RoundAggressorCompletedEventDto.cs |
| OnRoundLoseEventDto | 침략자 방어선 통과 (라운드 실패) | Events/OnRoundLoseEventDto.cs |
| OnBattleWinEventDto | 전투 승리 (모든 라운드 클리어) | Events/OnBattleWinEventDto.cs |
| OnBattleLoseEventDto | 전투 패배 (크리스탈 파괴) | Events/OnBattleLoseEventDto.cs |
| OnLifeCrystalPointChangedEventDto | 생명력 크리스탈 HP 변화 | Events/OnLifeCrystalPointChangedEventDto.cs |

---

## 보조: 기타

| 클래스 | 역할 | 경로 |
|--------|------|------|
| CameraControlManager | 페이즈/뷰 전환 시 카메라 위치/줌 애니메이션 | CameraControl/Scripts/CameraControlManager.cs |
| ObjectPoolingReferences | 풀링할 유닛 프리팹 참조. 싱글톤 | ObjectPool/Scripts/ObjectPoolingReferences.cs |
| AlertManager | 디버그 알림 메시지 출력. 싱글톤 | Ui/Scripts/AlertManager.cs |
| StatInfoPanel | 클릭된 유닛의 상세 스탯 패널 표시 | Ui/Scripts/StatInfoPanel.cs |
| StatCell | 유닛 스탯 하나를 표시하는 그리드 셀 | Ui/Scripts/StatCell.cs |
| UnitStatKindExtensions | 스탯 종류별 표시명과 포맷 규칙 확장 메서드 | Ui/Scripts/UnitStatKindExtensions.cs |
| ButtonHandler | 적군 진영 표시 버튼의 카메라 애니메이션 처리 | Ui/Scripts/ButtonHandler.cs |
