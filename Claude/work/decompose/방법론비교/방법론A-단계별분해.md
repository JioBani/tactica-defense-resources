# 방법론 A: 단계별 분해 — TACD-110 시너지

## Phase A: Task 목록

### 동작 트리 (상위 1-2 depth)

시너지 시스템의 라이프사이클을 중심으로 동작 트리를 구성한다.

```
시너지 시스템
├── 1. 시너지 데이터 정의
│   ├── 소환술사 효과 정의 (SynergyType.SummonerEffect)
│   └── 소환수 특성 정의 (SynergyType.SummonTrait)
│
├── 2. 소환수 특성 랜덤 부여
│   ├── 전장 시작 시 특성 풀 분배
│   └── 소환수별 특성 결정
│
├── 3. 시너지 카운트·임계치 계산
│   ├── 배치/제거 시 카운트 재계산
│   └── 임계치 판정 (소환술사 효과 2/4/6/8, 소환수 특성 2/3/4)
│
├── 4. 시너지 효과 적용/해제
│   ├── SSE 부여/제거 (활성/비활성)
│   └── 티어 변경 시 효과 갱신
│
├── 5. 개별 시너지 효과 구현
│   ├── 스탯 수정 시너지
│   └── 행동/메카닉 변경 시너지
│
└── 6. 시너지 정보 UI
    ├── 활성 시너지 목록 표시
    ├── 진행도(카운트/임계치) 표시
    └── 소환수별 특성 배치 정보
```

### 상호작용 분석 (상위)

**1. 인접 기능**
- TACD-80 유닛 수정자 시스템: 시너지 효과가 StatModifier를 추가/제거 (이미 구현됨)
- TACD-81 소환수 배치/제거: 배치 이벤트(`OnDefenderPlacementChangedEventDto`, `OnDefenderChangedEventDto`)를 구독 (이미 구현됨)
- TACD-237 소환술사 육성: 소환술사 레벨/승급에 따라 시너지 상수가 강화 (경계 밖)

**2. 상위 흐름**
- RoundManager: Maintenance→Ready→Combat 페이즈. 특성 랜덤 부여 시점은 "전장 시작 시" = Ready 또는 Combat 진입 시
- MarketManager: 소환수 구매 시점, 소환술사 편성 시스템(미구현)

**3. 공유 시스템**
- GlobalEventBus: 배치/판매 이벤트
- StatusEffect/StatusEffectController: SSE 부여 인프라
- ScriptableObject 데이터 파이프라인

**4. 데이터**
- UnitDefinitionData.Synergies: 현재 소환술사 효과만 반환. 소환수 특성을 포함하도록 확장 필요
- SynergyDefinitionData: 이미 SynergyType 필드 존재
- SynergyTier: 소환술사 효과 4단계 vs 소환수 특성 3단계 (tiers 배열로 이미 분리 가능)

**5. 플레이어**
- 시너지 정보 확인 (UI)
- 소환수 특성 확인
- 전장 배치로 시너지 조합 조정

### Task 경계 판단

현재 코드베이스에서 이미 구현된 것:
- SynergyManager, SynergyController, SynergyActivation (카운트·임계치·SSE 부여/해제 프레임워크)
- SynergyDefinitionData, SynergyType, SynergyTier (데이터 모델)
- 5개 구체 시너지 (Bruiser, Arcanist, Freljord, Warmonger, Gunslinger)
- SynergyStatusEffect, TierLinkedStatusEffect (SSE 프레임워크)

기획서 기반으로 아직 구현되지 않은 것:
- **소환수 특성 랜덤 부여 시스템** (전장마다 소환수 풀에 8개 특성 랜덤 분배)
- **UnitDefinitionData의 런타임 특성 보유** (현재 소환술사 효과만 정적으로 보유)
- **시너지 정보 UI** (현재 디버그 인스펙터만 존재)
- **소환술사 효과 임계치(2/4/6/8) vs 소환수 특성 임계치(2/3/4) 분리** (현재는 tiers로 데이터 레벨에서만 분리)
- **역할군별 특성 부여 확률 보정**
- **개별 소환수 특성 효과 구현** (8개 특성의 구체적 효과)
- **소환술사 편성에 따른 시너지 목록 동적 결정** (현재 Inspector에 하드코딩)

