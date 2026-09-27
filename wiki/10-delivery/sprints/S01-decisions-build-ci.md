---
title: "S01: 기술 결정 확정과 빌드 · CI 기반"
type: sprint
sprint: "S01"
status: done
prd: [PRD-001]
started: 2026-09-27
finished: 2026-09-27
adrs: [ADR-0011, ADR-0012, ADR-0013, ADR-0014, ADR-0015, ADR-0016, ADR-0017, ADR-0018, ADR-0019, ADR-0020, ADR-0021, ADR-0022, ADR-0023]
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
| S01-T01 | 사전 확인: Aspire 9.x 마이너 버전, 패키지 버전 정합, 라이선스 | FR-01, NFR-05 | Aspire 9.x 마이너 버전(net8.0 AppHost 지원, 패치 · 지원 기간과 .NET 8 지원 종료 관계, 최소 SDK 기능 밴드 → `global.json` 버전 · rollForward), PostgreSQL 호스팅의 `AddDatabase` 실제 DB 생성 · `WithCreationScript` 지원 여부, 클라이언트 통합의 net8.0 대상 EF Core 의존 버전(EF Core 9 전이 유입 시 미사용), EF Core · Npgsql · EFCore.NamingConventions · EF Design · dotnet-ef 8.0.x 패치 정합, PostgreSQL 이미지 메이저 태그(추천 17, Aspire · Testcontainers 양쪽 명시 · 한 곳 관리), 테스트 도구 10종(xUnit v2/v3, Test.Sdk, AwesomeAssertions, NSubstitute, Testcontainers.PostgreSql, Respawn, TimeProvider.Testing, NetArchTest.Rules, coverlet.collector, ReportGenerator), GitHub Actions 액션 고정 버전, 도입 패키지별 버전 · 라이선스 · 출처가 표로 기록된다. 상용 라이선스 0건. PRD Q19 해소 | - | done | 40332ef, 3b70c39, d98d079, 42a41b4, f770325 |
| S01-T02 | ADR (데이터 · 인프라): Aspire 로컬 오케스트레이션, 마이그레이션 적용 방식 · 리셋 정책, UUID v7, Command 트랜잭션 경계 · UoW | FR-01 | ADR 4건이 사용자 확인 후 `accepted`로 커밋된다. **Aspire**: Write / Read를 `ReferenceExpression`으로 조립(Read에 `Options=-c default_transaction_read_only=on`), DB 이름 `emergency_hub_employee` / 리소스 `employee-db` 분리, 롤 모델(`employee_app`이 DB · 스키마 소유자, MigrationService · Api 모두 `employee_app`, 슈퍼유저는 생성 스크립트만), GRANT · PG15+ public CREATE, user-secrets 비밀번호 매개변수, 클라이언트 통합 vs `AddDbContext`+`UseNpgsql(EnableRetryOnFailure)` 선택, 지원 기간. **트랜잭션**: 실행 전략 안에서 SaveChanges · 커밋만 재시도(`acceptAllChangesOnSuccess: false` → 커밋 뒤 `AcceptAllChanges`, Handler는 1회 실행), Read Committed, Handler 저장 금지, 23505 → Result 변환 위치(`ux_employees_email` 최종 방어), Outbox 확장 지점(커밋 전). **마이그레이션**: MigrationService는 `MigrateAsync`만(DB 생성은 AppHost) + `WaitForCompletion`, Phase 4 전 리셋 허용과 절차(폴더 삭제 → InitialCreate 재생성 → 볼륨 삭제) · 기록 방법 · 불변 규칙과의 관계, `__EFMigrationsHistory`는 snake_case 예외. **UUID**: UUIDNext API, 밀리초 내 단조성, Npgsql 바이트 순서 전제(S03-T05 검증), DB 기본값 · PG18 `uuidv7()` 미사용 이유. ADR 목록 갱신. 문서 점검 Node 스크립트(상대 링크, 앵커, frontmatter 필수 키) 추가 | T01 | done | 5d7196e, 608f828, 5590fd6, 34349d1 |
| S01-T03 | ADR (애플리케이션 구조): Mediator 직접 구현, Controller, Scrutor(0010 구체화), FluentValidation, Swashbuckle, 로깅(Serilog + OTLP) | FR-01 | ADR 6건이 사용자 확인 후 `accepted`로 커밋된다. Mediator ADR에 `ICommand : ICommand<Unit>`, Handler 타입 캐시, 파이프라인 순서(로깅 → 검증 → 트랜잭션 → Handler, 트랜잭션은 Command만). Scrutor ADR은 0010을 대체하지 않고 구체화함을 명시(0010 본문 불변). 로깅 ADR에 Serilog / OTel 로그 중복 방지 방식, `EnableSensitiveDataLogging`은 Development만 · SQL 파라미터 값 미기록 | T01, T02 | done | 8db56e7, 3f0e048, 519718c, 35e2f20 |
| S01-T04 | ADR (테스트 도구 · 도입 보류)과 tech-stack · 기준 문서 🟡 해소, ADR 충돌 기준 문서 수정 | FR-01, FR-11, NFR-03 | AwesomeAssertions(xUnit 버전 · 주변 도구 고정 포함), Respawn(Npgsql adapter, `SchemasToInclude=public`, 제외 테이블, 쓰기 연결, TRUNCATE 권한) · 커버리지(coverlet + ReportGenerator) ADR 2건과 도입 보류 ADR 1건(브로커, Outbox / Inbox, Gateway, 로그 수집기, 재검토 시점 포함)이 `accepted`로 커밋된다. FR-01 나열 항목의 🟡가 사라지고(grep 대상: tech-stack, testing-strategy, coding-conventions, database, logging-observability, 09-memory/design) 남은 🟡는 처리 방식 표로 남는다. ADR과 직접 충돌하는 기준 문서를 수정한다: coding-conventions Handler SaveChanges 예시, tdd-guide Handler "저장" 표현, clean-architecture Endpoints · Minimal API → Controller, database.md UUID 🟡. 코드 이후 갱신분(저장소 구조, ERD)만 S04-T02에 남긴다. `09-memory/design.md` 확정 표 갱신 | T02, T03 | done | 297a099, 5b94975, 94beaaa, 26b2ce4, 21c7f12, 24ee26b |
| S01-T05 | 빌드 설정: sln, `global.json`, `Directory.Build.props` / `Directory.Packages.props`, `.editorconfig`, `.gitattributes`, 도구 매니페스트, 빈 골격 프로젝트 | FR-02, NFR-01, NFR-06 | 설정 파일과 빈 `BuildingBlocks.Domain` / `BuildingBlocks.Domain.UnitTests` 골격이 커밋된다. clean-architecture 공통 빌드 설정 표와 coding-conventions `.editorconfig` 표 항목 충족. CS1591 오류화 · InternalsVisibleTo는 `IsTestProject`가 아닌 프로젝트만(IVT 대상 `$(AssemblyName).UnitTests`, `DynamicProxyGenAssembly2`), `tests/**`는 CA1707 등 테스트 이름 예외, `Migrations/**` generated_code, `.gitattributes`(`* text=auto eol=lf`), 테스트 패키지 · coverlet.collector 중앙 등록, `CentralPackageTransitivePinningEnabled`, EF Design `PrivateAssets=all`, 도구 매니페스트(dotnet-ef 8.0.x = EF 런타임 패치, ReportGenerator). 스크래치 clone에서 `dotnet tool restore` 성공, `dotnet build` 경고 0 · 오류 0, `dotnet format --verify-no-changes` 성공, 부정 점검(경고 삽입 시 빌드 실패) 기록. `.gitignore` 비밀 파일 제외(`git check-ignore -v`). ADR 커밋이 T05보다 앞섬(`git log`, FR-01) | T01~T04 | done | 1444f1f, 0f0a54a, 8b4c0dd, 22b15cf |
| S01-T06 | BuildingBlocks.Domain과 단위 테스트 | FR-04, FR-09, FR-11, NFR-01, NFR-03 | `Entity<TId>`(`TId : struct, IEquatable<TId>`, 같은 런타임 타입 + 같은 Id 동등성), `AggregateRoot<TId>`(`IReadOnlyCollection` 노출, protected `Raise`, public `ClearDomainEvents`), 마커 `IDomainEvent`, 마커 `IRepository` 정의, `Error`(non-sealed record, private 생성자 + 팩토리, 생성 시 예외: 범위 1001~99999, NNN=000, T ↔ `ErrorType` 불일치) + `ValidationError` 파생, 2자리 `ErrorType`(10 · 20 · 30 · 40 · 51 · 52 · 91 · 92 · 93, T = 값 / 10), `Result` / `Result<T>`(실패 시 `Value` 접근 `InvalidOperationException`, Error null `ArgumentException`, Error → Result&lt;T&gt; 암시적 변환). abstract 기반 클래스 · Error / Result는 sealed 예외로 문서화. 단위 테스트가 성공 / 실패 / 엣지와 공통 코드 전수 `[Theory]` 대조를 검증. FR-04 인수 테스트(참조 어셈블리 System.* / netstandard만). `dotnet list <Domain.csproj> package --include-transitive` 0건(`PrivateAssets=all` 분석기 제외), CS1591 부정 점검. error-codes.md ErrorType 표 갱신 | T05 | done | 7ba9121, df00b97, b7f2d2b, f5b9187 |
| S01-T07 | CI: GitHub Actions PR 워크플로 | FR-10, NFR-03, NFR-07 | `develop` · `main` 대상 PR 트리거로 restore → build → `dotnet format --verify-no-changes` → test(`--collect "XPlat Code Coverage" --settings coverlet.runsettings`) → 커버리지 보고(도구 매니페스트 ReportGenerator, NFR-03 대상 어셈블리, 테스트 · Migrations · 생성 코드 제외)가 정의된다. `ubuntu-24.04`, `permissions: contents: read`, 액션 버전 고정, `setup-dotnet`은 `global-json-file`, trx · 리포트 아티팩트 업로드. 로컬에서 같은 순서가 모두 성공한다. PR 통과 · 소요 시간은 DoD로 판정(실패 시 T07 재작업, 통과 전 태그 보류) | T05, T06 | done | b5f8d3e, 0335d14, b8b155a, 0169713 |

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

