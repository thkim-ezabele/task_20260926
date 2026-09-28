---
name: prd
description: 새 토픽(PRD)을 만든다. 사용자 인터뷰로 PRD를 채우고, orchestrator · dba · developer 에이전트가 병렬 리뷰한 뒤 orchestrator가 스프린트를 나눈다. 승인되면 PRD · 스프린트 문서, 토픽 브랜치, Draft PR을 만든다. 사용자가 /prd 로 직접 실행한다.
disable-model-invocation: true
argument-hint: "[PRD 원문 또는 파일 경로 (선택)]"
---

# /prd: 토픽 생성

PRD 하나를 토픽 하나로 만든다. 흐름과 규칙의 원본은 `wiki/10-delivery/README.md`, `wiki/10-delivery/agents.md`다.

- 입력: `$ARGUMENTS` (PRD 원문, 파일 경로, 또는 비어 있음)
- 에이전트: `orchestrator`, `dba`, `developer` (Agent 도구의 `subagent_type`)
- 템플릿: `wiki/_templates/prd.md`, `wiki/_templates/sprint.md`

## 규칙

- **사용자 승인 전에는 커밋 · push · 브랜치 생성 · PR 생성을 하지 않는다.** 승인 지점은 ② PRD 초안 확인, ⑥ 최종 승인 두 곳이다.
- 병렬 리뷰는 **한 메시지에서 Agent 도구를 여러 번 호출**해 동시에 실행한다. 에이전트는 결과만 반환하고, 문서는 이 스킬(메인 세션)이 쓴다.
- 에이전트 결과를 사용자에게 보고할 때는 원문을 그대로 붙이지 말고 요약한다.
- 모든 문서는 한국어, frontmatter와 변경 이력 규칙(`wiki/README.md`)을 따른다.

## 절차

### ⓪ 사전 점검

1. `git status --short`로 작업 트리를 확인한다(`wiki/08-worklog/raw/` 변경은 무시). 다른 변경이 있으면 멈추고 사용자에게 알린다.
2. 현재 브랜치가 `develop`인지 확인한다. 아니면 `git checkout develop && git pull --ff-only`를 제안한다.
3. **진행 중인 토픽이 있는지** 확인한다. 진행 중인 토픽의 문서는 토픽 브랜치에만 있으므로 브랜치로 판단한다: `git fetch --prune` 후 `git branch -a --list "*feature/prd-*"` 또는 `gh pr list --state open --search "head:feature/prd-"`에 결과가 있으면 멈추고 알린다. 토픽은 한 번에 하나만 진행한다.
4. 다음 번호를 정한다: PRD는 `develop`의 `prd/` 파일과 병합된 토픽 브랜치 이름(`git log --oneline --merges develop`의 `feature/prd-NNN`) 중 가장 큰 번호 + 1, 스프린트 시작 번호는 `sprints/`의 가장 큰 번호 + 1 (없으면 `PRD-001`, `S01`).

### ① 인터뷰

`$ARGUMENTS`로 받은 원문(또는 파일 내용)을 먼저 읽고, 아래 항목 중 **빠졌거나 모호한 것만** 묻는다. AskUserQuestion은 선택지가 분명한 질문에 쓰고, 서술형은 일반 질문으로 묻는다. 한 번에 2~4개씩, 여러 번에 나눠 묻는다.

| 영역 | 항목 |
|---|---|
| 배경 | 목적, 해결할 문제, 사용자 / 이해관계자 |
| 기능 | 기능 목록, 우선순위, 인수 조건(완료 판단 기준) |
| 비기능 | 성능(예: N명 전파 M초 이내), 가용성, 보안 · 권한, 개인정보 |
| 데이터 | 다루는 데이터, 보존 기간, 민감 정보 |
| 연동 | 외부 시스템(SMS 사업자, 푸시, 메일), 다른 서비스 |
| 경계 | 범위 밖, 제약(기한, 기술), 이미 결정된 사항 |

- "모름 / 추후"도 답으로 받는다. 그런 항목은 PRD의 "질문과 답변" 표에 미해결로 남긴다.
- 토픽 이름(영어 kebab-case, 브랜치 · 파일명에 사용)을 정한다. 예: `emergency-broadcast`

### ② PRD 초안

