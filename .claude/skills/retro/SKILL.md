---
name: retro
description: 토픽(PRD)의 모든 스프린트가 끝난 뒤 토픽 회고를 한다. dba · developer · reviewer · tester의 관점별 회고를 orchestrator가 통합(FR 충족 표, 파이프라인 분석, 개선안)하고, 승인되면 회고 문서를 쓰고 토픽 PR 병합과 릴리스(v0.N.0)까지 진행한다. 사용자가 /retro PRD-NNN 으로 직접 실행한다.
disable-model-invocation: true
argument-hint: "PRD-NNN (예: PRD-001)"
---

# /retro: 토픽 회고와 릴리스

흐름의 원본은 `wiki/10-delivery/agents.md`의 `/retro` 절, Git 규칙은 `wiki/04-development/git-workflow.md`다.

- 입력: `$ARGUMENTS` = PRD ID. 비어 있으면 현재 토픽 브랜치의 PRD를 쓴다.
- 에이전트: `orchestrator`, `dba`, `developer`, `reviewer`, `tester`
- 템플릿: `wiki/_templates/retro.md`

## 규칙

- **사용자 승인 지점**: ④ 회고 내용과 개선안 처리, ⑥ 병합 · 릴리스(되돌리기 어려우므로 단계별로 확인).
- `main` / `develop`에는 직접 push하지 않는다. 병합은 모두 GitHub PR로 한다.
- 커밋에서 `wiki/08-worklog/raw/`는 제외한다.

## 절차

### ⓪ 진입 점검

1. PRD 문서를 찾고, 현재 브랜치가 PRD의 `branch`인지 확인한다.
2. PRD의 `sprints`가 **모두 `status: done`**이고 `sprint/SNN` 태그가 있는지 확인한다. 아니면 멈추고 남은 스프린트를 알린다.
3. 백로그 / 기술부채에 `new` 항목이 없는지 확인한다. 있으면 멈추고 알린다.
4. 작업 트리가 깨끗하고 원격과 동기화되어 있는지 확인한다(`git status`, `git fetch` 후 `git status -sb`).

### ① 자료 수집 (메인 세션)

에이전트에게 넘길 자료 목록을 만든다. 파일 내용을 복사하지 말고 경로와 명령 결과 요약을 넘긴다.

- PRD, 스프린트 문서들(진행 기록 포함), `backlog.md`, `tech-debt.md`
- 토픽 커밋: `git log --oneline develop..HEAD`, 단계별 개수(`Stage:` footer 기준), 반려 커밋 수
- 스프린트 태그: `git tag --list "sprint/*"`
- 토픽 PR: `gh pr view <pr> --json number,title,url,commits`
- 이 토픽 기간의 worklog: 스프린트 문서 `worklogs` 필드

### ② 관점별 회고 (병렬)

한 메시지에서 `dba`, `developer`, `reviewer`, `tester`를 동시에 호출한다.

```
mode: retro
PRD: <경로>
스프린트: <경로 목록>
자료: <① 요약>
반환: 에이전트 정의의 retro 반환 형식
```

### ③ 통합

`orchestrator`를 `mode: retro-integrate`로 호출하고, 네 결과와 ① 자료를 넣는다.

### ④ 브리핑과 승인

사용자에게 보고하고 **승인을 받는다.**

- 요약, FR 충족 표(충족 / 부분 / 이관), 계획 대비 실제, 파이프라인 반려 분석, 백로그 / 기술부채 추이
- 개선안 목록: 항목마다 **반영 / 보류**를 사용자에게 고르게 한다(AskUserQuestion, multiSelect).
- ADR 후보

### ⑤ 반영

1. 회고 문서를 쓴다: `wiki/10-delivery/retros/RETRO-PRD-NNN.md` (템플릿 구성, 개선안 표의 "처리"에 반영 / 보류(BL-NNN))
2. 보류한 개선안은 `backlog.md`에 행으로 추가한다(`status: open`, 출처 `RETRO-PRD-NNN`).
3. PRD의 `retro` 필드와 README PRD 목록에 회고 링크를 적는다.
4. 워크플로우 · 규칙이 바뀌는 개선안이면 `wiki/09-memory/`(policies / design) 갱신 대상을 보고한다(반영은 개선안 브랜치에서).
5. 커밋: `docs(retro): RETRO-PRD-NNN <토픽 제목> 회고` + footer `Stage: orchestrator`, push

**반영하기로 한 개선안**은 토픽 PR과 섞지 않는다. 병합 · 릴리스 후 `develop`에서 `feature/retro-prd-NNN-<설명>` 브랜치를 만들어 별도 PR로 반영한다(⑦에서 안내).

### ⑥ 병합과 릴리스 (단계마다 사용자 확인)

1. **토픽 PR 병합**
   - PR 본문의 FR 충족 요약과 회고 링크를 갱신한다(`gh pr edit`).
   - PRD를 `status: done`으로 바꾸고, README PRD 목록 상태를 갱신해 커밋 · push한다: `docs(prd): PRD-NNN 완료`
   - `gh pr ready <pr>` → `gh pr merge <pr> --merge` (**Merge commit**, squash 금지)
2. **릴리스 브랜치**
   - `git checkout develop && git pull --ff-only`
   - `git checkout -b release/0.N.0` (N은 PRD의 `release`)
   - 버전 파일이 있으면(`Directory.Build.props`의 `<Version>`) `0.N.0`으로 올리고 커밋: `chore(release): v0.N.0`
   - push 후 `main` 대상 PR 생성 → `gh pr merge --merge`
3. **태그**
   - `git checkout main && git pull --ff-only`
   - `git tag -a v0.N.0 -m "v0.N.0: PRD-NNN <토픽 제목>"` → `git push origin v0.N.0`
4. **develop 역병합**
   - `git checkout -b chore/backmerge-v0.N.0 main` → push → `develop` 대상 PR 생성 → `gh pr merge --merge`
   - GitHub가 차이가 없다고 PR 생성을 거부하면 역병합을 건너뛰고 그 사실을 보고한다.
5. `git checkout develop && git pull --ff-only`, 지워진 원격 브랜치 정리(`git fetch --prune`, 로컬 브랜치 삭제)

### ⑦ 마무리 보고

- 회고 문서, FR 충족 결과, 병합된 PR, 릴리스 태그
- 반영하기로 한 개선안과 다음 할 일: `feature/retro-prd-NNN-*` 브랜치에서 반영
- 다음 토픽은 `/prd`로 시작
- 세션을 마치기 전에 `/worklog` 실행을 안내한다
