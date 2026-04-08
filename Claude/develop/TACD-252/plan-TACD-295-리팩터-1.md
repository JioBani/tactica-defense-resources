# Plan: TACD-295 리팩터 - 단위 1

> SynergyIndicator 티어 도트를 fillAmount 비율 표시로 변경 + 프리팹 SerializeField 연결

## 완료 정의

- CD-1. 시너지 바인딩 시 티어 수만큼 도트가 표시되고 나머지는 숨겨진다
- CD-2. 각 도트의 inner 채움 비율이 현재 카운트에 따라 올바르게 표시된다
- CD-3. 소환수 배치/제거/판매 시 도트의 채움 비율이 실시간으로 갱신된다
- CD-4. 6티어 초과 시너지가 있어도 에러 없이 6개까지만 표시된다
- CD-5. 프리팹의 참조가 새 구조에 맞게 연결된다

## 변경 파일

### 1. `Assets/Scenes/Battle/Feature/Ui/SynergyInfo/Scripts/SynergyIndicator.cs` (수정)

#### SerializeField 변경

- 제거: `countText` (TMP_Text), `tierDots` (Image[]), `activeDotColor` (Color), `inactiveDotColor` (Color)
- 추가: `tierDots` (GameObject[]) — 외부 Dot의 GameObject 배열 (SetActive 제어용)
- 추가: `innerImages` (Image[]) — inner Image 배열 (fillAmount 제어용)
- using TMPro 제거 (더 이상 TMP_Text 사용 안 함)

#### 메서드 변경

**Bind()**: 기존 로직 유지하되 tierDots가 GameObject[]이므로 `tierDots[i].SetActive(i < tierCount)` 으로 변경. 6개 초과 시너지 대응을 위해 `Math.Min(tierCount, tierDots.Length)` 사용. (CD-1, CD-4)

**RefreshDisplay()**: 완전 교체
- `GetNextThreshold()`, `GetActiveDotCount()` 호출 제거
- `countText` 업데이트 제거
- 티어 수와 도트 수 중 작은 값으로 루프 (CD-4)
- 각 도트 i에 대해:
  - 구간 시작: i == 0이면 0, 아니면 `tiers[i-1].RequiredCount`
  - 구간 끝: `tiers[i].RequiredCount`
  - count가 구간 끝 이상이면 fillAmount = 1
  - count가 구간 시작 이하이면 fillAmount = 0
  - 그 사이면 fillAmount = (count - 구간시작) / (구간끝 - 구간시작)
- `innerImages[i].fillAmount = 계산값` 설정 (CD-2, CD-3)

**제거**: `GetNextThreshold()`, `GetActiveDotCount()` — 더 이상 불필요

#### 기존 유지

- `icon`, `button` SerializeField
- `_activation` 필드, `BoundActivation` 프로퍼티
- `OnClicked` 이벤트
- `HandleSynergyRecalculated()` — RefreshDisplay() 호출 (CD-3)
- `HandleClick()`, `Awake()`, `OnDestroy()` 그대로

### 2. 프리팹 SerializeField 연결 (MCP `manage_prefabs` modify_contents)

- `SynergyIndicator` 컴포넌트의 `tierDots` 배열에 TierDot_0~5의 GameObject 연결
- `SynergyIndicator` 컴포넌트의 `innerImages` 배열에 TierDot_0_inner~5_inner의 Image 연결

## 비율 계산 예시

Tiers = [RequiredCount: 2, RequiredCount: 4, RequiredCount: 6]

| count | Dot 0 (0~2) | Dot 1 (2~4) | Dot 2 (4~6) |
|-------|-------------|-------------|-------------|
| 0     | 0.0         | 0.0         | 0.0         |
| 1     | 0.5         | 0.0         | 0.0         |
| 2     | 1.0         | 0.0         | 0.0         |
| 3     | 1.0         | 0.5         | 0.0         |
| 6     | 1.0         | 1.0         | 1.0         |
