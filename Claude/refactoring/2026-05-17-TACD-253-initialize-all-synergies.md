# 2026-05-17 TACD-253 — 시너지 초기화 흐름 단일 메서드 통합 + 필드 명명 대칭

> 본 사례는 [2026-05-17-TACD-253-summontraitstore-removal.md](2026-05-17-TACD-253-summontraitstore-removal.md) 의 후속 사례다.
> Store 제거 후에도 `SynergyManager.Start()` 안에 SummonerEffect 초기화 4 메서드 호출 + SummonTrait 통합 1 메서드 호출이 흩어져 있어 추가 정리.

## 상황

직전 리팩토링 후 `SynergyManager.Start()` 구조:

```csharp
private void Start()
{
    var summoners = SummonerManager.Instance.Summoners;

    // === SummonerEffect 초기화 ===
    BuildUnitSynergyMap(summoners);          // _unitSynergyMap 채움
    BuildSynergyMembersMap(summoners);       // _synergyMembers + _synergyMembersPublic 채움
    var uniqueEffects = new HashSet<...>();
    foreach (...) { uniqueEffects.Add(...); }
    InitializeSynergyActivations(uniqueEffects);  // _synergyActivations + _controllers 채움

    // === SummonTrait 분배 + 통합 ===
    DistributeAndIntegrateSummonTraits(summoners);  // _unitSummonTraitMap 의 역할인 summonTraitMap + _synergyActivations + _synergyMembers + _controllers 합산
}
```

두 가지 문제:

**1. 명명 비대칭 + 오해 소지**
- `BuildUnitSynergyMap` / `BuildSynergyMembersMap` / `InitializeSynergyActivations` 은 이름상 "모든 시너지" 를 다루는 듯 보이지만 실제로는 **SummonerEffect 만** 처리한다.
- 코드를 처음 보는 사람은 `InitializeSynergyActivations` 가 SummonTrait 도 다룰 것이라 오해하기 쉽다.
- 필드 `_unitSynergyMap` 도 SummonerEffect 전용임에도 일반 명명이라 `summonTraitMap` 과 비대칭.

**2. 멤버 변수 매개 암묵적 의존**
- `BuildSynergyMembersMap` 은 `_synergyMembers` 와 `_synergyMembersPublic` 을 *쓰고*, `DistributeAndIntegrateSummonTraits` 는 그 사전에 *추가* 한다.
- 호출 순서 의존이 시그니처에 드러나지 않고 *멤버 변수의 상태* 로만 표현됨 — 호출 순서를 바꾸거나 빠뜨려도 컴파일러는 잡지 못한다.
- 각 메서드가 *어디서 호출되어야 안전한지* 가 코드만으로 추적이 어렵다.

## 결정

`Start()` 의 시너지 초기화 흐름 전체를 **단일 메서드 `InitializeAllSynergies(summoners)`** 안으로 통합한다.
기존의 4 개 분리 메서드 (`BuildUnitSynergyMap`, `BuildSynergyMembersMap`, `InitializeSynergyActivations`, `DistributeAndIntegrateSummonTraits`) 는 모두 제거.
공통 유틸리티 `AddSynergyMember(synergy, unit)` 1 개만 유지 (Phase 2 에서 두 종류 매핑 합산할 때 두 번 호출).

`InitializeAllSynergies` 는 내부적으로 4 phase 구조:

```
Phase 1. 데이터 결정 — unit → synergy 매핑 두 종류 구축
   ├─ SummonerEffect: 편성에서 결정적으로 _unitSummonerEffectMap 채움
   └─ SummonTrait: SummonTraitService.Distribute → summonTraitMap 채움

Phase 2. 시너지 → unit 역참조 인덱스 구축 (두 종류 합산)
   ├─ foreach _unitSummonerEffectMap → AddSynergyMember
   └─ foreach summonTraitMap → AddSynergyMember

Phase 3. 유니크 시너지마다 SynergyActivation + Controller 생성
   └─ _synergyMembers.Keys 순회

Phase 4. 외부 노출 readonly 뷰 재구성
   └─ _synergyMembersPublic 재할당
```

동시에 필드 명명도 대칭화:
- `_unitSynergyMap` → `_unitSummonerEffectMap` (`summonTraitMap` 과 대칭)

Before:

```csharp
Start() {
    BuildUnitSynergyMap(summoners);        // 멤버변수 _unitSynergyMap 변경
    BuildSynergyMembersMap(summoners);     // 멤버변수 _synergyMembers + Public 변경
    InitializeSynergyActivations(hashSet); // 멤버변수 _synergyActivations + _controllers 변경
    DistributeAndIntegrateSummonTraits(summoners);  // 위 멤버변수 4개 + summonTraitMap 변경
}
```

