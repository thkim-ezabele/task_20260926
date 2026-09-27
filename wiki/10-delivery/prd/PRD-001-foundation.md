---
title: "PRD-001: 기반 구축 (인프라 · .NET 8 솔루션 기본 설계)"
type: prd
prd: "001"
status: stable
received: 2026-09-27
sprints: [S01, S02, S03, S04]
branch: "feature/prd-001-foundation"
pr: 7
release: "v0.1.0"
retro: "RETRO-PRD-001"
aliases: [PRD-001]
tags: [delivery, prd]
created: 2026-09-27
updated: 2026-09-28
---

# PRD-001: 기반 구축 (인프라 · .NET 8 솔루션 기본 설계)

- 받은 날: 2026-09-27
- 스프린트: [S01](../sprints/S01-decisions-build-ci.md), [S02](../sprints/S02-building-blocks.md), [S03](../sprints/S03-aspire-employee.md), [S04](../sprints/S04-tests-docs-evidence.md)
- 토픽 브랜치: `feature/prd-001-foundation` · PR: [#7](https://github.com/thkim-ezabele/task_20260926/pull/7) · 릴리스: `v0.1.0` · 회고: [RETRO-PRD-001](../retros/RETRO-PRD-001.md)

## 원문

> 이제 위키 구축은 마무리 하고 실제 프로젝트 구현 시작을 위한 아키텍쳐 설계를 진행하려고해
> 해당 프로젝트는 직원들의 긴급연락망 구축을 위한 시스템이야 우리는 인프라 설계부터 .net8 프로젝트 기본 설계까지만 진행하는 요구사항을 설계하는게 목표야

### 인터뷰 정리

| 항목 | 답변 |
|---|---|
| 산출물 | 설계 문서 + 실제로 빌드 · 실행되는 뼈대 코드 |
| 인프라 범위 | 로컬 개발 환경만. **.NET Aspire AppHost**로 구성한다(docker compose는 만들지 않음). 운영 배포는 Phase 4 |
| Aspire 버전 | **Aspire 9.x + .NET 8 SDK**로 통일(ADR 0001 유지) |
| 솔루션 범위 | BuildingBlocks + 샘플 서비스 1개(**Employee**). 나머지 서비스는 도메인 토픽에서 추가 |
| CI | 포함 (GitHub Actions 빌드 · 테스트) |
| 메시지 브로커 | 현재 도입하지 않음 |
| API Gateway | 현재 도입하지 않음. 대신 Aspire 솔루션 형태로 구성 |
| 로그 수집기 | 현재 도입하지 않음 (로컬 관측은 Aspire 대시보드) |
| Mediator | 직접 구현 |
| API 스타일 | Controller |
| DI 타입 검색 | Scrutor |
| UUID v7 | `IIdGenerator` 추상화 + UUIDNext 구현 |
| 단언 라이브러리 | AwesomeAssertions |
| 결정 기록 시점 | 이 토픽 안에서 ADR로 확정 (구현 전에 작성) |

## 분석

### 목적

위키 단계에서 정한 아키텍처 결정(ADR 0001~0010)과 기준 문서를 **실제로 빌드 · 실행 · 테스트되는 솔루션 뼈대**로 옮긴다. 이후 도메인 토픽(Phase 2~)이 "구조를 고민하지 않고 기능만 추가"할 수 있는 출발점을 만든다.

- 미정이던 기술 선택을 ADR로 확정하고, 도입하지 않기로 한 항목도 근거와 함께 남긴다.
- 로컬 인프라(Aspire AppHost + PostgreSQL)를 명령 하나로 띄운다(마이그레이션 적용 포함).
- 레이어 규칙, CQRS 파이프라인, DI 자동 등록, 읽기 / 쓰기 연결 분리, DB 명명 · 타입 규칙이 샘플 서비스 하나에서 끝까지(요청 → DB → 응답) 동작함을 테스트로 증명한다.

### 기능 요구사항

| ID | 요구사항 | 우선순위 | 인수 조건 |
|---|---|---|---|
| FR-01 | **기술 결정 ADR 작성.** ① 사전 확인: 고정할 Aspire 9.x 마이너 버전의 net8.0 AppHost 지원 · 패치 여부, EF Core · Npgsql · EFCore.NamingConventions 8.0.x 정합, 도입 패키지 라이선스 ② 결정(결정마다 1건): Aspire 로컬 오케스트레이션(Write / Read 연결 매핑, 로컬 계정 방침 포함), Mediator 직접 구현(형태 · 파이프라인 순서), Command 트랜잭션 경계 · Unit of Work, Controller, Scrutor(ADR 0010을 구체화하는 새 ADR, 대체 아님), UUID v7(`IIdGenerator` + UUIDNext), AwesomeAssertions, 마이그레이션 적용 방식 · 운영 전 리셋 정책, FluentValidation, 로깅 구현(Serilog + OTLP 싱크), Swashbuckle, Respawn · 커버리지 도구 ③ **도입 보류**(1건): 메시지 브로커, Outbox / Inbox, API Gateway, 로그 수집기 | 상 | ADR 파일은 **사용자 확인 후** `accepted`로 커밋되고, 구현 작업보다 먼저 커밋된다. 나열한 항목의 🟡가 tech-stack · 기준 문서에서 해소되고, 남은 🟡 항목은 처리 방식(이후 토픽 등) 표로 남는다 |
| FR-02 | **빌드 설정.** `EmergencyHub.sln`, `global.json`(8.0.x), `Directory.Build.props`(공통 속성, 테스트 프로젝트 · `DynamicProxyGenAssembly2` 대상 `InternalsVisibleTo` 일괄 설정, BuildingBlocks만 `GenerateDocumentationFile` + CS1591 오류화), `Directory.Packages.props`(중앙 버전 관리), `.editorconfig`(`Migrations/**`는 `generated_code = true`), dotnet-ef 로컬 도구 매니페스트(`.config/dotnet-tools.json`, 8.0.x) | 상 | 새 clone에서 `dotnet build`가 경고 0, 오류 0으로 성공한다(마이그레이션 생성 코드 포함) |
| FR-03 | **Aspire 구성.** `EmergencyHub.AppHost`: PostgreSQL 리소스(비밀번호는 user-secrets 매개변수로 고정, 데이터 볼륨), Database `emergency_hub_employee`(리소스 이름 `employee-db`와 분리), 생성 스크립트로 `employee_app` 롤과 권한 부여, Api에 `ConnectionStrings__Write` / `ConnectionStrings__Read` 주입(Read에 `Options=-c default_transaction_read_only=on`), `WaitFor(db)`, **MigrationService** 완료 후 Api 시작(`WaitForCompletion`). `EmergencyHub.ServiceDefaults`: 헬스체크, OpenTelemetry(트레이스 · 메트릭), 서비스 디스커버리, 복원력, Serilog → OTLP 로그 전송 | 상 | `dotnet run --project <AppHost>` 한 번으로 PostgreSQL → 마이그레이션 → Employee Api 순으로 뜨고, Api가 Write / Read 연결을 모두 받는다. 대시보드에서 Api Healthy와 로그 · 트레이스가 보인다(스크린샷 · 실행 기록으로 증빙, Aspire.Hosting.Testing 스모크 테스트는 CI 시간을 보고 선택) |
| FR-04 | **BuildingBlocks.Domain.** `Entity<TId>`, `AggregateRoot<TId>`(도메인 이벤트 **수집까지만**), `IDomainEvent`, `Error(int Code, string Message, ErrorType Type)`와 팩토리, `Result` / `Result<T>`(`Error` → `Result<T>` 변환), 마커 `IRepository`. Aggregate 팩토리는 ID를 인자로 받는다(ID는 Handler가 `IIdGenerator`로 생성) | 상 | 단위 테스트(성공 / 실패 / 엣지)가 통과하고, 에러 코드의 유형 자리와 `ErrorType`이 일치함을 검증한다. 프레임워크 패키지를 참조하지 않는다 |
| FR-05 | **BuildingBlocks.Application.** `ICommand` / `IQuery` / Handler 인터페이스(`ICommand : ICommand<Unit>`로 통일), **직접 구현한 디스패처**(Handler 타입 캐시)와 데코레이터 파이프라인 **로깅 → 검증(FluentValidation) → 트랜잭션 → Handler**(트랜잭션은 Command에만), `IUnitOfWork`, 마커 `IReadRepository` / `IService`, `IIdGenerator`. 트랜잭션 데코레이터가 실행 전략(`CreateExecutionStrategy().ExecuteAsync`) 안에서 트랜잭션 → `SaveChangesAsync` → 커밋을 맡고, Handler는 저장을 부르지 않는다. Outbox 저장을 같은 트랜잭션에 끼울 확장 지점을 남긴다 | 상 | 파이프라인이 위 순서로 실행되고, 검증 실패 시 Handler가 호출되지 않으며(실패도 로그에 남음), 실패 `Result`면 커밋하지 않음을 테스트로 확인한다 |
| FR-06 | **BuildingBlocks.Infrastructure.** EF Core 공통 설정(snake_case, 유니크 인덱스 `ux_` · 체크 제약 `ck_<table>_<rule>` 명명 도우미, 강타입 ID 변환 + `ValueGeneratedNever`, 감사 컬럼 shadow property + `SaveChangesInterceptor`(`TimeProvider`, `DateTimeOffset` UTC), `xmin` 동시성 토큰 공통 매핑), `RepositoryBase` / `ReadRepositoryBase`, 읽기 전용 DbContext 기반(NoTracking 기본, SaveChanges 차단), `AddConventionalServices`(Scrutor `Scan` + `Decorate`), UUIDNext 기반 `IIdGenerator`, `TimeProvider` 등 공통 등록 | 상 | `ValidateOnBuild` / `ValidateScopes` 상태에서 마커 구현 타입이 모두 Scoped로 해석된다. 생성된 UUID가 v7이고 문자열(빅 엔디언) 기준으로 생성 순서대로 정렬되며, 같은 밀리초 안에서도 단조 증가한다 |
| FR-07 | **공통 API 처리.** `Result` → RFC 9457 `ProblemDetails`(정수 `code`, `traceId`) 변환, `InvalidModelStateResponseFactory`로 바인딩 오류를 `1001` 형식으로 변환, 정의되지 않은 enum 정수는 Validator에서 `1002`로 거부, `DbUpdateConcurrencyException` → 충돌 `Result`, 전역 예외 처리(`IExceptionHandler`), Controller 기반 설정, Swashbuckle(OpenAPI). 공통 · Employee 에러 코드 번호를 할당한다 | 상 | 실패 `Result`, 바인딩 오류, 정의되지 않은 enum 입력, 처리되지 않은 예외가 [API 설계](../../04-development/api-guidelines.md) · [에러 코드](../../05-api/error-codes.md) 형식(`code` 포함)으로 응답된다 |
| FR-08 | **Employee 샘플 서비스.** Domain / Application / Infrastructure / Api 4개 레이어, Command 1개(직원 등록, 이메일 중복 검사) + Query 1개(직원 단건 조회), 쓰기 / 읽기 DbContext, 쓰기 DbContext용 `IDesignTimeDbContextFactory`(읽기 DbContext는 마이그레이션 대상 제외), 초기 마이그레이션(`--idempotent` SQL을 dba가 검토), **MigrationService** 프로젝트. 샘플 테이블: `id`(uuid v7), `display_name`, `email`(`ux_`), `employee_status`(smallint + `ck_`), `created_at` / `updated_at`(timestamptz), `xmin` (세부 구성은 스프린트 계획 리뷰에서 확정) | 상 | Aspire로 띄운 상태에서 등록 → 조회가 HTTP로 동작한다. 통합 테스트로 등록 → 조회, 이메일 중복 → 충돌 응답, 체크 제약 위반 거부, 동시성 충돌 → 충돌 `Result`(인위적 재현), **읽기 연결로 쓰기 시도 시 DB 거부**, UUID v7의 DB 정렬 순서를 검증한다 |
| FR-09 | **테스트 프로젝트.** `tests/BuildingBlocks/EmergencyHub.BuildingBlocks.{Domain,Application,Infrastructure}.UnitTests`(+ `EmergencyHub.BuildingBlocks.Api.UnitTests`, [ADR-0024](../../03-architecture/adr/0024-building-blocks-api-for-common-http-handling.md)), Employee Domain · Application 단위 테스트, 통합 테스트(Testcontainers PostgreSQL — Aspire와 같은 메이저 버전 태그 고정 — + `WebApplicationFactory`, 실제 마이그레이션 적용(`EnsureCreated` 금지), Respawn 초기화, Aspire와 같은 실행 전략), 아키텍처 테스트(NetArchTest: 의존성 규칙마다 1개 + 컨벤션 규칙 — sealed, Command / Query / Response는 record, Repository 인터페이스의 마커 상속) | 상 | `dotnet test`가 모두 통과하고, 아키텍처 테스트가 [의존성 규칙](../../03-architecture/clean-architecture.md#의존성-규칙)을 규칙마다 검증한다 |
| FR-10 | **CI.** GitHub Actions에서 PR(`develop`, `main` 대상)마다 restore → build → `dotnet format --verify-no-changes` → test(Docker 필요) → 커버리지 보고(coverlet + ReportGenerator). 첫 스프린트 앞부분에 배치해 첫 push부터 돌게 한다 | 상 | 토픽 PR에서 워크플로가 통과한다. 실패 테스트를 넣은 임시 브랜치에서 실패로 표시됨을 한 번 확인하고 기록을 남긴다 |
| FR-11 | **문서 갱신.** 결정 반영: clean-architecture(저장소 구조에 `src/Aspire/*`, `tests/BuildingBlocks/*`, Gateway · `deploy/` 제거, Outbox 보류 반영), tech-stack, database(UUID v7, DB 이름 · 계정, Aspire 연결 매핑, 리셋 정책, Employee 코드 표, ERD), logging-observability(Aspire 연동), error-codes, coding-conventions(SaveChanges 책임 예시). `todo` 문서 작성: local-setup(dotnet ef `--context` 사용법, 볼륨 · 비밀번호 초기화 포함), configuration, environments, ci-cd, troubleshooting | 중 | 해당 문서가 `draft` 이상이다. "새 환경에서 실행됨"은 CI 클린 러너 통과 + local-setup 수동 재현 기록(worklog)으로 증빙한다 |

### 비기능 요구사항

| ID | 요구사항 | 측정 기준 |
|---|---|---|
| NFR-01 | 빌드 품질 | `TreatWarningsAsErrors` 상태에서 경고 0 (생성 코드는 분석 제외) |
| NFR-02 | 레이어 규칙 준수 | 아키텍처 테스트 통과(CI 필수) |
| NFR-03 | 테스트 커버리지 | BuildingBlocks · Employee Domain / Application 라인 커버리지 80% 목표. coverlet + ReportGenerator로 CI에서 측정 · 보고만 하고 필수 체크는 걸지 않는다 |
| NFR-04 | 로컬 실행 용이성 | 사전 준비(.NET 8 SDK, Docker)만 된 새 환경에서 clone → 명령 1개로 전체 실행(마이그레이션 자동 적용 포함) |
| NFR-05 | 라이선스 | 상용 라이선스 패키지 사용 금지. 도입 패키지마다 라이선스를 확인해 기록한다 |
| NFR-06 | 비밀 정보 | DB 비밀번호 등 비밀 값을 저장소에 커밋하지 않는다(Aspire 매개변수 / user-secrets) |
| NFR-07 | CI 소요 시간 | PR 워크플로 10분 이내 (컨테이너 이미지 pull 시간 포함) |

### 범위 밖 (Out of Scope)

- 메시지 브로커, 통합 이벤트 발행, Outbox / Inbox 처리 (ADR 0004는 유지, 도입 시점은 이후 토픽)
- 도메인 이벤트의 저장 후 디스패치 (이번에는 `AggregateRoot`의 수집까지만)
- API Gateway, 로그 수집기(Seq 등), 운영 배포(docker compose, Kubernetes), CD
- 인증 / 권한 (Identity 토픽), 감사 컬럼 `created_by`
- `Idempotency-Key` 적용 (샘플 등록 POST에도 적용하지 않음)
- Employee의 **실제 도메인 모델**: 샘플 유스케이스는 구조 검증용이며 Phase 2에서 다시 설계한다(운영 배포 전까지 마이그레이션 리셋 허용)
- 비트 플래그 규칙의 샘플 검증 (Phase 2)
- Employee 외 서비스(Identity, Contact Network, Emergency, Notification)
- 프론트엔드

### 영향 범위

| 대상 | 내용 |
|---|---|
| 서비스 / 바운디드 컨텍스트 | BuildingBlocks(전 서비스 공통), Employee(샘플, MigrationService 포함), Aspire AppHost / ServiceDefaults(신규) |
| 데이터 (DB, 스키마) | PostgreSQL(Aspire 컨테이너), Database `emergency_hub_employee`(리소스 `employee-db`), 롤 `employee_app`, 초기 마이그레이션 1건(`employees` 테이블) |
| 이벤트 / API | 이벤트 없음. Employee API 2개(등록, 조회) |
| ADR 후보 | Aspire 로컬 오케스트레이션(로컬 DB 구성 포함), Mediator 직접 구현, Command 트랜잭션 경계 · UoW, Controller, Scrutor(ADR 0010을 구체화하는 새 ADR), UUID v7, AwesomeAssertions, 마이그레이션 적용 방식 · 리셋 정책, FluentValidation, 로깅 구현(Serilog + OTLP), Swashbuckle, Respawn · 커버리지 도구, 도입 보류(브로커 · Outbox / Inbox · Gateway · 로그 수집기). 번호는 작성 순서로 정하며 파일은 사용자 확인 후 만든다 |

## 리뷰 요약

> `/prd`의 병렬 리뷰(orchestrator / dba / developer) 결과와 반영 내용을 요약합니다.

| 관점 | 주요 발견 | 반영 |
|---|---|---|
| orchestrator | DB 이름 불일치, Write / Read 연결과 마이그레이션 적용 주체 누락, 샘플 마이그레이션과 불변 규칙 충돌, Outbox 보류 미반영, ADR 0010 "보완" 표현, ADR 범위 모호, 수동 인수 조건 증빙 부족, CI 배치 시점 | FR-01 · 03 · 08 · 10 · 11, NFR-03 · 04 · 07, 영향 범위, 범위 밖(리셋 정책) |
| dba | Aspire 재시도 실행 전략과 트랜잭션 충돌, Aspire 연결 키와 ADR 0009 불일치, 로컬 DB 계정, 설계 시점 팩터리, `ux_` / `ck_` 명명 빈틈, 샘플 스키마 구성, `xmin` 동시성, 리셋 정책, 패키지 · 이미지 버전 정합 | FR-01 · 03 · 05 · 06 · 07 · 08 · 09 · 11 |
| developer | SaveChanges 책임 미정, BuildingBlocks 테스트 프로젝트 누락, Mediator 형태, 파이프라인 순서, 바인딩 오류 · 정의되지 않은 enum, Serilog와 OTel 로그 중복, 감사 컬럼 방식, 생성 코드 경고, CI format 단계 | FR-02 · 04 · 05 · 06 · 07 · 09 · 10 |

## 질문과 답변

| # | 질문 | 답변 | 반영 |
|---|---|---|---|
| Q1 | 산출물은 설계 문서만인가, 뼈대 코드까지인가 | 설계 + 뼈대 코드 | 목적, FR-02~09 |
| Q2 | 인프라는 어느 환경까지인가 | 로컬만. Aspire AppHost로 구성(docker compose 없음) | FR-03, 범위 밖 |
| Q3 | Aspire 버전과 SDK | Aspire 9.x + .NET 8 SDK | FR-01, FR-02 |
| Q4 | 솔루션 범위 | BuildingBlocks + Employee 샘플 1개 | FR-04~08 |
| Q5 | 미정 기술 선택 | 브로커 · Gateway · 로그 수집기 미도입, Mediator 직접 구현, Controller, 나머지 추천안(Scrutor, UUIDNext, AwesomeAssertions) | FR-01, [ADR-0015](../../03-architecture/adr/0015-custom-mediator-pipeline.md), [ADR-0016](../../03-architecture/adr/0016-use-controllers-for-api.md), [ADR-0017](../../03-architecture/adr/0017-scrutor-for-convention-based-di.md), [ADR-0021](../../03-architecture/adr/0021-test-tooling-xunit-v3-and-awesomeassertions.md)(AwesomeAssertions), [ADR-0023](../../03-architecture/adr/0023-deferred-adoptions.md)(미도입 항목) |
| Q6 | 결정 기록 시점 | 이 토픽 안에서 ADR로 확정 | FR-01 |
| Q7 | CI 포함 여부 | 포함 | FR-10 |
| Q8 | Database 이름 | 기준 문서대로 `emergency_hub_employee`(리뷰 3건 일치, 질문 없이 반영) | FR-03, 영향 범위 |
| Q9 | Command 저장 · 커밋 주체와 재시도 실행 전략 (BQ1) | 트랜잭션 데코레이터가 실행 전략 안에서 트랜잭션 → SaveChanges → 커밋. Handler는 저장하지 않음. 재시도 유지 | FR-05, FR-11, [ADR-0014](../../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md). 구체화: ADR-0014 · [ADR-0015](../../03-architecture/adr/0015-custom-mediator-pipeline.md)(데코레이터는 Handler를 전략 밖에서 1회 실행 후 `IUnitOfWork.CommitAsync`를 부르고, 트랜잭션 → SaveChanges → 커밋은 UnitOfWork가 실행 전략 안에서 수행) |
| Q10 | 로컬 마이그레이션 적용 방식 (BQ2) | 별도 MigrationService(Worker), Api는 완료 대기 | FR-03, FR-08, NFR-04, [ADR-0012](../../03-architecture/adr/0012-migration-apply-and-pre-production-reset.md) |
| Q11 | FR-01 추가 확정 범위 (BQ3) | FluentValidation, 로깅(Serilog + OTLP), Swashbuckle, Respawn + 커버리지 도구 모두 확정. Outbox / Inbox는 보류 ADR에 포함 | FR-01, FR-03, FR-05, FR-07, FR-09, FR-10, NFR-03, [ADR-0018](../../03-architecture/adr/0018-use-fluentvalidation.md), [ADR-0019](../../03-architecture/adr/0019-use-swashbuckle-openapi.md), [ADR-0020](../../03-architecture/adr/0020-logging-with-serilog-and-otlp.md), [ADR-0022](../../03-architecture/adr/0022-respawn-and-coverage-tooling.md)(Respawn · 커버리지), [ADR-0023](../../03-architecture/adr/0023-deferred-adoptions.md)(Outbox / Inbox 보류) |
| Q12 | 로컬 DB 계정 | 생성 스크립트로 `employee_app` 롤 생성 | FR-03, [ADR-0011](../../03-architecture/adr/0011-use-aspire-local-orchestration.md) |
| Q13 | 운영 전 마이그레이션 리셋 | 운영 배포(Phase 4) 전까지 허용 | FR-01, FR-11, 범위 밖, [ADR-0012](../../03-architecture/adr/0012-migration-apply-and-pre-production-reset.md) |
| Q14 | 파이프라인 순서 | 로깅 → 검증 → 트랜잭션 → Handler | FR-05, [ADR-0015](../../03-architecture/adr/0015-custom-mediator-pipeline.md) |
| Q15 | 도메인 이벤트 범위 | 수집까지만, 디스패치는 이후 토픽 | FR-04, 범위 밖 |
| Q16 | 샘플 테이블 구성 | id · display_name · email(ux_) · employee_status(smallint + ck_) · 감사 컬럼 · xmin + 이메일 중복 검사. 세부는 스프린트 계획 리뷰에서 확정 | FR-08 |
| Q17 | Idempotency-Key | 이 토픽에서는 적용하지 않음 | 범위 밖 |
| Q18 | ADR 단위 | 결정마다 1건, 도입 보류 항목만 1건으로 묶음 | FR-01, [ADR-0023](../../03-architecture/adr/0023-deferred-adoptions.md) |
| Q19 | 고정할 Aspire 9.x 마이너 버전 | 해소: Aspire 9.5.2 (S01-T01, [패키지 버전 · 라이선스](../../03-architecture/package-versions.md#aspire)) | FR-01 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | PRD 접수, 인터뷰 정리, 초안 작성 |
| 2026-09-27 | - | 병렬 리뷰(orchestrator / dba / developer) 통합 반영, 차단 질문 3건 답변 반영 |
| 2026-09-27 | - | 스프린트 분할(S01~S04, 작업 22개) 승인, `stable` |
| 2026-09-27 | developer | Q19 해소: Aspire 9.5.2 (S01-T01) |
| 2026-09-27 | developer | Q9 · Q10 · Q12 · Q13 반영 열에 ADR 0011 · 0012 · 0014 링크 추가 (S01-T02) |
| 2026-09-27 | developer | Q5 · Q11 · Q14 반영 열에 ADR 0015~0020 링크 추가 (S01-T03) |
| 2026-09-27 | developer | Q9 반영 열에 "구체화: ADR-0014 · 0015" 주석, Q5 · Q11 · Q18 반영 열에 ADR 0021~0023 링크 추가, 결론 본문 불변 (S01-T04) |
| 2026-09-27 | developer | FR-09 테스트 프로젝트 목록에 `EmergencyHub.BuildingBlocks.Api.UnitTests` 비고(ADR-0024) 추가 (S02-T08) |
| 2026-09-27 | orchestrator | S02 결과 리뷰: FR-05 본문의 "트랜잭션 데코레이터가 실행 전략 안에서 SaveChanges · 커밋"은 ADR-0014 · 0015 기준으로 해석(데코레이터는 `CommitAsync`만, 실행 전략 · SaveChanges는 UnitOfWork). FR-06의 `Decorate`는 ADR-0017의 `TryDecorate`로 해석. 결론 본문 불변 |
| 2026-09-28 | orchestrator | S04 계획 확정: 인수 조건 해석 기록(결론 본문 불변). FR-03 스크린샷은 실행 기록 · 대시보드 DOM 덤프 · dev-certs 출력으로 대체(스크린샷은 사용자 추가 항목), 선택 스모크는 미도입 · BL. FR-10 실패 표시는 임시 Draft PR로 확인. FR-11 재현 기록 위치는 S04 스프린트 문서 증빙 절. NFR-03 "BuildingBlocks"는 Domain · Application · Infrastructure · Api 4개. NFR-04 같은 머신 재현의 warm 한계 기록. NFR-07은 S04 DoD에서 판정 |
| 2026-09-28 | orchestrator | S04-T04 인수 조건 해석 보충: FR-03 대시보드 증빙은 http 프로필 사용(이 PC dev-certs 미신뢰, BL-099). https 프로필은 dev-certs 확인 출력과 OTLP 0건 여부를 재현 차이점으로 기록, 사용자 https 재확인 절차는 S04 결과 리뷰 사용자 확인 사항 |
| 2026-09-28 | orchestrator | 토픽 회고 [RETRO-PRD-001](../retros/RETRO-PRD-001.md) 링크 추가 |