### Task 목록

| # | Task | 설명 |
|---|------|------|
| T1 | 소환수 특성 데이터 모델 확장 | 소환수 특성 정의 데이터 생성, 유닛에 런타임 특성 보유 구조 확장 |
| T2 | 소환수 특성 랜덤 부여 시스템 | 전장 시작 시 소환수 풀에 8개 특성을 랜덤 분배하는 로직 |
| T3 | 시너지 카운트·임계치 확장 | 소환수 특성을 시너지 카운트에 반영, SynergyManager가 동적 시너지 목록을 처리 |
| T4 | 개별 소환수 특성 효과 구현 | 8개 소환수 특성별 SynergyController + SynergyStatusEffect 구현 |
| T5 | 시너지 정보 UI | 활성 시너지 목록, 진행도, 소환수별 특성 배치 정보를 표시하는 UI |
| T6 | 소환술사 편성 연동 | SynergyManager의 시너지 목록을 소환술사 편성에서 동적으로 결정 |

---

## Phase B: Task별 Sub-task

### Task 1: 소환수 특성 데이터 모델 확장

**동작 트리 (상세)**
```
소환수 특성 데이터 모델
├── 1-1. 소환수 특성 SynergyDefinitionData 에셋 생성
│   ├── 8개 특성별 SynergyDefinitionData SO 생성
│   ├── SynergyType = SummonTrait 설정
│   └── 임계치 2/3/4 tiers 설정
│
├── 1-2. SynergyId enum 확장
│   └── 8개 소환수 특성 ID 추가
│
├── 1-3. 유닛의 런타임 특성 보유 구조
│   ├── UnitDefinitionData.Synergies에 런타임 특성 포함
│   └── Defender/Unit에 런타임 특성 슬롯 추가
│
└── 1-4. SynergyDefinitionData 기반 임계치 분화 검증
    └── 소환술사 효과(2/4/6/8) vs 소환수 특성(2/3/4)이 tiers로 올바르게 표현되는지 확인
```

**상호작용 매트릭스**

| 대상 | 트리거 | 데이터 흐름 | 효과 |
|------|--------|------------|------|
| UnitDefinitionData | - | 시너지 → Synergies 프로퍼티에 특성 포함 | Synergies 반환값 변경 |
| Defender | 특성 부여 시 | 특성 데이터 → Defender 런타임 슬롯 | 시너지 보유 여부 판정 변경 |
| SynergyController.HasSynergy | - | 런타임 특성 포함 여부 조회 | 카운트 대상 범위 확장 |
| SynergyControllerFactory | - | 새 SynergyId → switch 분기 | Factory 등록 필요 |

**보조 관점**
- 데이터: 8개 특성의 이름, 아이콘, 설명, 역할군 분류 필요 (기획)
- UI: 특성 아이콘/이름은 SynergyDefinitionData에서 제공
- 기반: SynergyId enum 확장은 코드 전체에 영향

**Sub-task 목록**

| # | Sub-task | 업무 분류 |
|---|----------|----------|
| 1-1 | 8개 소환수 특성 기획 (이름, 설명, 역할군 분류, 아이콘 정의) | 게임플레이 기획 |
| 1-2 | 8개 소환수 특성 임계치(2/3/4) 수치 설정 | 스케일링 디자인 |
| 1-3 | SynergyId enum에 8개 소환수 특성 ID 추가 | 구현 |
| 1-4 | 8개 소환수 특성 SynergyDefinitionData SO 에셋 생성 (SynergyType=SummonTrait, tiers 설정) | 콘텐츠 디자인 |
| 1-5 | Defender에 런타임 소환수 특성 슬롯 추가 (부여/조회/해제 인터페이스) | 구현 |
| 1-6 | UnitDefinitionData.Synergies가 런타임 특성을 포함하여 반환하도록 확장 | 구현 |
| 1-7 | 8개 소환수 특성 아이콘 제작 | 비주얼 |

---

### Task 2: 소환수 특성 랜덤 부여 시스템

