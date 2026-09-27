---
title: "Emergency Hub Backend Wiki"
type: index
status: stable
tags: [home]
aliases: [Home, 위키 홈]
created: 2026-09-27
updated: 2026-09-27
---

# Emergency Hub Backend Wiki

> **직원 긴급연락망 서비스** Emergency Hub 백엔드 시스템의 설계, 개발, 운영 문서입니다.
> 위키 관련 문서는 모두 `wiki/` 폴더 안에서 관리하며, `wiki/` 폴더가 곧 **Obsidian vault**입니다.

| 항목 | 내용 |
|---|---|
| 언어 / 런타임 | C# / .NET 8 (LTS) |
| 아키텍처 | MSA · Clean Architecture |
| 설계 방법론 | DDD · EDA · TDD |
| 데이터베이스 | PostgreSQL |
| 형상관리 / CI | GitHub / GitHub Actions |

## 📚 목차

### 00. 개요
- [프로젝트 개요](00-overview/project-overview.md)
- [로드맵](00-overview/roadmap.md)
- [용어집](00-overview/glossary.md)

### 01. 시작하기
- [사전 준비 사항](01-getting-started/prerequisites.md)
- [로컬 개발 환경 구성](01-getting-started/local-setup.md)
- [트러블슈팅](01-getting-started/troubleshooting.md)

### 02. 도메인 (DDD)
- [도메인 개요](02-domain/domain-overview.md)
- [유비쿼터스 언어](02-domain/ubiquitous-language.md)
- [바운디드 컨텍스트 & 컨텍스트 맵](02-domain/bounded-contexts.md)
- [도메인 모델 (Aggregate / Entity / Value Object)](02-domain/domain-model.md)
- [도메인 이벤트](02-domain/domain-events.md)

### 03. 아키텍처
- [아키텍처 개요](03-architecture/architecture-overview.md)
- [MSA 서비스 카탈로그](03-architecture/service-catalog.md)
- [Clean Architecture & 솔루션 구조](03-architecture/clean-architecture.md)
- [이벤트 기반 아키텍처 (EDA)](03-architecture/event-driven-architecture.md)
- [서비스 간 통신](03-architecture/service-communication.md)
- [기술 스택](03-architecture/tech-stack.md)
- [아키텍처 결정 기록 (ADR)](03-architecture/adr/README.md)

### 04. 개발 가이드
- [코딩 컨벤션](04-development/coding-conventions.md)
- [Git 워크플로우 (Git Flow)](04-development/git-workflow.md)
- [TDD 가이드](04-development/tdd-guide.md)
- [테스트 전략](04-development/testing-strategy.md)
- [API 설계 가이드](04-development/api-guidelines.md)
- [데이터베이스 (PostgreSQL)](04-development/database.md)
- [로깅 & 관측성](04-development/logging-observability.md)

### 05. API & 이벤트 명세
- [API 레퍼런스](05-api/api-reference.md)
- [이벤트 카탈로그](05-api/event-catalog.md)
- [에러 코드](05-api/error-codes.md)

### 06. 배포
- [환경 구성](06-deployment/environments.md)
- [CI/CD (GitHub Actions)](06-deployment/ci-cd.md)
- [컨테이너 & 로컬 인프라](06-deployment/containers.md)
- [설정 & 시크릿 관리](06-deployment/configuration.md)

### 07. 운영
- [운영 런북](07-operations/runbook.md)
- [보안 & 개인정보](07-operations/security.md)

### 08. 작업 로그
- [작업 로그 (Worklog)](08-worklog/README.md)

### 09. 장기기억
- [장기기억 (Project Memory)](09-memory/README.md): 새 세션이 이어받을 프로젝트 핵심 요약 (루트 `CLAUDE.md`가 자동 로드)

### 10. 개발 관리
- [개발 관리 (Delivery)](10-delivery/README.md): 토픽(PRD) → 스프린트 → 회고 흐름, Git 운영, ID 체계
- [에이전트 워크플로우](10-delivery/agents.md): `/prd` · `/sprint` · `/retro`와 서브에이전트 구성
- [백로그](10-delivery/backlog.md)
- [기술부채](10-delivery/tech-debt.md)

---

## 📝 문서 작성 규칙

