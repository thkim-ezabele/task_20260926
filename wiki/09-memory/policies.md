---
title: "정책 / 규칙"
type: memory
tags: [memory]
created: 2026-09-27
updated: 2026-09-27
---

# 정책 / 규칙

> [장기기억](README.md) · 원본: [위키 홈 작성 규칙](../README.md), [Worklog](../08-worklog/README.md)

## 개발 흐름 (설계 확정, 에이전트·스킬 구현 전)

원본: [개발 관리](../10-delivery/README.md), [에이전트 워크플로우](../10-delivery/agents.md)

1. **`/prd` 토픽 생성**: 인터뷰로 PRD 작성 → orchestrator ∥ dba ∥ developer 병렬 리뷰 → orchestrator가 스프린트 분할 → 승인 → `feature/prd-NNN-*` 브랜치 + Draft PR
2. **`/sprint SNN`**: 에이전트별 계획 리뷰 → 작업(`SNN-TNN`)마다 dba → developer → reviewer → tester (진입 점검, 반려 시 회귀, 작업당 3회 넘으면 중단) → orchestrator 결과 리뷰 · 백로그/기술부채 정리 → push, 태그 `sprint/SNN`
3. **`/retro PRD-NNN`**: 에이전트별 회고 → orchestrator 통합(FR 충족 표, 개선안) → 토픽 PR Merge commit → `release/0.N.0` → `v0.N.0`

- PRD 하나 = 토픽 브랜치 하나 = 릴리스 하나. 토픽은 한 번에 하나. 스프린트는 범위 고정.
- 커밋은 작업자 단계마다 로컬 커밋(footer `Stage:`), push는 스프린트 종료 때. 재작업은 새 커밋(reset 금지).
- 백로그(`BL-NNN`)·기술부채(`TD-NNN`)는 발견 즉시 `new`로 기록, 스프린트 종료 때 orchestrator가 정리.
- 흐름 제어는 스킬(메인 세션), 판단은 orchestrator. 서브에이전트는 다른 서브에이전트를 부를 수 없다. 에이전트 모델은 메인 세션 상속.
- 작업 관리는 GitHub Issues가 아니라 위키에서 한다.

## 기록

- **ADR**: 아키텍처, 기술 선택처럼 "왜"가 중요한 결정. 한 번 쓰면 고치지 않고, 바뀌면 새 ADR로 대체한다. ADR 파일은 사용자에게 확인한 뒤에 만든다.
- **Worklog**: 세션마다 "무엇을 요청하고 무엇을 했나". 사용자가 세션 종료 전에 직접 `/worklog`를 실행해서 작성한다. **Claude가 알아서 작성하지 않는다.**
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
- **Git Flow**: `main`(릴리스, 태그 `vX.Y.Z`) / `develop`(통합, GitHub 기본 브랜치). 토픽은 `feature/prd-*`(Draft PR → Merge commit), 토픽 밖 작업은 `feature/*`(Squash merge), `release/*`·`hotfix/*`는 `main`으로(Merge commit). 원본: [Git 워크플로우](../04-development/git-workflow.md)
- `main` / `develop`에 직접 push하지 않는다. GitHub Free private라 브랜치 보호가 불가해 규칙으로 지킨다.
- 커밋 메시지는 Conventional Commits 형식을 쓴다(예: `docs(worklog): ...`, `feat(employee): ... (S01-T02)` + footer `Stage: developer`).
- `git flow` 설정은 `.git/config`에만 있어서 새로 clone하면 다시 설정해야 한다(방법은 Git 워크플로우 문서).

## 작업 환경

- Windows 11, Claude Code(Git Bash / PowerShell)
- `python`은 Windows 스토어 스텁이라 실행되지 않는다. 스크립트는 **Node.js**로 작성한다.
- .NET SDK, Docker, GitHub CLI(`gh`, 로그인됨)가 설치되어 있다.
