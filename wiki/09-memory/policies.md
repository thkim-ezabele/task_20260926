---
title: "정책 / 규칙"
type: memory
tags: [memory]
created: 2026-09-27
updated: 2026-09-29
---

# 정책 / 규칙

> [장기기억](README.md) · 원본: [위키 홈 작성 규칙](../README.md), [Worklog](../08-worklog/README.md)

## 개발 흐름 (확정, `/prd` · `/sprint` 운영 완료 · `/retro` 미실행)

원본: [개발 관리](../10-delivery/README.md), [에이전트 워크플로우](../10-delivery/agents.md)

1. **`/prd` 토픽 생성**: 인터뷰로 PRD 작성 → orchestrator ∥ dba ∥ developer 병렬 리뷰 → orchestrator가 스프린트 분할 → 승인 → `feature/prd-NNN-*` 브랜치 + Draft PR
2. **`/sprint SNN`**: 에이전트별 계획 리뷰 → 작업(`SNN-TNN`)마다 dba → developer → reviewer → tester (진입 점검, 반려 시 회귀, 작업당 3회 넘으면 중단) → orchestrator 결과 리뷰 · 백로그/기술부채 정리 → push, 태그 `sprint/SNN`
3. **`/retro PRD-NNN`**: 에이전트별 회고 → orchestrator 통합(FR 충족 표, 개선안) → 토픽 PR Merge commit → `release/0.N.0` → `v0.N.0`

