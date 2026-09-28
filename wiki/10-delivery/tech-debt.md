---
title: "기술부채"
type: index
status: stable
tags: [delivery, tech-debt]
aliases: [Tech Debt]
created: 2026-09-27
updated: 2026-09-28
---

# 기술부채

> 알면서 타협한 구현, 누락된 테스트, 미뤄 둔 리팩터링처럼 나중에 갚아야 할 기술적 빚을 기록합니다.
> 새 기능이나 미룬 요구사항은 [백로그](backlog.md)에 적습니다.
>
> [개발 관리](README.md)

## 작성 규칙

- 타협한 즉시 `new` 상태로 행을 추가하고 그 단계의 커밋에 포함한다. 영향도와 상환 계획은 스프린트 종료 때 orchestrator가 정리한다. 코드에는 `// TODO(TD-NNN): ...` 주석으로 위치를 남긴다. ID는 `TD-NNN`으로 순서대로 붙이고 재사용하지 않는다.
- 행은 지우지 않는다. 갚으면 상태를 `resolved`로 바꾸고 해결 커밋 / 작업 ID를 적는다.
- **유형**: `코드` · `설계` · `테스트` · `문서` · `인프라`
- **영향도**: `상`(장애·보안·데이터 위험) / `중`(변경 비용 증가) / `하`(가독성·정리)
- **상태**: `new`(미정리) · `open` · `planned:SNN` · `resolved`

## 목록

