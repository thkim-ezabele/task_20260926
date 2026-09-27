---
title: "S01: 기술 결정 확정과 빌드 · CI 기반"
type: sprint
sprint: "S01"
status: active
prd: [PRD-001]
started: 2026-09-27
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
| S01-T01 | 사전 확인: Aspire 9.x 마이너 버전, 패키지 버전 정합, 라이선스 | FR-01, NFR-05 | Aspire 9.x 마이너 버전(net8.0 AppHost 지원, 패치 · 지원 기간과 .NET 8 지원 종료 관계, 최소 SDK 기능 밴드 → `global.json` 버전 · rollForward), PostgreSQL 호스팅의 `AddDatabase` 실제 DB 생성 · `WithCreationScript` 지원 여부, 클라이언트 통합의 net8.0 대상 EF Core 의존 버전(EF Core 9 전이 유입 시 미사용), EF Core · Npgsql · EFCore.NamingConventions · EF Design · dotnet-ef 8.0.x 패치 정합, PostgreSQL 이미지 메이저 태그(추천 17, Aspire · Testcontainers 양쪽 명시 · 한 곳 관리), 테스트 도구 10종(xUnit v2/v3, Test.Sdk, AwesomeAssertions, NSubstitute, Testcontainers.PostgreSql, Respawn, TimeProvider.Testing, NetArchTest.Rules, coverlet.collector, ReportGenerator), GitHub Actions 액션 고정 버전, 도입 패키지별 버전 · 라이선스 · 출처가 표로 기록된다. 상용 라이선스 0건. PRD Q19 해소 | - | doing | |
| S01-T02 | ADR (데이터 · 인프라): Aspire 로컬 오케스트레이션, 마이그레이션 적용 방식 · 리셋 정책, UUID v7, Command 트랜잭션 경계 · UoW | FR-01 | ADR 4건이 사용자 확인 후 `accepted`로 커밋된다. **Aspire**: Write / Read를 `ReferenceExpression`으로 조립(Read에 `Options=-c default_transaction_read_only=on`), DB 이름 `emergency_hub_employee` / 리소스 `employee-db` 분리, 롤 모델(`employee_app`이 DB · 스키마 소유자, MigrationService · Api 모두 `employee_app`, 슈퍼유저는 생성 스크립트만), GRANT · PG15+ public CREATE, user-secrets 비밀번호 매개변수, 클라이언트 통합 vs `AddDbContext`+`UseNpgsql(EnableRetryOnFailure)` 선택, 지원 기간. **트랜잭션**: 실행 전략 안에서 SaveChanges · 커밋만 재시도(`acceptAllChangesOnSuccess: false` → 커밋 뒤 `AcceptAllChanges`, Handler는 1회 실행), Read Committed, Handler 저장 금지, 23505 → Result 변환 위치(`ux_employees_email` 최종 방어), Outbox 확장 지점(커밋 전). **마이그레이션**: MigrationService는 `MigrateAsync`만(DB 생성은 AppHost) + `WaitForCompletion`, Phase 4 전 리셋 허용과 절차(폴더 삭제 → InitialCreate 재생성 → 볼륨 삭제) · 기록 방법 · 불변 규칙과의 관계, `__EFMigrationsHistory`는 snake_case 예외. **UUID**: UUIDNext API, 밀리초 내 단조성, Npgsql 바이트 순서 전제(S03-T05 검증), DB 기본값 · PG18 `uuidv7()` 미사용 이유. ADR 목록 갱신. 문서 점검 Node 스크립트(상대 링크, 앵커, frontmatter 필수 키) 추가 | T01 | todo | |
| S01-T03 | ADR (애플리케이션 구조): Mediator 직접 구현, Controller, Scrutor(0010 구체화), FluentValidation, Swashbuckle, 로깅(Serilog + OTLP) | FR-01 | ADR 6건이 사용자 확인 후 `accepted`로 커밋된다. Mediator ADR에 `ICommand : ICommand<Unit>`, Handler 타입 캐시, 파이프라인 순서(로깅 → 검증 → 트랜잭션 → Handler, 트랜잭션은 Command만). Scrutor ADR은 0010을 대체하지 않고 구체화함을 명시(0010 본문 불변). 로깅 ADR에 Serilog / OTel 로그 중복 방지 방식, `EnableSensitiveDataLogging`은 Development만 · SQL 파라미터 값 미기록 | T01, T02 | todo | |
| S01-T04 | ADR (테스트 도구 · 도입 보류)과 tech-stack · 기준 문서 🟡 해소, ADR 충돌 기준 문서 수정 | FR-01, FR-11, NFR-03 | AwesomeAssertions(xUnit 버전 · 주변 도구 고정 포함), Respawn(Npgsql adapter, `SchemasToInclude=public`, 제외 테이블, 쓰기 연결, TRUNCATE 권한) · 커버리지(coverlet + ReportGenerator) ADR 2건과 도입 보류 ADR 1건(브로커, Outbox / Inbox, Gateway, 로그 수집기, 재검토 시점 포함)이 `accepted`로 커밋된다. FR-01 나열 항목의 🟡가 사라지고(grep 대상: tech-stack, testing-strategy, coding-conventions, database, logging-observability, 09-memory/design) 남은 🟡는 처리 방식 표로 남는다. ADR과 직접 충돌하는 기준 문서를 수정한다: coding-conventions Handler SaveChanges 예시, tdd-guide Handler "저장" 표현, clean-architecture Endpoints · Minimal API → Controller, database.md UUID 🟡. 코드 이후 갱신분(저장소 구조, ERD)만 S04-T02에 남긴다. `09-memory/design.md` 확정 표 갱신 | T02, T03 | todo | |
| S01-T05 | 빌드 설정: sln, `global.json`, `Directory.Build.props` / `Directory.Packages.props`, `.editorconfig`, `.gitattributes`, 도구 매니페스트, 빈 골격 프로젝트 | FR-02, NFR-01, NFR-06 | 설정 파일과 빈 `BuildingBlocks.Domain` / `BuildingBlocks.Domain.UnitTests` 골격이 커밋된다. clean-architecture 공통 빌드 설정 표와 coding-conventions `.editorconfig` 표 항목 충족. CS1591 오류화 · InternalsVisibleTo는 `IsTestProject`가 아닌 프로젝트만(IVT 대상 `$(AssemblyName).UnitTests`, `DynamicProxyGenAssembly2`), `tests/**`는 CA1707 등 테스트 이름 예외, `Migrations/**` generated_code, `.gitattributes`(`* text=auto eol=lf`), 테스트 패키지 · coverlet.collector 중앙 등록, `CentralPackageTransitivePinningEnabled`, EF Design `PrivateAssets=all`, 도구 매니페스트(dotnet-ef 8.0.x = EF 런타임 패치, ReportGenerator). 스크래치 clone에서 `dotnet tool restore` 성공, `dotnet build` 경고 0 · 오류 0, `dotnet format --verify-no-changes` 성공, 부정 점검(경고 삽입 시 빌드 실패) 기록. `.gitignore` 비밀 파일 제외(`git check-ignore -v`). ADR 커밋이 T05보다 앞섬(`git log`, FR-01) | T01~T04 | todo | |
| S01-T06 | BuildingBlocks.Domain과 단위 테스트 | FR-04, FR-09, FR-11, NFR-01, NFR-03 | `Entity<TId>`(`TId : struct, IEquatable<TId>`, 같은 런타임 타입 + 같은 Id 동등성), `AggregateRoot<TId>`(`IReadOnlyCollection` 노출, protected `Raise`, public `ClearDomainEvents`), 마커 `IDomainEvent`, 마커 `IRepository` 정의, `Error`(non-sealed record, private 생성자 + 팩토리, 생성 시 예외: 범위 1001~99999, NNN=000, T ↔ `ErrorType` 불일치) + `ValidationError` 파생, 2자리 `ErrorType`(10 · 20 · 30 · 40 · 51 · 52 · 91 · 92 · 93, T = 값 / 10), `Result` / `Result<T>`(실패 시 `Value` 접근 `InvalidOperationException`, Error null `ArgumentException`, Error → Result&lt;T&gt; 암시적 변환). abstract 기반 클래스 · Error / Result는 sealed 예외로 문서화. 단위 테스트가 성공 / 실패 / 엣지와 공통 코드 전수 `[Theory]` 대조를 검증. FR-04 인수 테스트(참조 어셈블리 System.* / netstandard만). `dotnet list <Domain.csproj> package --include-transitive` 0건(`PrivateAssets=all` 분석기 제외), CS1591 부정 점검. error-codes.md ErrorType 표 갱신 | T05 | todo | |
| S01-T07 | CI: GitHub Actions PR 워크플로 | FR-10, NFR-03, NFR-07 | `develop` · `main` 대상 PR 트리거로 restore → build → `dotnet format --verify-no-changes` → test(`--collect "XPlat Code Coverage" --settings coverlet.runsettings`) → 커버리지 보고(도구 매니페스트 ReportGenerator, NFR-03 대상 어셈블리, 테스트 · Migrations · 생성 코드 제외)가 정의된다. `ubuntu-24.04`, `permissions: contents: read`, 액션 버전 고정, `setup-dotnet`은 `global-json-file`, trx · 리포트 아티팩트 업로드. 로컬에서 같은 순서가 모두 성공한다. PR 통과 · 소요 시간은 DoD로 판정(실패 시 T07 재작업, 통과 전 태그 보류) | T05, T06 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

