---
name: worklog
description: 현재 세션의 작업 로그(worklog)를 wiki/08-worklog 에 작성하고 장기기억(wiki/09-memory)을 갱신한다. 사용자가 세션 종료 전에 /worklog 로 직접 실행한다.
disable-model-invocation: true
argument-hint: "[세션 제목 (선택)]"
---

# Worklog 작성

현재 세션에서 한 일을 `wiki/08-worklog/` 규칙에 맞춰 정리본으로 남기고, 장기기억을 갱신한다.

- 규칙: `wiki/08-worklog/README.md`, `wiki/09-memory/README.md`, `wiki/README.md`(frontmatter 스키마)
- 형식: `wiki/_templates/worklog.md`
- 세션 ID: `${CLAUDE_SESSION_ID}` (앞 8자리가 raw 로그의 `session` 값이다. 치환되지 않았으면 raw 로그의 마지막 세션 헤더를 사용한다)
- 사용자 지정 제목: `$ARGUMENTS` (비어 있으면 세션 내용으로 짓는다)

## 절차

1. **파일 위치 결정**
   - 오늘 날짜(로컬 시간) 기준으로 `wiki/08-worklog/YYYY-MM/` 폴더를 사용한다 (없으면 생성).
   - 같은 날짜 파일 중 가장 큰 `NN` 에 1을 더한다. 이 세션의 로그가 이미 있으면(frontmatter `session` 이 같으면) 새로 만들지 말고 그 파일을 보완한다.
   - 파일명: `YYYY-MM-DD-NN-kebab-case-title.md` (제목은 영어 kebab-case, 문서 본문 제목은 한국어)

2. **재료 수집**
   - 이 세션의 대화 내용 (목표, 요청, 수행 작업, 결정, 문제)
   - `wiki/08-worklog/raw/YYYY-MM-DD.md` 에서 이 세션 ID의 프롬프트 원문
   - `git status --short`, `git diff --stat`, 이 세션 동안의 `git log --oneline` 으로 변경 파일과 커밋

3. **작성** (템플릿 구조 그대로)
   - **frontmatter**: `{{date}}` 등 자리표시자를 실제 값으로 채운다. `title`, `aliases`, `session`, `model`, `adrs` 를 맞춘다. 보완하는 경우 `updated` 만 바꾼다.
   - **주요 프롬프트**: 원문 중 의미 있는 요청만 골라 인용한다. 단순 확인("응", "진행해")은 뺀다. 원문이 길면 요약하고, 원문은 raw 로그 링크로 대신한다.
   - **작업 내용 / 변경 파일**: 실제로 한 일만 적는다. 시도했다가 되돌린 것은 "이슈 / 배운 점"에 적는다.
   - **결정 사항**: 아키텍처, 기술 스택, 규칙에 영향을 주는 결정은 ADR 후보로 표시한다. ADR 파일은 **사용자에게 물어본 뒤에만** 만든다.
   - **다음 할 일**: 다음 세션이 바로 시작할 수 있을 만큼 구체적으로 적는다.
   - 한국어로, 사실만 적는다. 추측으로 채우지 말고 모르는 칸은 `-` 로 둔다.

4. **목록 갱신**
   - `wiki/08-worklog/README.md` 의 `로그 목록` 표에 한 줄을 추가한다 (오래된 순).

5. **장기기억 갱신** (`wiki/09-memory/`, 작성 원칙은 그 폴더의 README)
   - `sessions.md`: 이력 표에 세션 행을 추가(또는 보완)하고, `현재 상태`와 `다음 할 일`을 이번 worklog 기준으로 **교체**한다. 끝난 할 일은 지운다.
   - 이번 세션에서 바뀐 것이 있을 때만 해당 문서를 고친다: 프로젝트 개념 → `project.md`, 설계 / ADR → `design.md`, 정책 / 규칙 / 작업 환경 → `policies.md`.
   - 고친 문서는 frontmatter `updated` 를 갱신한다. 요약과 링크만 두고 짧게 유지한다.

6. **마무리 보고**
   - 작성한 파일 경로, 3~5줄 요약, 장기기억에서 바뀐 부분, ADR 후보를 보여준다.
   - 커밋할지 물어본다. 동의하면 worklog, raw 로그, 장기기억, 이번 세션 변경분을 커밋한다. push는 따로 확인을 받는다. 커밋 메시지: `docs(worklog): WL-YYYY-MM-DD-NN <제목>`
