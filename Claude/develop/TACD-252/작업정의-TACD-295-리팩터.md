# TACD-295 리팩터: SynergyIndicator 티어 도트 비율 표시로 변경

> 상위 이슈: TACD-252 [전장 - 시너지] - 시너지 정보 UI

## 요구사항

SynergyIndicator의 티어 진행도 표시 방식을 변경한다. 기존의 도트 색상 전환 + 숫자 텍스트 방식 대신, 각 도트 안의 채움 비율로 진행도를 나타낸다. 숫자 텍스트는 제거한다.

## 배경지식

### 표시 방식

각 도트는 하나의 티어 구간을 나타내며, 도트 안의 inner 영역이 해당 구간 내 진행률만큼 채워진다.

예: 2/4/6/8 시너지 (4티어)
- 카운트 1 → 첫 번째 도트가 50% 채워짐 (1/2)
- 카운트 3 → 첫 번째 도트 100%, 두 번째 도트 50% 채워짐
- 카운트 6 → 첫 번째~세 번째 도트 100%, 네 번째 도트 0%

예: 3/6/9 시너지 (3티어)
- 카운트 2 → 첫 번째 도트가 66% 채워짐 (2/3)

### 도트 가시성

- 시너지의 티어 수만큼만 도트를 표시한다 (나머지 숨김)
- 예: 2/4 시너지(2티어) → 도트 2개만 표시
- 도트는 가로 정렬, 간격 1

### 제약

- 도트는 6개까지 사전 배치. 현재 6티어 초과 시너지는 없을 예정이나, 초과 시 에러 없이 6개까지만 표시한다.

### 프리팹 변경사항 (사용자가 수동 수정 완료)

- 프리팹 경로: `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/SynergyIndicator.prefab`
- 각 도트(TierDot_0~5)마다 inner 이미지가 자식으로 추가됨
- 숫자 텍스트(CountText) 제거됨

## 완료 정의

- CD-1. 시너지 바인딩 시 티어 수만큼 도트가 표시되고 나머지는 숨겨진다
- CD-2. 각 도트의 inner 채움 비율이 현재 카운트에 따라 올바르게 표시된다
- CD-3. 소환수 배치/제거/판매 시 도트의 채움 비율이 실시간으로 갱신된다
- CD-4. 6티어 초과 시너지가 있어도 에러 없이 6개까지만 표시된다
- CD-5. 프리팹의 참조가 새 구조에 맞게 연결된다

## 작업 단위

### [x] 단위 1: SynergyIndicator 리팩터 + 프리팹 연결

- CD-1 ~ CD-5 전체

## 기존 코드 참조

- `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyIndicator.cs` — 수정 대상
- `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/SynergyIndicator.prefab` — 사용자가 변경한 프리팹
- `Assets/Common/Data/Synergies/SynergyTier.cs` — 티어 구조 (Tier, RequiredCount)
- `Assets/Scenes/Battle/Feature/Synergy/Scripts/SynergyActivation.cs` — 카운트, 활성 티어, 정의 데이터

## 미결사항

- 없음
