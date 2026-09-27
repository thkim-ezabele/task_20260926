# Emergency Hub Backend Wiki

> **직원 긴급연락망 서비스** Emergency Hub 백엔드 시스템의 설계, 개발, 운영 문서입니다.
> 위키 관련 문서는 모두 `wiki/` 폴더 안에서 관리합니다.

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
- [Git 워크플로우 (GitHub Flow)](04-development/git-workflow.md)
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

---

## 📝 문서 작성 규칙

- 모든 문서는 Markdown(`.md`)으로, **한국어**로 작성합니다.
- 폴더/파일명은 `kebab-case`로 하고, 폴더 앞의 숫자 접두어로 읽는 순서를 표시합니다.
- 아직 작성하지 않은 항목은 `> TODO:`로 표시합니다.
- 기술적 의사결정은 [ADR](03-architecture/adr/README.md)로 기록합니다.
- 세션별 작업 내용은 [Worklog](08-worklog/README.md)로 기록합니다 (세션 종료 전 `/worklog`).
- 문서를 변경하면 하단의 `변경 이력`을 갱신합니다.
- 다이어그램은 GitHub에서 렌더링되는 [Mermaid](https://mermaid.js.org/)를 사용합니다.

## 📌 문서 상태 범례

| 표시 | 의미 |
|---|---|
| 🟢 확정 | 합의 완료 |
| 🟡 검토 중 | 초안 / 논의 중 |
| ⚪ 미작성 | 뼈대만 존재 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 위키 기본 구조 생성 |
| 2026-09-27 | - | 08. 작업 로그 섹션 추가 |