**동작 트리 (상세)**
```
소환수 특성 랜덤 부여
├── 2-1. 부여 시점 결정
│   ├── "전장 시작 시" = RoundManager 특정 페이즈 진입 시
│   └── 매 전장(라운드)마다 재부여
│
├── 2-2. 특성 풀 구성
│   ├── 소환수 32체에 8개 특성 분배
│   ├── 동일 소환수도 전장마다 다른 특성 가능
│   └── 역할군에 따른 확률 보정
│
├── 2-3. 분배 알고리즘
│   ├── 역할군 우선 특성 vs 범용 특성 확률 분리
│   └── 각 소환수에 특성 1개 부여
│
├── 2-4. 부여 결과 저장 및 알림
│   ├── Defender에 특성 기록
│   ├── SynergyController에 알림 (카운트 재계산 트리거)
│   └── UI 갱신 이벤트 발행
│
└── 2-5. 라운드 종료 시 특성 해제
    └── 전장 종료 → 특성 초기화 → 다음 전장 재부여
```

**상호작용 매트릭스**

| 대상 | 트리거 | 데이터 흐름 | 효과 |
|------|--------|------------|------|
| RoundManager | Ready/Combat 진입 → 특성 부여 트리거 | 현재 라운드 정보 | 페이즈 전환 시 부여 실행 |
| Defender/DefenderManager | - | 전체 소환수 목록 → 부여 대상 | 각 Defender에 특성 기록 |
| SynergyController | 특성 부여 완료 → 카운트 재계산 | 새 Synergies 목록 | 카운트 변경, SSE 부여 |
| MarketManager | 소환수 풀 정보 | 전체 소환수 32체 목록 | 부여 대상 결정 |
| UnitDefinitionData | - | 역할군 정보 | 확률 보정에 사용 |
| GlobalEventBus | 특성 부여 완료 이벤트 발행 | 부여 결과 | UI 갱신 트리거 |

**보조 관점**
- 데이터: 역할군별 확률 보정 테이블 (ScriptableObject), 8개 특성-역할군 매핑
- 기반: 전장 시작/종료 시 부여/해제 훅을 RoundManager에 연결

**Sub-task 목록**

| # | Sub-task | 업무 분류 |
|---|----------|----------|
| 2-1 | 특성 랜덤 부여 시점 기획 (전장 시작 = Ready 진입? 소환수 구매 시 즉시?) | 게임플레이 기획 |
| 2-2 | 역할군별 특성 부여 확률 보정 수치 설정 (역할군 우선/범용 비율) | 스케일링 디자인 |
| 2-3 | 특성-역할군 매핑 기획 (어떤 특성이 어떤 역할군에 우선인지) | 게임플레이 기획 |
| 2-4 | TraitAssigner 서비스 클래스 구현 (소환수 풀 대상 8개 특성 랜덤 분배 알고리즘) | 구현 |
| 2-5 | 역할군별 확률 보정 Config SO 구현 (TraitProbabilityConfig) | 구현 |
| 2-6 | RoundManager/페이즈 훅 연동 (전장 시작 시 TraitAssigner 실행, 종료 시 초기화) | 구현 |
| 2-7 | 특성 부여 완료 이벤트 DTO 및 발행 (GlobalEventBus) | 구현 |

---

### Task 3: 시너지 카운트·임계치 확장

**동작 트리 (상세)**
```
시너지 카운트·임계치 확장
├── 3-1. SynergyManager 시너지 목록 동적화
│   ├── 현재: Inspector에 하드코딩된 allSynergies
│   └── 목표: 소환술사 효과(편성 기반) + 소환수 특성(8개 고정) 합산
│
├── 3-2. SynergyController.HasSynergy 확장
│   ├── 현재: UnitDefinitionData.Synergies.Contains(Definition)
│   └── 목표: 런타임 특성도 포함하여 판정
│
├── 3-3. 카운트 규칙 적용
│   ├── 중복 카운트 방지 (동일 소환수 = UnitDefinitionData.ID 기준)
│   ├── 다운되어도 카운트 유지
│   └── 배치/제거 시만 재계산
│
└── 3-4. 특성 변경 시 시너지 재계산
    ├── 전장 시작 시 새 특성 부여 → 전체 재계산
    └── 라운드 종료 시 특성 해제 → 전체 재계산
```

**상호작용 매트릭스**