- PRD 하나 = 토픽 브랜치 하나 = 릴리스 하나. 토픽은 한 번에 하나. 스프린트는 범위 고정.
- 커밋은 작업자 단계마다 로컬 커밋(footer `Stage:`), push는 스프린트 종료 때. 재작업은 새 커밋(reset 금지).
- 백로그(`BL-NNN`)·기술부채(`TD-NNN`)는 발견 즉시 `new`로 기록, 스프린트 종료 때 orchestrator가 정리. **스프린트 밖 대상만** 올리고, 같은 스프린트 인계 메모는 `handoff`로 진행 기록 · 다음 작업 입력에 넘긴다.
- ADR 작업은 developer 1차(`adr_drafts`, 파일 없음) → 사용자 확인(요약 표) → 2차(`accepted` 파일) → reviewer · tester. 문서 작업은 reviewer 점검표 대신 문서 규칙, tester는 명령 기반 점검표.
- `/sprint` 시작 시 환경 점검(Docker, SDK, gh). 종료 시 push → 토픽 PR CI 통과 확인 → DoD 기록 → 태그 `sprint/SNN`(CI 통과 전 태그 금지).
- 판정 · 기록만 남는 단계 커밋은 `docs(sprint): SNN-TNN <단계> 판정`. 문서 변경 시 `node scripts/check-docs.js`(링크 · 앵커 · frontmatter · 표 구조).
- S02 회고 규칙: 파이프라인 표 dba 열이 "해당 없음"이면 dba 호출 생략, reviewer PASS는 tester 커밋에 병합(작업당 커밋 약 3개). 완료 조건은 5~7문장(세부 단언은 handoff). 테스트 범위: 도메인 로직은 TDD 전체, 기반 · 셋팅은 완료 조건 항목당 성공 / 실패 / 엣지 최소 1개, tester는 빈 곳만 보강. 완료 조건 항목은 작업자가 혼자 스프린트 밖으로 내보내지 않음(BLOCKED로 판단받음). 원본: [에이전트 워크플로우](../10-delivery/agents.md#테스트-범위)
- RETRO-PRD-001 반영(PR #13): 문서 작업은 점검표 D1~D5 · 반려 분류(코드 컨벤션 / 누락 / 설계 / 버그, 문서 형식 / 사실 / 누락 / 범위 밖), 인계 메모에 알려진 잡음 · 제외 기준, CI로만 판정할 조건은 스프린트 종료 판정, 대리 승인은 스프린트 문서 목록 → `/retro` ④ 일괄 추인, dba 생성 SQL 점검표 a~g(원본 database.md), 환경 점검에 Docker API · dev-certs. 원본: [에이전트 워크플로우](../10-delivery/agents.md)
- 병행 토픽 예외(PRD-002 선례): 같은 폴더에서 다른 세션이 작업 중이면 git worktree로 분리, 토픽 브랜치에는 새 파일만, 공유 문서 · 번호는 첫 작업(재기준화)에서.
- 회고는 PRD 종료 뒤 `/retro PRD-NNN`에서만 한다(2026-09-28 사용자 지시, S05부터). 스프린트 종료 때는 스프린트 회고 · 스킬 / 에이전트 개선 · apply_before_next 반영을 하지 않고, 회고 절에는 "토픽 회고에서 다룸" 한 줄만 둔다(S05는 "/retro 입력용 메모").
- 스프린트 속도 튜닝(2026-09-28 사용자 결정, S06부터): 계획 리뷰 developer 1명 + orchestrator, 작업은 dba(DB 작업만) → developer → 검증(reviewer 코드 리뷰만 ∥ tester 실행 검증 작업당 1회), 진행 기록 한 행 200자 · 대조표 문서 미기록, 대응표 전수 대조는 종료 때 1회, CI 확인 · sprint 태그는 PRD 마지막 스프린트 push 뒤 일괄. 원본: [에이전트 워크플로우](../10-delivery/agents.md) RETRO-PRD-002에서 유지 결정(대리).
- 오케스트레이션 세션은 스프린트 승인 지점 · 결정 · `/retro` 개선안 처리를 **모두 직접 판단**한다(2026-09-29 사용자 지시, 사용자에게 묻지 않음). 추인은 `/retro` 회고 문서 "사용자 결정 필요"에 모은다. ADR 파일 작성은 여전히 사용자 확인 뒤.
- S03부터 오케스트레이션 세션이 스프린트 세션에 SendMessage로 지시하고 승인 지점(계획 리뷰 · 결정 · BLOCKED · 결과 리뷰)을 처리한다. 스킬 개선은 토픽 `/retro`에서 한꺼번에(진행 중 변경 금지, 사용자 지시 예외: 2026-09-28 속도 튜닝).
- 흐름 제어와 커밋은 스킬(메인 세션), 판단은 orchestrator. 서브에이전트는 다른 서브에이전트를 부를 수 없다. 에이전트 모델은 메인 세션 상속.
- 구현: `.claude/agents/`(orchestrator, dba, developer, reviewer, tester), `.claude/skills/`(prd, sprint, retro). 에이전트는 프롬프트 첫 줄 `mode:`로 작업 구분.
- 작업 관리는 GitHub Issues가 아니라 위키에서 한다.

## 기록

- **ADR**: 아키텍처, 기술 선택처럼 "왜"가 중요한 결정. 한 번 쓰면 고치지 않고, 바뀌면 새 ADR로 대체한다. ADR 파일은 사용자에게 확인한 뒤에 만든다.
- **Worklog**: 세션마다 "무엇을 요청하고 무엇을 했나". 사용자가 세션 종료 전에 직접 `/worklog`를 실행해서 작성한다. **Claude가 알아서 작성하지 않는다.** (2026-09-28 사용자 지시로 `sprint` · `worklog` · `retro`의 `disable-model-invocation`을 제거해 오케스트레이션 세션이 대신 실행함. 유지 여부는 사용자 결정 대기, [RETRO-PRD-001](../10-delivery/retros/RETRO-PRD-001.md#사용자-결정-필요))
- **프롬프트 원문**: `UserPromptSubmit` hook이 `08-worklog/raw/`에 자동으로 기록한다.
- **장기기억**: 이 폴더. `/worklog` 실행 시 함께 갱신한다.

## 문서

- 모든 위키 문서는 `wiki/` 안에서 한국어 Markdown으로 관리한다. 위키는 Obsidian vault다.
- 모든 문서에 frontmatter(`type`, `status`, `tags`, `created`, `updated`)를 둔다. 스키마는 [위키 홈](../README.md)의 작성 규칙을 따른다.
- 링크는 GitHub에서도 동작하도록 **상대경로 마크다운 링크**(`[텍스트](경로.md)`)를 쓰고, `[[wikilink]]`는 쓰지 않는다.
- 새 문서는 `_templates/`의 템플릿으로 만든다.

## Git

- 원격: `origin` = GitHub `thkim-ezabele/task_20260926`
- 커밋은 요청이 있을 때만 하고, push는 따로 확인을 받는다.
- **Git Flow**: `main`(릴리스, 태그 `vX.Y.Z`) / `develop`(통합, GitHub 기본 브랜치). 토픽은 `feature/prd-*`(Draft PR), 토픽 밖 작업은 `feature/*`, `release/*`·`hotfix/*`는 `main`으로. **모든 PR은 Merge commit**(Squash · Rebase는 GitHub 설정에서 끔, 병합 커밋 제목 = PR 제목). 원본: [Git 워크플로우](../04-development/git-workflow.md)
- `main` / `develop`에 직접 push하지 않는다. GitHub Free private라 브랜치 보호가 불가해 규칙으로 지킨다.
- 커밋 메시지는 Conventional Commits 형식을 쓴다(예: `docs(worklog): ...`, `feat(employee): ... (S01-T02)` + footer `Stage: developer`).
- `git flow` 설정은 `.git/config`에만 있어서 새로 clone하면 다시 설정해야 한다(방법은 Git 워크플로우 문서).

## 작업 환경

- Windows 11, Claude Code(Git Bash / PowerShell)
- `python`은 Windows 스토어 스텁이라 실행되지 않는다. 스크립트는 **Node.js**로 작성한다.
- .NET SDK(8.0.425 사용, `global.json` 8.0.400 + latestFeature), Docker, GitHub CLI(`gh`, 로그인됨)가 설치되어 있다.
- Windows 깊은 경로에서 clone하면 MAX_PATH로 빌드가 실패할 수 있다(짧은 경로 사용).
- 이 PC Docker Desktop 4.26(Engine API 1.43)에서는 통합 테스트(Testcontainers 4.15.0)에 `DOCKER_API_VERSION=1.43`이 필요하다(CI 무관, BL-102). 개발 인증서 미신뢰면 AppHost https 프로필 대시보드에 로그 · 추적이 안 보이므로 `--launch-profile http`를 쓴다(BL-099). Git Bash 내장 curl 8.6.0은 명령줄 한글을 CP949로 보내므로 인라인 한글 요청은 `C:\Windows\System32\curl.exe`로 보낸다(BL-142).
- 실행 증빙은 `wiki/10-delivery/evidence/<작업 ID>/`에 README(요약) · 마스킹한 원문 로그 · 캡처로 둔다(S03). 로컬 AppHost 볼륨은 `emergency-hub-postgres-data`만 지울 수 있다(다른 프로젝트 볼륨 금지).
