# 작업 수행

Jira 티켓 번호: $ARGUMENTS

## 참조 문서

먼저 `Claude/work/TACD-{번호}/` 디렉토리에 기존 작업 파일이 있는지 확인한다.
- 파일이 있으면: 작업정의.md와 작업현황판.md를 읽고, 마지막 진행 상태부터 이어서 수행한다.
- 파일이 없으면: 아래 참조 문서를 읽고 처음부터 수행한다.

### 문서 읽기 규칙

- 현재 컨텍스트에서 이미 읽은 문서는 다시 읽지 않는다.
- 컨텍스트 컴팩트 발생 시에만 필요한 문서를 다시 읽는다.
- 다시 읽을 때는 필요한 구간만 offset/limit으로 읽는다.

아래 문서를 읽고 숙지한 뒤, 작업지시서의 루틴을 순서대로 수행한다:

1. `Claude/work/작업지시서.md` - 작업 루틴 (전체 워크플로우)
2. `.claude/docs/develop.md` - 개발 지침
3. `.claude/docs/git.md` - Git 커밋 지침
4. `.claude/docs/jira.md` - Jira 작업 지침

## 실행

`Claude/work/작업지시서.md`의 **작업 루틴**을 1단계부터 순서대로 수행한다.

- Jira 티켓 번호: `$ARGUMENTS`
- 작업 파일 위치: `Claude/work/$ARGUMENTS/`