| 대상 | 트리거 | 데이터 흐름 | 효과 |
|------|--------|------------|------|
| SynergyManager | 소환술사 편성 변경 / 전장 시작 | 시너지 목록 | Controller 생성/파기 |
| SynergyController | 배치 변경 이벤트 | HasSynergy 판정 | 카운트 재계산 |
| SynergyActivation | CountUnique 결과 | 유니크 수 | 임계치 판정, 티어 이벤트 |
| TraitAssigner | 특성 부여/해제 완료 | 이벤트 | 전체 시너지 재계산 트리거 |
| Defender | - | 런타임 특성 슬롯 조회 | HasSynergy에 반영 |

**보조 관점**
- 기반: SynergyManager 초기화 로직 리팩토링 (Inspector 주입 → 동적 생성)

**Sub-task 목록**

| # | Sub-task | 업무 분류 |
|---|----------|----------|
| 3-1 | SynergyController.HasSynergy 확장: 런타임 특성 슬롯 포함 판정 | 구현 |
| 3-2 | SynergyManager 시너지 목록 동적화 (소환술사 효과 + 소환수 특성 합산) | 구현 |
| 3-3 | 특성 부여/해제 시 전체 시너지 재계산 트리거 구현 | 구현 |
| 3-4 | 시너지 카운트·임계치 규칙 검증 (중복 방지, 다운 유지, 배치/제거 시 재계산) | 테스트 |

---

### Task 4: 개별 소환수 특성 효과 구현

**동작 트리 (상세)**
```
개별 소환수 특성 효과 구현
├── 4-1. 특성별 SynergyController 구현
│   ├── 단순형 (스탯 수정만): BruiserSynergyController 패턴
│   ├── 복합형 (행동 변경 포함): FreljordSynergyController/GunslingerSynergyController 패턴
│   └── 확장형 (비소속 유닛 영향): ArcanistSynergyController 패턴
│
├── 4-2. 특성별 SynergyStatusEffect 구현
│   ├── 스탯 수정 SSE
│   ├── Hook 기반 행동 변경 SSE (IOnAttackHitHook, IOnActionStateChangedHook 등)
│   └── 부수 효과 SE (TierLinkedStatusEffect)
│
├── 4-3. SynergyControllerFactory 등록
│   └── 새 SynergyId → Controller 매핑 추가
│
└── 4-4. 티어별 상수 설정
    └── 각 특성의 티어 1/2/3 수치 설정 (SynergyDefinitionData.tiers.constants)
```

**상호작용 매트릭스**

| 대상 | 트리거 | 데이터 흐름 | 효과 |
|------|--------|------------|------|
| SynergyControllerFactory | SynergyActivation 생성 시 | SynergyId → Controller | switch 분기 확장 |
| StatusEffectController | SSE Apply/Remove | Context + StatusEffect | 유닛에 효과 부여 |
| UnitStat (StatModifier) | SSE 내부 | 수정자 추가/제거 | 스탯 변경 |
| HookProvider | SSE 인터페이스 감지 | IOnAttackHitHook 등 | 행동 훅 등록 |
| StatusEffectDefinitionData | SSE 생성 시 | 메타데이터 | SE 식별용 |

**보조 관점**
- 데이터: 8개 특성별 티어 수치 (기획 필요)
- 기반: Hook 인터페이스가 부족하면 새 Hook 추가 필요

**Sub-task 목록**

| # | Sub-task | 업무 분류 |
|---|----------|----------|
| 4-1 | 8개 소환수 특성 효과 기획 (각 특성이 어떤 효과를 주는지, 스탯 변경/행동 변경 여부) | 게임플레이 기획 |
| 4-2 | 8개 소환수 특성 티어별 수치 설정 (3단계별 상수값) | 스케일링 디자인 |
| 4-3 | 특성별 SynergyController + SynergyStatusEffect 구현 (8개) | 구현 |
| 4-4 | 특성별 StatusEffectDefinitionData SO 생성 | 콘텐츠 디자인 |
| 4-5 | SynergyControllerFactory에 8개 특성 등록 | 구현 |
| 4-6 | 필요 시 새 Hook 인터페이스 추가 (기존 IOnAttackHitHook, IOnActionStateChangedHook 외) | 구현 |
| 4-7 | 개별 특성 효과 동작 검증 | 테스트 |

