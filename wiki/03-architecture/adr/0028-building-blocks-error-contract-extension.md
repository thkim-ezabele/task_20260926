---
title: "ADR-0028: BuildingBlocks 오류 계약 확장 (상세 Conflict 오류, 413 · 415)"
type: adr
adr: "0028"
status: accepted
date: 2026-09-28
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0028]
tags: [adr, architecture, api]
created: 2026-09-28
updated: 2026-09-28
---

# ADR-0028: BuildingBlocks 오류 계약 확장 (상세 Conflict 오류, 413 · 415)

## 배경 (Context)

- 일괄 등록은 DB에 이미 있는 이메일을 `409` + **충돌 행 번호**로 알려야 하고(FR-06, Q14), `POST /api/employee`는 본문 1 MiB 초과를 `413`, 지원하지 않는 Content-Type을 `415`로 공통 정수 코드와 함께 돌려줘야 한다(FR-05 입력 경로 표, [ADR-0025](0025-api-rule-exceptions-for-assignment-endpoints.md), Q15).
- 지금 계약(S05-T01 확인, 코드 `src/BuildingBlocks`)으로는 표현할 수 없다.
  - `FieldError.Create`는 검증 실패(`ErrorType.Validation`) 유형의 오류만 받는다. 409 행 오류를 담을 수 없다.
  - `ValidationError`는 `sealed`이고 대표 코드가 `1001`로 고정이다.
  - `ErrorProblemDetails`는 `errors` 확장을 `ValidationError`에만 넣는다.
  - `ErrorStatusCodes`는 `ErrorType` 9종(`Validation` · `NotFound` · `Conflict` · `BusinessRule` · `Unauthorized` · `Forbidden` · `Internal` · `External` · `Unavailable`)을 상태 코드로 바꾸고, 그 밖의 값은 `500`이다. `413` · `415`가 없다.
  - `BadHttpRequestException`은 원래 상태 코드(413 등)와 관계없이 모두 `400` · `1001`로 응답한다(TD-021). multipart `InvalidDataException`은 `9001`이다.
