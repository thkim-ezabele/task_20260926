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

- 위키 뼈대(00~09)와 ADR 0001~0006이 있다. 대부분의 문서는 아직 `status: todo`다.
- 기록 체계를 갖췄다: `/worklog` 스킬(정리본 + 장기기억 갱신), 프롬프트 원문 hook, 장기기억(이 폴더, `CLAUDE.md`가 import).
- 위키는 Obsidian vault 형식이다(frontmatter, `_templates/`, `.obsidian/`).
- 기본 개발 흐름(PRD → 스프린트 → 개발, 백로그·기술부채 기록)은 방향만 정했고, 세부 설계는 아직이다.
- 코드는 아직 없다.
- git: `origin/main`에는 `a7214b1`까지만 push했다. `54d0347`(WL-02 첫 버전)은 로컬 커밋만 했고, 그 뒤 변경분(장기기억, Obsidian 재구성, WL-02 보완)은 아직 커밋하지 않았다.

## 다음 할 일

- [ ] **PRD → 스프린트 → 개발 흐름 세부 설계**: 스프린트, 백로그, 기술부채 문서의 폴더 구조, 템플릿, frontmatter, worklog·ADR·장기기억과의 연결 방식
- [ ] 새 세션에서 `CLAUDE.md` import로 장기기억이 자동으로 로드되는지 확인
- [ ] 커밋 작성자 이메일 확인 (현재 전역 설정 `taehoon365@gmail.com`)
- [ ] 프로젝트 범위(In / Out of Scope)와 이해관계자 정의
- [ ] Git 워크플로우(브랜치 전략, 커밋 규칙) 작성

## 이력

| 세션 | 날짜 | 요약 |
|---|---|---|
| [WL-2026-09-27-01](../08-worklog/2026-09/2026-09-27-01-wiki-structure.md) | 2026-09-27 | 위키 기본 구조와 ADR 0001~0006 작성 (드라이브 고장으로 사후 복원) |
| [WL-2026-09-27-02](../08-worklog/2026-09/2026-09-27-02-worklog-convention.md) | 2026-09-27 | worklog 체계, 프롬프트 hook, GitHub 저장소 구성 → 개발 흐름 방향 확정, 장기기억(09-memory), Obsidian 재구성 |
