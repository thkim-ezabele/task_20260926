---
title: "API 설계 가이드"
type: doc
status: draft
tags: [development]
created: 2026-09-27
updated: 2026-09-27
---

# API 설계 가이드

> 서비스가 외부(API Gateway 경유)에 공개하는 HTTP API의 설계 규칙입니다. developer는 이 규칙대로 엔드포인트를 만들고, reviewer와 tester는 이 기준으로 판정합니다.
> 관련: [에러 코드](../05-api/error-codes.md), [코딩 컨벤션 · CQRS](coding-conventions.md#cqrs-규칙), [ADR-0008 정수 코드](../03-architecture/adr/0008-integer-codes-and-bitmask.md)
>
> [위키 홈](../README.md)

> API 스타일은 `[ApiController]` Controller([ADR-0016](../03-architecture/adr/0016-use-controllers-for-api.md)), OpenAPI 도구는 Swashbuckle([ADR-0019](../03-architecture/adr/0019-use-swashbuckle-openapi.md))입니다.

## URL 및 리소스 네이밍

- 기본 형식: `/api/v{major}/{resources}/{id}/{sub-resources}`
- 리소스는 **복수형 명사, kebab-case, 소문자**: `/api/v1/employees`, `/api/v1/contact-groups`
- 경로 매개변수는 리소스 ID(UUID 문자열)만 쓴다: `/api/v1/employees/{employeeId}`
- 중첩은 **한 단계까지**: `/api/v1/emergencies/{emergencyId}/responses` (두 단계 이상이면 최상위 리소스 + 필터로 바꾼다)
- CRUD로 표현하기 어려운 동작은 **동작 결과를 하위 리소스로** 만들고 `POST`한다: `POST /api/v1/emergencies/{emergencyId}/broadcasts` (전파 실행). 경로에 동사(`/sendNotification`)를 쓰지 않는다.
- 쿼리 매개변수는 camelCase: `?status=1&sort=-createdAt`

## HTTP 메서드 & 상태 코드

CQRS에 맞춰 **Query는 `GET`, Command는 `POST` / `PUT` / `PATCH` / `DELETE`** 입니다.

| 메서드 | 용도 | 성공 응답 |
|---|---|---|
| `GET` | Query. 부작용 없음 | `200` + 본문 |
| `POST` | 생성 Command | `201` + `Location` 헤더 + `{ "id": "..." }` |
| `POST` (동작 리소스) | 동작 Command (전파 실행 등) | `202`(비동기 처리 시작) 또는 `200` |
| `PUT` | 전체 교체 Command | `204` |
| `PATCH` | 부분 변경 Command | `204` |
| `DELETE` | 삭제 Command | `204` (이미 없으면 `404`) |

- Command는 **생성한 ID 정도만** 반환한다. 변경 후 상태가 필요하면 클라이언트가 Query로 다시 조회한다.
- 실패 상태 코드는 에러 유형으로 정해진다: `400` 검증, `401` 인증, `403` 권한, `404` 없음, `409` 충돌, `422` 업무 규칙, `500` / `502` / `503` 서버 · 외부([에러 코드 체계](../05-api/error-codes.md#에러-코드-체계)).

### 멱등성

- `PUT`, `DELETE`는 멱등하게 구현한다.
- **긴급 전파처럼 중복 실행이 치명적인 `POST`는 `Idempotency-Key` 헤더(UUID)를 필수로 받는다.** 같은 키의 재요청은 처음 결과를 돌려주거나 `409`(`3002 Common.DuplicateRequest`)를 반환한다. 대상 엔드포인트는 PRD에서 정한다.

## API 버저닝

- **URL 경로의 주 버전**으로 관리한다: `/api/v1/...`
- 하위 호환이 깨지는 변경(필드 삭제 / 이름 변경 / 타입 변경, 코드값 의미 변경)만 주 버전을 올린다. 필드 추가, 새 엔드포인트, 새 코드값 추가는 같은 버전에서 한다.
- 새 주 버전을 내면 이전 버전은 최소 한 릴리스 동안 유지하고, 응답 헤더 `Deprecation`, `Sunset`으로 알린다.

## 요청 / 응답 포맷

| 항목 | 규칙 |
|---|---|
| 형식 | `application/json`, UTF-8 |
| 속성 이름 | camelCase |
| ID | UUID 문자열 (`"0192a1b3-..."`) |
| 시각 | ISO 8601 **UTC** (`"2026-09-27T05:03:12.418Z"`) |
| 날짜 | `"2026-09-27"` |
| **코드값** | **정수** (`"employeeStatus": 1`). 문자열 enum 금지(`JsonStringEnumConverter` 사용 안 함) |
| **비트 플래그** | **정수** (`"notificationChannels": 3`) |
| null | 값이 없으면 `null`로 보낸다(속성을 생략하지 않음) |
| 컬렉션 | 없으면 빈 배열 `[]` (`null` 금지) |
| 최상위 응답 | 객체. 목록도 배열을 바로 반환하지 않고 [페이지 응답](#페이징--정렬--필터링)으로 감싼다 |

- 요청 / 응답 모델은 `record`로 만든다(`...Request`, `...Response`). 도메인 엔티티를 그대로 직렬화하지 않는다.
- 정수 코드의 의미는 OpenAPI 문서의 설명과 [데이터베이스 · 코드 정의](database.md#코드-정의) 표로 제공한다.

## 에러 응답 포맷 (ProblemDetails)

모든 실패 응답은 **RFC 9457 `application/problem+json`** 입니다. `Result` 실패는 Api 레이어에서 이 형식으로 바꾸고, 예상하지 못한 예외는 전역 예외 처리기가 `9001`로 바꿉니다.

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "이미 등록된 이메일입니다.",
  "instance": "/api/v1/employees",
  "code": 23001,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

검증 실패(`400`)는 필드별 오류를 `errors`에 담습니다.

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Bad Request",
  "status": 400,
  "detail": "요청 값이 올바르지 않습니다.",
  "code": 1001,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errors": {
    "email": [{ "code": 21001, "message": "이메일 형식이 아닙니다." }],
    "notificationChannels": [{ "code": 1002, "message": "정의되지 않은 채널입니다." }]
  }
}
```

- 확장 필드 `code`(정수 에러 코드)와 `traceId`는 **항상** 넣는다. 클라이언트는 `code`로 분기한다.
- `detail`에 개인정보, 내부 구현(스택 트레이스, SQL)을 넣지 않는다.

공통 변환 규칙 (BuildingBlocks.Api, [ADR-0024](../03-architecture/adr/0024-building-blocks-api-for-common-http-handling.md), S02-T06)

| 항목 | 규칙 |
|---|---|
| `status` | `ErrorType`으로 정한다([에러 코드 체계](../05-api/error-codes.md#에러-코드-체계)). 예약 값 `None` · 정의되지 않은 값은 `500` |
| `type` / `title` | `https://httpstatuses.io/{status}` / 상태 코드의 표준 문구(`Conflict` 등) |
| `detail` | `Error.Message`. 전역 예외 처리기는 오류의 고정 메시지만 쓴다(예외 메시지 · 스택 미노출) |
| `instance` | 요청 경로(`PathBase + Path`). 쿼리 문자열은 넣지 않는다(개인정보가 들어갈 수 있음) |
| `code` | JSON 숫자 |
| `traceId` | W3C trace-id(`Activity.Current.TraceId`, 32자리 16진수). `Activity`가 없거나 W3C 형식이 아니면 `HttpContext.TraceIdentifier` |
| `errors` | `ValidationError`일 때만. 키는 속성 경로를 `.` 조각마다 camelCase로 바꾼 값(`Items[0].Name` → `items[0].name`, 객체 수준은 `""`), 값은 `{ code, message }` 배열(생성 순서 유지) |
| 바인딩 오류 | `InvalidModelStateResponseFactory` → `400` · `1001`. 필드마다 코드 `1001`, 메시지는 `1001`의 고정 문구(프레임워크 메시지에 입력 값이 들어가므로). 모델 상태 키의 JSON 경로 접두사 `$.`는 떼고 `$`는 `""` |
| 예외 | `BadHttpRequestException` → `400` · `1001`. 예외 분류기(`IExceptionClassifier`) 결과가 있으면 그 오류(예: 재시도 한도 초과 → `503` · `9003`), 없으면 `500` · `9001`. 변환되지 않은 DB 예외(23514 · 25006)도 `9001` |

- Controller는 실패 `Result`를 `return result.Error.ToProblemResult();`로 돌려준다(`ErrorProblemResult`, `ActionResult<T>`로 암시적 변환).

## 페이징 / 정렬 / 필터링

- 목록 조회는 **offset / limit** 페이징을 기본으로 한다: `?offset=0&limit=20` (기본 20, 최대 100). 대량 목록에는 커서 페이징을 검토한다(🟡 필요 시).
- 정렬: `sort=필드[,필드]`, 내림차순은 `-` 접두사: `?sort=-createdAt,name`. 허용 필드는 엔드포인트마다 정하고, 그 외는 `400`(`1003`).
- 필터: 필드 이름의 쿼리 매개변수. 코드값은 정수, 비트 플래그는 **하나라도 포함** 조건으로 해석한다: `?notificationChannels=5`
- 응답:

```json
{
  "items": [ { "id": "...", "name": "..." } ],
  "totalCount": 132,
  "offset": 0,
  "limit": 20
}
```

## 인증 헤더

- `Authorization: Bearer <access token>`. 토큰 발급과 검증 방식(JWT / OIDC)은 Identity 서비스 설계 때 정한다(🟡).
- 권한은 토큰의 권한 비트 마스크로 확인한다([ADR-0008](../03-architecture/adr/0008-integer-codes-and-bitmask.md)). 인증 실패 `401`(`5001`), 권한 부족 `403`(`5002`).
- 응답 헤더 `traceparent`를 돌려주어, 문제 신고 시 TraceId로 로그를 찾을 수 있게 한다([로깅 & 관측성](logging-observability.md#분산-추적-opentelemetry--correlation-id)).

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 기본 규칙 초안: URL, 메서드 · 상태 코드(CQRS), 멱등성(`Idempotency-Key`), 버저닝, 정수 코드 직렬화, ProblemDetails(`code`, `traceId`), 페이징 · 정렬 · 필터, 인증 헤더 |
| 2026-09-27 | developer | API 스타일(Controller, ADR-0016) · OpenAPI 도구(Swashbuckle, ADR-0019) 확정 반영 (S01-T04) |
| 2026-09-27 | developer | 에러 응답 공통 변환 규칙 표(status · type · instance · traceId · errors 키 · 바인딩 오류 · 예외 판정), `ToProblemResult` (S02-T06) |
