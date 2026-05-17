# 2026-05-17 TACD-253 — 시너지 초기화 트리거 비대칭 해소

## 상황

`SynergyManager`가 SummonerEffect와 SummonTrait 두 종류의 시너지를 다루는데, **초기화 트리거가 비대칭**으로 갈라져 있었다.

| 시너지 종류 | 초기화 위치 | 트리거 |
|---|---|---|
| SummonerEffect | `SynergyManager.Start()` 자체 | 없음 (편성 데이터 직접 읽음) |
| SummonTrait | `SynergyManager.HandleSummonTraitsDistributed` | `OnSummonTraitsDistributedEventDto` 구독 |

추가로 이벤트 체인이 3 단으로 늘어나 있었다:

```
RoundManager.Start
    → publish OnBattleStartEventDto
        → SummonTraitDistributor.OnBattleStart
            → SummonTraitStore.Initialize
            → publish OnSummonTraitsDistributedEventDto
                → SynergyManager.HandleSummonTraitsDistributed
                    → _synergyActivations / _synergyMembers / _controllers 합산
```

흐름 추적이 길고, *왜 SummonerEffect 만 다른 위치에서 처리되는지* 가 코드만 봐서는 안 보였다.

## 결정

**분배 호출과 SummonTrait 통합 처리를 `SynergyManager.Start()` 안의 SummonerEffect 초기화 옆으로 이동**한다.
분배 *로직 자체* 는 별도 클래스(`SummonTraitService`)로 분리된 상태 유지. `SynergyManager` 가 그 서비스를 직접 호출.

Before:

```csharp
// RoundManager.Start  : publish OnBattleStartEventDto
// SummonTraitDistributor : 구독 → Distribute → Store.Initialize → publish OnSummonTraitsDistributedEventDto
// SynergyManager.Start : SummonerEffect 만 초기화
// SynergyManager.HandleSummonTraitsDistributed : 별도 핸들러에서 SummonTrait 통합
```

After:

```csharp
// SynergyManager.Start
//   - SummonerEffect 초기화
//   - SummonTraitService.Distribute(...) 호출 → SummonTraitStore.Initialize
//   - SummonTrait 통합 (활성화/역참조/컨트롤러)
// SummonTraitDistributor / OnSummonTraitsDistributedEventDto / OnBattleStartEventDto 폐기 (다른 소비자 없으면)
```

라이프사이클 의존이 추가될 가능성에 대비하여 SynergyManager 의 SummonTrait 호출 지점에 다음 TODO 주석:

```
// TODO: 전장 초기화 로직(편성 주입·씬 데이터 로드 등)이 본 호출보다 늦게 완료되어야 하는 의존성이
//       추가되면, RoundManager 등에서 적절한 시점에 이벤트(OnBattleStartEventDto 류)를 발행하고
//       본 호출을 그 구독 핸들러로 이동하여 명시적 라이프사이클 결합으로 전환한다.
```

## 결정 근거

1. **트리거 대칭성 우선** — 같은 클래스(`SynergyManager`)가 다루는 같은 범주(시너지)의 초기화가 트리거 두 개로 갈라져 있으면, 코드 독해자가 *"왜 한쪽만 다른 방식인가"* 를 매번 추론해야 한다. 추론 비용은 누적된다.

2. **이벤트는 비용** — 이벤트는 발행자·구독자를 분리하지만 *흐름 추적 비용* 을 발생시킨다 (publish 지점에서 어떤 구독자가 반응하는지 정적 탐색만으로 확정 불가). 비용 대비 분리 가치가 분명하지 않으면 직접 호출이 더 명확하다.

3. **이벤트의 정당한 도입 시점** — *(a) 구독자가 다수이거나*, *(b) 발행 시점과 처리 시점이 라이프사이클상 분리되어야 하거나*, *(c) 발행자가 구독자를 알아서는 안 되는 책임 경계가 있을 때*. 본 사례는 셋 다 해당하지 않음 (구독자 1 명 / 같은 Start 시점 / 같은 시너지 영역 안의 호출).

4. **분리해도 좋은 것: 계산 로직** — 분배 *알고리즘* 은 별도 클래스(`SummonTraitService`)로 분리하는 게 옳다 (단위 테스트 용이성·재사용성). 분리해도 *호출 위치* 는 단순할 수 있다 — 분리 ≠ 이벤트 도입.

5. **추측 의존성보다 명시적 의존성** — *"언젠가 라이프사이클 의존성이 생길 수 있으니 이벤트로 빼두자"* 는 추측 기반 설계. 그 시점이 오면 그때 바꾸되, *어떤 시점에 어떻게 바꿔야 하는지* 를 TODO 주석으로 남겨 두는 것으로 충분.

## 재사용 가능 원칙 후보

(누적 후 정제 예정 — 잠정 표현)

- **R-1. 트리거 대칭** : 같은 클래스 안에서 같은 범주의 작업이 여러 트리거로 갈라져 있으면 통합을 검토한다. 통합이 어려운 *진짜 라이프사이클 차이* 가 있을 때만 분리 유지.
- **R-2. 이벤트 도입 정당화 3 기준** : (a) 다수 구독자 / (b) 라이프사이클 분리 / (c) 책임 경계 보존 — 중 하나도 해당 없으면 직접 호출.
- **R-3. 책임 분리와 호출 위치는 독립** : 계산 로직을 별도 클래스로 분리하는 것과 그 호출을 이벤트로 트리거하는 것은 별개 결정.
- **R-4. 추측 의존성은 TODO 로** : "지금은 단순 호출, 의존성 생기면 이벤트화" 같은 *조건부 미래 변경* 은 코드 주석으로 남기고 현재는 단순 형태 유지.
