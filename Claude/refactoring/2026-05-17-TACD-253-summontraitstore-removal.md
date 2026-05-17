# 2026-05-17 TACD-253 — SummonTraitStore 제거 (SceneSingleton 회피 + 응집도 통합)

> 본 사례는 [2026-05-17-TACD-253-synergy-init-symmetry.md](2026-05-17-TACD-253-synergy-init-symmetry.md) 의 후속 사례다.
> 트리거 비대칭 해소 후, 데이터 저장 책임의 SceneSingleton 분리 자체가 적절했는지 추가 재검토.

## 상황

리팩토링 1차 (init-symmetry) 후 구조:

```
SynergyManager
    ├─ 분배 호출: SummonTraitService.Distribute(...)
    ├─ 저장 호출: SummonTraitStore.Instance.Initialize(traitMap)  ←  SceneSingleton 경유
    └─ 통합: _unitSummonTraitMap (자체 보유) + _synergyActivations / _synergyMembers / _controllers

SummonTraitStore (SceneSingleton)
    ├─ _traitMap : SerializableDictionary<UnitLoadOutData, SynergyDefinitionData>  (인스펙터 가시성)
    ├─ Initialize / GetTrait / Clear / All
    └─ OnEnable: GlobalEventBus.Subscribe<OnBattleWin/Lose> → Clear  (라이프사이클 자기관리)
```

여기서 데이터 중복 + 책임 분산이 보였다:
- 동일한 분배 결과 데이터가 `SummonTraitStore._traitMap` 과 `SynergyManager._unitSummonTraitMap` 양쪽에 존재
- 라이프사이클 관리 (전장 종료 초기화) 가 Store 의 OnEnable/OnDisable + GlobalEventBus 구독으로 들어가 있음
- Store 의 외부 read 소비자가 *현재* 0 명 — SerializableDictionary 의 인스펙터 가시성 외에는 사용처 없음
- SceneSingleton 자체가 초기화 코드(_dontDestroyOnLoad / OnEnable / OnDisable / Awake) 를 강제 — 단순 데이터 보관에 비해 비용 큼

## 결정

**SummonTraitStore 클래스 자체를 제거**하고, 트레이트 맵을 `SynergyManager` 의 `[SerializeField] SerializableDictionary` 필드로 직접 보유.
전장 종료 정리도 `SynergyManager` 가 직접 OnBattleWin/Lose 를 구독하여 처리.

Before:

```csharp
// SummonTraitStore : SceneSingleton — OnEnable/OnDisable, BattleWin/Lose subscribe, Initialize/Clear/GetTrait/All
// SynergyManager : _unitSummonTraitMap 자체 보유 + SummonTraitStore.Initialize 호출 + HandleDefenderChanged 시 자기 맵 사용
```

After:

```csharp
// SummonTraitStore : 제거
// SynergyManager :
//     [SerializeField] summonTraitMap (SerializableDictionary)  — 인스펙터 가시성 + 데이터 보유
//     OnBattleWin/Lose 핸들러 — summonTraitMap.Clear()
//     GetSummonTrait(unit) public API — 외부 단건 조회 통로
//     DistributeAndIntegrateSummonTraits — Distribute 결과를 summonTraitMap 에 직접 채움
```

미래 확장 가이드는 `summonTraitMap` 필드의 XML doc 에 TODO 로 남김:

> *SummonTrait 관련 동작과 데이터의 응집도가 높아지면(전용 로직이 늘어나 SynergyManager 본 책임과 분리되기 시작하면) 분리한다. 분리 시 **데이터와 동작을 함께 묶어** 응집도를 유지한다 — 단순 데이터 홀더(Store) + 외부 서비스로 갈라놓는 패턴은 회피 (이전 SummonTraitStore + SummonTraitDistributor 분리 시 발생했던 스파게티 결합 회귀 방지).*

## 결정 근거

1. **SceneSingleton 의 비용** — SceneSingleton 은 *전역 접근* 을 제공하지만 그 대가로 *초기화 코드, 라이프사이클 자기관리, 인스턴스 추적, _dontDestroyOnLoad 설정, 중복 방지 가드* 등 보일러플레이트를 강제한다. 단순 *데이터 보관* 용도라면 이 비용이 정당화되지 않는다.

2. **실제 소비자 부재** — Store 의 `GetTrait` / `All` public API 를 호출하는 프로덕션 코드가 0 곳. *언젠가 필요할 수 있다* 는 추측 의존성으로 SceneSingleton 을 유지하는 것은 비용 측면에서 손해.

3. **데이터·동작 응집** — 트레이트 맵을 *사용하는* 로직(분배 통합, Defender 스폰 시 시너지 부여, 라이프사이클 정리)이 모두 SynergyManager 에 있다. 데이터가 사용처와 같은 클래스에 있으면 *어디에서 누가 언제 쓰는지* 가 코드 한 곳으로 모인다.

4. **단순 데이터 홀더 + 외부 서비스 분리 안티패턴** — Store(데이터) + Distributor(쓰기) + SynergyManager(읽기) 식 분리는 *데이터와 그 데이터를 쓰는 동작이 한 클래스에 응집되지 않는* 형태로, 변경 시 세 클래스를 동시에 봐야 하는 결합을 만든다. *어떤 책임이 늘어나면* 분리할 가치가 생기지만, 책임이 비어 있는 시점에 분리하면 텅 빈 분리의 비용만 남는다.

5. **인스펙터 가시성은 [SerializeField] 만으로 충분** — SerializableDictionary 의 [SerializeField] 만 있으면 인스펙터에서 런타임 상태가 보인다. 그 가시성을 얻기 위해 MonoBehaviour + SceneSingleton 까지 둘 필요가 없다.

6. **미래의 분리 가이드는 코드 안에** — *언젠가 분리할 수도 있는 시점* 의 기준(*"동작과 데이터의 응집도가 높아질 때"*)과 분리 방향(*"데이터+동작 한 클래스로"*)을 코드 옆 TODO 로 남겨, 미래 작업자가 같은 함정(Store+Service 분리)을 다시 파지 않게 한다.

## 재사용 가능 원칙 후보

(누적 후 정제 예정 — 잠정 표현)

- **R-5. SceneSingleton 회피 기본값** : 전역 접근이 *실제로 다수 호출자에서 필요할 때만* SceneSingleton 도입. 단순 데이터 보관 / 단일 호출자라면 일반 MonoBehaviour 또는 호출자 내부 필드로 둔다.
- **R-6. 추측 소비자로 책임 분리하지 않기** : *"언젠가 외부에서 읽을 수도 있으니 Store 로 분리"* 식의 추측은 분리 비용을 즉시 발생시키고 이득은 지연된다. 실제 소비자가 등장하면 그때 분리한다.
- **R-7. 데이터·동작 응집 우선** : 같은 데이터를 *읽고 쓰는 동작* 들이 한 클래스 안에 있으면 변경 시 추적이 한 곳에서 끝난다. 분리는 책임이 *실제로 갈라지기 시작할 때* 의 신호로 트리거한다.
- **R-8. 분리 가이드는 코드 안 TODO 로** : *미래의 분리 시점·방향* 을 코드 옆에 남겨두면 추측 분리를 미루면서도 같은 함정(잘못된 분리 형태)을 반복하지 않는다.
