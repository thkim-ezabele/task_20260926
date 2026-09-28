---
title: "API 설계 가이드"
type: doc
status: draft
tags: [development]
created: 2026-09-27
updated: 2026-09-29
---

# API 설계 가이드

> 서비스가 외부(API Gateway 경유)에 공개하는 HTTP API의 설계 규칙입니다. developer는 이 규칙대로 엔드포인트를 만들고, reviewer와 tester는 이 기준으로 판정합니다.
> 관련: [에러 코드](../05-api/error-codes.md), [코딩 컨벤션 · CQRS](coding-conventions.md#cqrs-규칙), [ADR-0008 정수 코드](../03-architecture/adr/0008-integer-codes-and-bitmask.md)
>
> [위키 홈](../README.md)

> API 스타일은 `[ApiController]` Controller([ADR-0016](../03-architecture/adr/0016-use-controllers-for-api.md)), OpenAPI 도구는 Swashbuckle([ADR-0019](../03-architecture/adr/0019-use-swashbuckle-openapi.md))입니다.
> 과제 필수 3개 엔드포인트(`/api/employee`)는 이 문서의 일부 규칙에서 벗어납니다. 범위와 내용은 [규칙 예외](#규칙-예외-과제-api-명세-adr-0025)를 봅니다.

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
- 실패 상태 코드는 에러 유형으로 정해진다: `400` 검증, `413` 본문 크기 초과, `415` 지원하지 않는 Content-Type, `401` 인증, `403` 권한, `404` 없음, `409` 충돌, `422` 업무 규칙, `500` / `502` / `503` 서버 · 외부([에러 코드 체계](../05-api/error-codes.md#에러-코드-체계)). `413` · `415`의 근거는 [ADR-0028](../03-architecture/adr/0028-building-blocks-error-contract-extension.md)이다.

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
  "instance": "/api/employee",
  "code": 23001,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

검증 실패(`400`)는 필드별 오류를 `errors`에 담습니다. 행별 충돌(`409`, 상세 Conflict 오류 `ConflictError`)도 같은 모양의 `errors`를 씁니다([ADR-0028](../03-architecture/adr/0028-building-blocks-error-contract-extension.md)).

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Bad Request",
  "status": 400,
  "detail": "요청 값이 올바르지 않습니다.",
  "code": 1001,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errors": {
    "email": [{ "code": 21004, "message": "이메일 형식이 올바르지 않습니다." }],
    "notificationChannels": [{ "code": 1002, "message": "정의되지 않은 채널입니다." }]
  }
}
```

- 확장 필드 `code`(정수 에러 코드)와 `traceId`는 **항상** 넣는다. 클라이언트는 `code`로 분기한다.
- `detail`에 개인정보, 내부 구현(스택 트레이스, SQL)을 넣지 않는다.

공통 변환 규칙 (BuildingBlocks.Api, [ADR-0024](../03-architecture/adr/0024-building-blocks-api-for-common-http-handling.md), S02-T06)

| 항목 | 규칙 |
|---|---|
| `status` | `ErrorType`으로 정한다([에러 코드 체계](../05-api/error-codes.md#에러-코드-체계)). `PayloadTooLarge` → `413`, `UnsupportedMediaType` → `415`(ADR-0028). 예약 값 `None` · 정의되지 않은 값은 `500` |
| `type` / `title` | `https://httpstatuses.io/{status}` / 상태 코드의 표준 문구(`Conflict` 등) |
| `detail` | `Error.Message`. 전역 예외 처리기는 오류의 고정 메시지만 쓴다(예외 메시지 · 스택 미노출) |
| `instance` | 라우트 템플릿이 있으면 `PathBase` + 라우트 템플릿(예: `/api/employee/{name}`, 대소문자 그대로, 모든 엔드포인트), 없으면 요청 경로(`PathBase + Path`)다. 템플릿은 현재 엔드포인트, 없으면 `IExceptionHandlerFeature.Endpoint`(500 경로)의 `RouteEndpoint` 템플릿이다. 템플릿이 없는 응답: 라우팅 전 오류, 일치하는 엔드포인트 없음(404 · 405), `[Consumes]` 불일치 415(라우팅이 `RouteEndpoint`가 아닌 엔드포인트를 고름). 쿼리 문자열은 넣지 않는다(개인정보가 들어갈 수 있음). 요청 완료 로그 `RequestPath` · 추적 span `url.path` · 요청 안 로그의 호스팅 범위 `RequestPath`도 같은 템플릿이다(`PathBase` 없음, [로깅 · 공통 필드](logging-observability.md#공통-필드-enricher), [ADR-0025](../03-architecture/adr/0025-api-rule-exceptions-for-assignment-endpoints.md#이름-경로-매개변수와-개인정보)) |
| `code` | JSON 숫자 |
| `traceId` | W3C trace-id(`Activity.Current.TraceId`, 32자리 16진수). `Activity`가 없거나 W3C 형식이 아니면 `HttpContext.TraceIdentifier` |
| `errors` | `ValidationError`(`400`)와 `ConflictError`(`409`, ADR-0028)일 때만. 상세 없는 `409`(`3001` · `3003` · `23001` 경합)에는 없다. 키는 속성 경로를 `.` 조각마다 camelCase로 바꾼 값(`Items[0].Name` → `items[0].name`, 객체 수준은 `""`), 값은 `{ code, message }` 배열(생성 순서 유지) |
| 바인딩 오류 | `InvalidModelStateResponseFactory` → `400` · `1001`. 필드마다 코드 `1001`, 메시지는 `1001`의 고정 문구(프레임워크 메시지에 입력 값이 들어가므로). 모델 상태 키의 JSON 경로 접두사 `$.`는 떼고 `$`는 `""` |
| 예외 | `BadHttpRequestException` → `StatusCode`가 `413`이면 `413` · `1004`, 그 밖은 `400` · `1001`(TD-021 부분 상환, ADR-0028). 예외 분류기(`IExceptionClassifier`) 결과가 있으면 그 오류(예: 재시도 한도 초과 → `503` · `9003`), 없으면 `500` · `9001`. 변환되지 않은 DB 예외(23514 · 25006)도 `9001` |
| `415` | `[Consumes]`에 없는 Content-Type은 라우팅이 본문 없는 `415`로 끝내므로 상태 코드 페이지 처리기가, `[FromBody]` 액션의 Content-Type 없음은 클라이언트 오류 팩토리(`IClientErrorFactory` 데코레이터)가 `415` · `1005`로 바꾼다. `[FromBody]`가 없는 액션은 Content-Type이 없으면 액션까지 간다(일괄 등록은 전용 바인더가 raw body로 보고 내용으로 판별하며 `415`로 거절하지 않는다, ADR-0026 2절 · S06-T05). 그 밖의 클라이언트 오류 결과(`NotFound()` 등)는 프레임워크 기본 그대로다(S06-T01 실측, ADR-0028) |

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

## 규칙 예외 (과제 API 명세, ADR-0025)

과제 필수 3개 엔드포인트는 과제 명세를 그대로 따르고, 아래 항목에서만 이 문서의 규칙과 다르게 한다. 원본(항목별 규칙과 예외 값)은 [ADR-0025 예외 목록](../03-architecture/adr/0025-api-rule-exceptions-for-assignment-endpoints.md#예외-목록)이다.

| 적용 범위 (이 3개만) | 다른 점 |
|---|---|
| `GET /api/employee?page={page}&pageSize={pageSize}` | 버전 없는 단수형 경로, `page`(1부터, 기본 1, 1 ~ 100,000) / `pageSize`(기본 20, 1 ~ 100), 응답 `{ items, totalCount, page, pageSize }`, `sort` 없음(`joined_on` → `id` 고정) |
| `GET /api/employee/{name}` | 경로 매개변수가 ID가 아니라 이름(앞뒤 공백 제거 + NFC 뒤 정확히 일치, 동명이인이면 입사일이 빠른 1명) |
| `POST /api/employee` | 여러 직원을 한 번에 등록, `201` + `{ "count": N, "ids": [...] }`(`Location` 없음), `[Consumes]` 4종(multipart · form-urlencoded · `text/csv` · `application/json`), 실패에 `413` · `415` 추가. 입력 처리는 [ADR-0026](../03-architecture/adr/0026-employee-bulk-import-input-processing.md) |

- 표에 없는 규칙(camelCase, 정수 코드값, `null` 속성 유지, 빈 컬렉션 `[]`, 최상위 객체, `ProblemDetails`의 `code` · `traceId`)은 이 3개 엔드포인트에도 그대로 적용한다.
- 새 엔드포인트(ID 조회, 동명이인 전체 조회 BL-121 등)는 예외를 쓰지 않고 이 문서의 기본 규칙(`/api/v1/...`)을 따른다.
- **일괄 등록 크기 한도 · 전송 형식 오류 판정**(S06-T05 실측, [ADR-0026](../03-architecture/adr/0026-employee-bulk-import-input-processing.md) 1절 · [ADR-0028](../03-architecture/adr/0028-building-blocks-error-contract-extension.md)): 액션 특성 `RequestSizeLimit` · `RequestFormLimits`(`MultipartBodyLengthLimit` · `ValueLengthLimit`)를 모두 1 MiB로 두고, 폼 값 공급자를 빼(`DisableFormValueProviders`) 전용 바인더만 본문을 읽는다. 폼 값 공급자를 두면 폼 한도 초과(`InvalidDataException`)가 값 공급자 → ModelState → `400` · `1001`이 되기 때문이다(multipart `file` · `data` · form-urlencoded 실측). 판정 기준은 예외 메시지가 아니라 **바이트 수**다: 본문 전체가 `RequestSizeLimit`을, multipart 본문이 `MultipartBodyLengthLimit`을, `data` 값이 `ValueLengthLimit`을 넘으면 바인더가 서버와 같은 `BadHttpRequestException`(413)을 던지고 전역 예외 처리기가 `413` · `1004`로 응답한다(네 입력 경로 모두 바인더에서 판정, Kestrel이 먼저 한도를 적용하면 본문을 읽는 중에 같은 예외가 난다). boundary 없음 · 빈 boundary, 잘리거나 boundary가 없는 multipart, multipart 헤더 수 · 길이 한도 위반, 같은 필드 두 번은 한도 초과가 아니라 전송 형식 오류라 ModelState(키 `""`) → `400` · `1001`이다. 바인더는 프레임워크 폼 해독을 쓰지 않고 원래 바이트를 넘기므로 잘못된 UTF-8은 모든 입력 경로에서 `21022`다.

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
| 2026-09-28 | developer | ADR-0025 · 0026 · 0028 반영: 규칙 예외 절(과제 3개 엔드포인트, ADR 링크), 실패 상태 `413` · `415`, 409 `errors`(상세 Conflict 오류), `instance` · 요청 로그 · 추적 span의 라우트 템플릿(S07-T02), `BadHttpRequestException` 413 · `[Consumes]` 415 변환(S06-T01), 400 예시 코드 21001(폐기) → 21004, 409 예시 `instance`를 `/api/employee`로 (S05-T02) |
| 2026-09-28 | developer | S06-T01 구현 반영: 공통 변환 규칙의 "S06-T01부터" 문구를 현재형으로, `errors`(`ConflictError`, 상세 없는 409 제외), `415` 행(라우팅 415 → 상태 코드 페이지, `[FromBody]` Content-Type 없음 → 클라이언트 오류 팩토리, 실측) (S06-T01) |
| 2026-09-28 | developer | 규칙 예외 절에 일괄 등록 크기 한도 · 전송 형식 오류 판정 문단(바이트 수 기준 413 · 1004, 형식 오류 400 · 1001, 폼 값 공급자 제외), `415` 행에 Content-Type 없음의 판정 결과(내용 판별) (S06-T05) |
| 2026-09-29 | developer | `instance` 행을 구현으로: `PathBase` + 라우트 템플릿(500 경로는 `IExceptionHandlerFeature.Endpoint`), 템플릿 없는 응답 목록(404 · 405 · `[Consumes]` 415), 호스팅 로그 범위 `RequestPath` 포함 (S07-T02) |
