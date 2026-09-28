---
title: "에러 코드"
type: doc
status: draft
tags: [api]
created: 2026-09-27
updated: 2026-09-28
---

# 에러 코드

> API 에러 응답과 `Result` 실패에 쓰는 **정수 에러 코드** 체계와 목록입니다. 로그 이벤트 ID 범위도 여기서 함께 정합니다.
> 결정 근거: [ADR-0008 코드값 정수화](../03-architecture/adr/0008-integer-codes-and-bitmask.md), [ADR-0028 오류 계약 확장](../03-architecture/adr/0028-building-blocks-error-contract-extension.md) · 응답 형식: [API 설계 가이드 · 에러 응답](../04-development/api-guidelines.md#에러-응답-포맷-problemdetails)
>
> [위키 홈](../README.md)

## 에러 코드 체계

에러 코드는 **5자리 정수** `S T NNN`입니다.

```
  2   2   001
  │   │   └── NNN: 일련번호 (001~999)
  │   └────── T  : 오류 유형 (1~9)
  └────────── S  : 서비스 (0~9)
→ 22001 = Employee 서비스 / 대상 없음 / 1번
```

| S | 서비스 | 범위 |
|---|---|---|
| 0 | 공통 (BuildingBlocks) | 1001 ~ 9999 |
| 1 | Identity | 11001 ~ 19999 |
| 2 | Employee | 21001 ~ 29999 |
| 3 | Contact Network | 31001 ~ 39999 |
| 4 | Emergency | 41001 ~ 49999 |
| 5 | Notification | 51001 ~ 59999 |
| 6 ~ 8 | (예비) | |
| 9 | API Gateway | 91001 ~ 99999 |

> 서비스 구성은 [서비스 카탈로그](../03-architecture/service-catalog.md)가 확정되면 맞춥니다(현재 🟡 검토 중). 번호는 한 번 배정하면 바꾸지 않습니다.

| T | 오류 유형 (`ErrorType` = 값) | HTTP 상태 | 예 |
|---|---|---|---|
| 1 | 검증 실패 (`Validation` = 10) | 400 | 필수 값 누락, 형식 오류, 정의되지 않은 코드값 |
| 1 | 요청 본문 크기 초과 (`PayloadTooLarge` = 11) | 413 | 본문 1 MiB 초과 (S06-T01에서 추가, ADR-0028) |
| 1 | 지원하지 않는 형식 (`UnsupportedMediaType` = 12) | 415 | 지원하지 않는 Content-Type (S06-T01에서 추가, ADR-0028) |
| 2 | 대상 없음 (`NotFound` = 20) | 404 | 없는 직원 ID |
| 3 | 충돌 (`Conflict` = 30) | 409 | 중복 이메일, 동시성 충돌, 이미 처리된 요청 |
| 4 | 업무 규칙 위반 (`BusinessRule` = 40) | 422 | 허용되지 않은 상태 전이, 종료된 긴급 상황 변경 |
| 5 | 인증 필요 (`Unauthorized` = 51) | 401 | 토큰 없음, 만료된 토큰 |
| 5 | 권한 없음 (`Forbidden` = 52) | 403 | 권한 비트 없음 |
| 6 ~ 8 | (예비) | | |
| 9 | 내부 오류 (`Internal` = 91) | 500 | 처리되지 않은 예외 |
| 9 | 외부 연동 (`External` = 92) | 502 | SMS 사업자 오류 |
| 9 | 일시적 장애 (`Unavailable` = 93) | 503 | 재시도 가능한 일시 장애 |

`ErrorType`은 BuildingBlocks.Domain의 `short` enum이고 값은 2자리입니다. **`T = (short)ErrorType / 10`** 이며, 유형 자리 1 · 5 · 9는 HTTP 상태가 둘 이상이라 1의 자리로 구분합니다(T = 1은 `400` · `413` · `415`, [ADR-0028](../03-architecture/adr/0028-building-blocks-error-contract-extension.md)). `PayloadTooLarge` · `UnsupportedMediaType`는 S06-T01에서 코드에 추가하며, 그 전까지 코드의 `ErrorType`은 9종입니다. 예비 유형 자리(T = 6 ~ 8)는 쓰지 않습니다. `0`(`None`)은 코드값 규칙에 따른 예약 값이라 `Error`에 쓰지 않습니다. 배포된 값은 바꾸거나 재사용하지 않습니다.

규칙

- 에러 코드는 `Error` 타입의 `int Code`로 정의하고, 서비스별 `<Aggregate>Errors` 정적 클래스에 모은다([코딩 컨벤션 · 예외 처리](../04-development/coding-conventions.md#예외-처리-규칙)). `Error`는 유형별 팩토리(`Error.Validation` · `NotFound` · `Conflict` · `BusinessRule` · `Unauthorized` · `Forbidden` · `Internal` · `External` · `Unavailable`, S06-T01부터 `PayloadTooLarge` · `UnsupportedMediaType` 추가)로만 만든다.
- **코드의 유형 자리(T)와 `ErrorType`이 일치해야 한다(`T = 값 / 10`).** `Error`는 생성 시점에 다음을 검사하고 어기면 예외를 던진다: 범위 1001 ~ 99999(`ArgumentOutOfRangeException`), 일련번호(NNN) 000 금지, 예비 서비스 자리(S = 6 ~ 8) 금지, T ↔ `ErrorType` 불일치(각 `ArgumentException`), 빈 메시지. 규칙 위반은 프로그래밍 오류이므로 `Result`가 아니라 예외다.
- **HTTP 상태는 T가 아니라 `ErrorType`으로 정한다**(위 표). 변환은 API 계층의 공통 변환기가 한다([ADR-0016](../03-architecture/adr/0016-use-controllers-for-api.md)).
- 검증 실패는 `ValidationError`(`Error` 파생)로 표현한다. 대표 코드는 `1001`이고 필드별 상세(속성 경로, 정수 코드, 메시지)를 담으며, 필드별 코드도 검증 실패 유형(`Validation`)이어야 한다([ADR-0018](../03-architecture/adr/0018-use-fluentvalidation.md)). DB에 이미 있는 값의 행별 충돌은 상세 Conflict 오류(대표 코드와 상세 코드 모두 `Conflict` 유형, `409` `errors`)로 표현한다(S06-T01, [ADR-0028](../03-architecture/adr/0028-building-blocks-error-contract-extension.md)).
- 배포된 코드는 의미를 바꾸거나 재사용하지 않는다. 폐기하면 표에 `폐기`로 남긴다.
- 메시지(`detail`)는 사람이 읽는 설명이고 바뀔 수 있다. **클라이언트는 메시지가 아니라 코드로 분기**한다.
- 코드를 추가하는 작업은 이 문서의 표를 함께 갱신한다. reviewer는 코드와 표가 일치하는지 확인한다.

```csharp
public static class EmployeeErrors
{
    public static readonly Error NotFound =
        Error.NotFound(22001, "직원을 찾을 수 없습니다.");

    public static readonly Error DuplicateEmail =
        Error.Conflict(23001, "이미 등록된 이메일입니다.");
}
```

## 로그 이벤트 ID 범위

로그 이벤트 ID(`[LoggerMessage(EventId = ...)]`)도 같은 서비스 자리(S)를 쓰고, **유형 자리를 0으로** 둡니다. 에러 코드(T = 1~9)와 번호가 겹치지 않습니다.

| 서비스 | 로그 이벤트 ID 범위 | 예 |
|---|---|---|
| 공통 | 1 ~ 999 | 1 = 요청 처리 중 처리되지 않은 예외 |
| Identity | 10001 ~ 10999 | |
| Employee | 20001 ~ 20999 | 20001 = `EmployeeRegistered` |
| Contact Network | 30001 ~ 30999 | |
| Emergency | 40001 ~ 40999 | |
| Notification | 50001 ~ 50999 | |
| API Gateway | 90001 ~ 90999 | |

### 공통 하위 범위

공통 범위(1 ~ 999)는 BuildingBlocks 구성 요소별로 나눕니다. 각 구성 요소는 자기 하위 범위 안에서만 번호를 붙이고, 번호를 추가하면 아래 표를 함께 갱신합니다(BL-028).

| 하위 범위 | 구성 요소 | 비고 |
|---|---|---|
| 1 | 전역 예외 처리기(BuildingBlocks.Api) | 요청 처리 중 처리되지 않은 예외. 이 번호는 전역 예외 처리기에서만 쓴다(S02-T06) |
| 2 ~ 100 | (예비) | |
| 101 ~ 199 | Mediator 파이프라인(BuildingBlocks.Application) | 로깅 데코레이터 |
| 201 ~ 299 | 영속성(BuildingBlocks.Infrastructure) | UnitOfWork, 예외 변환(S02-T07) |
| 301 ~ 399 | API 공통 처리(BuildingBlocks.Api) | 예외 분류 · 잘못된 요청 · 요청 중단 · 바인딩 오류(S02-T06, 301 ~ 304 사용) |
| 400 ~ 999 | (예비) | |

### Mediator 로그 이벤트

로깅 데코레이터(`MediatorLogs`, [ADR-0015](../03-architecture/adr/0015-custom-mediator-pipeline.md))가 요청 하나에 한 줄을 남깁니다. 속성은 요청 형식 이름, 에러 코드, `ErrorType`(정수), 경과 시간뿐이고 요청 · 응답 값과 오류 메시지는 남기지 않습니다. 예외는 여기서 기록하지 않습니다(이벤트 ID 1, 전역 예외 처리기). 단위 테스트(`MediatorLogsTests`)가 이 표와 정의를 대조합니다.

| 이벤트 ID | 이름 | 수준 | 메시지 템플릿 |
|---|---|---|---|
| 101 | `CommandSucceeded` | `Debug` | `Command {RequestName} succeeded in {ElapsedMilliseconds} ms` |
| 102 | `CommandFailed` | `Information` | `Command {RequestName} failed with error {ErrorCode} of type {ErrorType} in {ElapsedMilliseconds} ms` |
| 103 | `QuerySucceeded` | `Debug` | `Query {RequestName} succeeded in {ElapsedMilliseconds} ms` |
| 104 | `QueryFailed` | `Information` | `Query {RequestName} failed with error {ErrorCode} of type {ErrorType} in {ElapsedMilliseconds} ms` |

- 실패 로그는 Handler 실패 · 검증 실패(1001) · 커밋 실패(3001 등)를 모두 포함합니다(로깅 데코레이터가 가장 바깥).
- `RequestName`은 요청 형식의 짧은 이름(`Type.Name`)입니다. `ElapsedMilliseconds`는 `TimeProvider`로 잰 밀리초(실수)입니다.

### 영속성 로그 이벤트

UnitOfWork(`PersistenceLogs`, BuildingBlocks.Infrastructure)가 영속성 예외를 `Result`로 바꿀 때 한 줄을 남깁니다([데이터베이스 · 영속성 예외 변환](../04-development/database.md#영속성-예외-변환)). 속성은 제약 이름 · SqlState · 엔티티 형식 짧은 이름(중복 제거, 쉼표로 연결) · 에러 코드뿐이고 `Detail` · `MessageText` · 값 · 키와 예외 객체는 남기지 않습니다. 단위 테스트(`PersistenceLogsTests`)가 이 표와 정의를 대조합니다.

| 이벤트 ID | 이름 | 수준 | 메시지 템플릿 |
|---|---|---|---|
| 201 | `UniqueConstraintViolationMapped` | `Debug` | `Unique constraint {ConstraintName} violated with SqlState {SqlState} on {EntityTypes}, returned error {ErrorCode}` |
| 202 | `UniqueConstraintViolationUnmapped` | `Warning` | `Unique constraint {ConstraintName} violated with SqlState {SqlState} on {EntityTypes} has no error mapping, returned error {ErrorCode}` |
| 203 | `ConcurrencyConflictDetected` | `Debug` | `Concurrency conflict on {EntityTypes}, returned error {ErrorCode}` |

- 실패 `Result`는 로깅 데코레이터가 102(`Information`)로 이미 남기므로 변환 로그는 `Information`을 쓰지 않습니다. 202는 매핑 누락 또는 성공한 커밋의 재시도 오보(TD-010, 제약 이름 `pk_...`) 신호라 `Warning`입니다.
- 재시도 로그는 따로 두지 않습니다(EF Core 실행 전략 자체 로그). 변환하지 않는 예외(23514 등)는 여기서 기록하지 않고 전역 예외 처리기(이벤트 ID 1)가 남깁니다.

### API 로그 이벤트

전역 예외 처리기와 바인딩 오류 응답(`ApiLogs`, BuildingBlocks.Api, S02-T06)이 남깁니다. 속성은 예외 형식 이름 · HTTP 메서드 · 상태 코드 · 에러 코드 · `ErrorType`(정수) · 필드 이름뿐이고, 요청 경로 · 쿼리 · 값과 **예외 메시지는 남기지 않습니다**. 예외는 메시지를 뺀 사본(형식 이름 · 스택 트레이스 · 내부 예외 사슬만, `RedactedException`)으로 넘깁니다. 변환되지 않은 DB 예외(23514 · 25006)의 메시지에 제약 이름 · SQL · 값이 들어가고, BuildingBlocks.Api는 Npgsql 형식을 몰라 골라 낼 수 없기 때문입니다([ADR-0024](../03-architecture/adr/0024-building-blocks-api-for-common-http-handling.md)). 단위 테스트(`ApiLogsTests`)가 이 표와 정의를 대조합니다.

| 이벤트 ID | 이름 | 수준 | 메시지 템플릿 |
|---|---|---|---|
| 1 | `UnhandledException` | `Error` | `Unhandled exception {ExceptionType} while processing {RequestMethod} request, returned error {ErrorCode}` |
| 301 | `ExceptionClassified` | `Warning` | `Exception {ExceptionType} classified as error {ErrorCode} of type {ErrorType}` |
| 302 | `BadHttpRequestRejected` | `Information` | `Bad HTTP request rejected with status {StatusCode}, returned error {ErrorCode}` |
| 303 | `RequestAborted` | `Information` | `Request aborted by client with exception {ExceptionType}, returned error {ErrorCode}` |
| 304 | `ModelBindingFailed` | `Debug` | `Request model binding failed for fields {FieldNames}, returned error {ErrorCode}` |

- 1은 예외 분류기(`IExceptionClassifier`)가 모두 `null`을 돌려준 예외(9001)에만 씁니다. 분류된 예외(예: 재시도 한도 초과 → 9003)는 301, `BadHttpRequestException`(400 · 1001)은 302, 클라이언트가 요청을 끊어 난 취소 · 입출력 예외는 303입니다(판정은 9001 그대로, 수준만 낮춤).
- 프레임워크 `ExceptionHandlerMiddleware`도 같은 예외를 이벤트 ID 1 · `Error`로 원본 메시지와 함께 남기므로, `AddBuildingBlocksApi`가 범주 `Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware`를 끕니다(Microsoft.Extensions.Logging 필터). Serilog를 쓰는 호스트는 같은 범주를 `MinimumLevel.Override`로도 꺼야 합니다(ServiceDefaults).

### Employee 로그 이벤트

Employee.Application `EmployeeLogs`(S03-T01)와 Employee.MigrationService `MigrationServiceLogs`(S03-T03)가 남깁니다. `EmployeeLogs`의 속성은 직원 ID뿐이고 이름 · 이메일은 남기지 않습니다([로깅 · 개인정보](../04-development/logging-observability.md#개인정보--보안)). 단위 테스트(`EmployeeLogsTests`)가 이 표와 정의를 대조합니다.

| 이벤트 ID | 이름 | 수준 | 메시지 템플릿 |
|---|---|---|---|
| 20001 | `EmployeeRegistered` | `Information` | `Employee {EmployeeId} registered` |
| 20901 | `MigrationsApplied` | `Information` | `Migrations applied for {DbContextType} in {ElapsedMilliseconds} ms` |
| 20902 | `MigrationsFailed` | `Error` | `Migrations failed for {DbContextType} with exception {ExceptionType} and SqlState {SqlState} after {ElapsedMilliseconds} ms, exit code {ExitCode}` |

- 하위 범위: 20001 ~ 20899는 Employee.Application(`EmployeeLogs`), 20901 ~ 20999는 Employee.MigrationService(`MigrationServiceLogs`, S03-T03)입니다. 단위 테스트(`MigrationServiceLogsTests`)가 20901 · 20902 정의를 대조합니다.
- 20901 · 20902는 MigrationService Worker가 적용 한 번에 한 줄만 남깁니다. 속성은 DbContext 형식 이름 · 경과 시간(밀리초) · 실패 때 예외 형식 이름 · SqlState(예외 사슬의 `DbException`, 없으면 `null`) · 종료 코드(정수, 1 실패 · 2 취소)와 예외 객체이고, 연결 문자열 · 호스트 · 사용자 · 비밀번호는 넣지 않습니다([데이터베이스 · 마이그레이션 규칙](../04-development/database.md#마이그레이션-규칙)).
- 20001은 등록 Handler가 커밋 전에 남깁니다. 커밋이 실패하면(경합 23505 → 23001 등) 로깅 데코레이터가 같은 요청의 실패(102)를 뒤이어 남기므로, 등록 확정 여부는 102 유무와 함께 봅니다.

## 공통 에러 코드

BuildingBlocks가 정의하고 모든 서비스가 씁니다.

| 코드 | `ErrorType` | HTTP | 이름 | 의미 | 상태 |
|---|---|---|---|---|---|
| 1001 | `Validation` | 400 | `Common.ValidationFailed` | 요청 검증 실패 (상세는 `errors`에 필드별로, `ValidationError`) | 사용 |
| 1002 | `Validation` | 400 | `Common.InvalidCode` | 정의되지 않은 코드값 / 비트 플래그 | 사용 |
| 1003 | `Validation` | 400 | `Common.InvalidPaging` | 페이징 · 정렬 매개변수 오류 | 사용 |
| 1004 | `PayloadTooLarge` | 413 | `Common.PayloadTooLarge` | 요청 본문이 허용 크기를 넘음 (Kestrel `BadHttpRequestException` 413, 폼 한도 초과) | 예약: S06-T01 (바인더의 폼 한도 초과는 S06-T05) |
| 1005 | `UnsupportedMediaType` | 415 | `Common.UnsupportedMediaType` | 지원하지 않는 요청 Content-Type (`[Consumes]` 불일치) | 예약: S06-T01 |
| 2001 | `NotFound` | 404 | `Common.NotFound` | 리소스 없음 (서비스별 코드가 없을 때) | 사용 |
| 3001 | `Conflict` | 409 | `Common.ConcurrencyConflict` | 동시 수정 충돌 (낙관적 잠금) | 사용 |
| 3002 | `Conflict` | 409 | `Common.DuplicateRequest` | 같은 `Idempotency-Key`로 이미 처리됨 | 사용 |
| 3003 | `Conflict` | 409 | `Common.UniqueConstraintViolated` | 매핑 없는 유니크 제약 위반 (PostgreSQL `23505`, 서비스 매핑이 있으면 서비스 코드) | 사용 |
| 5001 | `Unauthorized` | 401 | `Common.Unauthenticated` | 인증 필요 | 사용 |
| 5002 | `Forbidden` | 403 | `Common.Forbidden` | 권한 없음 | 사용 |
| 9001 | `Internal` | 500 | `Common.Unexpected` | 예상하지 못한 오류 (전역 예외 처리기) | 사용 |
| 9002 | `External` | 502 | `Common.ExternalServiceFailed` | 외부 시스템 오류 | 사용 |
| 9003 | `Unavailable` | 503 | `Common.TemporarilyUnavailable` | 일시적 장애 (재시도 가능) | 사용 |

이름 `Common.X`는 BuildingBlocks.Domain `CommonErrors.X` 필드입니다. 단위 테스트(`CommonErrorsTests`)가 이 표의 `사용` 행의 코드 · 유형과 필드 목록을 전수 대조합니다. `예약` 행은 적힌 작업에서 코드에 추가하고 `사용`으로 바꿉니다(코드 · 표 대조에서 예약 행은 따로 셉니다). 1004 · 1005의 근거는 [ADR-0028](../03-architecture/adr/0028-building-blocks-error-contract-extension.md)입니다.

## 서비스별 에러 코드

서비스 코드가 생기면 서비스별 표를 추가합니다. 형식은 공통 표와 같습니다.

### Employee 에러 코드

Employee.Domain `EmployeeErrors`가 정의합니다(S03-T01 선배정, PRD-002 S05-T02 재배정). 단위 테스트(`EmployeeErrorsTests`)가 이 표의 `사용` 행의 코드 · 유형과 필드 목록을 전수 대조합니다. `예약` 행은 적힌 작업에서 상수를 추가하고 `사용`으로 바꾸며, `폐기` 행은 적힌 작업에서 상수를 지웁니다. 번호는 폐기해도 재사용하지 않습니다(코드 · 표 대조에서 예약 · 폐기 행은 따로 셉니다). 새 코드의 근거는 [ADR-0026](../03-architecture/adr/0026-employee-bulk-import-input-processing.md) · [ADR-0027](../03-architecture/adr/0027-case-insensitive-unique-email-with-normalized-column.md)입니다.

| 코드 | `ErrorType` | HTTP | 이름 | 의미 | 상태 |
|---|---|---|---|---|---|
| 21001 | `Validation` | 400 | `Employee.DisplayNameRequired` | PRD-001 샘플 `displayName` 필수 | 폐기 (PRD-002, 상수 삭제 S05-T04) |
| 21002 | `Validation` | 400 | `Employee.DisplayNameTooLong` | PRD-001 샘플 `displayName` 길이 초과 | 폐기 (PRD-002, 상수 삭제 S05-T04) |
| 21003 | `Validation` | 400 | `Employee.EmailRequired` | `email` 필수 (누락 · 빈 값 · 공백만). 판정 원본은 Email Value Object `Create` | 사용 |
| 21004 | `Validation` | 400 | `Employee.EmailInvalid` | `email` 형식 오류. 앞뒤 공백 제거 뒤 `@`가 정확히 하나, 공백 없음, domain에 `.` 포함, 첫 · 끝 `.`과 연속 `.` 거부(예: `a@.com` · `a@com.` · `a@b..c`). 판정 원본은 Email Value Object | 사용 |
| 21005 | `Validation` | 400 | `Employee.EmailTooLong` | `email` 길이 초과 (앞뒤 공백 제거 뒤 254자 초과). 판정 원본은 Email Value Object | 사용 |
| 21006 | `Validation` | 400 | `Employee.EmployeeStatusRequired` | PRD-001 샘플 `employeeStatus` 필수 (상태는 API에 노출하지 않음, 등록 시 Active 고정) | 폐기 (PRD-002, 상수 삭제 S05-T04) |
| 21007 | `Validation` | 400 | `Employee.NameRequired` | `name` 필수 (누락 · 빈 값 · 공백만). `GET /api/employee/{name}`의 공백 제거 뒤 빈 이름(400)에도 쓴다(S07-T02). 판정 원본은 Name Value Object | 사용 |
| 21008 | `Validation` | 400 | `Employee.NameTooLong` | `name` 길이 초과 (앞뒤 공백 제거 + NFC 뒤 UTF-16 100자 초과). 판정 원본은 Name Value Object | 사용 |
| 21009 | `Validation` | 400 | `Employee.NameInvalidCharacter` | `name`에 허용하지 않는 문자가 있음: 제어 문자(Cc, `char.IsControl`, 탭 포함) 또는 짝 없는 서로게이트. 판정 원본은 Name Value Object | 사용 |
| 21010 | `Validation` | 400 | `Employee.PhoneNumberRequired` | `tel` 필수 (누락 · 빈 값 · 공백만). 판정 원본은 PhoneNumber Value Object | 사용 |
| 21011 | `Validation` | 400 | `Employee.PhoneNumberInvalidCharacter` | `tel`에 ASCII 숫자와 `-` 밖의 문자가 있음 (`+` · 공백 포함). 판정 원본은 PhoneNumber Value Object | 사용 |
| 21012 | `Validation` | 400 | `Employee.PhoneNumberDigitCountOutOfRange` | `tel` 숫자 자리 수가 8 ~ 15 밖. 판정 원본은 PhoneNumber Value Object | 사용 |
| 21013 | `Validation` | 400 | `Employee.PhoneNumberTooLong` | `tel` 전체 길이 20자 초과. 판정 원본은 PhoneNumber Value Object | 사용 |
| 21014 | `Validation` | 400 | `Employee.PhoneNumberInvalidHyphen` | `tel` 맨 앞 · 맨 뒤 · 연속 하이픈. 판정 원본은 PhoneNumber Value Object | 사용 |
| 21015 | `Validation` | 400 | `Employee.JoinedOnRequired` | `joined` 필수 (누락 · 빈 값 · 공백만). 판정 원본은 JoinedOn Value Object | 사용 |
| 21016 | `Validation` | 400 | `Employee.JoinedOnInvalidFormat` | `joined`가 `yyyy-MM-dd` 정확 형식 · 있는 날짜가 아님 (`2000-2-3` · `2000-02-30`). 판정 원본은 JoinedOn Value Object | 사용 |
| 21017 | `Validation` | 400 | `Employee.JoinedOnTooEarly` | `joined`가 1900-01-01 이전 (`1899-12-31`). 판정 원본은 JoinedOn Value Object | 사용 |
| 21018 | `Validation` | 400 | `Employee.DuplicateEmailInRequest` | 같은 요청 안 이메일 중복 (`NormalizedEmail` 서수 비교, 두 행 모두 표시) | 예약: S06-T04 |
| 21019 | `Validation` | 400 | `Employee.CsvColumnCountMismatch` | CSV 행의 열 개수가 4가 아님 | 예약: S06-T02 |
| 21020 | `Validation` | 400 | `Employee.CsvUnclosedQuote` | CSV 닫히지 않은 따옴표 | 예약: S06-T02 |
| 21021 | `Validation` | 400 | `Employee.CsvUnexpectedQuote` | CSV 따옴표 없는 필드 안의 `"` | 예약: S06-T02 |
| 21022 | `Validation` | 400 | `Employee.ImportInvalidUtf8` | 입력이 올바른 UTF-8이 아님 (CP949 등, 경로 `""`) | 예약: S06-T02 |
| 21023 | `Validation` | 400 | `Employee.JsonSyntaxInvalid` | JSON 문법 오류 (끝 쉼표 · 주석 · `[..],[..]`, 경로 `""`) | 예약: S06-T03 |
| 21024 | `Validation` | 400 | `Employee.JsonItemNotObject` | JSON 항목이 객체가 아님 (`null` · 숫자, 경로 `rows[n]`) | 예약: S06-T03 |
| 21025 | `Validation` | 400 | `Employee.JsonValueNotString` | JSON 항목의 필드 값이 문자열이 아님 (`"joined": 20000101` 등) | 예약: S06-T03 |
| 21026 | `Validation` | 400 | `Employee.JsonDuplicateProperty` | JSON 항목에 대소문자만 다른 중복 속성 | 예약: S06-T03 |
| 21027 | `Validation` | 400 | `Employee.ImportTooManyRows` | 행 수가 1,000을 넘음 (경로 `""`) | 예약: S06-T02 · S06-T03 |
| 21028 | `Validation` | 400 | `Employee.ImportInputEmpty` | 빈 입력 (입력 없음, 길이 0, BOM이나 공백만 있음, form-urlencoded `data` 키 없음) | 예약: S06-T04 |
| 21029 | `Validation` | 400 | `Employee.ImportMultipleSources` | multipart `file`과 `data`를 함께 보냄 | 예약: S06-T04 |
| 21030 | `Validation` | 400 | `Employee.RowErrorsTruncated` | 행 오류가 100개를 넘어 잘림 (101번째 항목, 경로 `""`) | 예약: S06-T04 |
| 22001 | `NotFound` | 404 | `Employee.NotFound` | 직원 없음 (이름 조회에 일치하는 직원 없음, S07-T02) | 사용 |
| 23001 | `Conflict` | 409 | `Employee.DuplicateEmail` | 이메일 중복 (`normalized_email` = Email Value Object의 `ToLowerInvariant` 값 기준). DB 중복 사전 조회(`409` + 충돌 행 번호, 상세 Conflict 오류)와 유니크 인덱스 `ux_employees_normalized_email` 위반(`23505`, 행 번호 없음) 매핑이 같은 인스턴스를 씀. 매핑 이름 교체는 S05-T04, 그 전까지는 `ux_employees_email` | 사용 |
| 23002 | `Conflict` | 409 | `Employee.RowConflictsTruncated` | 행 충돌이 100개를 넘어 잘림 (101번째 항목, 경로 `""`) | 예약: S06-T04 |

- 필드 코드(21003 ~ 21005, 21007 ~ 21017)는 Employee Value Object `Create(string?)`가 돌려주는 `Result`의 오류이고, 일괄 등록 Handler가 행 경로(`rows[n].email` 등, 1부터)로 `ValidationError`(1001)의 `errors`에 옮깁니다([ADR-0026](../03-architecture/adr/0026-employee-bulk-import-input-processing.md) 7 · 8절). 길이는 `string.Length`(UTF-16 코드 단위)로 잽니다(이모지는 한 글자가 2).
- 파싱 · 입력 코드(21019 ~ 21030)도 같은 `errors`에 담깁니다. 요청 전체 오류(21022 · 21023 · 21027 · 21030)의 경로는 `""`, 행 전체 오류(21019 ~ 21021, 21024)는 `rows[n]`입니다.
- 한 필드 안에서는 첫 실패만 보고합니다. S05-T02 이전 샘플의 판정 순서(`RegisterEmployeeCommandValidator`, 이름: 필수 → 길이, 이메일: 필수 → 길이 → 형식)는 S05-T04에서 샘플과 함께 없어지고, 새 순서는 Value Object가 정합니다(S05-T03): name 21007 → 21009 → 21008, email 21003 → 21005 → 21004, tel 21010 → 21011 → 21013 → 21014 → 21012, joined 21015 → 21016 → 21017(`Name` · `Email` · `PhoneNumber` · `JoinedOn` 문서 주석과 단위 테스트가 원본).

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 에러 코드 체계(5자리 `S T NNN`), 유형 ↔ HTTP 대응, 로그 이벤트 ID 범위, 공통 에러 코드 |
| 2026-09-27 | developer | `ErrorType` 2자리 값(10 · 20 · 30 · 40 · 51 · 52 · 91 · 92 · 93)과 `T = 값 / 10`, HTTP 상태는 `ErrorType`으로 정함, `Error` 생성 시 검증 규칙(범위 · NNN 000 · 예비 S · T 불일치), `ValidationError`, 공통 코드 표에 `ErrorType` · `CommonErrors` 대응 (S01-T06) |
| 2026-09-27 | developer | 공통 로그 이벤트 ID 하위 범위(1 전역 예외, 101~199 Mediator, 201~299 영속성, 301~399 API)와 Mediator 로그 이벤트 101~104 (S02-T02, BL-028) |
| 2026-09-27 | developer | 공통 코드 3003 `Common.UniqueConstraintViolated`(BL-019), 영속성 로그 이벤트 201~203 (S02-T07) |
| 2026-09-27 | developer | API 로그 이벤트 1 · 301~304(예외 메시지 미기록, 프레임워크 예외 미들웨어 로그 끄기), 공통 하위 범위 표 비고 갱신. 새 에러 코드 할당 없음(BL-052) (S02-T06) |
| 2026-09-27 | developer | Employee 에러 코드 표(21001 ~ 21006, 22001, 23001)와 Employee 로그 이벤트 20001 `EmployeeRegistered` (S03-T01) |
| 2026-09-27 | developer | Employee 로그 하위 범위(20001 ~ 20899 Application, 20901 ~ 20999 MigrationService)와 MigrationService 로그 이벤트 20901 `MigrationsApplied` · 20902 `MigrationsFailed` (S03-T03) |
| 2026-09-28 | developer | ADR-0026 · 0027 · 0028 반영: `ErrorType` `PayloadTooLarge` = 11 · `UnsupportedMediaType` = 12(T = 1, 413 · 415), 공통 1004 · 1005 예약(S06-T01), 공통 · Employee 표에 상태 열(사용 · 예약 · 폐기), Employee 21001 · 21002 · 21006 폐기, 21003 ~ 21005 · 22001 · 23001 설명 갱신(23001은 `ux_employees_normalized_email`), 새 코드 21007 ~ 21030 · 23002 예약(행마다 구현 작업 ID) (S05-T02) |
| 2026-09-28 | developer | Employee 21007 ~ 21017 상태를 예약 → 사용(Value Object `Name` · `PhoneNumber` · `JoinedOn`, 설명에 판정 원본 추가), 21003 ~ 21005 설명의 `(S05-T03부터)` 삭제, 필드별 판정 순서를 "첫 실패만 보고" 항목에 추가 (S05-T03) |
