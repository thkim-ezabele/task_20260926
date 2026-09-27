---
title: "에러 코드"
type: doc
status: draft
tags: [api]
created: 2026-09-27
updated: 2026-09-27
---

# 에러 코드

> API 에러 응답과 `Result` 실패에 쓰는 **정수 에러 코드** 체계와 목록입니다. 로그 이벤트 ID 범위도 여기서 함께 정합니다.
> 결정 근거: [ADR-0008 코드값 정수화](../03-architecture/adr/0008-integer-codes-and-bitmask.md) · 응답 형식: [API 설계 가이드 · 에러 응답](../04-development/api-guidelines.md#에러-응답-포맷-problemdetails)
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
| 2 | 대상 없음 (`NotFound` = 20) | 404 | 없는 직원 ID |
| 3 | 충돌 (`Conflict` = 30) | 409 | 중복 이메일, 동시성 충돌, 이미 처리된 요청 |
| 4 | 업무 규칙 위반 (`BusinessRule` = 40) | 422 | 허용되지 않은 상태 전이, 종료된 긴급 상황 변경 |
| 5 | 인증 필요 (`Unauthorized` = 51) | 401 | 토큰 없음, 만료된 토큰 |
| 5 | 권한 없음 (`Forbidden` = 52) | 403 | 권한 비트 없음 |
| 6 ~ 8 | (예비) | | |
| 9 | 내부 오류 (`Internal` = 91) | 500 | 처리되지 않은 예외 |
| 9 | 외부 연동 (`External` = 92) | 502 | SMS 사업자 오류 |
| 9 | 일시적 장애 (`Unavailable` = 93) | 503 | 재시도 가능한 일시 장애 |

`ErrorType`은 BuildingBlocks.Domain의 `short` enum이고 값은 2자리입니다. **`T = (short)ErrorType / 10`** 이며, 유형 자리 5와 9는 HTTP 상태가 둘 이상이라 1의 자리로 구분합니다. `0`(`None`)은 코드값 규칙에 따른 예약 값이라 `Error`에 쓰지 않습니다. 배포된 값은 바꾸거나 재사용하지 않습니다.

규칙

- 에러 코드는 `Error` 타입의 `int Code`로 정의하고, 서비스별 `<Aggregate>Errors` 정적 클래스에 모은다([코딩 컨벤션 · 예외 처리](../04-development/coding-conventions.md#예외-처리-규칙)). `Error`는 유형별 팩토리(`Error.Validation` · `NotFound` · `Conflict` · `BusinessRule` · `Unauthorized` · `Forbidden` · `Internal` · `External` · `Unavailable`)로만 만든다.
- **코드의 유형 자리(T)와 `ErrorType`이 일치해야 한다(`T = 값 / 10`).** `Error`는 생성 시점에 다음을 검사하고 어기면 예외를 던진다: 범위 1001 ~ 99999(`ArgumentOutOfRangeException`), 일련번호(NNN) 000 금지, 예비 서비스 자리(S = 6 ~ 8) 금지, T ↔ `ErrorType` 불일치(각 `ArgumentException`), 빈 메시지. 규칙 위반은 프로그래밍 오류이므로 `Result`가 아니라 예외다.
- **HTTP 상태는 T가 아니라 `ErrorType`으로 정한다**(위 표). 변환은 API 계층의 공통 변환기가 한다([ADR-0016](../03-architecture/adr/0016-use-controllers-for-api.md)).
- 검증 실패는 `ValidationError`(`Error` 파생)로 표현한다. 대표 코드는 `1001`이고 필드별 상세(속성 경로, 정수 코드, 메시지)를 담으며, 필드별 코드도 검증 실패 유형(T = 1)이어야 한다([ADR-0018](../03-architecture/adr/0018-use-fluentvalidation.md)).
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
| 301 ~ 399 | API 공통 처리(BuildingBlocks.Api) | ProblemDetails 변환, 바인딩 오류(S02-T06) |
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

## 공통 에러 코드

BuildingBlocks가 정의하고 모든 서비스가 씁니다.

| 코드 | `ErrorType` | HTTP | 이름 | 의미 |
|---|---|---|---|---|
| 1001 | `Validation` | 400 | `Common.ValidationFailed` | 요청 검증 실패 (상세는 `errors`에 필드별로, `ValidationError`) |
| 1002 | `Validation` | 400 | `Common.InvalidCode` | 정의되지 않은 코드값 / 비트 플래그 |
| 1003 | `Validation` | 400 | `Common.InvalidPaging` | 페이징 · 정렬 매개변수 오류 |
| 2001 | `NotFound` | 404 | `Common.NotFound` | 리소스 없음 (서비스별 코드가 없을 때) |
| 3001 | `Conflict` | 409 | `Common.ConcurrencyConflict` | 동시 수정 충돌 (낙관적 잠금) |
| 3002 | `Conflict` | 409 | `Common.DuplicateRequest` | 같은 `Idempotency-Key`로 이미 처리됨 |
| 3003 | `Conflict` | 409 | `Common.UniqueConstraintViolated` | 매핑 없는 유니크 제약 위반 (PostgreSQL `23505`, 서비스 매핑이 있으면 서비스 코드) |
| 5001 | `Unauthorized` | 401 | `Common.Unauthenticated` | 인증 필요 |
| 5002 | `Forbidden` | 403 | `Common.Forbidden` | 권한 없음 |
| 9001 | `Internal` | 500 | `Common.Unexpected` | 예상하지 못한 오류 (전역 예외 처리기) |
| 9002 | `External` | 502 | `Common.ExternalServiceFailed` | 외부 시스템 오류 |
| 9003 | `Unavailable` | 503 | `Common.TemporarilyUnavailable` | 일시적 장애 (재시도 가능) |

이름 `Common.X`는 BuildingBlocks.Domain `CommonErrors.X` 필드입니다. 단위 테스트(`CommonErrorsTests`)가 이 표의 코드 · 유형과 필드 목록을 전수 대조합니다.

## 서비스별 에러 코드

> TODO: 서비스 코드가 생기면 서비스별 표를 추가합니다. 형식은 공통 표와 같습니다.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 에러 코드 체계(5자리 `S T NNN`), 유형 ↔ HTTP 대응, 로그 이벤트 ID 범위, 공통 에러 코드 |
| 2026-09-27 | developer | `ErrorType` 2자리 값(10 · 20 · 30 · 40 · 51 · 52 · 91 · 92 · 93)과 `T = 값 / 10`, HTTP 상태는 `ErrorType`으로 정함, `Error` 생성 시 검증 규칙(범위 · NNN 000 · 예비 S · T 불일치), `ValidationError`, 공통 코드 표에 `ErrorType` · `CommonErrors` 대응 (S01-T06) |
| 2026-09-27 | developer | 공통 로그 이벤트 ID 하위 범위(1 전역 예외, 101~199 Mediator, 201~299 영속성, 301~399 API)와 Mediator 로그 이벤트 101~104 (S02-T02, BL-028) |
| 2026-09-27 | developer | 공통 코드 3003 `Common.UniqueConstraintViolated`(BL-019), 영속성 로그 이벤트 201~203 (S02-T07) |
