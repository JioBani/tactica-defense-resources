# GlobalEventBus — 시스템 관점

> 이 문서는 AI 에이전트가 코드 레이어에 맞게 이해하고 빠르게 코드를 찾아가도록 하는 시스템 관점 컨텍스트 문서다.
> 코드가 단일 진실이다. 이 문서는 코드를 재설명하지 않는다 — "인덱스 + α"에 한정한다.
>
> - **분류**: 인프라 — UR 미매핑 횡단 기반 (전장의 다수 기능이 발행처·구독처를 서로 모른 채 게임 사건을 주고받는 전역 이벤트 버스). 본래 클래스 단위라 일반 SR 에는 안 들어갈 내용이나, 중요 공통 기반이라 별도 문서화한다.

## 1. 한 줄 정의

게임 사건을 발행처와 구독처가 서로를 직접 참조하지 않고 이벤트 타입으로만 주고받게 하는, 타입별 구독자 목록을 보유한 정적 전역 이벤트 버스.

## 2. 도메인 ↔ 코드 매핑

> 인프라이므로 도메인 개념보다 주요 추상 위주다.

| 주요 추상 | 코드 식별자 | 위치(파일 경로) | 책임 (한 줄) |
|------------|------------|----------------|-------------|
| 버스를 통해 오갈 수 있는 사건의 자격 | `IGameEvent` (marker interface) | `Assets/Common/Scripts/GlobalEventBus/GlobalEventBus.cs` | 이벤트 타입이 버스에 실릴 수 있음을 표시하는 마커 |
| 전역 발행/구독 허브 | `GlobalEventBus` (static class) | `Assets/Common/Scripts/GlobalEventBus/GlobalEventBus.cs` | 이벤트 타입별 핸들러 목록을 보유하고 발행 시 통지 |
| 구체 이벤트(오브젝트 선택) | `OnObjectSelectedEvent` (struct) | `Assets/Common/Scripts/GlobalEventBus/OnObjectSelectedEvent.cs` | 선택된 오브젝트와 화면 좌표를 실어 나르는 사건 |

## 3. 구조 맵 (위치 중심)

진입점부터.

- **구독**: `GlobalEventBus.Subscribe<T>(Action<T>)` — `GlobalEventBus.cs`
- **구독 해제**: `GlobalEventBus.Unsubscribe<T>(Action<T>)` — `GlobalEventBus.cs`
- **발행**: `GlobalEventBus.Publish<T>(T)` — `GlobalEventBus.cs`
- **새 사건 정의**: `IGameEvent` 를 구현하는 `struct` 작성 — 예: `OnObjectSelectedEvent.cs`

### 3.1 제공 헬퍼 (다른 코드가 찾아오는 곳)

| 헬퍼 | 위치(파일 경로) | 이럴 때 쓴다 |
|------|----------------|-------------|
| `GlobalEventBus.Subscribe<T>` | `Assets/Common/Scripts/GlobalEventBus/GlobalEventBus.cs` | 특정 이벤트 타입을 수신하고 싶을 때 |
| `GlobalEventBus.Unsubscribe<T>` | `Assets/Common/Scripts/GlobalEventBus/GlobalEventBus.cs` | 수신을 멈출 때 (객체 파괴 시 등) |
| `GlobalEventBus.Publish<T>` | `Assets/Common/Scripts/GlobalEventBus/GlobalEventBus.cs` | 사건을 발행해 구독자에게 통지할 때 |
| `IGameEvent` | `Assets/Common/Scripts/GlobalEventBus/GlobalEventBus.cs` | 새 전역 이벤트 타입을 정의할 때 마커로 구현 |

### 3.2 의존 관계

- **밖으로 의존**: 없음 (`System` BCL 만 사용. 엔진·게임 모듈에 의존하지 않는 독립 코어)
- **밖에서 의존**: 발행처 `Selectable2D`(`Assets/Common/Scripts/Selectable/Selectable2D.cs`); 다수 구독처 — `Synergy*`(`Assets/Scenes/Battle/Feature/Synergy/`, `.../Ui/SynergyInfo/`), `Round*`(`.../Round/`), `Unit`계열 `Defender`·`Summoner`·`DefenderManager`(`.../Unit/`), `MarketManager`·`DefenderFusionManager`·`StatInfoPanel`·`BattleWin/LoseUiManager` 등 전장 전반

## 4. +α — 코드만 봐선 모르는 것

- **분할 의도 (전역 정적 버스)**: 발행처(`Selectable2D` 등 입력·상태 변화 지점)와 구독처(시너지·라운드·UI 등)가 서로를 컴파일 타임에 알지 못해도 사건을 주고받게 하려고 전역 정적 허브로 두었다. 씬 계층·DI 컨테이너를 거치지 않는 가장 가벼운 횡단 통지 경로다.
- **도메인적 의도**: "한 곳의 사건(예: 오브젝트 선택)에 여러 관심사가 동시에 반응한다"는 전장 UI/로직의 다대다 반응 구조를 이벤트 타입 단위 구독으로 인코딩했다.
- **비자명한 제약/관례**:
  - 이벤트 타입은 `struct` 제약이다 (`where T : struct, IGameEvent`) — 사건을 값 타입 불변 메시지로 다루려는 관례.
  - 정적·전역이라 생명주기 자동 정리가 없다. 구독한 객체는 파괴 시 반드시 `Unsubscribe` 해야 죽은 핸들러가 남지 않는다 (호출 누락은 코드에서 드러나지 않는 함정).
  - 단일 정적 `Handlers` 테이블을 공유하므로 씬 전환·테스트 간 상태가 자동 리셋되지 않는다.

## 5. 레이어 링크

- 전역 네비게이션: [index.md](../../index.md)
- UR 미매핑(횡단 기반): [game-system.md](../../ur/game-system.md) §시스템 인프라