> **문서 · 설정 작업 운영 (계획 리뷰 확정)**
>
> - **T01~T04**: dba는 DB 관련 내용이 있을 때만 검토합니다(없으면 "해당 없음" PASS). developer는 조사 기록 · ADR 초안 · 기준 문서 수정을 맡고 TDD · 빌드 전제는 적용하지 않습니다. reviewer는 진입 점검 15개를 "해당 없음"으로 두고 아래 표의 reviewer 열과 문서 규칙(템플릿, frontmatter 필수 키, 상대경로 링크, wikilink 금지, 기존 ADR 불변 `git diff`)으로 판정합니다. tester는 테스트 코드 대신 명령 기반 점검표(grep, 문서 점검 스크립트, PRD Q 결론 대조)로 검증하고 명령과 출력을 남깁니다.
> - **ADR 확인(N1)**: developer 1차 호출은 파일 없이 `adr_drafts`만 반환 → 스킬이 작업 단위로 사용자 확인 → developer 2차 호출이 `accepted` 파일 생성 → 커밋 → reviewer · tester는 커밋된 파일만 판정. 형식 반려는 새 커밋으로 고치고, 결정 내용이 바뀌는 반려는 developer 1차로 돌아가 다시 확인받습니다. push 전 토픽 브랜치 안의 수정은 ADR 불변 규칙 위반으로 보지 않습니다.
> - **T05**: reviewer는 빌드 설정 관련 항목(빌드 · 경고 0 · format · 두 기준 표)만 적용합니다. tester는 스크래치 clone에서 확인하고 부정 점검을 기록합니다.
> - **T06 · T07**: 표준 파이프라인. T06의 reviewer 아키텍처 테스트 항목은 "해당 없음"(S02-T05 전)이고, 프레임워크 비참조는 tester가 검증합니다.
> - ADR과 draft 기준 문서가 충돌하면 ADR을 따르고, 같은 스프린트 안에서 기준 문서를 고칩니다.

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S01-T01 | EF Core / Npgsql / NamingConventions / dotnet-ef / PostgreSQL 이미지 정합 확인 | 조사 결과 기록(코드 없음) | 출처 링크, 버전 표기, FR-01① 누락 확인 | 모든 행에 버전 · 라이선스 · 출처, 스크래치 디렉터리에서 고정 버전으로 `dotnet restore` 성공 기록 |
| S01-T02 | 4건 모두 DB 관련, 내용 검토 | ADR 초안(파일은 사용자 확인 후 생성, 메모 N1) | 템플릿 · frontmatter · 링크, ADR 0005 / 0009 정합, 기존 ADR 불변 | PRD Q9 · Q10 · Q12 · Q13 결론과 본문 일치, `accepted`, 목록 링크 유효 |
| S01-T03 | 트랜잭션 데코레이터 순서가 T02와 맞는지, 로깅 ADR의 EF 민감 데이터 로깅 금지 | ADR 초안(N1) | 0003 / 0007 / 0010 정합, `git diff`로 0010 불변 확인 | Q5 · Q11 · Q14 결론과 일치, 6건 `accepted`, 링크 유효 |
| S01-T04 | Respawn 부분만(초기화 대상, 마이그레이션 ADR에서 정한 이력 테이블 제외) | ADR 초안(N1), tech-stack · 기준 문서 수정 | 템플릿, 문서 간 표기 일치, ADR 0004 유지 명시 | FR-01 항목 🟡 잔여 0건(grep 결과 첨부), 남은 🟡는 처리 방식 표에 있음 |
| S01-T05 | dotnet-ef와 EF Core 버전 일치 확인 | 설정 파일 작성 | 빌드 · format 성공, coding-conventions 부합 | 스크래치 clone에서 `dotnet tool restore` · `dotnet build` 경고 0 기록 (생성 코드 경고는 S03-T02에서 재검증) |
| S01-T06 | 해당 없음 | TDD | 표준 진입 점검(CS1591 포함) | 인수 조건 대조, `dotnet test`, PackageReference 0건 |
| S01-T07 | 해당 없음 | 워크플로 작성 | `global.json` 기반 setup-dotnet, 비밀 없음, 트리거 확인 | 같은 명령을 로컬에서 순서대로 실행해 기록. 실패 표시 확인은 S04-T04 |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다 (S01은 단위만. 아키텍처는 S02-T05, 통합은 S03-T05부터)
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다 (S01은 해당 없음. ADR · 기준 문서는 T02~T04에서 갱신)
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S01` 태그를 붙였다
- [ ] 토픽 Draft PR에서 CI 워크플로가 통과하고 소요 시간을 기록했다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| 2026-09-27 | - | 계획 리뷰 | 승인 | 4개 에이전트 리뷰 + orchestrator 통합. 작업 7개 · 순서 · ADR 13건 유지, 완료 조건 보강, N1 · 문서 작업 운영 확정, Q1~Q5 추천안 승인 |
| 2026-09-27 | - | 계획 변경 | - | N1 반영: `/sprint` 스킬 · 에이전트 정의 · agents.md 수정을 토픽 브랜치에 `chore(agents)` 커밋 (토픽 밖 작업 규칙의 예외, Q5) |
| 2026-09-27 | S01-T01 | dba | BLOCKED | EF 계열 8.0.31 정합(Npgsql.EFCore 8.0.11, NamingConventions 8.0.3), Aspire `AddDatabase` 실제 생성 · `WithCreationScript`는 9.2.0+, 클라이언트 통합 net8.0에 EF9 유입 없음, PG 17 양쪽 명시 필수(Testcontainers 기본 생성자 Obsolete), 상용 라이선스 0건. 막힘: Aspire 9.x 전 버전 NuGet 폐기(out of support), 클라이언트 통합 9.5.2의 NU1902(OpenTelemetry.Api 1.9.0). Docker 꺼짐으로 생성 스크립트 동작 미실측 |
| 2026-09-27 | S01-T01 | dba | PASS | 사용자 결정: Aspire 9.5.2 유지(지원 종료는 Aspire ADR 명시 + TD-005, 재검토 BL-002). NU1902는 T02(BL-004), 생성 스크립트 실측은 BL-003으로 이관 후 재판정 |
| 2026-09-27 | S01-T01 | developer | PASS | `package-versions.md` 신규(패키지 · 라이선스 · 출처 표), PRD Q19 해소. Aspire 9.5 지원 종료 2025-11-11, global.json 8.0.400 + latestFeature 권장(로컬 SDK 8.0.202는 xunit.v3 빌드 실패 → 8.0.425 설치 필요), MessagePack 2.5.192 취약(AppHost 전이) → 2.5.305 고정 + `NuGetAuditMode=all`, OTel 1.19.x, 상용 0건. 스크래치(SDK 8.0.425)에서 restore · build 경고 0 · test 통과 |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

### 결과 (2026-09-27 승인)

- 리뷰: dba · developer · reviewer · tester 병렬 → orchestrator 통합. 작업 추가 · 삭제 · 순서 변경 없음, ADR 13건 유지. 작업별 완료 조건을 보강했다(위 작업 표).
- **N1 확정**: ADR 초안 확인 흐름(위 "문서 · 설정 작업 운영"). T06은 S01에 남긴다(옮기면 T07 커버리지 입력이 없음).
- **확정된 질문**
  - Q1 ErrorType: 2자리 값(Validation=10, NotFound=20, Conflict=30, BusinessRule=40, Unauthorized=51, Forbidden=52, Internal=91, External=92, Unavailable=93), T = 값 / 10. "HTTP 상태는 ErrorType으로 정한다" 유지
  - Q2 Error: non-sealed record + BuildingBlocks.Domain의 `ValidationError` 파생(필드별 상세)
  - Q3 트랜잭션 재시도: SaveChanges · 커밋만 재시도, Handler 1회 실행, Read Committed
  - Q4 로컬 DB 롤: `employee_app`이 DB · 스키마 소유자, MigrationService · Api 모두 `employee_app`, 슈퍼유저는 생성 스크립트만
  - Q5 N1 반영 커밋: 토픽 브랜치 `chore(agents)`
- **기본값**: `__EFMigrationsHistory`는 snake_case 예외, PostgreSQL 17 추천(T01 확인), xUnit 버전은 T04 확정, ReportGenerator는 도구 매니페스트, push 뒤 CI 실패 시 T07 재작업 · 태그 보류, Error 규칙은 팩토리 예외 + 전수 `[Theory]` 모두
- **위험**: ADR 13건 · 확인 3회가 T02~T04에 집중(high), Aspire 9.x 기능의 net8.0 동작 미확인(high, T01 필수 확인), .NET 8 지원 종료 2026-11-10(medium), ErrorType 계약 변경 시 S02-T06 연쇄(medium), 분석기 경고 오류화(medium), 줄바꿈 차이(low), CI 첫 실행이 스프린트 종료 뒤(low)
- **기록한 후보**: TD-001, TD-002, BL-001

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
| TD-001 | 마이그레이션 전용 DB 롤 분리 | 계획 리뷰(Q4) | |
| TD-002 | .NET 8 / Aspire 9.x 지원 종료에 따른 메이저 업그레이드 | 계획 리뷰 | |
| BL-001 | 문서 점검 Node 스크립트 CI 편입 검토 | 계획 리뷰 | |
| BL-002 | Aspire 9.x 지원 종료 대응(.NET 10 · Aspire 13 전환 재검토) | S01-T01 | |
| BL-003 | 롤 생성은 초기화 스크립트, `WithCreationScript`는 CREATE DATABASE 한 문장 + Docker 재시작 검증 | S01-T01 | |
| BL-004 | 클라이언트 통합 판단 근거에 NU1902 추가 | S01-T01 | |
| TD-003 | Npgsql.EFCore EF 의존 상한 없음 → 전이 고정 필수 | S01-T01 | |
| TD-004 | Testcontainers `PostgreSqlBuilder` 이미지 인자 필수(CS0618) | S01-T01 | |
| TD-005 | Aspire 9.x 지원 종료 상태로 9.5.2 사용 | S01-T01 | |
| TD-006 | 클라이언트 통합 사용 시 OpenTelemetry 계열 버전 혼재 | S01-T01 | |
| BL-005 | 로컬 SDK 8.0.425 설치(사용자 작업) | S01-T01 | |
| BL-006 | T05: `NuGetAuditMode=all`, MessagePack 2.5.305 고정, global.json 8.0.400 + latestFeature | S01-T01 | |
| BL-007 | T03 로깅 ADR: ServiceDefaults OTel 1.19.x 고정 | S01-T01 | |
| BL-008 | BL-004 판단 자료: OTel 1.19.x와 함께면 NU1902 · TD-006 해소 | S01-T01 | |
| BL-009 | ServiceDiscovery · Http.Resilience 사용 여부 결정 | S01-T01 | |
| TD-007 | MessagePack 2.5.305 수동 고정 유지(Aspire.Hosting 전이 취약) | S01-T01 | |
| TD-008 | net8.0 앱에 Microsoft.Extensions.* · DiagnosticSource 10.0.0 전이 유입 | S01-T01 | |
| TD-009 | xUnit v2 선택 시 Legacy 폐기 상태 | S01-T01 | |

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
| 2026-09-27 | - | 계획 리뷰 반영, `active` |
