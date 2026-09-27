---
title: "WL-2026-09-27-03: Git Flow · 개발 흐름 · 기준 문서 · 에이전트 워크플로우 · 위키 정비"
type: worklog
date: 2026-09-27
session: "a5919677"
model: "Claude Code · Claude Opus 5.5"
sprint:
adrs: ["0001", "0002", "0003", "0004", "0005", "0006", "0007", "0008", "0009", "0010"]
aliases: [WL-2026-09-27-03]
tags: [worklog]
created: 2026-09-27
updated: 2026-09-27
---

# WL-2026-09-27-03: Git Flow · 개발 흐름 · 기준 문서 · 에이전트 워크플로우 · 위키 정비

- 원문 로그: [raw/2026-09-27](../raw/2026-09-27.md) (session `a5919677`)
- 관련: PR #1 ~ #4, ADR-0007 ~ 0010, 커밋 `ecc78a7`, `686c6c0`, `dbbbb0b`, `f37a118`, `c047953`, `f4d4758`

## 목표

> 프로젝트 워크플로우를 설계해 위키에 적용하고, 확정되지 않은 기본 문서를 정리한다. Git Flow로 Git을 구성하고, PRD → 스프린트 → 개발 흐름을 서브에이전트 기반으로 설계 · 구현한다.

## 주요 프롬프트

1. "해당 프로젝트의 워크플로우를 작성해서 wiki에 적용하고 아직 확정되지 않은 기본 wiki 문서들 정리를 해보자. 먼저 git flow 전략을 이용할꺼야"
2. "개발 흐름 세부 설계 진행해줘 설계안을 바로 반영하지 말고 나에게 먼저 브리핑 진행 후에 반영해야해"
3. "prd 스킬을 이용하면 기본적인 정보들에 대해 나에게 정보를 취득해 갈수 있도록 하고 … 서브에이전트는 오케스트레이터, dba, developer로 구성 … sprint 스킬 … dba, developer, reviewer, tester 에이전트가 차례대로 … 회귀해서 다시 진행"
4. "sprint 스킬 마지막에 오케스트레이터가 결과를 보고 … 백로그 또는 기술부채에 대해 정리 … 토픽에 대해 회고하는 워크플로우도 넣어야겠어"
5. "pr 생성은 … 토픽 생성 및 종료 시점에 브랜치 생성 및 pr만 … 커밋은 각 작업자별 작업 이후 바로"
6. "postgresql, c# 기준으로 컨벤션 … 코드값의 경우에는 string 형태 절대 금지 … bit masking … cqrs … 엣지케이스"
7. "읽기전용 db 분리는 하지 않지만 설정상에서는 나눠지도록 … dto는 record … repository … 쿼리 이외의 분기 처리 … 람다 … di 주입시 scope … type을 searching해서 자동으로 주입"
8. "로그 컨벤션 … 출력형태는 가독성좋은 string 형태 그리고 파일로 남길때는 json 형태"
9. "지금 wiki 구축되지 않은 항목 있는지부터 파악해줘" → "1~5번 추천대로 진행하고 ADR 근거 채우기도 허용"

## 작업 내용

