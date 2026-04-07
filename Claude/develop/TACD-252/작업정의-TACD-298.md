# TACD-298: [시너지 > 정보 UI] - 전체 설계

> 상위 이슈: TACD-252 [전장 - 시너지] - 시너지 정보 UI

## 요구사항

시너지 정보 UI의 전체 구조를 설계한다. 하위 스토리(TACD-295, TACD-296, TACD-297)가 공유할 클래스, 데이터 구조, UI 계층 구조를 먼저 정의하여 이후 작업의 기반을 마련한다.

구현 대상은 세 가지이다:
- **시너지 목록 패널** (TACD-295): 활성 시너지 아이콘, 티어, 진행률을 나열
- **시너지 상세 패널** (TACD-296): 클릭 시 효과 설명, 소환수 목록, 활성 소환수 구분
- **정렬** (TACD-297): 시너지 종류(소환술사 효과 / 소환수 특성)와 활성 상태에 따른 정렬

## 배경지식

### 시너지 시스템 현황

시너지는 **소환술사 효과**(소속 소환술사에 의해 고정)와 **소환수 특성**(전장마다 랜덤 부여)의 두 종류로 구분된다. 소환수 1체는 소환술사 효과 1개 + 소환수 특성 1개를 보유한다.

카운트 대상은 전장(BattleArea)에 배치된 서로 다른 종류의 소환수이며, 동일 소환수는 중복 카운트하지 않는다. 임계치는 소환술사 효과(2/4/6/8, 4단계), 소환수 특성(2/3/4, 3단계)으로 나뉜다.

### 기존 데이터 흐름

1. `SummonerManager.Summoners` - 편성에 포함된 소환술사 로드아웃 목록
2. `SummonerDefinitionData.SummonerEffect` - 소환술사가 소유하는 시너지 정의
3. `SummonerDefinitionData.SummonPool` - 소환술사의 소환수 풀 (UnitLoadOutData 배열)
4. `SynergyManager.SynergyActivations` - 모든 시너지의 카운트/티어 상태 (읽기 전용 딕셔너리)
5. `SynergyActivation` - 개별 시너지의 카운트, 활성 티어, 티어 변경 이벤트
6. `SynergyDefinitionData` - 시너지 이름, 아이콘, 설명, 티어 목록, 종류(SynergyType)

### UI가 필요로 하는 정보 요약

| 정보 | 출처 |
|------|------|
| 시너지 아이콘, 이름, 설명 | `SynergyDefinitionData` |
| 시너지 종류 (소환술사 효과 / 소환수 특성) | `SynergyDefinitionData.SynergyType` |
| 현재 카운트 | `SynergyActivation.Count` |
| 현재 활성 티어 | `SynergyActivation.ActiveTier` |
| 티어 임계치 목록 | `SynergyDefinitionData.Tiers` |
| 시너지에 해당하는 소환수 목록 | `SummonerDefinitionData.SummonPool` 역참조 |
| 현재 전장에 배치된 소환수 여부 | `DefenderManager` + 배치 상태 확인 |

### TFT 시너지 UI 레이아웃 참고

기획 이미지 기준:
- **좌측 상단(빨간색 영역)**: 활성 시너지 아이콘 목록이 세로로 나열
- **하단(파란색 영역)**: 소환술사 위치
- **초록색 영역**: 시너지 클릭 시 열리는 자세히 보기 패널

## 완료 정의

- 시너지 정보 UI의 UI 계층 구조(GameObject 트리)가 정의된다
- 하위 스토리가 공유할 스크립트 파일 구조(클래스 이름, 역할, 소속 경로)가 정의된다
- 시너지 목록 패널의 데이터 흐름이 정의된다: 어떤 데이터를 어디서 읽고, 어떤 이벤트로 갱신하는지
- 시너지 상세 패널의 데이터 흐름이 정의된다: 클릭 시 어떤 정보를 어디서 조합하는지
- 정렬 기준과 갱신 시점이 정의된다

