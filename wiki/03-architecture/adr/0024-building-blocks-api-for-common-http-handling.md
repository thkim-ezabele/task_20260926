---
title: "ADR-0024: 공통 API 처리 계층 BuildingBlocks.Api 신설"
type: adr
adr: "0024"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0024]
tags: [adr, architecture, api]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0024: 공통 API 처리 계층 BuildingBlocks.Api 신설

## 배경 (Context)

- FR-07은 `Result` → RFC 9457 `ProblemDetails`(정수 `code`, `traceId`) 변환, `InvalidModelStateResponseFactory`(바인딩 오류 `1001`), 전역 예외 처리(`IExceptionHandler`), Controller 기반 설정, Swashbuckle을 요구한다([PRD-001](../../10-delivery/prd/PRD-001-foundation.md), [API 설계](../../04-development/api-guidelines.md), [에러 코드](../../05-api/error-codes.md)).
- [ADR-0016](0016-use-controllers-for-api.md)과 [ADR-0019](0019-use-swashbuckle-openapi.md)는 이 공통 설정을 서비스마다 복사하지 않고 **공통 위치 하나**에 두기로 했지만, 위치(메모 N3)는 정하지 않았다.
- BuildingBlocks는 지금 Domain / Application / Infrastructure 세 프로젝트만 계획되어 있어 ASP.NET Core 코드를 둘 곳이 없다([Clean Architecture · 공통 빌딩 블록](../clean-architecture.md#공통-빌딩-블록-buildingblocks--shared-kernel)).
- Infrastructure 계열은 Api 호스트뿐 아니라 **MigrationService**(Worker)도 참조한다. MigrationService는 쓰기 DbContext로 `MigrateAsync`만 실행한다([ADR-0012](0012-migration-apply-and-pre-production-reset.md)).
- 23505 → `Result`(3003 `Common.UniqueConstraintViolated`) 변환은 Infrastructure가 제약 이름 매핑으로 처리하고, 23514는 변환하지 않고 예외로 올린다([ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md), S02-T07). 따라서 공통 API 처리에는 EF Core · Npgsql 타입이 필요하지 않아야 한다.
- 스프린트 S02 계획 리뷰에서 사용자는 N3를 "BuildingBlocks.Api 신설, ADR-0024로 기록"으로 승인했다([S02 계획 리뷰](../../10-delivery/sprints/S02-building-blocks.md#사용자-승인-2026-09-27)).

## 검토한 대안 (Options)

1. **BuildingBlocks.Infrastructure에 ASP.NET Core 의존 추가**: 장점: 프로젝트가 늘지 않는다. 단점: Infrastructure를 참조하는 MigrationService와 Infrastructure 단위 테스트까지 MVC · Swashbuckle · `ProblemDetails` 변환 코드(웹 API 의존)가 번진다. Infrastructure가 HTTP 응답 형식을 알게 되어 "영속성 · 외부 연동 구현"이라는 레이어 책임이 섞이고, "Infrastructure ↛ ASP.NET Core", "공통 API 처리 ↛ EF Core · Npgsql" 같은 규칙을 아키텍처 테스트로 걸 수 없다.
2. **EmergencyHub.ServiceDefaults에 둠**: 장점: 이미 Api 호스트가 참조하는 공통 프로젝트다. 단점: ServiceDefaults는 MigrationService도 참조하고([ADR-0020](0020-logging-with-serilog-and-otlp.md)), 책임이 호스팅 · 관측(헬스체크, OpenTelemetry, Serilog, 서비스 디스커버리)이라 HTTP API 규약(에러 형식, 바인딩, 문서화)과 성격이 다르다. Aspire 템플릿과의 차이도 더 커진다.
3. **서비스 Api 프로젝트마다 구현**: 장점: 서비스별로 자유롭다. 단점: ADR-0016 · 0019의 "공통 위치 하나" 결정과 어긋나고, 서비스가 늘수록 에러 응답 형식이 갈라진다.
4. **BuildingBlocks.Api 신설**: 장점: 웹 의존이 서비스 Api 레이어에만 머문다. Infrastructure 비참조를 규칙으로 걸 수 있어 공통 API 처리가 DB 기술을 모른다는 것이 테스트로 보장된다. 공통 규약을 한 곳에서 단위 테스트한다. 단점: 프로젝트와 테스트 프로젝트가 하나씩 늘고, EF Core 예외(`RetryLimitExceededException`)처럼 Infrastructure만 아는 예외를 분류할 경로가 따로 필요하다.

## 결정 (Decision)

**대안 4: `EmergencyHub.BuildingBlocks.Api`를 새로 만들고, 공통 API 처리는 모두 이 프로젝트에 둔다.**

### 프로젝트

- 위치: `src/BuildingBlocks/EmergencyHub.BuildingBlocks.Api/`. SDK는 `Microsoft.NET.Sdk`(클래스 라이브러리) + `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, 패키지는 `Swashbuckle.AspNetCore`(10.2.3, 중앙 버전 관리).
- BuildingBlocks이므로 `GenerateDocumentationFile` + CS1591 오류화 대상이다(FR-02).
- **참조**: `BuildingBlocks.Application`, `BuildingBlocks.Domain`만 참조한다. **`BuildingBlocks.Infrastructure`는 참조하지 않고**, EF Core · Npgsql 패키지와 그 타입(`DbUpdateException`, `PostgresException`, `SqlState` 등)도 쓰지 않는다.
- **참조하는 쪽**: 서비스 Api 레이어(`EmergencyHub.<Service>.Api`)만 참조한다. MigrationService · Infrastructure · Application · Domain · ServiceDefaults는 참조하지 않는다.

### 담는 것 / 담지 않는 것

| 담는 것 | 담지 않는 것 (위치) |
|---|---|
| `Result` / `Error` → `ProblemDetails` 변환(정수 `code`, `traceId`, `ValidationError` → `errors`), `ErrorType` → HTTP 상태 대응 | 23505 · 동시성 예외 → `Result` 변환 (Infrastructure, S02-T07) |
| `InvalidModelStateResponseFactory`(`1001`) | enum `1002` 규칙 · `WithError` 등 FluentValidation 확장 (BuildingBlocks.Application) |
| 전역 `IExceptionHandler`(예상 못한 예외 → `9001`, 로그 이벤트 ID 1) | `AddConventionalServices` · Scrutor (BuildingBlocks.Infrastructure, [ADR-0017](0017-scrutor-for-convention-based-di.md)) |
| Controller 기본 설정(`AddControllers` 옵션, `SuppressImplicitRequired...`, System.Text.Json 정수 enum) | DbContext 등록 · 연결 문자열 주입 (서비스 Infrastructure / Api 호스트, [ADR-0011](0011-use-aspire-local-orchestration.md)) |
| Swashbuckle 설정(문서 `v1`, 정수 enum 설명 필터, ProblemDetails 스키마, Development에서만 노출) | Serilog · OpenTelemetry · 헬스체크 (ServiceDefaults, [ADR-0020](0020-logging-with-serilog-and-otlp.md)) |
| 공개 진입점 확장 메서드(서비스 컬렉션용 등록 1개, `WebApplication` 파이프라인용 1개. 이름은 S02-T06에서 정함) | Controller · 업무 에러 코드 (서비스 Api / Domain) |

- `traceId`는 `Activity.Current?.TraceId`, 없으면 `HttpContext.TraceIdentifier`를 쓴다.
- 23514 · 25006 등 변환되지 않은 DB 예외는 일반 예외와 똑같이 `9001`로 응답한다. 응답과 로그에 제약 이름 · SQL · 파라미터 값을 넣지 않는다([ADR-0020](0020-logging-with-serilog-and-otlp.md)). 스택 트레이스는 응답에 넣지 않는다.

### Infrastructure 예외 분류 경로

`RetryLimitExceededException`(EF Core) → `9003`처럼 **Infrastructure 타입을 알아야 하는 예외 분류**는 포트로 뒤집는다(분류 포트 방식으로 확정).

- `BuildingBlocks.Application`에 예외 → `Error?` 분류 인터페이스를 둔다(이름은 S02-T01 / T06에서 정함. 분류하지 못하면 `null`).
- 구현은 `BuildingBlocks.Infrastructure`에 두고(`RetryLimitExceededException` → `Common.TemporarilyUnavailable` 9003, 그 밖은 `null`), Infrastructure 공통 등록 확장이 등록한다(S02-T07).
- BuildingBlocks.Api의 `IExceptionHandler`는 등록된 분류기를 차례로 묻고, 결과가 없으면 `9001`로 응답한다. 분류기가 없어도 동작한다(모두 `9001`).
- 타입 이름 문자열 비교로 Infrastructure 타입을 판별하지 않는다.

### 의존성 규칙 표 (초안)

S02-T05 아키텍처 테스트의 규칙 원본이다. [Clean Architecture · 의존성 규칙](../clean-architecture.md#의존성-규칙)에는 S04-T02에서 반영한다. 굵게 표시한 행 · 항목이 이 ADR로 추가된다.

| 프로젝트 | 참조 가능 | 참조 금지 (타입 의존 포함) |
|---|---|---|
| BuildingBlocks.Domain | (없음) | 다른 모든 프로젝트, EF Core, ASP.NET Core, 직렬화 라이브러리 |
| BuildingBlocks.Application | BuildingBlocks.Domain, FluentValidation, `Microsoft.Extensions.*.Abstractions` | BuildingBlocks.Infrastructure · Api, EF Core, Npgsql, Scrutor, ASP.NET Core |
| BuildingBlocks.Infrastructure | BuildingBlocks.Application · Domain, EF Core, Npgsql, Scrutor | **BuildingBlocks.Api, ASP.NET Core(`Microsoft.AspNetCore.*`)** |
| **BuildingBlocks.Api** | **BuildingBlocks.Application · Domain, ASP.NET Core, Swashbuckle** | **BuildingBlocks.Infrastructure, EF Core, Npgsql** |
| `<Service>.Domain` | BuildingBlocks.Domain | 그 밖의 모든 프로젝트 |
| `<Service>.Application` | `<Service>.Domain`, BuildingBlocks.Application · Domain | Infrastructure 계열, **Api 계열**, EF Core, ASP.NET Core |
| `<Service>.Infrastructure` | `<Service>.Application` · Domain, BuildingBlocks.Infrastructure · Application · Domain | `<Service>.Api`, **BuildingBlocks.Api, ASP.NET Core** |
| `<Service>.Api` | `<Service>.Application`, `<Service>.Infrastructure`(DI 등록만), **BuildingBlocks.Api**, ServiceDefaults | Controller에서 Infrastructure 타입 · Repository 사용 |
| `<Service>.MigrationService` | `<Service>.Infrastructure`, ServiceDefaults | `<Service>.Api`, **BuildingBlocks.Api** |

- 서비스끼리는 프로젝트를 참조하지 않는다(기존 규칙 유지).
- "BuildingBlocks.Api ↛ BuildingBlocks.Infrastructure · EF Core · Npgsql"와 "Infrastructure 계열(BuildingBlocks · `<Service>`) ↛ ASP.NET Core(`Microsoft.AspNetCore.*`)"는 문서 규칙에 그치지 않고 **아키텍처 테스트로 강제**한다(S02-T05).
- 아키텍처 테스트는 규칙마다 1개로 만들고 대상 타입이 1개 이상임을 단언한다(S02-T05). NetArchTest는 타입 의존을 검사하므로, 쓰지 않는 프로젝트 참조까지 막을지는 S02-T05에서 정한다.

## 결과 (Consequences)

- 긍정: 웹 API 의존(MVC · Swashbuckle · `ProblemDetails` 변환)이 서비스 Api 레이어에만 머물고, MigrationService와 Infrastructure는 HTTP 규약을 모른다. **"BuildingBlocks.Api ↛ Infrastructure" 규칙이 공통 API 처리가 `DbUpdateException` · `PostgresException` · `SqlState` 같은 EF / Npgsql 타입을 참조하지 않음을 보장**하므로, 23505 변환 책임이 Infrastructure(ADR-0014)에만 있다는 것이 아키텍처 테스트로 확인된다. 에러 응답 형식을 한 곳에서 단위 테스트한다.
- 부정: 프로젝트(`EmergencyHub.BuildingBlocks.Api`)와 테스트 프로젝트(`EmergencyHub.BuildingBlocks.Api.UnitTests`)가 하나씩 늘어 솔루션 · CI 빌드 대상이 커진다. Infrastructure 예외 분류를 위해 Application에 포트 하나와 Infrastructure 구현 하나가 더 생긴다(S02-T07 범위 증가).
- 참고: ServiceDefaults(Aspire 템플릿)도 ASP.NET Core 공유 프레임워크를 참조하므로 MigrationService의 의존 폐쇄에는 이미 `Microsoft.AspNetCore.App`이 있다. 이 결정이 막는 것은 공유 프레임워크 자체가 아니라, MigrationService와 Infrastructure로 **웹 API 규약 코드와 Swashbuckle**이 번지는 것이다.
- 후속:
  - S02-T06: 이 프로젝트와 `EmergencyHub.BuildingBlocks.Api.UnitTests`(sln 등록) 생성, 공개 진입점 이름 확정, `IExceptionHandler`에서 분류기 사용.
  - S02-T01 / T07: 예외 분류 포트(Application) · 구현(Infrastructure) 추가.
  - S02-T05: 위 의존성 규칙 표를 규칙 원본으로 아키텍처 테스트 작성(`Api ↛ Infrastructure`, `Infrastructure ↛ ASP.NET Core` 포함).
  - [PRD-001](../../10-delivery/prd/PRD-001-foundation.md) FR-09 테스트 프로젝트 목록에 `EmergencyHub.BuildingBlocks.Api.UnitTests` 비고(이 ADR 작성 시).
  - S04-T02: [Clean Architecture](../clean-architecture.md)의 레이어 표 · 의존성 규칙 · 디렉터리 구조 · 공통 빌딩 블록 표에 BuildingBlocks.Api 반영.
