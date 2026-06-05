# ObjectPool — 시스템 관점

> 이 문서는 **AI 에이전트가 코드 레이어에 맞게 이해하고 빠르게 코드를 찾아가도록** 하는 시스템 관점 컨텍스트 문서다.
> 코드가 단일 진실(single source of truth)이다. 이 문서는 코드를 재설명하지 않는다 — **"인덱스 + α"** 에 한정한다.
>
> - **분류**: 인프라 — UR 미매핑 횡단 기반 (투사체·침략자·소환수·말풍선 등 여러 기능이 공통으로 올라타는 GameObject 재사용 기반)
> - **이 문서가 다루는 것**: ① 이 대상이 시스템에서 무엇인가(한 줄) ② 주요 추상 ↔ 코드(파일 경로)의 매핑 ③ 무엇이 어디에 있는가(구조 맵) ④ 다른 코드가 가져다 쓸 헬퍼(Poolable·Spawn/DeSpawn) ⑤ 코드만 봐선 모르는 의도(+α).
> - **이 문서가 다루지 않는 것**: 코드의 동작을 풀어쓴 설명서, 알고리즘·자료구조 해설, 메서드 본문 요약.

---

## 1. 한 줄 정의

`ObjectPool` 은 프리팹 인스턴스를 종류별로 풀링하여 재사용할 책임을 지는 횡단 인프라로, 빈번히 생성·소멸되는 런타임 GameObject(투사체·침략자·소환수·말풍선)의 Instantiate/Destroy 비용을 풀 대여·반납으로 대체한다.

## 2. 주요 추상 ↔ 코드 매핑

| 주요 추상 | 코드 식별자 | 위치(파일 경로) | 책임 (한 줄) |
|----------|------------|----------------|-------------|
| 풀 관리자(전역 진입점) | `ObjectPooler` | `Assets/Common/Scripts/ObjectPool/ObjectPooler.cs` | 프리팹 종류별 풀을 보관하고 대여(Spawn)·반납(DeSpawn)을 중재 |
| 풀링 대상 표식 | `Poolable` | `Assets/Common/Scripts/ObjectPool/Poolable.cs` | 풀링되는 GameObject 가 자신의 소속 풀을 알고 스스로 반납하도록 하는 컴포넌트 |
| 전장 공용 풀 프리팹 참조 | `ObjectPoolingReferences` | `Assets/Scenes/Battle/Feature/ObjectPool/ObjectPoolingReferences.cs` | 전장 씬에서 여러 소비처가 공유하는 풀링 프리팹(예: `unitPrefab`)을 한 곳에서 노출 |

## 3. 구조 맵 (위치 중심)

무엇이 어디에 있는가. 진입점부터.

- **진입점(대여)**: `ObjectPooler.Spawn(GameObject, Transform, Vector2?)` / `ObjectPooler.SpawnUI(GameObject, RectTransform, Vector2)` — `Assets/Common/Scripts/ObjectPool/ObjectPooler.cs`
- **진입점(반납)**: `ObjectPooler.DeSpawn(Poolable)` — 같은 파일
- **풀 식별·내부 보관**: `Pair` 구조체, `poolingObjects` 딕셔너리 — 같은 파일 (풀 종류별 컨테이너)
- **반납 표식·셀프 반납**: `Poolable.DeSpawn()` / `Poolable.poolId` / `Poolable.SetPoolId(string)` — `Assets/Common/Scripts/ObjectPool/Poolable.cs`
- **전장 공용 프리팹 참조**: `ObjectPoolingReferences.unitPrefab` — `Assets/Scenes/Battle/Feature/ObjectPool/ObjectPoolingReferences.cs`

### 3.1 제공 헬퍼 (다른 코드가 찾아오는 곳)

다른 AI 에이전트가 "이 기능이 필요할 때 여기"라고 찾아올 대상.

| 헬퍼 | 위치(파일 경로) | 이럴 때 쓴다 |
|------|----------------|-------------|
| `ObjectPooler.Spawn` | `Assets/Common/Scripts/ObjectPool/ObjectPooler.cs` | 월드 공간 GameObject(투사체·침략자·소환수)를 풀에서 대여해 위치 지정·활성화할 때 |
| `ObjectPooler.SpawnUI` | `Assets/Common/Scripts/ObjectPool/ObjectPooler.cs` | UI(RectTransform) 오브젝트를 풀에서 대여해 앵커 위치로 배치할 때 (예: 말풍선) |
| `ObjectPooler.DeSpawn(Poolable)` | `Assets/Common/Scripts/ObjectPool/ObjectPooler.cs` | 대상의 `Poolable` 을 직접 들고 있을 때 풀로 반납할 때 |
| `Poolable.DeSpawn()` | `Assets/Common/Scripts/ObjectPool/Poolable.cs` | 풀링된 객체가 자기 자신을 반납할 때 (소비처가 풀 관리자를 몰라도 됨) |

### 3.2 의존 관계

- **밖으로 의존**: `SceneSingleton<T>` (`Assets/Common/Scripts/SceneSingleton/SceneSingleton.cs`) — 전역 단일 접근점, UnityEngine
- **밖에서 의존**: 투사체(`Projectile`/`ProjectileGenerator`, `Assets/Scenes/Battle/Feature/Projectiles/Scripts/`), 침략자(`Aggressor`/`RoundAggressorManager`, `Assets/Scenes/Battle/Feature/...`), 소환수(`UnitGenerator`, `Assets/Scenes/Battle/Feature/Unit/Unit/`), 라운드 정리(`RoundInfoViewer`), 말풍선(`BubbleMessage`/`BubbleMessageSpawner`, `Assets/Common/Scripts/BubbleMessage/`)

## 4. +α — 코드만 봐선 모르는 것

- **분할 의도**: 풀 관리자(`ObjectPooler`)와 풀 표식(`Poolable`)을 분리한 것은, 소비처가 풀 관리자를 직접 참조하지 않고도 `Poolable.DeSpawn()` 만으로 셀프 반납하게 하기 위함이다 — 반납 경로의 결합도를 낮춘다.
- **도메인적 의도**: 이 인프라는 어떤 사용자 기능에도 매핑되지 않는다. 전투 중 대량·고빈도로 명멸하는 객체(투사체·침략자·말풍선 등)를 GC/Instantiate 부담 없이 다루기 위한 공통 기반이며, 전장 도메인의 특정 규칙을 담지 않는다.
- **비자명한 제약/관례**: 풀 키는 프리팹 이름 + InstanceID 조합으로 구성되므로 같은 프리팹 에셋은 하나의 풀을 공유한다. `DeSpawn` 은 대상이 자신의 풀 소속이 아니면 반납을 거부하고 에러 로그를 남긴다. 풀에 반납된 객체는 비활성(`SetActive(false)`) 상태로 재사용 대기하므로, 소비처는 재대여 시 상태 초기화 책임을 진다.

## 5. 레이어 링크

- 전역 네비게이션: [index.md](../../index.md)
- UR 미매핑(횡단 기반): [game-system.md](../../ur/game-system.md) §시스템 인프라
