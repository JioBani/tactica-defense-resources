# Rx — 시스템 관점

> 이 문서는 **AI 에이전트가 코드 레이어에 맞게 이해하고 빠르게 코드를 찾아가도록** 하는 시스템 관점 컨텍스트 문서다.
> 코드가 단일 진실(single source of truth)이다. 이 문서는 코드를 재설명하지 않는다 — **"인덱스 + α"** 에 한정한다.
>
> - **분류**: 인프라 — UR 미매핑 횡단 기반 (특정 사용자 기능에 매핑되지 않고 마나·티어·스탯 등 여러 기능이 공통으로 사용)
> - **이 문서가 다루는 것**: ① 이 대상이 시스템에서 무엇인가(한 줄) ② 주요 추상 ↔ 코드(파일 경로)의 매핑 ③ 무엇이 어디에 있는가(구조 맵) ④ 다른 코드가 가져다 쓸 헬퍼 ⑤ 코드만 봐선 모르는 의도(+α).
> - **이 문서가 다루지 않는 것**: 코드의 동작을 풀어쓴 설명서, 메서드 본문 요약. 그건 코드를 보면 된다.

---

## 1. 한 줄 정의

`RxValue<T>` 는 값이 실제로 바뀔 때만 구독자에게 변경을 통지하는, 타입 무관 관찰 가능한 값(observable value) 컨테이너다.

## 2. 주요 추상 ↔ 코드 매핑

| 주요 추상 | 코드 식별자 | 위치(파일 경로) | 책임 (한 줄) |
|------------|------------|----------------|-------------|
| 관찰 가능한 값 | `RxValue<T>` | `Assets/Common/Scripts/Rx/RxValue.cs` | 단일 값을 보유하고 변경 시 통지할 책임을 진다 |
| 현재 값 접근/대입 | `RxValue<T>.Value` | `Assets/Common/Scripts/Rx/RxValue.cs` | 외부가 값을 읽고 쓰는 단일 표면 |
| 변경 통지 | `RxValue<T>.OnChange` | `Assets/Common/Scripts/Rx/RxValue.cs` | 값이 바뀌었음을 구독자에게 알리는 이벤트 표면 |

> 도메인 개념에 직접 대응하지 않는 순수 기술 기반이므로 위 표는 "주요 추상" 으로 읽는다.

## 3. 구조 맵 (위치 중심)

- **진입점**: `new RxValue<T>(T value)` — `Assets/Common/Scripts/Rx/RxValue.cs` (초기값으로 생성)
- **값 표면**: `RxValue<T>.Value` (get/set) — `Assets/Common/Scripts/Rx/RxValue.cs`
- **통지 표면**: `RxValue<T>.OnChange` (`event Action<T>`) — `Assets/Common/Scripts/Rx/RxValue.cs`
- **네임스페이스**: `Common.Scripts.Rxs`
- 디렉토리에는 단일 파일(`RxValue.cs`)만 존재한다. 별도 서브영역 없음.

### 3.1 제공 헬퍼 (다른 코드가 찾아오는 곳)

| 헬퍼 | 위치(파일 경로) | 이럴 때 쓴다 |
|------|----------------|-------------|
| `RxValue<T>` | `Assets/Common/Scripts/Rx/RxValue.cs` | 어떤 상태 값을 보유하면서 변경을 다른 코드에 통지하고 싶을 때 (마나·레벨·체력 등) |
| `RxValue<T>.OnChange` | `Assets/Common/Scripts/Rx/RxValue.cs` | 값 변경을 구독해 UI 갱신·파생 계산 등을 반응형으로 트리거하고 싶을 때 |
| `RxValue<T>.Value` | `Assets/Common/Scripts/Rx/RxValue.cs` | 현재 값을 읽거나, 통지를 동반해 새 값을 대입하고 싶을 때 |

### 3.2 의존 관계

- **밖으로 의존**: `System` (`Action<T>`), `UnityEngine` — 외부 게임 모듈 의존 없음 (자족적 기반)
- **밖에서 의존**: `MarketManager`(마나·배치 상한·레벨·리롤 비용·스캔 잠금 등) · `UnitStatSheet`(체력 등 스탯) · `SynergyActivation` · `SynergyDetailEffectSection` · `RoundManager` — `Assets/Scenes/Battle/Feature/**`

## 4. +α — 코드만 봐선 모르는 것

- **분할 의도**: 마나·티어·스탯 등 전장 전반에서 "값이 바뀌면 UI/파생 로직이 따라가야 하는" 상태가 반복 등장하므로, 그 변경 통지 책임을 한 곳으로 모은 횡단 기반이다. 특정 기능(소환 터미널·유닛 스탯 등)에 묶이지 않는 이유.
- **도메인적 의도**: 어느 한 사용자 기능에도 귀속되지 않는 순수 기술 인프라라 UR 트리에는 잎으로 등장하지 않는다 (UR 미매핑).
- **비자명한 제약/관례**: 변경 통지는 *실제로 값이 달라졌을 때만* 발생하는 것이 이 추상의 계약이다 (동일 값 재대입은 무통지). 따라서 `T.Equals` 가 의미 있게 정의된 타입을 전제하며, 초기 생성 시점에는 `OnChange` 가 호출되지 않는다 — 구독 직후 현재 값으로 동기화가 필요하면 소비처가 직접 초기 1회를 처리해야 한다.

## 5. 레이어 링크

- **인프라 문서** (`docs/system/infra/Rx.md` 위치):
  - 전역 네비게이션: [index.md](../../index.md)
  - UR 미매핑(횡단 기반): [game-system.md](../../ur/game-system.md) §시스템 인프라