| ID | 제목 | 유형 | 영향도 | 발생 | 상환 계획 | 상태 |
|---|---|---|---|---|---|---|
| TD-001 | 마이그레이션 전용 DB 롤과 애플리케이션 롤 분리 (로컬은 `employee_app` 단일 롤) | 설계 | 중 | S01 계획 리뷰(Q4) | Phase 4(배포 토픽)에서 마이그레이션 전용 롤 분리. security.md:71 정리(BL-038)와 연계 | open |
| TD-002 | .NET 8(2026-11-10) · Aspire 9.x 지원 종료에 따른 메이저 업그레이드(EF Core / Npgsql 포함) | 인프라 | 상 | S01 계획 리뷰 | BL-002 결정 뒤 .NET 10 · Aspire 13 · EF Core 메이저 전환 토픽. TD-005 · 007 · 008 · 012 연쇄 상환 | open |
| TD-003 | Npgsql.EFCore 8.0.11의 EF 의존에 상한이 없음: `CentralPackageTransitivePinningEnabled`로 EF 8.0.31 고정해야 EF9 유입 방지 | 인프라 | 하 | S01-T01 | CentralPackageTransitivePinning으로 완화. EF 메이저 전환(TD-002) 때 재검토 | open |
| TD-004 | Testcontainers `PostgreSqlBuilder` 매개변수 없는 생성자는 CS0618로 빌드 실패: 이미지 인자 필수 | 테스트 | 하 | S01-T01 | S03-T06 d2263d6: EmployeeDatabaseFixture가 new PostgreSqlBuilder(PostgresImage.Name)으로 이미지 인자 명시(태그는 Directory.Build.props 한 곳 → AssemblyMetadata) | resolved |
| TD-005 | Aspire 9.x 지원 종료(NuGet out of support) 상태로 9.5.2 사용: 보안 패치 없음. .NET 8 지원 종료와 함께 해소(재검토 BL-002) | 인프라 | 중 | S01-T01 | TD-002에 병합(Aspire 전환 시 상환). AppHost는 로컬 전용이라 운영 영향 제한적 | open |
| TD-006 | 클라이언트 통합을 쓰고 OpenTelemetry.Api만 1.15.3으로 고정하면 OpenTelemetry 1.9.0과 혼재: OTel 계열 전체 동일 버전 고정 필요(BL-004 결과에 따라 발생) | 인프라 | 하 | S01-T01 | 발생하지 않음: ADR-0011 클라이언트 통합 미사용 (608f828) | resolved |
| TD-007 | Aspire.Hosting 9.5.2 전이 MessagePack 2.5.192 취약(GHSA 11건, 높음 2건): 2.5.305 수동 고정 유지(AppHost 한정, Aspire 전환 시 제거) | 인프라 | 중 | S01-T01 | AppHost 한정 수동 고정 유지, NuGetAuditMode=all로 감시. Aspire 전환(TD-002) 때 제거 | open |
| TD-008 | net8.0 앱에 Microsoft.Extensions.* · System.Diagnostics.DiagnosticSource 10.0.0 전이 유입(Scrutor 7, Serilog.AspNetCore 10, OpenTelemetry 1.19). 취약점 없는 OTel은 모두 DiagnosticSource 10 요구 | 인프라 | 중 | S01-T01 | S02 · S03 실행 시 런타임 호환 관찰. S03-T05 실측: 실제 로드는 DI · Abstractions 모두 앱 로컬 10.0.0이라 혼합 없음, Scrutor Decorate 정상(TD-020 병합). .NET 10 전환(TD-002)으로 해소 | open |
| TD-009 | xUnit v2 선택 시 xunit 2.9.3이 NuGet Legacy 폐기(보안 수정만) | 테스트 | 하 | S01-T01 | 발생하지 않음: ADR-0021 xUnit v3 채택 (5b94975) | resolved |
| TD-010 | 커밋 응답 중 연결이 끊기면 실행 전략이 SaveChanges를 재실행해 pk_ 23505 · xmin 충돌을 잘못 보고할 수 있음. 운영 전 verifySucceeded 또는 멱등 키로 해소 | 설계 | 중 | S01-T02 | Phase 4 전 verifySucceeded 또는 Idempotency-Key로 해소. 재현 방법: S03-T06 장애 주입 도우미(쓰기 인터셉터 재등록)에 TransactionCommittedAsync에서 NpgsqlException(new TimeoutException())을 1회 던지는 인터셉터를 더해 재실행 오보고(3003 · 3001)를 Red로 둔 뒤 상환. S03에서는 BL-081 P9로 제외. S05-T01: PRD-002 위험 수용 · open 유지(2026-09-28 사용자 사전 합의). S05-T06에서 pk · ux 검사 순서 1회 실측, ADR ② 위험 항목 · S05-T06 실측: 같은 행이 pk · ux를 함께 위반하면 인덱스 생성(OID) 순서로 `pk_employees`가 먼저 보고되어 배치 재전송 오보고는 3003 · 로그 202(23001 아님), 인덱스 생성 순서가 바뀌면 달라질 수 있음. S06 동시 경합 테스트에 인계 · S06-T06 실측: 고정 IIdGenerator 같은 ID 재전송 → 409 · 3003 · 로그 202(ac654b7). InitialCreate 재생성 없음 | open |
| TD-011 | EF Core 8 MigrateAsync에 마이그레이션 잠금 없음(EF 9 추가): 적용 주체를 MigrationService 1개로 제한, Phase 4는 번들 / 스크립트 | 인프라 | 중 | S01-T02 | S03 확인: 적용 주체 MigrationService 1개(WithReplicas 없음), Api Migrate 호출 0건(T04 grep). Phase 4 번들 / 스크립트 전환은 남음 | open |
| TD-012 | ServiceDefaults가 Aspire 템플릿과 달라짐(OTel 로그 공급자 제거, 신호별 OTLP 내보내기, ADR-0020). Aspire 버전을 올릴 때 차이를 다시 적용해야 함 | 인프라 | 하 | S01-T03 | 1단계(템플릿 차이 주석 · ADR-0020 링크)는 S03-T03 19954f4에서 이행(ServiceDefaults csproj · ServiceDefaultsExtensions 주석). 남은 것: Aspire 전환(TD-002) 때 템플릿 차이 재적용 | open |
| TD-013 | NetArchTest.Rules 1.3.2(2021-05 이후 릴리스 없음) · NSubstitute.Analyzers.CSharp 1.0.17(2024-02 이후 없음) 유지 중단. 막히면 ArchUnitNET 전환 검토(ADR-0021) | 테스트 | 중 | S01-T04 | S02-T05 스파이크: NetArchTest 1.3.2가 net8.0 · internal · IL 의존 · 커스텀 규칙 모두 동작, ArchUnitNET 전환 불필요. 유지 중단 위험만 남음, .NET 10 전환(TD-002) 때 재검토 | open |
| TD-014 | 테스트 프로젝트 판별이 이름 규칙(MSBuildProjectName이 Tests로 끝남)에 의존: 규칙 밖 이름은 OutputType=Exe · 문서 생성 제외가 적용되지 않음. 아키텍처 테스트 / CI 점검으로 보완 검토 | 코드 | 하 | S01-T05 | 아키텍처 테스트로는 볼 수 없음(BL-080 병합). sln 파싱 Node 점검 스크립트를 CI에 넣는 방식으로 상환, BL-001과 함께 S04-T03에서 검토 | open |
| TD-015 | 강타입 ID용 ValueConverter 공통 등록(ConfigureConventions)과 AggregateRoot.DomainEvents Ignore 공통 처리를 BuildingBlocks 영속성 계층에서 제공(첫 DbContext 작업에서 반영) | 설계 | 중 | S01-T06 | S02-T04(3fe392a): ConfigureConventions 강타입 ID 변환 + ValueGeneratedNever, IgnoreAny<IDomainEvent> 공통 처리, 메타데이터 테스트로 확인 | resolved |
| TD-016 | Error는 non-sealed record라 컴파일러가 만드는 protected 복사 생성자로 어셈블리 밖에서도 파생 가능(불변식은 유지). coding-conventions:61 · Error.cs remarks의 "같은 어셈블리로 막음" 표현이 부정확(CS8878). 문서 수정 또는 아키텍처 테스트(BL-054)로 보완 | 코드 | 하 | S01-T06 | S04 해결: d699f27 Error.cs remarks CS8878 정정, 55eecfd coding-conventions 문구 정정(파생 차단 강제는 BL-029). BL-056 병합. S04-T02에서 coding-conventions:61 · Error.cs remarks 문구 정정, 강제는 BL-029 | resolved |
| TD-017 | Result<T>는 성공 값 null을 런타임에만 거부하고 `where T : notnull` 제약 없음(Mediator 인터페이스 전파 부담) | 코드 | 하 | S01-T06 | S02-T01 Mediator 인터페이스 정의 때 notnull 제약 전파 재판단 | open |
| TD-018 | GitHub Free private라 브랜치 보호 불가 → CI를 필수 체크로 걸 수 없음. "CI 미통과 PR 병합 금지"를 규칙으로만 지킴 | 인프라 | 중 | S01-T07 | 상환 수단 없음(요금제). "CI 통과 전 병합 금지"를 PR 체크리스트 · retro 점검으로 유지 | open |
| TD-019 | 테스트 프로젝트에 Microsoft.Extensions.Logging · DependencyInjection 8.0.1(Diagnostics.Testing 전이)과 *.Abstractions 10.0.0이 섞여 해석됨(메이저 혼합) | 의존성 | 하 | S02-T02 | .NET 10 전환(TD-002) 때 Microsoft.Extensions.* 메이저를 하나로 맞춤(테스트 프로젝트 한정) | open |
| TD-020 | Scrutor 7 Decorate가 keyed 서비스(IKeyedServiceProvider)에 의존: 앱 호스트 공유 프레임워크 DI 8.0과 Abstractions 10.0.0 혼합 런타임 호환 미확인 | 의존성 | 중 | S02-T03 | S03-T05 1530f60 실측: 혼합 없음(DI · Abstractions 모두 10.0.0), Decorate 101 · 103 정상 → TD-008 관찰에 병합 | resolved |
| TD-021 | BadHttpRequestException의 원래 상태 코드(413 · 408 등)를 모두 400 · 1001로 응답(원래 코드는 로그 302에만) | 설계 | 하 | S02-T06 | api-guidelines 정비 때(S04-T02 또는 이후) 413 · 408 상태 코드 보존 여부 결정. S03-T05 H2 현행 확인(31MiB 초과 → 400 · 1001, 413은 로그 302에만). BL-106 · BL-107과 함께 · S06-T01 부분 상환: BadHttpRequestException 413 → 413 · 1004(ADR-0028). 남은 부분은 408 등 그 밖의 상태 코드 | open |
| TD-022 | ExceptionHandlerMiddleware 범주 전체를 로그 필터로 끄므로 같은 범주의 다른 로그(응답 시작 후 Warning, 요청 중단 Debug)도 사라짐. 이벤트 ID 단위 필터 불가 | 설계 | 중 | S02-T06 | .NET 10 전환(BL-002 · TD-002) 때 ExceptionHandlerOptions 억제 옵션으로 범주 전체 필터 대체. BL-075와 짝 | open |
| TD-023 | 바인딩 오류 errors 키 순서가 ModelStateDictionary 열거 순서를 따르고 요청 속성 순서를 보장하지 않음 | 설계 | 하 | S02-T06 | 클라이언트 요구가 생기면 정렬 규칙 결정(T06 tester 후보 병합) | open |
| TD-024 | 아키텍처 테스트 프로젝트는 coverlet.collector를 쓰지 못함(계측된 제품 DLL을 검사해 Domain 규칙 실패). CI 로그에 수집기 없음 메시지 | 테스트 | 하 | S02-T05 | S04 해결: d699f27 ArchitectureTests 수집 제외를 의도된 구성으로 확정(cobertura 12 = 수집 12), S04-T03 ci-cd에 수집기 없음 메시지 원문 기록. S04 계획 확정: S04-T01(수집 제외 확정) · S04-T03(ci-cd 문서) 편입. S04-T01 커버리지 점검 때 ArchitectureTests 수집 제외 확정, CI 로그 '수집기 없음' 메시지 문서화 | resolved |
| TD-025 | Controller ↛ Repository 아키텍처 규칙은 시그니처 기준이라 메서드 본문 서비스 로케이터(GetRequiredService<IXxxRepository>())는 잡지 못함 | 테스트 | 하 | S02-T05 | 그 전까지 reviewer 점검표에 'Controller 본문 서비스 로케이터 금지', 필요해지면 Mono.Cecil IL 피연산자 규칙 추가 | open |
| TD-026 | Employee.Infrastructure가 IConfiguration을 전이 참조(Microsoft.Extensions.Configuration.Abstractions 8.0.0, BuildingBlocks.Infrastructure 경유)로 사용: 직접 참조 · package-versions.md 등록 여부 판단 필요(현재 빌드 · 감사 문제 없음) | 의존성 | 하 | S03-T02 | S04 종료 정리: 문서분(package-versions 전이 참조 행)은 S04-T02 완료, 직접 참조 여부는 TD-002 때 TD-019와 함께. S04 계획 확정: S04-T02 편입(문서 한 줄). 빌드 · 감사 문제 없음. S04-T02에서 package-versions.md에 전이 사용(BuildingBlocks.Infrastructure 경유) 한 줄, 직접 참조 여부는 .NET 10 전환(TD-002) 때 TD-019와 함께 | open |
| TD-027 | 테스트 fixture의 ApplyMigrationsAsync가 MigrationWorker 적용 코드를 복제(차이가 생겨도 못 잡음), EmployeeDatabaseFixture.OpenAsync · TestTriggers.OpenAsync 중복 | 테스트 | 하 | S03 결과 리뷰(S03-T06) | BL-103 해결 때 적용 코드를 Infrastructure 공용 메서드 하나로 모아 Worker · fixture가 같이 쓰고, OpenAsync는 fixture 한 곳으로. 그 전까지 fixture 주석에 복제 사실 · 원본 위치 | open |
| TD-028 | CSV 헤더 행 미지원(PRD-002 FR-03, S06-T02 구현): 헤더를 넣으면 첫 줄을 데이터로 읽어 날짜 형식 오류로 요청 전체를 거부함 | 설계 | 하 | PRD-002 범위 밖 (Q7 · FR-03) | BL-122(헤더 지원) 결정 때 함께 상환 · S05 종료 정리: S06-T02는 FR-03대로 헤더 미지원 구현, S07-T04 API 명세에 명시 | open |
| TD-029 | 제품 코드의 null-forgiving `!`(coding-conventions Nullable 규칙 위반): `InvalidModelStateResponses.cs:57` `FullName!`, `Employee.cs:40-44` EF 생성자 `null!`. EF 생성자 예외 규칙을 둘지 고칠지, 분석기 · 아키텍처 테스트 검사 방안 | 코드 | 하 | S06-T01 reviewer | /retro PRD-002에서 결정: (1) EF 생성자 `null!` 예외 명시 또는 수정 (2) InvalidModelStateResponses.cs:57은 다음 BuildingBlocks.Api 변경 때 속성 패턴으로 (3) 분석기 · grep 점검을 reviewer 진입 점검 · CI에 둘지. S06에서 같은 위반이 반려 2회 | open |
| TD-030 | 라우트 템플릿 도우미 RouteTemplatePath(약 10줄)를 BuildingBlocks.Api와 ServiceDefaults에 같은 internal 코드로 중복, 테스트 표도 두 벌. 두 프로젝트는 서로 참조할 수 없음(ADR-0024 의존성 표) | 설계 | 하 | S07-T02 developer | 트리거: 두 번째 서비스가 생길 때 공통 위치 재검토(S07 계획 리뷰 Q2 대리 승인) | new |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | `new` 상태 추가: 발견 즉시 기록, 스프린트 종료 때 정리 |
| 2026-09-27 | - | S01 종료 정리: TD-001~018 (resolved 2, planned 3, open 13, TD-005는 TD-002에 병합) |
| 2026-09-27 | orchestrator | S02 종료 정리: TD-015 resolved, TD-019~025 정리(planned:S03 1 · open 6), TD-013 · 014 상환 계획 갱신, `new` 0 |
| 2026-09-28 | orchestrator | S03 종료 정리: TD-004 · TD-020 resolved(TD-020은 TD-008 병합), TD-008 · 010 · 011 · 012 · 021 · 026 상환 계획 갱신, TD-027 추가, `new` 0 |
| 2026-09-28 | orchestrator | S04 계획 확정: TD-024 · TD-026 open → planned:S04, TD-016 배정을 S04-T01(코드) · S04-T05(문서)로 나눔 |
| 2026-09-28 | orchestrator | S04 종료 정리: TD-016 · TD-024 resolved, TD-026 open(문서분 완료), `new` 0 |
| 2026-09-28 | developer | S05-T01: TD-010 PRD-002 위험 수용 메모(S05-T06 실측, ADR ② 위험 항목), TD-028 추가(`new`) |
| 2026-09-28 | orchestrator | S05 종료 정리: TD-028 new → open(영향 하, S06-T02는 헤더 미지원으로 구현, S07-T04 명세에 명시), TD-010 open 유지(S05-T06 실측 반영), `new` 0 |
| 2026-09-29 | orchestrator | S06 종료 정리: TD-029 new → open(/retro PRD-002에서 점검 수단 결정), TD-021 S06-T01 부분 상환 비고, TD-010 S06-T06 실측 비고, `new` 0 |
