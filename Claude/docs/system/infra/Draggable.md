# Draggable — 시스템 관점

> 이 문서는 **AI 에이전트가 코드 레이어에 맞게 이해하고 빠르게 코드를 찾아가도록** 하는 시스템 관점 컨텍스트 문서다.
> 코드가 단일 진실(single source of truth)이다. 이 문서는 코드를 재설명하지 않는다 — **"인덱스 + α"** 에 한정한다.
>
> - **분류**: 인프라 — UR 미매핑 횡단 기반 (드래그앤드롭/드롭존. 소환수 배치·대기석·환원 등 여러 기능이 공통 사용)
> - **이 문서가 다루는 것**: ① 이 대상이 시스템에서 무엇인가(한 줄) ② 주요 추상 ↔ 코드(파일 경로)의 매핑 ③ 무엇이 어디에 있는가(구조 맵) ④ 다른 코드가 가져다 쓸 헬퍼 ⑤ 코드만 봐선 모르는 의도(+α).
> - **이 문서가 다루지 않는 것**: 코드의 동작을 풀어쓴 설명서, 알고리즘·자료구조 해설, 메서드 본문 요약. 그건 코드를 보면 된다.

---

## 1. 한 줄 정의

2D 월드 공간에서 오브젝트를 마우스로 끌어 드롭존으로 옮기는 드래그앤드롭 기반을 제공하고, 드롭 수용 가능 여부를 규칙(`IDropRule`)으로 위임받아 판정하는 횡단 인프라다.

## 2. 주요 추상 ↔ 코드 매핑

설명 열은 SR 수준(책임), 위치 열은 네비게이션(경로·타입).

| 주요 추상 | 코드 식별자 | 위치(파일 경로) | 책임 (한 줄) |
|------------|------------|----------------|-------------|
| 끌리는 대상 | `Draggable2D` | `Assets/Common/Scripts/Draggable/Draggable2D.cs` | 드래그 입력을 추적해 위치를 갱신하고, 드롭 시 대상 존의 수용 여부를 물어 이동을 확정하거나 시작 위치로 되돌린다 |
| 드롭 수용처 | `DropZone2D` | `Assets/Common/Scripts/Draggable/DropZone2D.cs` | 태그·규칙 집합으로 수용 가부를 판정하고, 드롭/이탈 시 등록된 규칙들에 통지한다 |
| 단일 점유 드롭존 | `ExclusiveDropZone2D` | `Assets/Common/Scripts/Draggable/ExclusiveDropZone2D.cs` | 한 칸에 하나만 점유하도록 강제하고, 새 점유자가 들어오면 기존 점유자를 이전 존으로 밀어낸다(스왑) |
| 드롭 규칙 | `IDropRule` | `Assets/Common/Scripts/Draggable/IDropRule.cs` | 존이 끌어다 쓰는 수용 판정·드롭 후·이탈 후 훅. 도메인별 배치 규칙을 존 밖에서 주입하는 확장점 |
| 드래그 단계 | `DragState` | `Assets/Common/Scripts/Draggable/DragState.cs` | 드래그 시작/진행/종료의 단계 구분 열거형 |

## 3. 구조 맵 (위치 중심)

무엇이 어디에 있는가. 진입점부터.

- **진입점**: `Draggable2D.BeginDrag()` / `Draggable2D.EndDrag()` — `Assets/Common/Scripts/Draggable/Draggable2D.cs` (드래그 수명 주기의 외부 트리거)
- **드롭 판정·이동 확정**: `Draggable2D.TryDrop()` (내부) → `Draggable2D.MoveToDropZone(DropZone2D)` (공개) — 같은 파일
- **수용 판정**: `DropZone2D.CanAccept(Draggable2D, DropZone2D)` — `DropZone2D.cs`
- **드롭/이탈 통지**: `DropZone2D.OnDrop(...)` / `DropZone2D.OnDragOut(...)` (virtual) — `DropZone2D.cs`, `ExclusiveDropZone2D` 에서 override
- **규칙 확장점**: `IDropRule` — `IDropRule.cs`, 존에 `AddRule` 로 주입
- **드래그 콜백**: `Draggable2D.OnDrop` / `OnDragStart` / `OnDragEnd` (Action 필드) — 소비처가 구독하는 외부 표면