- 에러 코드는 5자리 `S T NNN`이고 `T = (short)ErrorType / 10`이다. 유형 자리 5와 9는 HTTP 상태가 둘 이상이라 `ErrorType` 값의 1의 자리로 구분한다(`Unauthorized = 51` · `Forbidden = 52`, `Internal = 91` · `External = 92` · `Unavailable = 93`). T = 6 ~ 8은 예비다([에러 코드 · 체계](../../05-api/error-codes.md#에러-코드-체계)). `Error`는 생성 시점에 범위 · NNN 000 · 예비 서비스 자리(S = 6 ~ 8) · T ↔ `ErrorType` 일치를 검사한다. 예비 **유형** 자리(T = 6 ~ 8)는 검사하지 않는다(`Error.EnsureValidCode`).
- `Error` / `Result` 파생은 아키텍처 규칙 `ErrorAndResultAreNotDerived`가 막는다(예외 `ValidationError` · `Result<T>`, [테스트 전략 · 아키텍처 테스트](../../04-development/testing-strategy.md#아키텍처-테스트)).

## 검토한 대안 (Options)

### 409 행 오류

1. **`FieldError`가 모든 유형을 받게 넓힘**: 장점: 형식이 늘지 않는다. 단점: "필드별 상세는 검증 실패"라는 기존 불변식과 단위 테스트가 깨진다. `ValidationError`에 Conflict 코드가 섞일 수 있다.
2. **`ValidationError`를 열어(비 sealed · 대표 코드 가변) 409에도 씀**: 장점: 변환 코드 재사용. 단점: 이름과 의미(검증 실패)가 어긋나고, `1001` 고정 계약이 깨진다.
3. **새 상세 Conflict 오류 형식을 추가**(`Error` 파생 sealed record, 대표 코드는 Conflict 유형, 상세 항목도 Conflict 유형): 장점: 기존 `FieldError` · `ValidationError` 계약을 그대로 두고, 응답 모양은 `400`과 같게 맞출 수 있다. 단점: 형식 2개(오류 · 상세 항목)와 아키텍처 규칙 예외 하나가 늘어난다.

### 413 · 415의 `ErrorType`과 코드 자리

1. **T = 1에 넣음**: `PayloadTooLarge = 11`, `UnsupportedMediaType = 12`, 코드 `1004` · `1005`. 장점: "처리 전에 거부한 잘못된 요청"이라는 뜻이 검증 실패(T = 1)와 같은 무리이고, 1의 자리로 상태를 구분하는 기존 방식(T = 5 · 9)을 그대로 쓴다. 예비 자리를 쓰지 않는다. 단점: "T = 1 = 400"이라는 대응이 깨진다(HTTP 상태는 원래 `ErrorType`으로 정하므로 규칙 위반은 아니다).
2. **예비 유형 자리 T = 6에 넣음**: `PayloadTooLarge = 61`, `UnsupportedMediaType = 62`, 코드 `6001` · `6002`. 장점: T = 1은 계속 `400`만이다. 단점: 예비 자리 하나를 요청 전송 오류 두 개에 쓴다. 에러 코드 체계 표의 "6 ~ 8 예비"를 고쳐야 한다.
3. **T = 6 · 7을 하나씩**(`60` · `70`): 단점: 예비 자리 둘을 쓴다.

## 결정 (Decision)

**BuildingBlocks에 상세 Conflict 오류와 409 `errors`를 추가하고, `ErrorType`에 `PayloadTooLarge` · `UnsupportedMediaType`와 공통 코드를 추가한다. 기존 오류 · 응답 계약은 바꾸지 않는다.**

### 상세 Conflict 오류 (BuildingBlocks.Domain)

- `Error`를 파생한 **sealed record** 하나와 그 상세 항목 형식 하나를 추가한다(이름은 S06-T01에서 정함, 예: `ConflictError` · `ConflictDetail`).
- 대표 오류: 호출한 쪽이 준 **Conflict 유형 `Error`**(예: `EmployeeErrors.DuplicateEmail` `23001`)의 코드 · 메시지 · 유형을 그대로 쓴다. Conflict 유형이 아니면 `ArgumentException`.
- 상세 항목: 경로(`FieldError.PropertyName`과 같은 규칙, 예: `Rows[3].Email`), 정수 코드, 메시지. 코드는 **Conflict 유형 `Error`**에서 가져온다(아니면 `ArgumentException`). 항목은 하나 이상, `null` 원소 금지, 입력 컬렉션은 복사하고 순서를 유지한다(`ValidationError.Create`와 같은 규칙).
- 동등성은 대표 오류가 같고 상세 항목이 같은 순서로 같을 때 성립한다(`ValidationError`와 같음).
- 아키텍처 규칙 `ErrorAndResultAreNotDerived`의 예외 목록에 이 형식을 추가한다.
- `FieldError`(Validation 유형만)와 `ValidationError`(sealed, `1001` 고정)는 바꾸지 않는다.

### ProblemDetails `errors` 409 확장 (BuildingBlocks.Api)

- 상세 Conflict 오류는 `409` `ProblemDetails`의 `errors` 확장에 **`ValidationError`와 같은 모양**으로 담는다: 키는 경로를 `.` 조각마다 camelCase로 바꾼 값(`FieldErrorKeys`, `Rows[3].Email` → `rows[3].email`), 값은 `{ code, message }` 배열(생성 순서 유지). `code`는 대표 코드다.

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "이미 등록된 이메일입니다.",
  "instance": "/api/employee",
  "code": 23001,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errors": {
    "rows[3].email": [{ "code": 23001, "message": "이미 등록된 이메일입니다." }]
  }
}
```

- 상세가 없는 기존 Conflict(`3001` · `3003` · `23001` 경합 `23505`)는 지금처럼 `errors` 없이 나간다. `errors`는 `ValidationError`와 상세 Conflict 오류에만 있다.
- `detail`과 상세 메시지는 고정 문구이고 입력 값(이메일 등)을 넣지 않는다. `23505`의 `Detail`(값 포함)은 응답 · 로그에 나가지 않는다(NFR-04, [ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md)).

### `ErrorType`과 공통 코드 (코드 자리는 대안 1)

| `ErrorType` = 값 | T | HTTP | 공통 코드 | 이름 | 의미 |
|---|---|---|---|---|---|
| `PayloadTooLarge` = 11 | 1 | 413 | 1004 | `Common.PayloadTooLarge` | 요청 본문이 허용 크기를 넘음 |
| `UnsupportedMediaType` = 12 | 1 | 415 | 1005 | `Common.UnsupportedMediaType` | 지원하지 않는 요청 Content-Type |

- 예비 유형 자리(T = 6 ~ 8)는 쓰지 않고 계속 예비로 둔다.
- `Error`에 유형별 팩토리 `Error.PayloadTooLarge(code, message)` · `Error.UnsupportedMediaType(code, message)`를 추가한다("`Error`는 유형별 팩토리로만 만든다" 규칙). 새 유형도 `T = 값 / 10` 검사를 그대로 받는다.
- `ErrorStatusCodes`: `PayloadTooLarge` → `413`, `UnsupportedMediaType` → `415`(11종, 그 밖의 값은 지금처럼 `500`).
- `FieldError`에는 이 두 유형을 담을 수 없다(Validation 유형만, 바꾸지 않음).
- 배포된 `ErrorType` 값 9종은 바꾸지 않는다.

### 예외 · 프레임워크 응답 변환 (BuildingBlocks.Api)

- **`BadHttpRequestException`**: `StatusCode`가 `413`이면 `413` · `1004`로 응답한다. 그 밖의 상태 코드는 지금처럼 `400` · `1001`이다(TD-021 부분 상환, 나머지 408 등은 TD-021에 남김). 로그 302(`BadHttpRequestRejected`)의 템플릿은 그대로이고 돌려준 코드만 바뀐다.
- **`[Consumes]` 불일치 `415`**: `[ApiController]`의 프레임워크 `415` 응답을 공통 `ProblemDetails`(`415` · `1005`, `code` · `traceId`)로 바꾼다. 방법(클라이언트 오류 매핑 등)은 S06-T01에서 실측해 정한다.
- **multipart · form 한도 초과(`InvalidDataException`)**: 형식이 일반적인 예외라 BuildingBlocks.Api 전역 처리에서 `413`으로 바꾸지 않는다(다른 원인의 `InvalidDataException`까지 `413`이 됨). 폼을 읽는 Employee.Api 바인더가 `413` · `1004`로 바꾼다([ADR-0026](0026-employee-bulk-import-input-processing.md), S06-T05).

## 결과 (Consequences)

- 긍정: DB 중복 행 번호가 `400`과 같은 `errors` 모양으로 나가 클라이언트가 두 응답을 같은 코드로 처리한다. `413` · `415`가 정수 코드를 가진 `ProblemDetails`가 되어 FR-05 입력 경로 표를 채운다. 기존 `FieldError` · `ValidationError` · 상세 없는 `409`의 계약이 그대로다.
- 부정: `ErrorType`이 9종에서 11종이 되고, T = 1이 `400` · `413` · `415` 세 상태를 가진다(에러 코드 체계 표와 "T = 1 = 400"으로 읽던 설명을 고친다). BuildingBlocks.Domain에 형식 2개, 아키텍처 규칙 예외 하나가 늘어난다.
- 부정: `ErrorType` · 공통 코드 전수 대조 테스트(`ErrorTypeTests` · `CommonErrorsTests`)와 `ErrorStatusCodesTests`를 함께 고친다.
- 전제와 실측 작업:
  - Kestrel이 `RequestSizeLimit` 초과 때 `StatusCode = 413`인 `BadHttpRequestException`을 던지고 TestServer는 다르게 동작할 수 있다(S06-T05 · S06-T06, NFR-01).
  - `[Consumes]` 불일치 `415` 결과를 공통 `ProblemDetails`로 바꾸는 지점(S06-T01 단위 테스트, S06-T06 통합 테스트).
  - multipart · form 한도 초과가 `InvalidDataException`으로 나오는지와 바인더의 판정 기준(S06-T05).
- 후속: 구현은 S06-T01(상세 Conflict 오류, 409 `errors`, `ErrorType` · 팩토리 · 상태 코드, `BadHttpRequestException` 413, `415` 변환, 하위 호환 단위 테스트, 아키텍처 규칙 예외), S06-T05(바인더의 폼 한도 초과 413). [에러 코드](../../05-api/error-codes.md) 체계 표 · 공통 코드 표와 [API 설계 가이드](../../04-development/api-guidelines.md) 공통 변환 규칙은 S05-T02에서 이 ADR에 맞췄다.
- 확인: 2026-09-28 오케스트레이션 세션 대리 확인(대안 3 상세 Conflict 오류, 코드 자리 대안 1: `PayloadTooLarge = 11` · `UnsupportedMediaType = 12`, 공통 코드 `1004` · `1005`). `/retro` ④ 추인 대상이다([S05 대리 승인](../../10-delivery/sprints/S05-rebase-decisions-schema.md#대리-승인)).