- [x] 모든 작업이 `done`이거나 백로그로 이관되었다
- [x] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다 (S01은 단위만. 아키텍처는 S02-T05, 통합은 S03-T05부터)
- [x] 관련 위키 문서(API, 이벤트, DB)를 갱신했다 (S01은 해당 없음. ADR · 기준 문서는 T02~T04에서 갱신)
- [x] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
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
| 2026-09-27 | S01-T01 | reviewer | PASS | 코드 점검 15개 해당 없음. 출처 링크 · 버전 표기 · FR-01① · 완료 조건 전 항목 · frontmatter · 상대 링크 · ADR 불변 확인 |
| 2026-09-27 | S01-T01 | tester | PASS | 표 6개 46행 버전 · 라이선스 · 출처 누락 0(Node 검사), 스크래치 net8.0 sln(고정 39개, CPM · 전이 고정 · `NuGetAuditMode=all` · global.json 8.0.400 latestFeature, SDK 8.0.425) `dotnet restore` 경고 0, 해석 버전 불일치 0, MessagePack 고정 제거 대조군 NU1902 9 · NU1903 2, 취약 패키지 0, Q19 링크 · 앵커 유효, 상용 0건 |
| 2026-09-27 | S01-T02 | dba | PASS | ADR 4건 DB 입력 확정(Aspire 9.5.2 소스 확인): `WithReference(db)` 금지(슈퍼유저 자격 증명 주입) → Write / Read `ReferenceExpression` + `WithEnvironment`, 롤은 `WithInitFiles` init 스크립트 · 생성 스크립트는 CREATE DATABASE 한 문장, 클라이언트 통합 미사용 추천(풀 강제 · DI 인터셉터 불가), UUIDNext 단조성 · 바이트 순서 확인. Docker 꺼짐으로 BL-003 실측은 S03 이관 |
| 2026-09-27 | S01-T02 | developer 1차 | PASS | ADR 0011~0014 초안(`adr_drafts`), `scripts/check-docs.js` 작성(wiki/ 전체 결함 2건: raw-log frontmatter, 기존) |
| 2026-09-27 | S01-T02 | 사용자 확인 | 승인 | 초안 4건 수정 없음. 클라이언트 통합 미사용, ServiceDiscovery · Http.Resilience 0011 포함, 23514 미변환, 리셋 기록 footer 없음, Q12 롤 생성은 init 스크립트 + 생성 스크립트로 구체화 |
| 2026-09-27 | S01-T02 | developer 2차 | PASS | ADR 0011~0014 `accepted` 생성(초안과 diff 동일), ADR 목록 · design.md · PRD Q9/Q10/Q12/Q13 링크 갱신, 점검 결함 0 |
| 2026-09-27 | S01-T02 | reviewer | PASS | 템플릿 · frontmatter · 링크(check-docs 결함 0), ADR 0005 / 0009 정합, 0001~0010 불변, PRD Q 결론 일치, 완료 조건 필수 항목 전부 확인 |
| 2026-09-27 | S01-T02 | tester | PASS | Q9 · Q10 · Q12(승인된 구체화) · Q13 결론과 본문 grep 대조 일치, 0011~0014 accepted, check-docs 17개 결함 0, 스크립트 부정 점검 6종 exit 1 · 정상 exit 0, 완료 조건 필수 항목 전부 존재, 0001~0010 불변 |
| 2026-09-27 | S01-T03 | dba | PASS | Mediator 순서가 ADR-0014와 일치, Mediator ADR 필수 문장 8건 · 로깅 ADR DB 항목(EnableSensitiveDataLogging은 Development opt-in, 파라미터 값 미기록, EF 로그 수준, Include Error Detail · Persist Security Info 금지, Npgsql 추적 태그에 값 없음) 확정. NuGet 캐시 XML · DLL로 확인 |
| 2026-09-27 | S01-T03 | developer 1차 | PASS | ADR 0015~0020 초안 6건(`adr_drafts`), check-docs 결함 0 |
| 2026-09-27 | S01-T03 | 사용자 확인 | 승인 | 초안 6건 수정 없음, 결정 7건 추천안(Serilog OTLP 단일 경로, Swashbuckle, 직접 구현 + Scrutor Decorate, 로깅 데코레이터 수준, `WithError`, `SuppressImplicitRequired...` = true, 민감 데이터 로깅 Development opt-in) |
| 2026-09-27 | S01-T03 | developer 2차 | PASS | ADR 0015~0020 `accepted` 생성(초안과 diff 동일), ADR 목록 · design.md · PRD Q5/Q11/Q14 링크, check-docs 23개 결함 0, 0001~0014 불변 |
| 2026-09-27 | S01-T03 | reviewer | PASS | 템플릿 · frontmatter · 링크 결함 0, 0010 불변, 0003 / 0007 / 0010 / 0014 정합, 완료 조건 · dba 필수 문장 전부 반영, PRD Q5 · Q11 · Q14 일치 |
| 2026-09-27 | S01-T03 | tester | PASS | Q5 · Q11 · Q14 결론과 결정 문장 grep 대조 일치, 6건 accepted, 목록 6행 일치, check-docs 23개 결함 0, 필수 문장 7종 존재, 0001~0014 불변 |
| 2026-09-27 | S01-T04 | dba | PASS | Respawn 7.0.0 API 소스 확인, ADR 입력 확정(`TablesToIgnore`는 따옴표 없이 `public.__EFMigrationsHistory`, 쓰기 연결, employee_app 재현 추천, 테스트 전 ResetAsync, WithReseed false). database.md 12곳 · testing-strategy.md 2곳 수정안. Docker 꺼짐으로 실측은 S03-T05 |
| 2026-09-27 | S01-T04 | developer 1차 | PASS | ADR 0021~0023 초안, 기준 문서 10개 수정(🟡 대상 6개 0건, ADR 충돌 수정, package-versions BL-010 · 011 · 013 · 016 · 026, PRD Q9 주석). Aspire.Dashboard.Sdk는 Microsoft 사용 조건(무료) |
| 2026-09-27 | S01-T04 | 사용자 확인 | 승인 | 초안 3건 수정 없음. xUnit v3, 보류 재검토 트리거 그대로, Respawn · 커버리지 한 ADR(총 13건), NFR-03 BuildingBlocks = Domain · Application |
| 2026-09-27 | S01-T04 | developer 2차 | PASS | ADR 0021~0023 `accepted`(초안과 diff 동일), ADR 목록 · design.md · PRD 링크, check-docs wiki 전체 결함은 raw-log 2건(BL-018)뿐, 0001~0020 불변 |
| 2026-09-27 | S01-T04 | reviewer | REJECT → developer | [형식] 반려 1회. ① package-versions.md:176 설명 문단이 테스트 표 중간에 들어가 coverlet.collector 행이 표에서 떨어짐 ② package-versions.md:207 · 254 CI 작업 번호 S01-T06 → S01-T07(tech-stack · ADR과 불일치). 나머지(ADR 형식 · 불변, 🟡 0, ADR 충돌 수정, 0004 유지, 규칙 삭제 없음) 통과 |
| 2026-09-27 | S01-T04 | developer 2차(재작업) | PASS | 반려 사유 해결: 설명 문단을 테스트 표 뒤로 이동, CI 작업 번호 S01-T07로 정정(2곳), BL-041 용도 열 채택/미사용 반영. check-docs 결함 0 |
| 2026-09-27 | S01-T04 | reviewer(재판정) | PASS | 반려 사유 2건 해결 확인, 5b94975 이후 변경은 package-versions · backlog · 스프린트 문서뿐이라 이전 통과 항목 재사용, check-docs 결함은 BL-018뿐 |
| 2026-09-27 | S01-T04 | tester | PASS | 대상 6개 🟡 0건(Node 검색), wiki 전체 🟡 대조: FR-01 항목이 처리 방식 표 · BL-038 밖에 남은 곳 0건, 0021~0023 accepted, 0001~0020 불변, check-docs 결함은 BL-018뿐, 버전 5종 문서 간 일치, ADR 충돌 수정 확인, design.md 확정 표 0011~0023 13건 |
| 2026-09-27 | S01-T05 | dba | PASS | DB 패키지 · 전이 고정 4건 확정(EF · Relational 8.0.31, OpenTelemetry.Api 1.19.1 — 빼면 NU1902 실측, MessagePack 2.5.305), dotnet-ef 8.0.31 = EF 런타임. `generated_code`만으로는 CS1591이 안 꺼짐(실측) → Migrations 섹션에 `CS1591.severity = none` 필요 |
| 2026-09-27 | S01-T05 | developer | PASS | sln · global.json(8.0.400 latestFeature) · Directory.Build.props/targets · Directory.Packages.props(전이 고정 4건) · .editorconfig · .gitattributes · 도구 매니페스트 · .gitignore 보강, 빈 골격 2개. build 경고 0 · test 1/1 · format · 취약 0. 실측 수정: WarningsAsErrors에 CS1591을 넣으면 editorconfig none보다 우선 → 제외, `옵션:error`는 빌드 미적용 → `dotnet_diagnostic` 심각도로 강제. renormalize 불필요(LF 102/105). BL-047 문서 반영 |
| 2026-09-27 | S01-T05 | reviewer | PASS | 직접 build 경고 0 · format 0 · test 1/1, 공통 빌드 설정 표 · .editorconfig 표 일치, CS1591 · IVT 범위(msbuild 평가값), 전이 고정 · PrivateAssets · 도구 매니페스트 · .gitignore 비밀 파일, ADR 커밋 선행, 버전 표본 20종 일치, 취약 0 |
| 2026-09-27 | S01-T05 | tester | PASS | 짧은 경로 clone(8b4c0dd): SDK 8.0.425 선택, tool restore · ef 8.0.31, build 경고 0 · test 1/1 · format · 취약 0. 부정 점검 5종 기대대로(CS0219 · CS1591 오류, Migrations · 테스트는 무오류, IDE0161 · IDE0011 오류), IVT는 Domain만, check-ignore 비밀 파일 제외, ADR 커밋 선행, i/crlf 0 |
| 2026-09-27 | S01-T06 | dba | PASS | 해당 없음(DB 변경 없음). EF 매핑 주의점 6건 참고로 전달(강타입 ID 값 변환, DomainEvents Ignore, Id 기본값 동등성, 바인딩 생성자, Error 비저장, 동시성 토큰은 매핑 계층) |
| 2026-09-27 | S01-T06 | developer | PASS | TDD(Red 확인 후 구현). Entity · AggregateRoot · IDomainEvent · IRepository · ErrorType(2자리, None=0 예약) · Error(get 전용, private protected 생성자 + 팩토리 9개, 생성 시 검증) · ValidationError · FieldError · CommonErrors(11) · Result / Result<T>(Error.None 없음, 암시적 변환 3종). 테스트 197/197, 경고 0, format 통과, Domain 패키지 0건, 라인 98.2% · 분기 100%. error-codes.md ErrorType 표 갱신. BL-040 · BL-050 해결 |
| 2026-09-27 | S01-T06 | reviewer | PASS | 점검 15개 통과(#4 해당 없음). 직접 build 경고 0 · test 197/197 · format 0, Domain 패키지 0건. developer 판단 사항(Error.None 없음, private protected, None=0 예약 등) 기준 문서 · Q1/Q2와 충돌 없음, CA1716 억제는 규칙 부재로 사유 명시 조건 허용 |
| 2026-09-27 | S01-T06 | tester | PASS | FR-04 인수 테스트 5건 추가(참조 어셈블리 System.* 뿐, Domain 전체 Error T ↔ ErrorType, FR-04 타입 · 마커, AggregateRoot 수집만, 시나리오). test 202/202, 경고 0, format 0, Domain 패키지 0건, CS1591 부정 점검 오류 확인, 커버리지 라인 98.22% · 분기 100%(보고만), error-codes.md 대조 일치. coverlet.runsettings는 T07 완료 조건으로 처리 |
| 2026-09-27 | S01-T07 | dba | PASS | 해당 없음(DB 변경 없음). S03 CI 통합 테스트 대비 주의점 기록(BL-057) |
| 2026-09-27 | S01-T07 | developer | PASS | `.github/workflows/ci.yml`(develop · main PR, Draft 포함, ubuntu-24.04, contents: read, 액션 SHA 고정, restore → build → format → test(커버리지) → ReportGenerator → 아티팩트), `coverlet.runsettings`. 로컬 Windows · Linux 컨테이너에서 같은 순서 성공(test 202, 커버리지 98.1%), actionlint 오류 0. 실측 수정 2건(runsettings 주석 `--`, cobertura 중복 합산 경로). 관련 문서 4개 갱신 |
| 2026-09-27 | S01-T07 | reviewer | PASS | 직접 build 경고 0 · format · test 202, 트리거(develop · main, Draft 포함), 권한 contents: read · 비밀 0, global-json-file, 액션 SHA 3개 gh api 조회 일치, runsettings · ADR-0021/0022 · testing-strategy 정합, check-docs BL-018 외 0 |
| 2026-09-27 | S01-T07 | tester | PASS | 새 clone(b8b155a)에서 ci.yml run 단계 순서대로: Windows 7단계 rc=0 합계 30초, Linux sdk:8.0 합계 52초(로컬 참고치). test 202, 보고서 생성 · 대상 어셈블리만 · 중복 합산 없음(라인 98.1% · 분기 100%), actionlint 0, SHA 일치 |
| 2026-09-27 | - | 결과 리뷰 | 승인 | orchestrator 결과 리뷰 · 정리안(BL 64 · TD 18, new 0) · 회고 초안 승인. DoD: build 경고 0 · test 202/202 · format · check-docs(BL-018 외 0). push · 태그 · PR CI는 승인 후 진행 |

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

### 계획 대비 실제

- 작업 7개 모두 `done`. 이관 · 추가 · 삭제 · 순서 변경 없음. 계획 확정(ef1b3df) 이후 커밋 39개(N1 반영 `chore(agents)` 포함).
- ADR 13건(0011~0023) 모두 사용자 확인 후 `accepted`. 확인 3회 모두 초안 수정 없이 승인.
- 테스트 202/202, 빌드 경고 0, format 통과, BuildingBlocks.Domain 라인 98.1% · 분기 100%. CI 워크플로 로컬 재현 Windows 30초 · Linux 컨테이너 52초.
- 특이점: ① PRD 전제(Aspire 9.x 지원 중)가 틀림 — 9.x 전 버전 NuGet 폐기, 사용자 결정으로 9.5.2 유지(ADR-0011, TD-005, BL-002). ② Docker가 꺼져 있어 DB 실측을 S03으로 이관(BL-014 · 024 · 033). ③ 로컬 SDK 8.0.202로 xunit.v3 빌드 불가 → 스프린트 중 8.0.425 설치(BL-005). ④ 실측으로 설정 수정 4건(WarningsAsErrors의 CS1591 우선, generated_code의 CS1591 한계, runsettings 주석 `--`, cobertura 중복 합산 경로). ⑤ 후보 폭증(BL 64, TD 18) — 약 22건은 같은 스프린트 인계 메모.

### 요구사항 충족

| 요구사항 | 상태 | 근거 |
|---|---|---|
| FR-01 | 충족 | ADR 0011~0023 accepted(608f828 · 3f0e048 · 5b94975), package-versions.md, PRD Q19 해소, 대상 6개 문서 🟡 0건, ADR 커밋이 구현보다 앞섬 |
| FR-02 | 부분 | 빌드 설정 일체(0f0a54a), 새 clone 경고 0. 생성 코드 포함 재검증은 S03-T02(BL-045) |
| FR-04 | 충족 | Domain 기반 타입(df00b97), FR-04 인수 테스트 5건, Domain 패키지 참조 0건 |
| FR-09 | 부분 | Domain 단위 테스트만. Application · Infrastructure · 아키텍처 · 통합은 S02 · S03 |
| FR-10 | 부분 | ci.yml · coverlet.runsettings(0335d14), 로컬 전 단계 성공. PR 통과 · 소요 시간은 DoD, 실패 표시는 S04-T04 |
| FR-11 | 부분 | 기준 문서 7종 갱신(T04 · T05 · T06). 저장소 구조 · ERD · todo 문서는 S04 |
| NFR-01 | 충족 | 경고 0, 부정 점검 5종 기대대로 |
| NFR-03 | 부분 | 측정 · 보고 경로 정의, Domain 98.1%. Application · Employee는 이후 |
| NFR-05 | 충족 | 46행 버전 · 라이선스 · 출처, 상용 0건(Aspire.Dashboard.Sdk는 Microsoft 사용 조건, 무료) |
| NFR-06 | 부분 | .gitignore 비밀 제외, ci.yml 비밀 0. 실제 비밀은 S03-T04부터 |
| NFR-07 | 부분 | 로컬 참고치 30초 / 52초. 러너 실측은 DoD, Testcontainers 뒤 재측정 |

### 반려 분석

- reviewer 반려 1회(S01-T04, 형식): package-versions.md 표 중간 문단으로 행 이탈, CI 작업 번호 불일치. check-docs가 표 구조를 점검하지 않음. 형식 반려라 developer 2차 새 커밋(26b2ce4)으로 해결.
- dba BLOCKED 1회(S01-T01, 외부 제약): Aspire 9.x 폐기 · 클라이언트 통합 NU1902. 사용자 결정 후 재판정 PASS(40332ef → 3b70c39).

### S02에 주는 영향

- S02-T06 완료 조건 수정: "공통 에러 코드 할당" → "CommonErrors(11개) 재사용 + 매핑 없는 23505용 공통 Conflict 코드 추가"(BL-019 · 052).
- S02-T04 완료 조건 누락: ADR-0014가 배정한 23505 → Result 변환 추가. TD-015(DomainEvents Ignore), BL-023(EF 오류 로그 수준) 함께 검토.
- S02-T02: BL-053, BL-028. S02-T05: TD-013, BL-029, TD-014. S02-T01: BL-039, TD-017.
- 새 테스트 프로젝트 이름은 `*Tests`(TD-014), 커버리지 대상 포함 확인(BL-063). 새 패키지 전이 취약점이 NuGetAudit로 빌드를 막을 수 있음(TD-008 관찰).
- S02 계획 리뷰에서 N3(공통 API 처리 위치) 확정. BL-055(경고 억제 규칙)를 S02 시작 전 reviewer 기준으로 합의 권장. S03 대비 Docker 실행 환경 확보.

### ADR 후보 (사용자 확인 전, 목록만)

- GitHub Actions 액션 커밋 SHA 고정 + 버전 주석 정책(BL-058 Dependabot 결정 포함)
- 테스트 프로젝트 판별 이름 규칙(IsTestProject = `*Tests`)과 IVT · 문서 생성 · CS1591 범위(TD-014)
- 패키지 공급망 정책: CentralPackageTransitivePinning, NuGetAuditMode=all · Level=low, 취약 전이 의존 수동 고정 원칙
- SDK 고정 정책: global.json 8.0.400 + latestFeature(하한 근거 xunit.v3)
- ErrorType 2자리 값 체계, Error 생성 시 검증, Error.None 없음, Result 암시적 변환(ADR-0008 구체화)
- (하) 분석기 경고 강제 · 억제 규칙(BL-055) — coding-conventions로 충분한지 먼저 판단
- (후속) .NET 10 · Aspire 13 전환(ADR-0001 대체, BL-002)

## 생긴 백로그 / 기술부채

| ID | 제목 | 발생 작업 | 정리 결과 |
|---|---|---|---|
| TD-001 | 마이그레이션 전용 DB 롤 분리 | 계획 리뷰(Q4) | open |
| TD-002 | .NET 8 / Aspire 9.x 지원 종료에 따른 메이저 업그레이드 | 계획 리뷰 | open |
| BL-001 | 문서 점검 Node 스크립트 CI 편입 검토 | 계획 리뷰 | open |
| BL-002 | Aspire 9.x 지원 종료 대응(.NET 10 · Aspire 13 전환 재검토) | S01-T01 | open |
| BL-003 | 롤 생성은 초기화 스크립트, `WithCreationScript`는 CREATE DATABASE 한 문장 + Docker 재시작 검증 | S01-T01 | dropped → BL-014 |
| BL-004 | 클라이언트 통합 판단 근거에 NU1902 추가 | S01-T01 | done |
| TD-003 | Npgsql.EFCore EF 의존 상한 없음 → 전이 고정 필수 | S01-T01 | open |
| TD-004 | Testcontainers `PostgreSqlBuilder` 이미지 인자 필수(CS0618) | S01-T01 | planned:S03 |
| TD-005 | Aspire 9.x 지원 종료 상태로 9.5.2 사용 | S01-T01 | open → TD-002 |
| TD-006 | 클라이언트 통합 사용 시 OpenTelemetry 계열 버전 혼재 | S01-T01 | resolved |
| BL-005 | 로컬 SDK 8.0.425 설치(사용자 작업) | S01-T01 | done |
| BL-006 | T05: `NuGetAuditMode=all`, MessagePack 2.5.305 고정, global.json 8.0.400 + latestFeature | S01-T01 | done |
| BL-007 | T03 로깅 ADR: ServiceDefaults OTel 1.19.x 고정 | S01-T01 | done |
| BL-008 | BL-004 판단 자료: OTel 1.19.x와 함께면 NU1902 · TD-006 해소 | S01-T01 | done |
| BL-009 | ServiceDiscovery · Http.Resilience 사용 여부 결정 | S01-T01 | done |
| TD-007 | MessagePack 2.5.305 수동 고정 유지(Aspire.Hosting 전이 취약) | S01-T01 | open |
| TD-008 | net8.0 앱에 Microsoft.Extensions.* · DiagnosticSource 10.0.0 전이 유입 | S01-T01 | open |
| TD-009 | xUnit v2 선택 시 Legacy 폐기 상태 | S01-T01 | resolved |
| BL-010 | 클라이언트 통합의 net8.0 EF Core 의존 버전 · 하한 명시(T02) | S01-T01 | done |
| BL-011 | PostgreSQL 이미지 · .NET SDK 라이선스 행 추가 검토 | S01-T01 | done |
| BL-012 | Aspire 리소스 이름 밑줄 불가(ASPIRE006): `AddDatabase("employee-db", databaseName: ...)` | S01-T01 | done |
| BL-013 | 클라이언트 통합 · Dashboard.Sdk · Orchestration 폐기 표시 문서 반영 | S01-T01 | done |
| BL-014 | BL-003 실측을 S03 Aspire 작업 완료 조건에 편입 | S01-T02 | planned:S03 |
| BL-015 | 서비스 DB 2개 이상 시 REVOKE CONNECT, TEMPORARY FROM PUBLIC | S01-T02 | open |
| BL-016 | package-versions.md: WithInitFiles 표기, Npgsql.OpenTelemetry · HealthChecks.EFCore 행 | S01-T02 | done |
| BL-017 | local-setup / troubleshooting: MigrationService Waiting 멈춤 대응 | S01-T02 | planned:S04 |
| TD-010 | 커밋 결과 불명 시 재시도 오판(23505 · xmin) | S01-T02 | open |
| TD-011 | EF Core 8 MigrateAsync 잠금 없음 → 적용 주체 1개 | S01-T02 | open |
| BL-018 | raw-log frontmatter created · updated 누락(hook 템플릿 vs 예외) | S01-T02 | open |
| BL-019 | 매핑 없는 23505용 공통 Conflict 코드 할당 | S01-T02 | planned:S02 |
| BL-020 | check-docs.js 유형별 추가 필드 점검 검토 | S01-T02 | dropped → BL-001 |
| BL-021 | check-docs.js 공백 포함 링크 대상 형식 미점검 | S01-T02 | dropped → BL-001 |
| BL-022 | database.md "기본값(Read Committed)" → "명시" 문구 정리(T04) | S01-T02 | done |
| BL-023 | 23505 정상 경합 시 EF가 Error 로그 2건 기록 → 수준 조정 여부 | S01-T03 | open |
| BL-024 | S03-T05: 추적 db.connection_string 비밀번호 · 파라미터 값 미노출 실측 | S01-T03 | open |
| BL-025 | coding-conventions Command 반환 `Result<Unit>`(T04) | S01-T03 | done |
| BL-026 | package-versions에 Serilog.Sinks.Async · Enrichers.Environment 행(T04) | S01-T03 | done |
| BL-027 | logging-observability에 ADR-0020 반영(T04) | S01-T03 | done |
| BL-028 | Mediator 로깅 이벤트 ID 할당(S02-T02) | S01-T03 | planned:S02 |
| BL-029 | 아키텍처 테스트 후보: Handler ISender 금지, Validator Repository 금지(S02-T05) | S01-T03 | open |
| BL-030 | 헬스체크 경로 정리(S03-T04) | S01-T03 | planned:S03 |
| TD-012 | ServiceDefaults가 Aspire 템플릿과 달라짐 | S01-T03 | open |
| BL-031 | PRD Q9 결론에 "구체화: ADR-0014 · 0015" 주석 검토(T04) | S01-T03 | done |
| BL-032 | clean-architecture `Endpoints/` · Minimal API → `Controllers/` 수정(T04) | S01-T03 | done |
| BL-033 | S03-T05: fixture employee_app 재현, Respawn 이력 보존 · TRUNCATE 권한 · 25006 실측 | S01-T04 | planned:S03 |
| BL-034 | HasData 기준 데이터 → Respawn TablesToIgnore 규칙 | S01-T04 | open |
| BL-035 | 순환 FK 금지(Respawn DISABLE TRIGGER 슈퍼유저 필요) 스키마 리뷰 항목 | S01-T04 | open |
| BL-036 | S03-T02: 이력 테이블 이름이 TablesToIgnore와 일치하는지 확인 | S01-T04 | planned:S03 |
| BL-037 | T05: CPM에 OpenTelemetry.Api 1.19.1 전이 고정 | S01-T04 | done |
| BL-038 | grep 대상 밖 문서 보류 🟡 → ADR-0023 링크 | S01-T04 | open |
| BL-039 | coding-conventions `IIdGenerator.NewId()` 이름 S02-T01에 맞춤 | S01-T04 | planned:S04 |
| BL-040 | T06: `Result<T>` 값 암시적 변환 여부 | S01-T04 | done |
| TD-013 | NetArchTest.Rules · NSubstitute.Analyzers 유지 중단 | S01-T04 | open |
| BL-041 | package-versions 테스트 표 용도 열 "추천/대안" → "채택/미사용" | S01-T04 | done |
| BL-042 | clean-architecture 서비스 카탈로그 🟡(도메인 항목) 정리 위치 | S01-T04 | open |
| BL-043 | service-catalog.md:24 API Gateway 🟡를 BL-038/042 범위에 | S01-T04 | dropped → BL-038 |
| BL-044 | security.md:71 마이그레이션 전용 계정 🟡 표시 정리 | S01-T04 | dropped → BL-038 |
| BL-045 | S03-T02: 실제 EF 생성 파일에서 Migrations 섹션 경고 0 · format 재확인 | S01-T05 | planned:S03 |
| BL-046 | S03: EF Design 참조 위치 · dotnet ef 프로젝트 인자 통일 | S01-T05 | planned:S03 |
| BL-047 | 문서: generated_code + CS1591 none 병기 | S01-T05 | done |
| BL-048 | clean-architecture 저장소 구조 · 공통 빌드 설정 표 갱신(S04-T02) | S01-T05 | planned:S04 |
| BL-049 | local-setup: Windows MAX_PATH(MSB3101/MSB3030) 안내 | S01-T05 | planned:S04 |
| BL-050 | T06: 골격 확인 테스트 대체 · 삭제 | S01-T05 | done |
| BL-051 | IDE0005 빌드 강제 여부 | S01-T05 | open |
| TD-014 | 테스트 프로젝트 판별이 이름 규칙(*Tests)에 의존 | S01-T05 | open |
| TD-015 | 강타입 ID ValueConverter · DomainEvents Ignore 공통 처리 | S01-T06 | planned:S02 |
| BL-052 | S02-T06: CommonErrors 재사용, 매핑 · 변환만 구현 | S01-T06 | planned:S02 |
| BL-053 | S02-T02: CustomState 없는 실패는 Error.Validation(1001)로 감싸기 | S01-T06 | planned:S02 |
| BL-054 | 아키텍처 테스트 후보: Error/Result 파생 금지, Entity 파생 sealed | S01-T06 | dropped → BL-029 |
| TD-016 | Error 복사 생성자로 외부 파생 가능 | S01-T06 | planned:S04 |
| TD-017 | Result<T> notnull 제약 없음(런타임 거부만) | S01-T06 | open |
| BL-055 | coding-conventions 경고 억제 규칙 명문화 | S01-T06 | open |
| BL-056 | "ValidationError는 sealed" 명시(ADR 0018 문구 오해 방지) | S01-T06 | dropped → TD-016 |
| BL-057 | S03 CI 통합 테스트 대비: ubuntu 러너 · services 미사용, 이미지 pull 시간, 컨테이너 공유 | S01-T07 | planned:S03 |
| BL-058 | Dependabot(github-actions) 검토 | S01-T07 | open |
| BL-059 | CI 시간이 NFR-07에 가까워지면 NuGet 캐시 재판단 | S01-T07 | open |
| BL-060 | ReportGenerator 무료판 메서드 커버리지 표시 제한(기록) | S01-T07 | dropped |
| BL-061 | AggregateRoot protected 생성자 커버리지 제외 여부 | S01-T07 | dropped |
| TD-018 | 브랜치 보호 불가로 CI 필수 체크 미적용 | S01-T07 | open |
| BL-062 | 테스트 실패 시 커버리지 보고 생략 의도 명시 검토 | S01-T07 | planned:S04 |
| BL-063 | reportgenerator 입력 패턴 합산 수 확인(S03) | S01-T07 | planned:S04 |
| BL-064 | DoD: 러너 dotnet --info로 SDK 버전 확인 | S01-T07 | planned:S01 |

## 회고

### 잘된 점

- N1 ADR 확인 흐름(developer 1차 `adr_drafts` → 사용자 확인 → 2차 파일 생성)이 3회 모두 계획대로 돌았다. 결정 내용 때문에 되돌아간 반려 0.
- 문서 작업 운영(reviewer 점검 15개 해당 없음, tester 명령 기반 점검표)으로 코드 없는 작업에도 grep · Node 검사 · 부정 점검 같은 재현 가능한 증빙이 남았다.
- dba가 NuGet 캐시 · 패키지 소스로 사실을 확인해 설계 결함을 사전에 막았다(`WithReference(db)` 슈퍼유저 자격 증명 주입, Respawn `TablesToIgnore` 표기).
- 외부 사실(Aspire 폐기)을 BLOCKED로 올려 사용자 결정 → ADR · TD · BL 기록까지 한 흐름으로 처리했다.
- 새 clone · 실측으로 설정 결함 4건을 커밋 전에 잡았다.
- check-docs.js를 T02에서 먼저 만들어 이후 작업 전체에서 재사용했다. 반려는 1회(형식)뿐이었다.

### 문제

- 후보 폭증(BL 64, TD 18): 약 22건이 같은 스프린트의 다음 작업에 넘기는 인계 메모였는데 백로그를 인계 채널로 써서 정리 비용이 커졌다.
- Docker가 꺼진 상태로 진행해 DB 실측이 모두 S03으로 몰렸다.
- PRD 단계 전제(Aspire 9.x 지원, 로컬 SDK 충분)가 스프린트 안에서 깨졌다. 환경 · 외부 사실 사전 점검이 없었다.
- check-docs가 표 구조 결함을 못 잡아 reviewer 반려로 이어졌다. raw-log frontmatter 결함(BL-018)이 내내 잔여 결함으로 남았다.
- dba 단계 커밋 타입이 섞였다(`chore(sprint)` / `docs(sprint)`). 스프린트 frontmatter(adrs, finished) 갱신 단계가 없다. 커밋 약 2/3가 판정 기록 커밋이다.
- CI 첫 러너 실행이 스프린트 종료 뒤라 러너 고유 문제를 늦게 발견할 수 있다.
- ADR 확인 3회가 모두 "수정 없음" — 확인 자료가 과해 검토가 형식적이 됐을 가능성을 점검할 필요가 있다.

### 다음에 바꿀 것

- 같은 스프린트 안에서 반영할 인계 메모는 BL로 만들지 않고 진행 기록 · 다음 작업 입력으로 넘긴다(agents.md · 에이전트 정의에 기준 추가).
- `/sprint` 시작에 환경 사전 점검 단계 추가: Docker 실행, SDK 버전(global.json 충족), gh 인증, 필요 외부 서비스.
- 단계 커밋 타입 규칙 고정(산출물 없는 판정 · 기록은 `docs(sprint)`), 종료 단계에 frontmatter(adrs, finished, status) 갱신 추가.
- check-docs에 표 열 수 일관성 점검 추가, BL-018을 토픽 밖 작업으로 먼저 해결해 결함 0 기준선 확보.
- ADR 확인은 결정 문장 · 선택지 요약 위주로 보여 주는 형식 검토.
- CI 첫 실행을 앞당길지 결정(N2 임시 브랜치 예외와 함께 정책화 검토).

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-001 분할) |
| 2026-09-27 | - | 계획 리뷰 반영, `active` |
| 2026-09-27 | - | 결과 리뷰 · 백로그 / 기술부채 정리 · 회고, `done` |
