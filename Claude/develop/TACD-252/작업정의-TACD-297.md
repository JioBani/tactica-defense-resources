# TACD-297: [시너지 > 정보 UI] - 시너지 목록이 종류와 활성 상태에 따라 정렬된다

> 상위 이슈: TACD-252 [전장 - 시너지] - 시너지 정보 UI

## 요구사항

시너지 목록 패널(SynergyListPanel)의 인디케이터가 활성 상태와 진행 정도에 따라 자동으로 정렬된다. 활성 시너지가 비활성 시너지보다 상위에 표시되어, 플레이어가 현재 유효한 시너지를 즉시 파악할 수 있도록 한다. TFT의 시너지 UI 정렬 방식을 참고한다.

## 배경지식

- **시너지 활성/비활성**: 시너지의 유니크 유닛 카운트가 첫 번째 티어 임계치 이상이면 활성, 미달이면 비활성이다.
- **티어 진행률**: 현재 카운트가 다음 임계치까지 얼마나 도달했는지를 나타내는 비율이다. 최고 티어를 달성한 시너지는 진행률 1.0으로 최상위에 배치된다.
- **정렬 방식**: SynergyListPanel은 VerticalLayoutGroup을 사용하며, 자식 오브젝트의 sibling index 순서에 따라 시각적 순서가 결정된다. SetSiblingIndex를 호출하면 레이아웃이 자동으로 재배치된다.
- **갱신 시점**: 시너지 재계산(OnSynergyRecalculatedEventDto) 이벤트가 발행될 때마다 정렬이 갱신된다.

## 완료 정의

- 활성 시너지가 비활성 시너지보다 항상 목록 상위에 표시된다.
- 활성 시너지끼리는 티어 진행률이 높은 순서로 정렬된다 (최고 티어 달성 시너지가 가장 위).
- 동일 진행률의 활성 시너지끼리는 현재 활성 티어 단계가 높은 순서로 정렬된다.
- 비활성 시너지끼리는 카운트가 높은 순서로 정렬된다 (곧 활성화될 시너지가 위).
- 동일 카운트의 비활성 시너지끼리는 기존 순서가 유지된다 (안정 정렬).
- 시너지 재계산이 발생할 때마다 정렬이 자동으로 갱신된다.
- 초기화 시에도 정렬이 1회 실행된다.

## 작업 단위

상태: `[ ]` 대기 / `[plan]` plan 작성 중 / `[구현]` 구현 중 / `[테스트]` 테스트 중 / `[피드백]` 피드백 루프 중 / `[x]` 완료

### [구현] 단위 1: 정렬 로직 구현

- 활성 시너지가 비활성 시너지보다 항상 목록 상위에 표시된다.
- 활성 시너지끼리는 티어 진행률이 높은 순서로 정렬된다 (최고 티어 달성 시너지가 가장 위).
- 동일 진행률의 활성 시너지끼리는 현재 활성 티어 단계가 높은 순서로 정렬된다.
- 비활성 시너지끼리는 카운트가 높은 순서로 정렬된다 (곧 활성화될 시너지가 위).
- 동일 카운트의 비활성 시너지끼리는 기존 순서가 유지된다 (안정 정렬).
- 시너지 재계산이 발생할 때마다 정렬이 자동으로 갱신된다.
- 초기화 시에도 정렬이 1회 실행된다.

## 관련 기획서

- `Document/기획서/TFT 세트1 시너지 레퍼런스.md`
  - TFT 시너지 구조 참고. 정렬은 활성 시너지 우선, 진행률 순서.

## 기존 코드 참조

- `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyListPanel.cs`
  - SortIndicators() 빈 메서드가 이미 정의되어 있고, Start()와 HandleSynergyRecalculated()에서 호출되고 있다.
- `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyIndicator.cs`
  - BoundActivation 프로퍼티로 바인딩된 SynergyActivation에 접근 가능하다.
- `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyIndicatorService.cs`
  - CalculateRatios 메서드로 티어별 비율을 계산한다. 정렬용 진행률 계산 시 참고할 수 있다.
- `Assets/Scenes/Battle/Feature/Synergy/Scripts/SynergyActivation.cs`
  - Count (현재 유니크 유닛 수), ActiveTier.Value (현재 활성 티어, null이면 비활성), Definition.Tiers (티어 목록)를 제공한다.

## 미결사항

없음. 정렬 기준은 TACD-298 설계(plan-2 5절)에서 확정되었고, 구현 위치(SynergyListPanel.SortIndicators)와 구현 방식(SetSiblingIndex)도 확정되었다.
