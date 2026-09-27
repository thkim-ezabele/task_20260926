---
title: "S01: 기술 결정 확정과 빌드 · CI 기반"
type: sprint
sprint: "S01"
status: planned
prd: [PRD-001]
started:
finished:
adrs: []
worklogs: []
aliases: [S01]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-27
---

# S01: 기술 결정 확정과 빌드 · CI 기반

- PRD: [PRD-001](../prd/PRD-001-foundation.md)
- 토픽 브랜치: `feature/prd-001-foundation` · 스프린트 종료 태그: `sprint/S01`

## 목표

> PRD-001의 기술 결정이 모두 `accepted` ADR로 커밋되고, 새 clone에서 경고 0으로 빌드되며, BuildingBlocks.Domain 단위 테스트가 토픽 PR의 GitHub Actions에서 통과한다.

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S01-T01 | 사전 확인: Aspire 9.x 마이너 버전, 패키지 버전 정합, 라이선스 | FR-01, NFR-05 | Aspire 9.x 마이너 버전(net8.0 AppHost 지원, 패치 · 지원 기간), EF Core · Npgsql · EFCore.NamingConventions · dotnet-ef 8.0.x 조합, PostgreSQL 이미지 메이저 태그(Aspire · Testcontainers 공용), 도입 패키지별 버전 · 라이선스 · 출처가 표로 기록된다. 상용 라이선스 0건. PRD Q19 해소 | - | todo | |
| S01-T02 | ADR (데이터 · 인프라): Aspire 로컬 오케스트레이션, 마이그레이션 적용 방식 · 리셋 정책, UUID v7, Command 트랜잭션 경계 · UoW | FR-01 | ADR 4건이 사용자 확인 후 `accepted`로 커밋된다. Aspire ADR에 Write / Read 매핑(Read에 `default_transaction_read_only`), DB 이름 `emergency_hub_employee` / 리소스 `employee-db` 분리, `employee_app` 롤, user-secrets 비밀번호. 트랜잭션 ADR에 실행 전략 안 트랜잭션 → SaveChanges → 커밋, Handler 저장 금지, Outbox 확장 지점. 마이그레이션 ADR에 MigrationService + `WaitForCompletion`, Phase 4 전 리셋 허용. ADR 목록 갱신 | T01 | todo | |
| S01-T03 | ADR (애플리케이션 구조): Mediator 직접 구현, Controller, Scrutor(0010 구체화), FluentValidation, Swashbuckle, 로깅(Serilog + OTLP) | FR-01 | ADR 6건이 사용자 확인 후 `accepted`로 커밋된다. Mediator ADR에 `ICommand : ICommand<Unit>`, Handler 타입 캐시, 파이프라인 순서(로깅 → 검증 → 트랜잭션 → Handler, 트랜잭션은 Command만). Scrutor ADR은 0010을 대체하지 않고 구체화함을 명시(0010 본문 불변). 로깅 ADR에 Serilog / OTel 로그 중복 방지 방식 | T01, T02 | todo | |
| S01-T04 | ADR (테스트 도구 · 도입 보류)과 tech-stack · 기준 문서 🟡 해소 | FR-01, FR-11, NFR-03 | AwesomeAssertions, Respawn · 커버리지(coverlet + ReportGenerator) ADR 2건과 도입 보류 ADR 1건(브로커, Outbox / Inbox, Gateway, 로그 수집기, 재검토 시점 포함)이 `accepted`로 커밋된다. FR-01 나열 항목의 🟡가 사라지고 남은 🟡는 처리 방식 표로 남는다. `09-memory/design.md` 확정 표 갱신 | T02, T03 | todo | |
| S01-T05 | 빌드 설정: sln, `global.json`, `Directory.Build.props` / `Directory.Packages.props`, `.editorconfig`, dotnet-ef 도구 매니페스트 | FR-02, NFR-01, NFR-06 | 설정 파일이 커밋되고(InternalsVisibleTo 일괄, BuildingBlocks만 CS1591 오류화, `Migrations/**` generated_code, dotnet-ef 8.0.x), 로컬 clone에서 `dotnet build` 경고 0 · 오류 0, `dotnet tool restore` 성공. `.gitignore`에 비밀 파일 제외 | T01~T04 | todo | |
| S01-T06 | BuildingBlocks.Domain과 단위 테스트 | FR-04, FR-09, NFR-01, NFR-03 | `Entity<TId>`, `AggregateRoot<TId>`(이벤트 수집 · 비우기까지), `IDomainEvent`, `Error` + 팩토리, `Result` / `Result<T>`(Error → Result&lt;T&gt; 변환), `IRepository` 구현. 단위 테스트가 성공 / 실패 / 엣지와 에러 코드 유형 자리 ↔ `ErrorType` 일치를 검증. Domain 프로젝트의 프레임워크 패키지 참조 0건 | T05 | todo | |
| S01-T07 | CI: GitHub Actions PR 워크플로 | FR-10, NFR-03, NFR-07 | `develop` · `main` 대상 PR 트리거로 restore → build → `dotnet format --verify-no-changes` → test → 커버리지 보고가 정의된다. 로컬에서 같은 순서가 모두 성공한다. 스프린트 종료 push 뒤 토픽 Draft PR에서 통과하고 소요 시간이 기록된다(DoD) | T05, T06 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

