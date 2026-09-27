---
title: "세션 이력"
type: memory
tags: [memory]
created: 2026-09-27
updated: 2026-09-27
---

# 세션 이력

> [장기기억](README.md) · 원본: [Worklog 목록](../08-worklog/README.md)

## 현재 상태

- 위키 뼈대(00~10), ADR 0001~0010(모두 승인, 0001~0006 근거 보완 완료). 기준 문서(코딩, DB, 테스트, TDD, API, 에러 코드, 로깅, Clean Architecture, tech-stack)와 아키텍처 개요 · EDA · 통신 · 로드맵 · 사전 준비 · 보안은 `draft`.
- 남은 `todo` 15개: 기반 구축 때 쓸 것(local-setup, troubleshooting, ci-cd, containers, configuration, environments), 도메인 PRD 이후 쓸 것(02-domain 5개, glossary, api-reference, event-catalog, runbook).
- Git Flow 운영 중: `main` / `develop`(GitHub 기본 브랜치). 브랜치 보호 없음(Free private) → 직접 push 금지는 규칙. 로컬 `gitflow.*` 설정 완료.
- 개발 흐름과 에이전트 워크플로우 구현 완료: `.claude/agents/` 5개, 스킬 `/prd` · `/sprint` · `/retro` (**아직 한 번도 실행하지 않음**).
- 진행 중인 토픽 없음. 코드는 아직 없다.
- git: `develop` = `origin/develop`(`f4d4758`, PR #1~#4 병합). `main`은 `54d0347`(릴리스 없음). 이 세션 worklog와 위키 정비분은 `feature/wiki-baseline-docs` 브랜치.

## 다음 할 일

- [ ] `/prd`로 **PRD-001 기반 구축** 시험 운영 (솔루션 구조, BuildingBlocks, 빌드 설정, 아키텍처 테스트, CI)
- [ ] 기반 구축에서 ADR로 결정: Mediator 구현체, 단언 라이브러리, UUID v7 생성, API 스타일, 타입 검색 구현, 로그 수집기
- [ ] 첫 스프린트 후 에이전트 · 스킬 보완
- [ ] 프로젝트 범위(In / Out of Scope)와 이해관계자 정의
- [ ] 커밋 작성자 이메일 확인 (현재 전역 설정 `taehoon365@gmail.com`)

## 이력

| 세션 | 날짜 | 요약 |
|---|---|---|
| [WL-2026-09-27-01](../08-worklog/2026-09/2026-09-27-01-wiki-structure.md) | 2026-09-27 | 위키 기본 구조와 ADR 0001~0006 작성 (드라이브 고장으로 사후 복원) |
| [WL-2026-09-27-02](../08-worklog/2026-09/2026-09-27-02-worklog-convention.md) | 2026-09-27 | worklog 체계, 프롬프트 hook, GitHub 저장소 구성 → 개발 흐름 방향 확정, 장기기억(09-memory), Obsidian 재구성 |
| [WL-2026-09-27-03](../08-worklog/2026-09/2026-09-27-03-dev-workflow-and-agents.md) | 2026-09-27 | Git Flow 구성, 개발 흐름(토픽 → 스프린트 → 회고) 설계, 기준 문서 기본판, ADR 0007~0010, 에이전트 5개 · 스킬 3개 구현, 위키 미구축 항목 정비 |