---

### Task 5: 시너지 정보 UI

**동작 트리 (상세)**
```
시너지 정보 UI
├── 5-1. 활성 시너지 목록 패널
│   ├── SynergyManager.SynergyActivations 구독
│   ├── 활성(티어 있음)/비활성 구분 표시
│   ├── 소환술사 효과 / 소환수 특성 구분 표시
│   └── 시너지 아이콘 + 이름 + 현재 티어
│
├── 5-2. 시너지 진행도 표시
│   ├── 현재 카운트 / 다음 임계치
│   └── 티어별 임계치 목록과 달성 여부
│
├── 5-3. 시너지 상세 정보 팝업
│   ├── 시너지 설명 (플레이스홀더 치환)
│   ├── 현재 티어의 효과 수치
│   └── 보유 소환수 목록
│
├── 5-4. 소환수별 특성 배치 정보
│   ├── 각 소환수가 어떤 특성을 보유하는지 표시
│   └── 소환수 아이콘 위 특성 아이콘 오버레이
│
└── 5-5. UI 갱신 타이밍
    ├── 배치/제거 시 실시간 갱신
    ├── 전장 시작 시 특성 부여 후 갱신
    └── 티어 변경 시 강조 효과
```

**상호작용 매트릭스**

| 대상 | 트리거 | 데이터 흐름 | 효과 |
|------|--------|------------|------|
| SynergyManager | - | SynergyActivations 딕셔너리 | UI에 데이터 제공 |
| SynergyActivation.ActiveTier.OnChange | 티어 변경 | 새 티어 값 | UI 갱신 트리거 |
| SynergyActivation.OnTierActivated/Changed/Deactivated | 티어 이벤트 | 티어 정보 | 강조 효과 |
| GlobalEventBus (배치 변경) | 배치 이벤트 | Defender 정보 | 소환수 목록 갱신 |
| SynergyDefinitionData | - | 이름, 설명, 아이콘, tiers | 표시 정보 원본 |
| Defender | - | 런타임 특성 슬롯 | 특성 배치 정보 |
| 플레이어 | 시너지 항목 클릭 | - | 상세 팝업 표시 |

**보조 관점**
- 비주얼: UI 레이아웃, 색상 팔레트, 폰트(Maplestory Bold SDF)
- 기반: SynergyDefinitionData.Description 플레이스홀더 치환 로직

**Sub-task 목록**

| # | Sub-task | 업무 분류 |
|---|----------|----------|
| 5-1 | 시너지 정보 UI 레이아웃 기획 (활성 목록, 진행도, 특성 배치 정보 배치) | 게임플레이 기획 |
| 5-2 | 시너지 정보 UI 비주얼 디자인 (아이콘 배치, 색상, 티어별 강조) | 비주얼 |
| 5-3 | SynergyInfoPanel MonoBehaviour 구현 (활성 시너지 목록 + 진행도 표시) | 구현 |
| 5-4 | SynergyDetailPopup 구현 (시너지 상세 정보 + 설명 플레이스홀더 치환) | 구현 |
| 5-5 | 소환수별 특성 오버레이 아이콘 표시 구현 | 구현 |
| 5-6 | SynergyActivation/GlobalEventBus 구독으로 실시간 UI 갱신 연결 | 구현 |
| 5-7 | 티어 활성화/변경 시 강조 연출 (트윈/이펙트) | 비주얼 |
| 5-8 | SynergyDefinitionData.Description 플레이스홀더 치환 유틸리티 구현 | 구현 |

---

### Task 6: 소환술사 편성 연동

**동작 트리 (상세)**
```
소환술사 편성 연동
├── 6-1. SynergyManager의 allSynergies를 편성에서 결정
│   ├── 편성에 포함된 소환술사 → 해당 소환술사 효과 수집
│   └── 소환수 특성 8개는 항상 포함
│
├── 6-2. 편성 변경 시 시너지 목록 재구성
│   ├── 기존 Controller Dispose
│   ├── 새 시너지 목록으로 Controller 재생성
│   └── 카운트 재계산
│
└── 6-3. 전장 진입 시 최종 시너지 목록 확정
    └── 소환술사 편성이 확정된 시점에 SynergyManager 초기화
```