- 모든 문서는 Markdown(`.md`)으로, **한국어**로 작성합니다.
- 폴더/파일명은 `kebab-case`로 하고, 폴더 앞의 숫자 접두어로 읽는 순서를 표시합니다.
- 아직 작성하지 않은 항목은 `> TODO:`로 표시합니다.
- 기술적 의사결정은 [ADR](03-architecture/adr/README.md)로 기록합니다.
- 세션별 작업 내용은 [Worklog](08-worklog/README.md)로 기록합니다 (세션 종료 전 `/worklog`).
- 프로젝트 개념, 설계, 정책이 바뀌면 [장기기억](09-memory/README.md)도 갱신합니다.
- 문서를 변경하면 하단의 `변경 이력`과 frontmatter의 `updated`를 갱신합니다.
- 다이어그램은 GitHub와 Obsidian에서 모두 렌더링되는 [Mermaid](https://mermaid.js.org/)를 사용합니다.

### Obsidian 규칙

- **링크**: GitHub에서도 동작하도록 상대경로 마크다운 링크(`[텍스트](경로.md)`)를 씁니다. `[[wikilink]]`는 쓰지 않습니다. vault 설정(`.obsidian/app.json`)에서 마크다운 링크와 상대경로가 기본값으로 지정되어 있습니다.
- **템플릿**: 새 문서는 [`_templates/`](_templates/)의 템플릿(`doc`, `adr`, `worklog`, `prd`, `sprint`, `retro`)으로 만듭니다. Obsidian의 Templates 코어 플러그인을 켜면 바로 쓸 수 있습니다.
- **첨부 파일**: `_assets/`에 둡니다.
- **공유 범위**: `.obsidian/`의 vault 설정은 git으로 공유하고, 개인 작업 상태(`workspace*.json`)와 커뮤니티 플러그인은 제외합니다.

### Frontmatter

모든 문서는 맨 위에 YAML frontmatter를 둡니다. 본문에는 상태를 따로 적지 않습니다.

| 필드 | 필수 | 값 |
|---|---|---|
| `title` | ✅ | 문서 제목 (본문 H1과 동일) |
| `type` | ✅ | `doc` · `index` · `adr` · `worklog` · `raw-log` · `memory` · `prd` · `sprint` · `retro` |
| `status` | `doc`, `index`, `adr`, `prd`, `sprint` | 아래 상태 범례 참고 |
| `tags` | ✅ | 섹션 태그 (예: `[architecture]`, `[adr, architecture]`) |
| `created` / `updated` | ✅ | `YYYY-MM-DD` |
| `aliases` | 선택 | 검색용 별칭 (예: `[ADR-0001]`, `[WL-2026-09-27-01]`) |

유형별 추가 필드:
- `adr`: `adr`(번호), `date`, `deciders`, `supersedes`, `superseded_by`
- `worklog`: `date`, `session`, `model`, `sprint`, `adrs`
- `prd`: `prd`(번호), `received`, `sprints`, `branch`, `pr`, `release`, `retro`
- `sprint`: `sprint`(번호), `prd`, `started`, `finished`, `adrs`, `worklogs`
- `retro`: `prd`, `sprints`, `release`, `date`

## 📌 문서 상태 범례

**일반 문서** (`doc`, `index`)

| `status` | 표시 | 의미 |
|---|---|---|
| `stable` | 🟢 확정 | 합의 완료 |
| `draft` | 🟡 검토 중 | 초안 / 논의 중 |
| `todo` | ⚪ 미작성 | 뼈대만 존재 |

**ADR** (`adr`)

| `status` | 의미 |
|---|---|
| `proposed` | 제안 |
| `accepted` | 승인 |
| `deprecated` | 폐기 |
| `superseded` | 대체됨 (`superseded_by`에 새 ADR 번호) |

**PRD** (`prd`)

| `status` | 의미 |
|---|---|
| `draft` | 접수, 분석 / 질문 중 |
| `stable` | 요구사항 확정 (사용자 확인), 토픽 진행 중 |
| `done` | 모든 스프린트 종료, 회고 · 병합 · 릴리스 완료 |
| `superseded` | 새 PRD로 대체됨 |

**스프린트** (`sprint`)

| `status` | 의미 |
|---|---|
| `planned` | 계획 작성, 승인 대기 |
| `active` | 진행 중 |
| `done` | 종료 (정리 · push · `sprint/SNN` 태그 완료) |

본문 표 안의 🟢 / 🟡 / ⚪ 표시는 문서 안 개별 항목의 상태를 나타낼 때만 씁니다.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 위키 기본 구조 생성 |
| 2026-09-27 | - | 08. 작업 로그 섹션 추가 |
| 2026-09-27 | - | 09. 장기기억 섹션 추가, Obsidian vault 형식(frontmatter, 템플릿, vault 설정)으로 재구성 |
| 2026-09-27 | - | 10. 개발 관리 섹션 추가, frontmatter에 `prd` / `sprint` / `retro` 유형 추가 |
