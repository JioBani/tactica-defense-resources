# 데이터 구조 설계

Jira 티켓 번호: $ARGUMENTS

## 참조 문서

먼저 `Claude/work/$ARGUMENTS/` 디렉토리에 기존 작업 파일이 있는지 확인한다.
- `데이터 설계 작업 현황.md`가 있으면: 마지막 진행 상태부터 이어서 수행한다.
- 파일이 없으면: 아래 참조 문서를 읽고 처음부터 수행한다.

### 문서 읽기 규칙

- 현재 컨텍스트에서 이미 읽은 문서는 다시 읽지 않는다.
- 컨텍스트 컴팩트 발생 시에만 필요한 문서를 다시 읽는다.
- 다시 읽을 때는 필요한 구간만 offset/limit으로 읽는다.

아래 문서를 읽고 숙지한 뒤, 워크플로우를 순서대로 수행한다:

1. `Claude/data-design/데이터설계-워크플로우.md` - 데이터 설계 워크플로우 (전체 흐름)
2. `.claude/docs/develop.md` - 개발 지침
3. `.claude/docs/git.md` - Git 커밋 지침
4. `.claude/docs/jira.md` - Jira 작업 지침

## 실행

### 초기 설정 (새 작업인 경우)

1. 해당 일감의 브랜치 생성 및 전환 (`$ARGUMENTS`)
2. 작업 디렉토리 생성 (`Claude/work/$ARGUMENTS/`)
3. `데이터 설계 작업 현황.md`를 템플릿에서 생성

### 워크플로우 수행

`Claude/data-design/데이터설계-워크플로우.md`의 Phase 1부터 순서대로 수행한다.

- Jira 티켓 번호: `$ARGUMENTS`
- 작업 파일 위치: `Claude/work/$ARGUMENTS/`