1. 템플릿으로 `wiki/10-delivery/prd/PRD-NNN-<topic>.md`를 작성한다(`develop`의 작업 트리에 작성, 커밋하지 않음). `status: draft`
   - **원문** 절: 사용자가 준 원문을 그대로 둔다. 인터뷰로만 받았다면 인터뷰 답변을 정리해 "원문(인터뷰 정리)"으로 둔다.
   - **분석** 절: 목적, 기능 요구사항(`FR-NN`, 우선순위, 인수 조건), 비기능 요구사항(`NFR-NN`), 범위 밖, 영향 범위
2. 사용자에게 요구사항 표를 요약해 보여주고 **확인을 받는다.** 수정 요청이 있으면 반영하고 다시 확인한다.

### ③ 병렬 리뷰

한 메시지에서 세 에이전트를 동시에 호출한다. 프롬프트 형식:

```
mode: prd-review
PRD: wiki/10-delivery/prd/PRD-NNN-<topic>.md
프로젝트 맥락: wiki/09-memory/ 를 먼저 읽을 것
반환: 에이전트 정의의 "리뷰 반환 형식"(YAML)
```

### ④ 통합

1. `orchestrator`를 `mode: prd-integrate`로 호출한다. 프롬프트에 세 리뷰 결과(YAML)를 그대로 넣는다.
2. `blocking_questions`가 있으면 사용자에게 묻고(①의 방식), 답을 PRD에 반영한다. 답이 요구사항을 크게 바꿨다면 영향을 받는 에이전트만 ③을 다시 실행한다.
3. `prd_changes`를 PRD에 반영하고, "리뷰 요약" 표와 "질문과 답변" 표를 채운다.

### ⑤ 스프린트 분할

`orchestrator`를 `mode: sprint-split`로 호출한다. 프롬프트에 PRD 경로, 통합 결과, **스프린트 시작 번호**를 넣는다.

### ⑥ 브리핑과 승인

사용자에게 다음을 보고하고 **승인을 받는다.**

- PRD 요약: 목적, FR / NFR 개수, 범위 밖
- 리뷰 요약: 관점별 주요 발견, 반영한 것, ADR 후보
- 스프린트 분할안: 스프린트별 목표와 작업 표(ID, 작업, FR, 완료 조건, 의존), `unassigned` 요구사항
- 승인하면 할 일: 브랜치 `feature/prd-NNN-<topic>` 생성, 커밋, push, Draft PR 생성

수정 요청이 있으면 해당 단계(②, ④, ⑤)로 돌아간다.

### ⑦ 반영

1. 브랜치를 만든다: `git checkout -b feature/prd-NNN-<topic>` (작성한 PRD가 함께 넘어온다)
2. 스프린트 문서를 템플릿으로 만든다: `wiki/10-delivery/sprints/SNN-<kebab-title>.md`, `status: planned`, 작업 표를 분할안으로 채운다(상태 `todo`). 분할안의 `kind: doc` 작업은 작업 이름 뒤에 "(문서 작업)", `evidence_frame: true` 작업은 "(증빙 틀)"로 표시한다.
3. PRD를 갱신한다: `status: stable`, `sprints`, `branch`, `release: "v0.N.0"`(`main`의 마지막 `v0.X.0` 태그 + 1, 없으면 `v0.1.0`)
4. `wiki/10-delivery/README.md`의 PRD 목록과 스프린트 목록, `wiki/00-overview/roadmap.md` 일정표에 행을 추가한다.
5. 커밋한다(`wiki/08-worklog/raw/`는 제외):
   ```
   docs(prd): PRD-NNN <토픽 제목> 토픽 생성

   - 요구사항 FR N개, NFR N개
   - 스프린트 SNN~SNN 계획

   Stage: orchestrator
   ```
6. push: `git push -u origin feature/prd-NNN-<topic>`
7. Draft PR을 만든다: `gh pr create --draft --base develop --title "feat(<scope>): PRD-NNN <토픽 제목>"`. 본문은 `.github/pull_request_template.md` 구조로, 토픽 절에 PRD와 스프린트 링크를 채운다.
8. PR 번호를 PRD의 `pr` 필드와 README PRD 목록에 적고 커밋 · push한다: `docs(prd): PRD-NNN Draft PR 연결`

### ⑧ 마무리 보고

- 만든 파일, 브랜치, Draft PR 링크
- 다음 단계: `/sprint SNN` (첫 스프린트)
- ADR 후보가 있으면 목록 (ADR 파일은 사용자 확인 후에만 만든다)
