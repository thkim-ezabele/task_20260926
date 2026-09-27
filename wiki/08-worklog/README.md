---
title: "작업 로그 (Worklog)"
type: index
status: stable
tags: [worklog]
created: 2026-09-27
updated: 2026-09-27
---

# 작업 로그 (Worklog)

> 세션마다 무엇을 요청했고 무엇을 했는지 시간 순서대로 기록합니다.
>
> [위키 홈](../README.md)

## ADR과의 차이

| 구분 | ADR | Worklog |
|---|---|---|
| 목적 | **왜** 이렇게 결정했나 | **무엇을** 요청하고 수행했나 |
| 주기 | 중요한 결정이 있을 때 | 세션마다 |
| 수정 | 한 번 쓰면 고치지 않음 (대체만 가능) | 해당 세션 안에서 보완 가능 |

Worklog의 결정 중 아키텍처에 영향을 주는 것은 [ADR](../03-architecture/adr/README.md)로 승격하고, 서로 링크를 겁니다.

## 구성

```
08-worklog/
├── README.md        # 이 문서 (목록)
├── raw/             # 프롬프트 원문 (hook이 자동 기록, 일 단위)
│   └── YYYY-MM-DD.md
└── YYYY-MM/         # 세션 정리본 (월 단위 폴더)
    └── YYYY-MM-DD-NN-kebab-title.md
```

- **정리본**: 세션을 끝내기 전에 `/worklog` 스킬로 작성합니다. `NN`은 그날의 세션 순번이고, 템플릿은 [`_templates/worklog.md`](../_templates/worklog.md)입니다. 이때 [장기기억](../09-memory/README.md)도 함께 갱신합니다.
- **원문**: `.claude/settings.json`의 `UserPromptSubmit` hook(`.claude/hooks/log-prompt.js`)이 프롬프트를 자동으로 덧붙입니다. 직접 수정하지 않습니다. 검색 결과가 어수선해지지 않도록 Obsidian 검색과 그래프에서는 제외했습니다.

## 로그 목록

| ID | 날짜 | 제목 | 관련 ADR |
|---|---|---|---|
| [WL-2026-09-27-01](2026-09/2026-09-27-01-wiki-structure.md) | 2026-09-27 | 위키 기본 구조 및 초기 ADR 작성 | 0001~0006 |
| [WL-2026-09-27-02](2026-09/2026-09-27-02-worklog-convention.md) | 2026-09-27 | Worklog 체계 · 장기기억 · Obsidian 재구성 | - |
| [WL-2026-09-27-03](2026-09/2026-09-27-03-dev-workflow-and-agents.md) | 2026-09-27 | Git Flow · 개발 흐름 · 기준 문서 · 에이전트 워크플로우 · 위키 정비 | 0001~0010 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
