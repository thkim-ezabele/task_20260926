---
title: "직원 API (Employee)"
type: doc
status: draft
tags: [api, employee]
created: 2026-09-28
updated: 2026-09-28
---

# 직원 API (Employee)

> Employee 서비스가 공개하는 HTTP API의 요청 · 응답 · 에러 코드 명세입니다(PRD-001 FR-08, S03-T04). 공통 형식은 [API 설계 가이드](../04-development/api-guidelines.md), 코드의 원본은 [에러 코드](error-codes.md)입니다.
>
> [위키 홈](../README.md) · [API 레퍼런스](api-reference.md)

> **S05-T04에서 이 문서의 API(`/api/v1/employees` 등록 · 조회, `EmployeesController`)를 코드에서 지웠습니다**(PRD-002 FR-01). 지금 Employee Api의 HTTP 엔드포인트는 [헬스 엔드포인트](#헬스-엔드포인트)뿐입니다. 아래 등록 · 조회 명세는 `v0.1.0`(PRD-001) 기록이며, PRD-002 API(`POST /api/employee`, `GET /api/employee` · `/api/employee/{name}`)는 S06-T05 · S07에서 이 문서에 씁니다.

## 개요

| 항목 | 값 |
|---|---|
| 기본 경로 | `/api/v1/employees` |
| 구현 | `EmergencyHub.Employee.Api` `EmployeesController`(`[ApiController]`, `ISender`만 주입, [ADR-0016](../03-architecture/adr/0016-use-controllers-for-api.md)) |
| 형식 | 요청 `application/json`, 성공 응답 `application/json`, 실패 응답 `application/problem+json`(UTF-8) |
| 속성 이름 · 코드값 | camelCase, 코드값은 **정수**(`JsonStringEnumConverter` 없음, [ADR-0008](../03-architecture/adr/0008-integer-codes-and-bitmask.md)) |
| 인증 | 없음(Identity 토픽 전) |
| OpenAPI | Development 환경에서만 `/swagger/v1/swagger.json` · `/swagger`([ADR-0019](../03-architecture/adr/0019-use-swashbuckle-openapi.md)) |

| 메서드 | 경로 | 동작 | 성공 |
|---|---|---|---|
| `POST` | `/api/v1/employees` | 직원 등록(Command `RegisterEmployeeCommand`) | `201` + `Location` + `{ "id" }` |
| `GET` | `/api/v1/employees/{id}` | 직원 단건 조회(Query `GetEmployeeByIdQuery`, 읽기 전용 연결) | `200` + 직원 |

## 코드값

`employeeStatus`(`EmployeeStatus`, DB `employee_status smallint`)

| 값 | 이름 | 의미 |
|---|---|---|
| 0 | `Unknown` | 예약 값. 요청에 쓰면 `1002` |
| 1 | `Active` | 재직(활성) |
| 2 | `Inactive` | 비활성 |

## POST /api/v1/employees (직원 등록)

요청 본문 (`RegisterEmployeeRequest`)

| 속성 | 형식 | 필수 | 규칙 | 실패 코드 |
|---|---|---|---|---|
| `displayName` | string | 예 | 앞뒤 공백을 지운 뒤 1 ~ 100자(`string.Length`, 이모지는 한 글자가 2) | 누락 · 빈 값 · 공백만 `21001`, 길이 초과 `21002` |
| `email` | string | 예 | 앞뒤 공백을 지운 뒤 254자 이하, `@`가 정확히 하나이고 앞뒤가 비어 있지 않음. 저장 · 중복 판정은 Trim + 소문자(Invariant) 정규화 값 | 누락 · 빈 값 · 공백만 `21003`, 형식 `21004`, 길이 초과 `21005`, 중복 `23001` |
| `employeeStatus` | integer | 예 | 정의된 값(1 · 2) | 누락 · `null` `21006`, 정의되지 않은 값(0 · 99 등) `1002` |

```json
{
  "displayName": "홍길동",
  "email": "hong@example.com",
  "employeeStatus": 1
}
```

- 필수 여부 · 길이 · 형식은 Validator가 판정합니다(모델 바인딩의 암묵적 필수 검사는 끔, `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`). 누락 · `null` 문자열은 빈 문자열로 Command에 넘겨 `21001` · `21003`이 됩니다.
- 한 필드에서는 첫 실패 규칙 하나만 담기고(`RuleLevelCascadeMode = Stop`), 여러 필드의 실패는 한 응답의 `errors`에 함께 담깁니다.

성공 응답: `201 Created`

```http
HTTP/1.1 201 Created
Location: http://localhost:5180/api/v1/employees/0192a1b3-0000-7000-8000-000000000001
Content-Type: application/json; charset=utf-8

{ "id": "0192a1b3-0000-7000-8000-000000000001" }
```

- `id`는 UUID v7이고 Handler가 만듭니다([ADR-0013](../03-architecture/adr/0013-uuid-v7-with-uuidnext.md)).
- `Location`은 조회 엔드포인트의 라우트 이름(`GetEmployeeById`)으로 만들어 `GET /api/v1/employees/{id}`와 같은 경로입니다.

실패 응답

| HTTP | `code` | 경우 |
|---|---|---|
| 400 | `1001` | 검증 실패. 필드별 코드(`21001` ~ `21006`, `1002`)는 `errors`에 담김. JSON 파싱 오류 · 형식 불일치(예: `employeeStatus`에 문자열) 같은 바인딩 오류도 `1001`(필드 코드 `1001`) |
| 409 | `23001` | 이메일 중복(정규화한 값 기준). Handler 사전 검사와 동시 요청 경합(유니크 인덱스 `ux_employees_email` 위반 → 23505 변환)이 같은 코드 |
| 409 | `3003` | 매핑 없는 유니크 제약 위반(기본 키 중복 등, 정상 흐름에서는 나오지 않음) |
| 503 | `9003` | 일시 장애가 재시도 한도를 넘음(DB 연결 끊김 등) |
| 500 | `9001` | 예상하지 못한 오류 |

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Bad Request",
  "status": 400,
  "detail": "요청 값이 올바르지 않습니다.",
  "instance": "/api/v1/employees",
  "code": 1001,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errors": {
    "displayName": [{ "code": 21001, "message": "표시 이름은 필수입니다." }],
    "employeeStatus": [{ "code": 1002, "message": "정의되지 않은 코드값입니다." }]
  }
}
```

- `errors`의 키는 camelCase 속성 이름이고 값은 `{ code, message }` 배열입니다. `detail` · `message`는 고정 문구이며 입력 값을 담지 않습니다. 메시지 문구의 원본은 코드(`EmployeeErrors` · `CommonErrors`)입니다.

## GET /api/v1/employees/{id} (직원 조회)

경로 매개변수

| 이름 | 형식 | 규칙 |
|---|---|---|
| `id` | UUID 문자열 | 경로 제약(`:guid`)을 두지 않습니다. UUID가 아닌 값은 라우팅 404가 아니라 바인딩 오류 `400` · `1001`입니다 |

성공 응답: `200 OK` (`EmployeeResponse`, 읽기 전용 DbContext 프로젝션)

```json
{
  "id": "0192a1b3-0000-7000-8000-000000000001",
  "displayName": "홍길동",
  "email": "hong@example.com",
  "employeeStatus": 1,
  "createdAt": "2026-09-27T05:03:12.418+00:00",
  "updatedAt": "2026-09-27T05:03:12.418+00:00"
}
```

| 속성 | 형식 | 설명 |
|---|---|---|
| `id` | UUID 문자열 | 직원 ID |
| `displayName` | string | 표시 이름(앞뒤 공백 제거 값) |
| `email` | string | 정규화한 이메일(소문자) |
| `employeeStatus` | integer | [코드값](#코드값) |
| `createdAt` · `updatedAt` | string (ISO 8601) | 감사 시각(UTC, 오프셋 `+00:00`) |

실패 응답

| HTTP | `code` | 경우 |
|---|---|---|
| 400 | `1001` | `id`가 UUID 형식이 아님(바인딩 오류) |
| 404 | `22001` | 직원 없음(빈 UUID `00000000-...` 포함) |
| 503 | `9003` | 일시 장애가 재시도 한도를 넘음 |
| 500 | `9001` | 예상하지 못한 오류(예: 읽기 전용 연결에 쓰기 시도 25006) |

## 헬스 엔드포인트

API 계약은 아니지만 같은 호스트가 노출합니다([로깅 & 관측성 · 헬스체크](../04-development/logging-observability.md#헬스체크)).

| 경로 | 검사 | 응답 |
|---|---|---|
| `/health/live` | 없음 | `200` `Healthy` |
| `/health/ready` | 쓰기 · 읽기 DbContext 연결(`ready` 태그 2개) | `200` `Healthy` / `503` `Unhealthy`(본문은 상태 문자열만) |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-28 | developer | 문서 생성: 직원 등록 · 조회 요청 · 응답 · 에러 코드, 코드값 표, 헬스 엔드포인트 (S03-T04) |
| 2026-09-28 | developer | PRD-001 샘플 API 제거 안내(현재 엔드포인트는 헬스뿐, 등록 · 조회 명세는 `v0.1.0` 기록) (S05-T04) |