- **Git Flow 구성**
  - 작업 트리가 깨끗하지 않아 `git flow init`이 거부되어, `gitflow.*` 설정을 직접 넣고 `develop`을 만들었다(태그 prefix `v`).
  - GitHub: 기본 브랜치 `develop`, Rebase merge 끔, 병합 후 브랜치 자동 삭제. 브랜치 보호는 Free private 저장소라 불가(HTTP 403) → 규칙으로 운영.
  - `git-workflow.md`(Git Flow), PR 템플릿 작성. 첫 feature PR(#1)로 흐름 검증.
- **개발 흐름 설계 (브리핑 → 반영)**: `wiki/10-delivery/` 신설. 토픽(PRD) → 스프린트 → 회고, ID 체계, 백로그 / 기술부채(`new` → 스프린트 종료 때 정리), 템플릿 `prd` · `sprint` · `retro`.
  - Git 운영: 토픽 브랜치 `feature/prd-NNN-*` + Draft PR, 작업자 단계별 로컬 커밋, 스프린트 종료 시 push + `sprint/SNN` 태그, 토픽 PR은 Merge commit, 토픽 = 릴리스.
  - `agents.md`: 에이전트 구성, `/prd` · `/sprint` · `/retro` 흐름, 인계 계약(YAML), 회귀 규칙(작업당 3회)
- **기준 문서 기본판** (PR #3): 코딩 컨벤션, 데이터베이스, 테스트 전략, Clean Architecture, 로깅 & 관측성
- **ADR 0007 ~ 0010** 작성(사용자 요청): CQRS, 정수 코드 · 비트 마스킹, 읽기 / 쓰기 DB 분리 · Repository 규칙, DI 자동 등록
- **에이전트 · 스킬 구현** (PR #4): `.claude/agents/` 5개, `.claude/skills/` `prd` · `sprint` · `retro`
- **위키 미구축 항목 점검 · 정비** (브랜치 `feature/wiki-baseline-docs`): 문서 55개의 상태 / TODO 점검 → 뼈대만 있는 문서 25개를 작성 시점별(지금 / 기반 구축 / 도메인 PRD 후)로 분류
  - `tech-stack.md`를 현재 결정에 맞게 수정(MediatR · FluentAssertions는 라이선스로 후보 전환, EF Core · NSubstitute 등 확정)
  - 에이전트 기준 문서 작성: `tdd-guide`, `api-guidelines`, `error-codes`(5자리 `S T NNN`, 로그 이벤트 ID `S0NNN`)
  - ADR 0001~0006의 비어 있던 배경 · 대안 · 선정 사유 보완(결정 변경 없음, 사용자 허용)
  - `architecture-overview`, `event-driven-architecture`, `service-communication`, `roadmap`, `prerequisites`, `security` 초안, `project-overview`에 범위 · 이해관계자는 PRD-001 인터뷰에서 받는다고 명시
  - 병렬 fork 3개로 나눠 작성하고, 링크 · 앵커를 스크립트로 점검

### 변경 파일

| 파일 | 변경 | 설명 |
|---|---|---|
| `wiki/04-development/git-workflow.md` | 수정 | Git Flow, 토픽 브랜치, 병합 방식, 태그, 역병합 |
| `.github/pull_request_template.md` | 추가 | 토픽 단위 PR 템플릿 |
| `wiki/10-delivery/` (README, agents, backlog, tech-debt, prd/, sprints/, retros/) | 추가 | 개발 관리 체계 |
| `wiki/_templates/prd.md`, `sprint.md`, `retro.md` | 추가 | 템플릿 |
| `wiki/_templates/worklog.md`, `.claude/skills/worklog/SKILL.md` | 수정 | `sprint` 필드 |
| `wiki/04-development/coding-conventions.md`, `database.md`, `testing-strategy.md`, `logging-observability.md` | 수정 | 기준 문서 기본판 (`draft`) |
| `wiki/03-architecture/clean-architecture.md` | 수정 | 레이어, 솔루션 구조, CQRS, DI |
| `wiki/03-architecture/adr/0007` ~ `0010` | 추가 | ADR |
| `.claude/agents/*.md` (5개), `.claude/skills/{prd,sprint,retro}/SKILL.md` | 추가 | 에이전트 · 스킬 |
| `wiki/README.md`, `wiki/00-overview/roadmap.md`, `wiki/09-memory/policies.md`, `design.md`, `.gitignore` | 수정 | 목차, frontmatter 유형, 장기기억, `logs/` |
| `wiki/03-architecture/tech-stack.md` | 수정 | 현재 결정 반영 |
| `wiki/04-development/tdd-guide.md`, `api-guidelines.md`, `wiki/05-api/error-codes.md` | 수정 | 에이전트 판정 기준 문서 초안 |
| `wiki/03-architecture/adr/0001` ~ `0006` | 수정 | 비어 있던 근거 보완 |
| `wiki/03-architecture/architecture-overview.md`, `event-driven-architecture.md`, `service-communication.md` | 수정 | 아키텍처 문서 초안 |
| `wiki/00-overview/roadmap.md`, `project-overview.md`, `wiki/01-getting-started/prerequisites.md`, `wiki/07-operations/security.md` | 수정 | 초안 / 범위 안내 |

## 결정 사항

| 결정 | 이유 | ADR |
|---|---|---|
| Git Flow (`main` 릴리스 / `develop` 통합), GitHub 기본 브랜치 `develop` | 사용자 지정 | - |
| 브랜치 보호 없이 규칙으로 직접 push 금지 | Free private 저장소 제한 | - |
| 작업 관리는 GitHub Issues가 아니라 위키 | 증빙이 git 이력으로 남음 | - |
| PRD = 토픽 브랜치 = 릴리스, 토픽은 한 번에 하나, 스프린트는 범위 고정 | PR은 토픽 단위로만(사용자) | - |
| 작업자 단계별 로컬 커밋, push는 스프린트 종료 때, 토픽 PR은 Merge commit | 단계별 증빙 보존 | - |
| 흐름 제어 · 커밋은 스킬, 판단은 orchestrator | 서브에이전트는 서브에이전트를 부를 수 없음 | - |
| 단위 테스트는 developer(TDD), tester는 통합 · 인수 | ADR-0006과 순서 충돌 해소 | - |
| 반려 작업당 3회 상한, 에이전트 모델은 메인 세션 상속, 첫 토픽은 기반 구축 | 추천안 확정 | - |
| CQRS 적용 | 사용자 요청 | 0007 |
| 코드값 문자열 절대 금지, 조합은 비트 마스킹, API · 이벤트 · 에러 코드도 정수 | 사용자 요청 | 0008 |
| 읽기 / 쓰기 연결 · DbContext 분리(현재 같은 DB), Repository는 람다 LINQ 쿼리만 | 사용자 요청 | 0009 |
| 마커 인터페이스 · 기반 클래스 + 어셈블리 검색으로 Scoped 자동 등록 | 사용자 요청 | 0010 |
| 모델 클래스는 모두 `record` | 사용자 요청 | - |
| 로그: 콘솔은 텍스트, 파일은 JSON(CLEF), 구조화 로그 | 사용자 요청 | ADR 후보 (수집기 결정 시) |
| 에러 코드 5자리 `S T NNN`(서비스 · 유형 · 일련번호), 로그 이벤트 ID는 유형 자리 0 | 정수 코드 정책(ADR-0008)을 에러 · 로그에 일관 적용 | - |
| 과거 ADR의 빈 근거는 보완 허용(결정 변경 없이, 하단에 보완 표기) | 과제 증빙 보강, 사용자 허용 | - |
| 긴급 전파 같은 중복 위험 `POST`는 `Idempotency-Key` 필수 | 중복 전파 방지 | - |
| .NET 8 유지 (지원 종료 2026-11-10을 인지한 상태에서) | 사용자 결정 | - |

## 이슈 / 배운 점

- `git flow init`은 작업 트리가 깨끗해야 한다. 설정을 직접 넣어 우회했고, 새로 clone하면 다시 설정해야 한다(방법은 `git-workflow.md`).
- 스쿼시 병합 직후 브랜치 비교(`git diff`)가 pull 전 상태로 차이를 보여 혼동했다. 병합 커밋과 브랜치 끝 커밋을 직접 비교해 동일함을 확인한 뒤 삭제했다.
- 병합 후 브랜치 자동 삭제 설정 때문에 `release/*`를 두 번 병합할 수 없어, 역병합용 `chore/backmerge-*` 브랜치를 규칙에 추가했다.
- 진행 중인 토픽의 PRD는 토픽 브랜치에만 있어 `develop`에서 보이지 않는다. `/prd` 사전 점검은 브랜치 / PR 기준으로 판단하도록 고쳤다.
- 라이선스 변경: MediatR v13+, FluentAssertions v8+ 상용화 → 구현체 선택을 기반 구축 토픽으로 미뤘다.
- 에이전트는 이 세션 안에서 바로 인식되었다(새 세션이 필요하다고 안내했던 것은 틀림).
- 로깅 문서의 예시 이벤트 ID(11001)가 새 에러 코드 체계에서 Identity 범위와 겹쳐 20001로 고쳤다.
- .NET 8 LTS 지원이 2026-11-10에 끝난다. 사용자가 현재 버전(.NET 8) 유지로 결정했다.

## 다음 할 일

- [ ] `/prd`로 **PRD-001 기반 구축** 토픽 시험 운영: 솔루션 구조, BuildingBlocks(Result, 마커, `AddConventionalServices`, 읽기 전용 DbContext 기반), `.editorconfig` · `Directory.Build.props` · `Directory.Packages.props`, 아키텍처 테스트, CI(GitHub Actions)
- [ ] 기반 구축 토픽에서 미정 항목을 ADR로 결정: Mediator 구현체, 단언 라이브러리, UUID v7 생성, API 스타일(Minimal API 기본안), 타입 검색 구현(Scrutor / 직접), 로그 수집기
- [ ] 첫 스프린트 후 에이전트 프롬프트 · 스킬 절차 보완 (회고 개선안 흐름)
- [ ] 남은 `todo` 문서: 기반 구축 때(local-setup, troubleshooting, ci-cd, containers, configuration, environments), 도메인 PRD 이후(02-domain 5개, glossary, api-reference, event-catalog, runbook)
- [ ] 프로젝트 범위(In / Out of Scope)와 이해관계자 정의 (PRD-001 인터뷰에서 함께)
- [ ] 커밋 작성자 이메일 확인 (현재 전역 설정 `taehoon365@gmail.com`)
