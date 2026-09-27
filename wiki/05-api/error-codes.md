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

| T | 오류 유형 (`ErrorType`) | HTTP 상태 | 예 |
|---|---|---|---|
| 1 | 검증 실패 (Validation) | 400 | 필수 값 누락, 형식 오류, 정의되지 않은 코드값 |
| 2 | 대상 없음 (NotFound) | 404 | 없는 직원 ID |
| 3 | 충돌 (Conflict) | 409 | 중복 이메일, 동시성 충돌, 이미 처리된 요청 |
| 4 | 업무 규칙 위반 (BusinessRule) | 422 | 허용되지 않은 상태 전이, 종료된 긴급 상황 변경 |
| 5 | 인증 / 권한 (Unauthorized / Forbidden) | 401 / 403 | 토큰 없음, 권한 비트 없음 |
| 6 ~ 8 | (예비) | | |
| 9 | 외부 연동 / 내부 오류 (External / Internal) | 502 / 503 / 500 | SMS 사업자 오류, 일시적 장애 |

규칙

- 에러 코드는 `Error` 타입의 `int Code`로 정의하고, 서비스별 `<Aggregate>Errors` 정적 클래스에 모은다([코딩 컨벤션 · 예외 처리](../04-development/coding-conventions.md#예외-처리-규칙)).
- **코드의 유형 자리(T)와 `ErrorType`이 일치해야 한다.** HTTP 상태는 `ErrorType`으로 결정한다.
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

## 공통 에러 코드

BuildingBlocks가 정의하고 모든 서비스가 씁니다.

| 코드 | 유형 | HTTP | 이름 | 의미 |
|---|---|---|---|---|
| 1001 | 검증 실패 | 400 | `Common.ValidationFailed` | 요청 검증 실패 (상세는 `errors`에 필드별로) |
| 1002 | 검증 실패 | 400 | `Common.InvalidCode` | 정의되지 않은 코드값 / 비트 플래그 |
| 1003 | 검증 실패 | 400 | `Common.InvalidPaging` | 페이징 · 정렬 매개변수 오류 |
| 2001 | 대상 없음 | 404 | `Common.NotFound` | 리소스 없음 (서비스별 코드가 없을 때) |
| 3001 | 충돌 | 409 | `Common.ConcurrencyConflict` | 동시 수정 충돌 (낙관적 잠금) |
| 3002 | 충돌 | 409 | `Common.DuplicateRequest` | 같은 `Idempotency-Key`로 이미 처리됨 |
| 5001 | 인증 | 401 | `Common.Unauthenticated` | 인증 필요 |
| 5002 | 권한 | 403 | `Common.Forbidden` | 권한 없음 |
| 9001 | 내부 오류 | 500 | `Common.Unexpected` | 예상하지 못한 오류 (전역 예외 처리기) |
| 9002 | 외부 연동 | 502 | `Common.ExternalServiceFailed` | 외부 시스템 오류 |
| 9003 | 외부 연동 | 503 | `Common.TemporarilyUnavailable` | 일시적 장애 (재시도 가능) |

## 서비스별 에러 코드

> TODO: 서비스 코드가 생기면 서비스별 표를 추가합니다. 형식은 공통 표와 같습니다.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 에러 코드 체계(5자리 `S T NNN`), 유형 ↔ HTTP 대응, 로그 이벤트 ID 범위, 공통 에러 코드 |