### 3.1 제공 헬퍼 (다른 코드가 찾아오는 곳)

다른 AI 에이전트가 "이 기능이 필요할 때 여기"라고 찾아올 대상.

| 헬퍼 | 위치(파일 경로) | 이럴 때 쓴다 |
|------|----------------|-------------|
| `Draggable2D` | `Assets/Common/Scripts/Draggable/Draggable2D.cs` | 오브젝트를 마우스 드래그로 옮기고 드롭존 간 이동을 다뤄야 할 때. `OnDrop(oldZone,newZone)` 등 콜백으로 이동 결과를 받는다 |
| `Draggable2D.MoveToDropZone(DropZone2D)` | 같은 파일 | 드래그 입력 없이 코드에서 강제로 특정 존으로 이동시킬 때(스왑·되돌리기 등) |
| `DropZone2D` | `Assets/Common/Scripts/Draggable/DropZone2D.cs` | 드래그 대상을 받는 칸/영역을 만들 때. `requiredTag` 로 수용 태그 제한, `AddRule` 로 규칙 주입 |
| `ExclusiveDropZone2D` | `Assets/Common/Scripts/Draggable/ExclusiveDropZone2D.cs` | 한 칸에 하나만 두고 들어오면 자리를 맞바꿔야 할 때(그리드 슬롯·대기석 등) |
| `IDropRule` | `Assets/Common/Scripts/Draggable/IDropRule.cs` | 존의 수용 가부·드롭 후 동작을 도메인 규칙으로 커스터마이즈할 때 구현 후 `AddRule` 로 등록 |

### 3.2 의존 관계

- **밖으로 의존**: UnityEngine (`MonoBehaviour`, `Camera`, `Physics2D`, `Collider2D`, `LayerMask`). 드롭존 탐지는 `"DropZone"` 레이어에 의존.
- **밖에서 의존**: 소환수 배치(`Assets/Scenes/Battle/Feature/Unit/.../Unit.cs`, `Defender.cs`), 대기석(`Assets/Scenes/Battle/Feature/WaitingArea/Scripts/WaitingArea.cs`, `WaitingAreaReferences.cs`), 환원·판매(`Assets/Scenes/Battle/Feature/Sell/Scripts/DefenderSideSell.cs`).

## 4. +α — 코드만 봐선 모르는 것

- **분할 의도**: 수용 판정을 `DropZone2D` 자신이 아니라 `IDropRule` 로 외부화한 것은, 드래그 기반(Common)을 도메인(배치 상한·역할군·환원 등 전장 규칙)으로부터 분리해 인프라가 특정 기능에 종속되지 않게 하기 위함이다. 도메인별 제약은 존을 상속하지 않고 규칙 주입으로 합성한다.
- **도메인적 의도**: `ExclusiveDropZone2D` 의 스왑(들어온 점유자가 기존 점유자를 이전 칸으로 밀어냄)은 소환수 그리드 배치·대기석에서 "두 유닛 자리 맞바꾸기" UX를 인프라 차원에서 지원하기 위한 것이다.
- **비자명한 제약/관례**: 드롭존으로 인식되려면 콜라이더가 `"DropZone"` 레이어에 있어야 한다(코드 상수). `requiredTag` 가 빈 문자열이면 태그 검사를 건너뛰어 누구나 수용한다. `DragState` 열거형은 이 폴더 내부에서 소비되지 않는 외부 표면용 단계 구분이다.

## 5. 레이어 링크

- **인프라 문서** (`docs/system/infra/Draggable.md` 위치):
  - 전역 네비게이션: [index.md](../../index.md)
  - UR 미매핑(횡단 기반): [game-system.md](../../ur/game-system.md) §시스템 인프라