> ADR · 문서 작업은 dba가 DB 관련 내용이 있을 때만 검토하고(없으면 "해당 없음" PASS), tester는 테스트 코드 대신 명령으로 확인 가능한 점검표로 검증합니다.

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S01-T01 | EF Core / Npgsql / NamingConventions / dotnet-ef / PostgreSQL 이미지 정합 확인 | 조사 결과 기록(코드 없음) | 출처 링크, 버전 표기, FR-01① 누락 확인 | 모든 행에 버전 · 라이선스 · 출처, 스크래치 디렉터리에서 고정 버전으로 `dotnet restore` 성공 기록 |
| S01-T02 | 4건 모두 DB 관련, 내용 검토 | ADR 초안(파일은 사용자 확인 후 생성, 메모 N1) | 템플릿 · frontmatter · 링크, ADR 0005 / 0009 정합, 기존 ADR 불변 | PRD Q9 · Q10 · Q12 · Q13 결론과 본문 일치, `accepted`, 목록 링크 유효 |
| S01-T03 | 해당 없음(트랜잭션 데코레이터 순서가 T02와 맞는지만) | ADR 초안(N1) | 0003 / 0007 / 0010 정합, `git diff`로 0010 불변 확인 | Q5 · Q11 · Q14 결론과 일치, 6건 `accepted`, 링크 유효 |
| S01-T04 | Respawn 부분만(초기화 대상, `__EFMigrationsHistory` 제외) | ADR 초안(N1), tech-stack · 기준 문서 수정 | 템플릿, 문서 간 표기 일치, ADR 0004 유지 명시 | FR-01 항목 🟡 잔여 0건(grep 결과 첨부), 남은 🟡는 처리 방식 표에 있음 |
| S01-T05 | dotnet-ef와 EF Core 버전 일치 확인 | 설정 파일 작성 | 빌드 · format 성공, coding-conventions 부합 | 스크래치 clone에서 `dotnet tool restore` · `dotnet build` 경고 0 기록 (생성 코드 경고는 S03-T02에서 재검증) |
| S01-T06 | 해당 없음 | TDD | 표준 진입 점검(CS1591 포함) | 인수 조건 대조, `dotnet test`, PackageReference 0건 |
| S01-T07 | 해당 없음 | 워크플로 작성 | `global.json` 기반 setup-dotnet, 비밀 없음, 트리거 확인 | 같은 명령을 로컬에서 순서대로 실행해 기록. 실패 표시 확인은 S04-T04 |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S01` 태그를 붙였다
- [ ] 토픽 Draft PR에서 CI 워크플로가 통과하고 소요 시간을 기록했다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| | | | | |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모 (계획 리뷰에서 확정):

- **N1 ADR 확인 시점**: developer 단계가 ADR 초안을 반환 → 스킬이 사용자에게 보여 확인 → 확인된 내용으로 `accepted` 파일 생성 · developer 커밋 → reviewer → tester. `/sprint` 스킬에 이 확인 단계가 없으므로 운영 방식을 확정한다.
- **ADR 번호**: 13건(결정 12 + 보류 1)을 작성 순서대로 0011부터 매긴다.
- **작업량**: ADR 작업 3개가 무겁다. 과하면 T06을 S02로 옮길 수 있으나, CI(T07)는 FR-10 "첫 스프린트 앞부분" 조건 때문에 남긴다(이 경우 test 단계는 테스트 프로젝트가 없어도 통과하게 한다).
- **.NET 8 지원 종료(2026-11-10)**: T01에서 확인한 Aspire 9.x 지원 기간을 Aspire ADR에 적고, 필요하면 기술부채로 등록한다.

## 결과 리뷰

> 스프린트 종료 시 orchestrator의 결과 리뷰(계획 대비 실제, 완료 조건 · FR 충족, 반려 분석)를 요약합니다.

-

## 생긴 백로그 / 기술부채

| ID | 제목 | 발생 작업 | 정리 결과 |
|---|---|---|---|
| | | | open / planned:SNN / dropped |

## 회고

### 잘된 점

-

### 문제

-

### 다음에 바꿀 것

-

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-001 분할) |
