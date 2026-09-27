---
title: "정책 / 규칙"
type: memory
tags: [memory]
created: 2026-09-27
updated: 2026-09-27
---

# 정책 / 규칙

> [장기기억](README.md) · 원본: [위키 홈 작성 규칙](../README.md), [Worklog](../08-worklog/README.md)

## 개발 흐름 (기본 방향 확정, 세부 설계 예정)

1. 사용자가 **PRD 형태의 요구사항**을 하나 준다.
2. 그 PRD를 **topic으로 스프린트**를 만들고 개발한다.
3. 개발하면서 생기는 **백로그와 기술부채를 반드시 기록**한다.

> 폴더 구조, 템플릿, 스프린트와 worklog / ADR의 연결 방식은 아직 설계하지 않았습니다. 설계 전에는 스프린트나 백로그 문서를 임의로 만들지 않습니다.

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

- 원격: `origin` = GitHub `thkim-ezabele/task_20260926`, 브랜치 `main`
- 커밋은 요청이 있을 때만 하고, push는 따로 확인을 받는다.
- 커밋 메시지는 Conventional Commits 형식을 쓴다(예: `docs(worklog): ...`, `chore: ...`). 세부 규칙은 [Git 워크플로우](../04-development/git-workflow.md)에서 정할 예정이다.

## 작업 환경

- Windows 11, Claude Code(Git Bash / PowerShell)
- `python`은 Windows 스토어 스텁이라 실행되지 않는다. 스크립트는 **Node.js**로 작성한다.
- .NET SDK, Docker, GitHub CLI(`gh`, 로그인됨)가 설치되어 있다.