## 작업 단위

### [x] 단위 1: UI 계층 구조 및 스크립트 구조 설계

- 시너지 정보 UI의 UI 계층 구조(GameObject 트리)가 정의된다
- 하위 스토리가 공유할 스크립트 파일 구조(클래스 이름, 역할, 소속 경로)가 정의된다

### [x] 단위 2: 데이터 흐름 및 갱신 설계

- 시너지 목록 패널의 데이터 흐름이 정의된다: 어떤 데이터를 어디서 읽고, 어떤 이벤트로 갱신하는지
- 시너지 상세 패널의 데이터 흐름이 정의된다: 클릭 시 어떤 정보를 어디서 조합하는지
- 정렬 기준과 갱신 시점이 정의된다

## 관련 기획서

- `Document/기획서/시너지 기획 메모.md`
  - 시너지 종류(소환술사 효과 / 소환수 특성), 카운트 규칙, 임계치, 시너지 정보 UI 필요성
- `Document/기획서/07 게임 시스템 기획서.md` (5장 시너지)
  - 카운트 규칙, 임계치, 효과 적용 대상, 소환술사 효과, 소환수 특성, 시너지 정보 UI
- `Document/기획서/TFT 세트1 시너지 레퍼런스.md`
  - TFT 시너지 구조 참고 (계열/직업 시너지, 티어별 임계치 패턴)

## 기존 코드 참조

- `Assets/Common/Data/Synergies/SynergyDefinitionData.cs` - 시너지 정의 (이름, 아이콘, 설명, 종류, 티어 목록)
- `Assets/Common/Data/Synergies/SynergyTier.cs` - 티어 구조체 (단계, 필요 카운트, 상수 딕셔너리)
- `Assets/Common/Data/Synergies/SynergyType.cs` - 시너지 종류 enum (SummonerEffect / SummonTrait)
- `Assets/Common/Data/Synergies/SynergyId.cs` - 시너지 ID enum
- `Assets/Scenes/Battle/Feature/Synergy/Scripts/SynergyManager.cs` - 시너지 매니저 (SynergyActivations 외부 조회용 프로퍼티 제공)
- `Assets/Scenes/Battle/Feature/Synergy/Scripts/SynergyActivation.cs` - 개별 시너지 카운트/티어 상태 (RxValue 기반 변경 알림)
- `Assets/Common/Data/Summoners/SummonerDefinitions/SummonerDefinitionData.cs` - 소환술사 정의 (SummonerEffect, SummonPool)
- `Assets/Common/Data/Units/UnitDefinitions/UnitDefinitionData.cs` - 유닛 정의 (Synergies 프로퍼티로 소유 시너지 목록 제공)
- `Assets/Scenes/Battle/Feature/Unit/Summoner/SummonerManager.cs` - 소환술사 매니저 (편성 데이터 접근)
- `Assets/Scenes/Battle/Feature/Unit/Defender/Defender.cs` - Defender의 시너지 보유/확인 (`AddSynergy`, `HasSynergy`, `Synergies`)

## 결정사항

1. **소환수 특성 UI 범위**: 현재 설계는 소환술사 효과만 우선 대상으로 삼는다. 소환수 특성은 동일한 구조로 확장 가능하게 설계한다. UI에서는 소환술사 효과와 소환수 특성을 종류 구분 없이 동일하게 표시한다.

2. **"시너지에 해당하는 소환수 목록" 역참조 방법**: 역참조 방법은 현재 설계 범위에서 제외한다. TACD-296(시너지 상세 패널) 구현 시점에 결정한다.

3. **시너지 설명 텍스트의 플레이스홀더 치환**: 플레이스홀더 치환 기능은 현재 작업에서 구현하지 않는다. 별도 작업으로 분리한다.

4. **UI 배치 레이아웃**: 프로토타입 수준의 레이아웃으로 진행한다. 이후 사용자가 직접 보면서 조정한다.