After:

```csharp
Start() {
    InitializeAllSynergies(summoners);
}

InitializeAllSynergies(summoners) {
    // Phase 1: 데이터 결정 — _unitSummonerEffectMap, summonTraitMap
    // Phase 2: 역참조 인덱스 — _synergyMembers
    // Phase 3: Activation + Controller — _synergyActivations, _controllers
    // Phase 4: 외부 뷰 — _synergyMembersPublic
}

AddSynergyMember(synergy, unit) { ... }  // Phase 2 안에서만 사용되는 유일한 헬퍼
```

## 결정 근거

1. **명명이 책임을 정확히 표현해야 한다** — `BuildUnitSynergyMap` 처럼 일반 명명을 한 메서드가 실제로는 SummonerEffect 전용이면, 메서드 이름이 *거짓말* 을 하게 된다. 호출자가 *모든 시너지를 다룬다* 고 오해하기 쉽다. 일반 명명은 *실제로 일반 책임* 일 때만 쓴다. SummonerEffect 전용이면 `BuildSummonerEffectMap` 이라 부르거나, 본 사례처럼 SummonerEffect 와 SummonTrait 를 한 메서드 안에 합쳐서 *진짜 일반 책임* 으로 만든다.

2. **멤버 변수 매개 의존성은 시그니처에 드러나지 않는다** — 메서드를 작게 쪼개면서 *각자가 멤버 변수를 통해 데이터를 주고받게* 만들면, 호출 순서가 메서드 시그니처 밖으로 빠져나가 *코드 한 곳을 봐서는 알 수 없는 흐름* 이 생긴다. 단일 메서드 + phase 구조로 합치면 흐름이 *코드 위에서 아래로 한 곳에 다 보인다*.

3. **메서드 분리는 이름이 정확할 때만 의미가 있다** — 분리된 메서드가 *그 자체로 의미 있는 한 가지 일* 을 한다면 분리가 가독성에 기여한다. 그러나 본 사례의 `BuildUnitSynergyMap` 처럼 *이름이 잘못되어 오히려 오해를 일으키는* 분리는 단점이 더 크다. 차라리 인라인하여 phase 주석으로 흐름을 표현하는 게 낫다.

4. **공통 유틸 헬퍼는 분리해도 무관** — `AddSynergyMember(synergy, unit)` 처럼 *입력 인자만으로 책임이 완결되는* 작은 유틸은 분리해도 멤버 변수 매개 의존성을 만들지 않는다 (인자로 받은 것만 처리하고 결과를 멤버 변수 하나에만 추가). 이런 헬퍼는 코드 중복 제거 효과가 명확하므로 분리 유지.

5. **필드 명명 대칭** — `summonTraitMap` 과 `_unitSummonerEffectMap` 처럼 *같은 차원의 두 데이터* 는 명명이 대칭이어야 한 곳을 보고 다른 한 곳을 추론하기 쉽다. `_unitSynergyMap` 같은 일반 명명은 *한 종류만 다루는데 일반인 척하는* 형태로, 미래에 *또 다른 일반 명명* 이 등장하면 충돌이 난다.

## 재사용 가능 원칙 후보

(누적 후 정제 예정 — 잠정 표현)

- **R-9. 일반 명명은 진짜 일반 책임일 때만** : 메서드/필드 이름이 일반(`UnitSynergy`, `Initialize`) 이면 그 메서드/필드도 *모든 종류를 다뤄야* 한다. 한 종류만 다루면서 일반 명명을 쓰면 거짓말. 종류 한정이면 종류명을 이름에 (`SummonerEffect`, `SummonTrait`).
- **R-10. 멤버 변수 매개 의존성 회피** : 메서드 A 가 멤버 변수를 채우고 메서드 B 가 그 멤버 변수를 읽는 구조는 시그니처에 드러나지 않는 *호출 순서 의존* 을 만든다. 단일 메서드 + phase 주석으로 합치거나, 인자/반환값으로 의존을 명시화한다.
- **R-11. 메서드 분리의 정당성 = 정확한 이름** : 메서드를 분리할 가치가 있으려면 *그 분리된 메서드가 한 가지 의미 있는 일을 하고 그 의미를 정확히 이름으로 표현* 할 수 있어야 한다. 두 조건 중 하나라도 미충족이면 인라인.
- **R-12. 같은 차원 데이터는 대칭 명명** : 짝이 되는 두 필드/변수 (예: SummonerEffect 매핑 vs SummonTrait 매핑) 는 명명이 대칭이어야 한 곳을 보고 다른 한 곳을 추론 가능. 한쪽만 일반 명명 하면 비대칭 인지 비용 발생.