**상호작용 매트릭스**

| 대상 | 트리거 | 데이터 흐름 | 효과 |
|------|--------|------------|------|
| 소환술사 편성 시스템 (TACD-237, 미구현) | 편성 확정 | 소환술사 목록 | 시너지 목록 결정 |
| SynergyManager | 편성 변경 이벤트 | 새 시너지 목록 | Controller 재구성 |
| UnitDefinitionData.SummonerEffect | - | 소환술사별 고유 효과 | 시너지 수집 |
| RoundManager | 전장 진입 시 | - | 초기화 시점 |

**보조 관점**
- 기반: 소환술사 편성 시스템(TACD-237)이 선행 또는 병행 구현 필요. 인터페이스만 정의하고 구현은 편성 에픽에서 진행 가능.

**Sub-task 목록**

| # | Sub-task | 업무 분류 |
|---|----------|----------|
| 6-1 | SynergyManager.InitializeSynergyActivations를 외부 주입 방식으로 리팩토링 (편성 시스템 인터페이스 정의) | 구현 |
| 6-2 | 소환술사 편성 → 소환술사 효과 목록 수집 로직 구현 | 구현 |
| 6-3 | 편성 변경 시 기존 Controller Dispose → 새 Controller 생성 로직 | 구현 |
| 6-4 | 소환술사 편성 연동 통합 테스트 | 테스트 |

---

## 최종 이슈 목록

```
TACD-110 [전장] 시너지
│
├── T1: 소환수 특성 데이터 모델 확장
│   ├── 1-1 [게임플레이 기획] 8개 소환수 특성 기획 (이름, 설명, 역할군 분류, 아이콘 정의)
│   ├── 1-2 [스케일링 디자인] 8개 소환수 특성 임계치(2/3/4) 수치 설정
│   ├── 1-3 [구현] SynergyId enum에 8개 소환수 특성 ID 추가
│   ├── 1-4 [콘텐츠 디자인] 8개 소환수 특성 SynergyDefinitionData SO 에셋 생성
│   ├── 1-5 [구현] Defender에 런타임 소환수 특성 슬롯 추가 (부여/조회/해제 인터페이스)
│   ├── 1-6 [구현] UnitDefinitionData.Synergies가 런타임 특성을 포함하여 반환하도록 확장
│   └── 1-7 [비주얼] 8개 소환수 특성 아이콘 제작
│
├── T2: 소환수 특성 랜덤 부여 시스템
│   ├── 2-1 [게임플레이 기획] 특성 랜덤 부여 시점 기획
│   ├── 2-2 [스케일링 디자인] 역할군별 특성 부여 확률 보정 수치 설정
│   ├── 2-3 [게임플레이 기획] 특성-역할군 매핑 기획
│   ├── 2-4 [구현] TraitAssigner 서비스 클래스 구현 (랜덤 분배 알고리즘)
│   ├── 2-5 [구현] 역할군별 확률 보정 Config SO 구현 (TraitProbabilityConfig)
│   ├── 2-6 [구현] RoundManager/페이즈 훅 연동 (전장 시작 시 실행, 종료 시 초기화)
│   └── 2-7 [구현] 특성 부여 완료 이벤트 DTO 및 GlobalEventBus 발행
│
├── T3: 시너지 카운트·임계치 확장
│   ├── 3-1 [구현] SynergyController.HasSynergy 확장: 런타임 특성 슬롯 포함 판정
│   ├── 3-2 [구현] SynergyManager 시너지 목록 동적화 (소환술사 효과 + 소환수 특성 합산)
│   ├── 3-3 [구현] 특성 부여/해제 시 전체 시너지 재계산 트리거 구현
│   └── 3-4 [테스트] 시너지 카운트·임계치 규칙 검증
│
├── T4: 개별 소환수 특성 효과 구현
│   ├── 4-1 [게임플레이 기획] 8개 소환수 특성 효과 기획
│   ├── 4-2 [스케일링 디자인] 8개 소환수 특성 티어별 수치 설정
│   ├── 4-3 [구현] 특성별 SynergyController + SynergyStatusEffect 구현 (8개)
│   ├── 4-4 [콘텐츠 디자인] 특성별 StatusEffectDefinitionData SO 생성
│   ├── 4-5 [구현] SynergyControllerFactory에 8개 특성 등록
│   ├── 4-6 [구현] 필요 시 새 Hook 인터페이스 추가
│   └── 4-7 [테스트] 개별 특성 효과 동작 검증
│
├── T5: 시너지 정보 UI
│   ├── 5-1 [게임플레이 기획] 시너지 정보 UI 레이아웃 기획
│   ├── 5-2 [비주얼] 시너지 정보 UI 비주얼 디자인
│   ├── 5-3 [구현] SynergyInfoPanel 구현 (활성 시너지 목록 + 진행도)
│   ├── 5-4 [구현] SynergyDetailPopup 구현 (상세 정보 + 플레이스홀더 치환)
│   ├── 5-5 [구현] 소환수별 특성 오버레이 아이콘 표시 구현
│   ├── 5-6 [구현] SynergyActivation/GlobalEventBus 구독으로 실시간 UI 갱신 연결
│   ├── 5-7 [비주얼] 티어 활성화/변경 시 강조 연출
│   └── 5-8 [구현] SynergyDefinitionData.Description 플레이스홀더 치환 유틸리티
│
└── T6: 소환술사 편성 연동
    ├── 6-1 [구현] SynergyManager 외부 주입 방식 리팩토링 (편성 인터페이스 정의)
    ├── 6-2 [구현] 소환술사 편성 → 소환술사 효과 목록 수집 로직
    ├── 6-3 [구현] 편성 변경 시 Controller Dispose → 재생성 로직
    └── 6-4 [테스트] 소환술사 편성 연동 통합 테스트
```

