# 작업 대시보드 갱신

현재 진행 중인 작업의 상태를 HTML 대시보드로 생성/갱신한다.

## 사용법

- `/dashboard` — 모든 티켓 전체 갱신
- `/dashboard TACD-219` — 해당 티켓만 갱신 (기존 데이터 유지)

## 수행 절차

### 1. 인자 파싱

- 인자가 있으면 `TACD-{번호}` 형태의 티켓 번호를 `targetTicket`으로 설정한다 (단일 갱신 모드)
- 인자가 없으면 `targetTicket = null` (전체 갱신 모드)

### 2. 데이터 수집

#### 전체 갱신 모드 (targetTicket == null)

아래 작업을 **병렬로** 수행한다:

1. **현재 브랜치 확인**: `git branch --show-current` 실행 → TACD-{번호} 추출
2. **작업 디렉토리 스캔**: `.claude/work/` 아래 `TACD-*/` 패턴으로 Glob 검색
3. 발견된 각 TACD 디렉토리에서 `작업정의.md`와 `작업현황판.md`를 **병렬로** Read

#### 단일 갱신 모드 (targetTicket != null)

아래 작업을 **병렬로** 수행한다:

1. **현재 브랜치 확인**: `git branch --show-current` 실행 → TACD-{번호} 추출
2. **해당 티켓의 작업 파일 읽기**: `.claude/work/{targetTicket}/작업정의.md`와 `작업현황판.md`를 Read
3. **기존 데이터 로드**: `Claude/dashboard/dashboard-data.json`이 존재하면 Read

### 3. 데이터 파싱

대상 티켓(들)에 대해 아래 JSON 구조를 만든다:

```json
{
  "activeTicket": "TACD-219",
  "updatedAt": "2026-02-21 14:30",
  "tickets": [
    {
      "id": "TACD-219",
      "title": "작업정의.md 1번째 줄의 제목에서 추출",
      "stage": 5,
      "summary": "작업정의.md의 '작업 내용' 또는 '일감 요약' 섹션 첫 문단",
      "completionCriteria": ["완료 정의 항목1", "항목2"],
      "checklist": [
        {"text": "항목 텍스트", "status": "completed", "section": "구현 항목"},
        {"text": "항목 텍스트", "status": "in-progress", "section": "구현 항목"},
        {"text": "항목 텍스트", "status": "pending", "section": "검증 항목"}
      ]
    }
  ]
}
```

#### 작업지시서 단계(stage) 판별 기준

| 조건 | stage |
|------|-------|
| 작업정의.md 없음 | 1 (작업 내용 확인) |
| 작업정의.md만 존재, 작업현황판.md 없음 | 2 (작업정의 작성) ~ 3 (구현 계획) |
| 작업현황판.md 존재, 모두 `[ ]` 또는 `[~]` 포함 | 4 (작업현황판 작성) ~ 5 (구현) |
| 작업현황판.md의 `[~]` 항목 존재 | 5 (구현 중) |
| 작업현황판.md의 모든 항목이 `[x]` | 6 (완료) |

더 정확한 판별:
- 작업현황판.md가 있고 `[ ]`만 있으면 → stage 4
- `[~]`가 하나라도 있거나 `[x]`와 `[ ]`가 혼재하면 → stage 5
- 전부 `[x]`이면 → stage 6

#### 체크리스트 파싱 규칙

작업현황판.md에서 아래 패턴을 추출한다:

- `- [x] 텍스트` → `{ status: "completed" }`
- `- [~] 텍스트` → `{ status: "in-progress" }`
- `- [ ] 텍스트` → `{ status: "pending" }`

`## 섹션제목` 아래의 항목들은 해당 섹션명을 `section` 값으로 사용한다.

#### 작업정의 파싱 규칙

- **title**: 첫 번째 `# ` 헤딩에서 `TACD-{번호}: ` 또는 `TACD-{번호} ` 접두어를 제거한 나머지
- **summary**: `## 작업 내용` 또는 `## 일감 요약` 섹션의 첫 문단 (마크다운 제거, 200자 이내)
- **completionCriteria**: `## 완료 정의` 또는 `## 완료 기준` 섹션의 `- ` 항목들

### 4. 데이터 병합

#### 전체 갱신 모드

- 수집된 모든 티켓으로 새 DATA를 구성한다
- `activeTicket`은 현재 브랜치, `updatedAt`은 현재 시각

#### 단일 갱신 모드

- `Claude/dashboard/dashboard-data.json`이 존재하면 기존 데이터를 로드한다
- 기존 `tickets` 배열에서 `targetTicket`과 같은 `id`의 항목을 찾아 **교체**한다
- 해당 `id`가 없으면 배열 **앞에 추가**한다
- `activeTicket`을 현재 브랜치로, `updatedAt`을 현재 시각으로 갱신한다
- `Claude/dashboard/dashboard-data.json`이 존재하지 않으면 해당 티켓만으로 새 DATA를 생성한다

### 5. 저장 및 HTML 생성

1. 완성된 DATA JSON을 `Claude/dashboard/dashboard-data.json`에 Write 한다 (다음 갱신 시 재사용)
2. `Claude/dashboard/dashboard-template.html`을 Read로 읽는다
3. 파일 내용에서 `__DASHBOARD_DATA__` 문자열을 DATA JSON으로 치환한다
4. 결과를 `Claude/dashboard/dashboard.html`에 Write 한다

### 6. 완료

"대시보드가 갱신되었습니다: Claude/dashboard/dashboard.html" 메시지를 출력한다.
브라우저에서 이미 열려있다면 새로고침하면 됩니다.

## 주의사항

- 이 skill은 **읽기 + HTML/JSON 생성만** 수행한다. 작업 파일(작업정의.md, 작업현황판.md)을 수정하지 않는다.
- 작업정의.md나 작업현황판.md가 없는 티켓은 있는 정보만으로 표시한다.
- JSON 값에 큰따옴표가 포함된 경우 이스케이프 처리한다.
- 최대한 빠르게 수행하기 위해 파일 읽기는 병렬로 처리한다.