---

## 상호작용 축이 추가로 도출한 항목

동작 트리만으로는 도출하기 어려웠을 항목들을 별도로 표시한다.

| # | 항목 | 도출 경로 | 설명 |
|---|------|----------|------|
| 1 | **2-7: 특성 부여 완료 이벤트 DTO** | TraitAssigner → SynergyController 연결 분석 | 특성 부여와 시너지 재계산이 별개 시스템이므로, 이벤트 기반 통신이 필요함을 상호작용 분석에서 발견 |
| 2 | **3-3: 특성 부여/해제 시 전체 재계산 트리거** | TraitAssigner → SynergyManager 연결 분석 | 기존 SynergyController는 Defender 배치 이벤트만 구독. 특성 변경은 새로운 트리거 소스로, 상호작용 분석 없이는 간과할 수 있었음 |
| 3 | **1-6: UnitDefinitionData.Synergies 반환값 확장** | SynergyController.HasSynergy → UnitDefinitionData 데이터 흐름 분석 | 현재 HasSynergy가 `UnitDefinitionData.Synergies.Contains`를 사용하므로, 런타임 특성이 이 프로퍼티에 포함되지 않으면 카운트에서 누락됨 |
| 4 | **5-8: 설명 플레이스홀더 치환 유틸리티** | SynergyDefinitionData → UI 데이터 흐름 분석 | Description 필드에 `@ConstantName@` 플레이스홀더가 이미 정의되어 있으나, 치환 로직이 존재하지 않음. UI 표시 시 필수 |
| 5 | **4-6: 새 Hook 인터페이스** | SynergyStatusEffect → HookProvider 효과 분석 | 기존 Hook은 AttackHit, ActionStateChanged 2종. 특성 효과에 따라 새로운 행동 훅(예: OnDamageReceived, OnSkillCast 등)이 필요할 수 있음 |
| 6 | **6-1: SynergyManager 외부 주입 리팩토링** | 소환술사 편성 시스템 → SynergyManager 트리거 분석 | 현재 allSynergies가 Inspector에 하드코딩. 편성 시스템과 연동하려면 동적 주입이 필요하며, 이는 소환술사 에픽이 아닌 시너지 에픽의 준비 작업 |
| 7 | **3-1: HasSynergy 런타임 특성 판정** | Defender 런타임 슬롯 → SynergyController 데이터 흐름 | 동작 트리에서는 "카운트 재계산"으로만 보이지만, 실제로는 HasSynergy 판정 로직 자체를 변경해야 하는 구현 이슈 |
