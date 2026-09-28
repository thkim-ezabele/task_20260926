---
title: "직원 API (Employee)"
type: doc
status: draft
tags: [api, employee]
created: 2026-09-28
updated: 2026-09-29
---

# 직원 API (Employee)

> Employee 서비스가 공개하는 HTTP API의 요청 · 응답 · 에러 코드 명세입니다(PRD-002 FR-11). 공통 형식은 [API 설계 가이드](../04-development/api-guidelines.md), 코드의 원본은 [에러 코드](error-codes.md)입니다. 과제 명세를 따르는 규칙 예외는 [ADR-0025](../03-architecture/adr/0025-api-rule-exceptions-for-assignment-endpoints.md), 일괄 등록 입력 처리는 [ADR-0026](../03-architecture/adr/0026-employee-bulk-import-input-processing.md)이 원본입니다.
>
> [위키 홈](../README.md) · [API 레퍼런스](api-reference.md) · [실행 증빙 S07-T04](../10-delivery/evidence/S07-T04/README.md)

> PRD-001 샘플 API(`/api/v1/employees` 등록 · 단건 조회)는 S05-T04에서 코드와 함께 지웠습니다(PRD-002 FR-01). 그 명세는 `v0.1.0` 태그의 이 문서에 있습니다.

## 개요

| 항목 | 값 |
|---|---|
| 기본 경로 | `/api/employee` (버전 없는 단수형, ADR-0025 예외) |
| 구현 | `EmergencyHub.Employee.Api` `EmployeeController`(`[ApiController]`, `ISender`만 주입, [ADR-0016](../03-architecture/adr/0016-use-controllers-for-api.md)) |
| 형식 | 성공 응답 `application/json; charset=utf-8`, 실패 응답 `application/problem+json`(UTF-8) |
| 속성 이름 · 코드값 | camelCase, 코드값은 **정수**([ADR-0008](../03-architecture/adr/0008-integer-codes-and-bitmask.md)) |
| 인증 | 없음(Identity 토픽 전, PRD-002 범위 밖) |
| OpenAPI | Development 환경에서만 `/swagger/v1/swagger.json` · `/swagger`([ADR-0019](../03-architecture/adr/0019-use-swashbuckle-openapi.md)). `paths`는 `/api/employee`(`post` · `get`)와 `/api/employee/{name}`(`get`) 두 개 |

| 메서드 | 경로 | 동작 | 성공 |
|---|---|---|---|
| `POST` | `/api/employee` | CSV · JSON으로 여러 직원을 한 번에 등록(Command `RegisterEmployeesCommand`). 한 행이라도 실패하면 0건 저장 | `201` + `{ "count", "ids" }`(`Location` 없음) |
| `GET` | `/api/employee?page={page}&pageSize={pageSize}` | 전체 목록 한 쪽(Query `ListEmployeesQuery`, 읽기 전용 연결) | `200` + `{ items, totalCount, page, pageSize }` |
| `GET` | `/api/employee/{name}` | 이름이 정확히 같은 직원 1명(Query `GetEmployeeByNameQuery`, 읽기 전용 연결) | `200` + 직원 |

## 직원 필드

등록 입력(CSV 열 · JSON 속성)과 조회 응답이 같은 이름을 씁니다. 규칙의 원본은 Domain Value Object이고, 코드별 판정은 [에러 코드 · Employee](error-codes.md#employee-에러-코드)에 있습니다. 한 필드에서는 첫 실패 코드 하나만 보고합니다.

| API 이름 | CSV 열 | 형식 | 규칙 | 실패 코드 |
|---|---|---|---|---|
| `id` | - | UUID 문자열 | 응답 전용. UUID v7, Handler가 생성([ADR-0013](../03-architecture/adr/0013-uuid-v7-with-uuidnext.md)) | - |
| `name` | 1 | string | 앞뒤 공백 제거 + NFC 정규화, 제어 문자 · 짝 없는 서로게이트 거부, 1 ~ 100자(UTF-16 단위) | 21007 필수, 21009 문자, 21008 길이 |
| `email` | 2 | string | `local@domain` 하나, 공백 · 제어 문자 없음, domain에 `.`, 254자 이하. 입력 표기 그대로 저장하고 중복은 대소문자 무시(`ToLowerInvariant` 값, [ADR-0027](../03-architecture/adr/0027-case-insensitive-unique-email-with-normalized-column.md)) | 21003 필수, 21005 길이, 21004 형식, 21018 요청 안 중복, 23001 DB 중복 |
| `tel` | 3 | string | ASCII 숫자와 `-`만(`+` 불허), 맨 앞 · 맨 뒤 · 연속 하이픈 불허, 숫자 8 ~ 15자리, 전체 20자 이하. 입력 그대로 저장, 중복 허용 | 21010 필수, 21011 문자, 21013 길이, 21014 하이픈, 21012 자리 수 |
| `joined` | 4 | string (`yyyy-MM-dd`) | 정확한 형식 · 있는 날짜(`2000-2-3` · `2000-02-30` 거부), 1900-01-01 이상, 미래 허용 | 21015 필수, 21016 형식, 21017 하한 |

- 직원 상태(`employee_status`)는 API에 노출하지 않습니다. 등록하면 Active(1)로 고정됩니다([데이터베이스 · 코드 정의](../04-development/database.md#코드-정의)).

## POST /api/employee (일괄 등록)

### 입력 경로

`[Consumes]` 4종 중 하나로 보냅니다. 전용 바인더가 전송 형식에서 입력 바이트 · 형식 · 출처만 꺼내고, 내용 해석은 Application(파서 · Handler)이 합니다([ADR-0026 1절](../03-architecture/adr/0026-employee-bulk-import-input-processing.md#1-전용-바인더-employeeapi)).

| 요청 Content-Type | 입력을 읽는 곳 | curl |
|---|---|---|
| `multipart/form-data` | 파일 필드 `file`(filename이 있는 파트) 또는 텍스트 필드 `data` | `-F "file=@경로"` / `-F 'data=...'` |
| `application/x-www-form-urlencoded` | 텍스트 필드 `data` | `--data-urlencode 'data=...'` |
| `text/csv` · `application/json` | 본문 전체(raw) | `-H 'Content-Type: text/csv' --data-binary '...'` |
| 없음 | 본문 전체(raw). `415`로 거절하지 않고 내용으로 형식을 판별합니다(빈 본문이면 `400` · 21028) | `-H 'Content-Type:'` |

- 필드 이름 `file` · `data`는 대소문자를 무시하고 그 밖의 폼 필드는 무시합니다. multipart에서 `file`과 `data`를 함께 보내면 `400` · 21029, form-urlencoded에 `data` 키가 없으면(`curl -d '김이름,...'`) `400` · 21028입니다.
- **형식 판별 순서**: Content-Type(raw 본문, multipart 파일 파트의 Content-Type: `text/csv` → CSV, `application/json` → JSON) → 파일 확장자(`.csv` · `.json`) → 내용(맨 앞 BOM과 공백을 건너뛴 첫 바이트가 `[` · `{`이면 JSON, 그 밖은 CSV). 텍스트 필드 `data`는 내용으로만 판별합니다([ADR-0026 2절](../03-architecture/adr/0026-employee-bulk-import-input-processing.md#2-형식-판별-순서)).
- **엄격 UTF-8**: 모든 입력 경로에서 바인더가 원래 바이트를 그대로 넘기고 Application이 한 곳에서 해독합니다. 폼 · multipart 값도 프레임워크 폼 해독을 거치지 않으므로, 잘못된 UTF-8(CP949 등)은 입력 경로와 관계없이 `400` · 21022입니다. 맨 앞 BOM(`EF BB BF`) 하나는 허용합니다.

### CSV 규칙

- **헤더 없음**: 모든 줄을 데이터로 읽습니다. 열 순서는 `name,email,tel,joined` 고정, 쉼표 구분, `\n` · `\r\n`, 빈 줄 무시, 모든 필드 앞뒤 공백 제거입니다.
- 헤더 행(`name,email,tel,joined`)을 넣으면 그 줄도 데이터로 읽어 요청 전체를 거부합니다. 실측 응답은 `rows[1].email` 21004 · `rows[1].tel` 21011 · `rows[1].joined` 21016입니다(아래 [F04](#실패-예시), 헤더 지원은 TD-028 · BL-122).
- 따옴표는 RFC 4180 규칙입니다(따옴표 안의 쉼표 · 줄바꿈 · `""` 허용). 열 개수가 4가 아니면 21019, 닫히지 않은 따옴표 21020, 따옴표 없는 필드 안의 `"`와 닫는 따옴표 뒤의 공백 아닌 문자(`"a"b`)는 21021이고 모두 그 행(`rows[n]`)의 오류입니다.
- 행 번호 `n`은 **레코드가 시작하는 물리 줄 번호**(1부터, 빈 줄 포함)입니다.

### JSON 규칙

- 배열 `[{...}]`, 단일 객체 `{...}`, 대괄호 없는 나열 `{...},{...}`를 받습니다(세 형태 모두 같은 결과). 항목 번호 `n`은 1부터입니다.
- 속성 이름은 대소문자를 무시하고 알 수 없는 속성은 무시합니다. 값이 문자열이 아니면 21025(`rows[n].필드`), 항목이 객체가 아니면(`null` · 숫자 등) 21024(`rows[n]`), 대소문자만 다른 중복 속성은 21026입니다. 속성이 없으면 그 필드의 필수 코드입니다.
- 끝 쉼표 · 주석 · `[..],[..]` · 최대 깊이 64 초과 등 문법 오류는 요청 전체 오류 21023(경로 `""`)이고, 오류 메시지에 `JsonException` 원문을 넣지 않습니다.

### 크기 · 행 수 한도

- 요청 본문 **전체 기준 1 MiB**(1,048,576바이트)입니다. 본문 전체 · multipart 본문 · `data` 값 중 하나라도 한도 바이트 수를 넘으면 `413` · 1004이고 0건 저장입니다(판정은 예외 메시지가 아니라 바이트 수, [API 설계 가이드 · 규칙 예외](../04-development/api-guidelines.md#규칙-예외-과제-api-명세-adr-0025)).
- 실제 Kestrel에서 한도를 넘는 본문을 보내면 클라이언트에는 `413` 응답 대신 연결 종료로 보일 수 있습니다(S06 실측). 판정은 서버 로그로 합니다. 2026-09-29 실측(1,048,577바이트 raw 본문)은 `413` · 1004 응답을 받았습니다.
- boundary 없음 · 빈 boundary, 잘리거나 끝 boundary가 없는 multipart, multipart 헤더 한도 위반, 같은 필드 두 번은 한도 초과가 아니라 전송 형식 오류로 `400` · 1001입니다(`errors`의 키 `""`).
- 행 수는 최대 1,000행입니다. 넘으면 `400` · 21027(경로 `""`, 빈 줄 제외 · 행 오류가 있는 행 포함)입니다.

### 처리 순서

Validator(겉모양: 빈 입력 21028 → 입력 출처 1002 → `file` · `data` 동시 21029 → 형식 1002, 한 요청에 하나만) → Handler(파싱 → 파싱 결과 행 0개 검사 → 행 검증 → 요청 안 이메일 중복 → DB 이메일 중복 사전 조회 → 저장). 앞 단계가 실패하면 멈추고, **한 행이라도 실패하면 아무것도 저장하지 않습니다**(최대 1,000개 Aggregate 한 트랜잭션, [ADR-0026 9절](../03-architecture/adr/0026-employee-bulk-import-input-processing.md#9-다중-aggregate-단일-트랜잭션-예외)).

- **행 0개**: 파싱은 됐지만 행이 0개이고 행 오류도 0개인 입력(JSON `[]`, NBSP만 있는 CSV 줄 등)은 Handler가 파싱 직후 `400` · 21028(경로 `""`)로 거부합니다. 행이 0개라도 행 오류가 있으면(JSON `[null]`) 21028이 아니라 `rows[n]` 행 오류입니다. JSON에서 NBSP만 있는 입력은 문법 오류 21023입니다.

### 성공 응답: `201 Created`

```http
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8

{"count":3,"ids":["01a0e9f8-0c81-7583-b824-80b1b1a3836b","01a0e9f8-0c81-7584-8bb8-be57ef12c4e9","01a0e9f8-0c81-7585-9998-4177d577ece4"]}
```

- `count`는 등록한 건수, `ids`는 **입력 순서**의 직원 ID입니다. 여러 리소스를 만들므로 `Location` 헤더는 없습니다(ADR-0025).

### 실패 응답

| HTTP | 최상위 `code` | 경우 | `errors` |
|---|---|---|---|
| 400 | `1001` | 필드 · 파싱 · 입력 오류(아래 행 오류 경로 규칙), 바인딩 · 전송 형식 오류 | 있음(경로별 `{ code, message }` 배열) |
| 409 | `23001` | DB에 이미 있는 이메일(사전 조회, 대소문자 무시) | 있음: 충돌한 행마다 `rows[n].email` · 23001 |
| 409 | `23001` | 동시 요청 경합(유니크 인덱스 `ux_employees_normalized_email` 위반 23505 → 제약 이름 매핑) | 없음(행 번호 없음) |
| 409 | `3003` | 매핑 없는 유니크 제약 위반(기본 키 중복 등, 정상 흐름에서는 나오지 않음) | 없음 |
| 413 | `1004` | 요청 본문 1 MiB 초과 | 없음 |
| 415 | `1005` | 지원하지 않는 Content-Type(`text/plain` 등) | 없음 |
| 503 | `9003` | 일시 장애가 재시도 한도를 넘음 | 없음 |
| 500 | `9001` | 예상하지 못한 오류 | 없음 |

**행 오류 경로 규칙**(`errors`의 키, [ADR-0026 6절](../03-architecture/adr/0026-employee-bulk-import-input-processing.md#6-행-번호와-오류-경로))

| 경로 | 뜻 | 코드 |
|---|---|---|
| `rows[n].name` · `rows[n].email` · `rows[n].tel` · `rows[n].joined` | n번째 행(CSV 물리 줄 · JSON 항목, 1부터)의 필드 오류 | 필드 코드(21003 ~ 21005, 21007 ~ 21017), 21018(요청 안 중복, 같은 값의 행을 모두 표시), 21025 · 21026(JSON 필드), 23001(409 충돌 행) |
| `rows[n]` | n번째 행 전체 오류 | 21019 ~ 21021(CSV), 21024(JSON 항목이 객체가 아님) |
| `""` | 요청 전체 오류 | 21022 · 21023 · 21027 · 21028 · 21029, 잘림 표시 21030 · 23002, 전송 형식 오류 1001 |

- 한 행에 여러 필드 오류가 있으면 필드마다 담깁니다. `400`의 행 오류와 `409`의 충돌 행은 각각 **최대 100개**까지 담고, 넘으면 101번째 항목으로 잘림 표시(`400`은 21030, `409`는 23002, 경로 `""`)를 붙입니다.
- `detail` · `message`는 고정 문구이고 입력한 이름 · 이메일 · 전화번호 값을 담지 않습니다(PRD-002 NFR-04).

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Bad Request",
  "status": 400,
  "detail": "요청 값이 올바르지 않습니다.",
  "instance": "/api/employee",
  "code": 1001,
  "traceId": "e443d06bbdb08b0d2f332715df06cd81",
  "errors": {
    "rows[1].tel": [{ "code": 21012, "message": "전화번호 숫자는 8 ~ 15자리여야 합니다." }],
    "rows[2].email": [{ "code": 21004, "message": "이메일 형식이 올바르지 않습니다." }],
    "rows[2].joined": [{ "code": 21016, "message": "입사일은 yyyy-MM-dd 형식의 있는 날짜여야 합니다." }]
  }
}
```

### curl 예시

bash(Git Bash · Linux · macOS)에서 저장소 루트 기준으로 씁니다. 파일 예시는 통합 테스트의 원문 예시 fixture(`tests/Services/Employee/EmergencyHub.Employee.IntegrationTests/TestData/Examples/`의 `original-example.csv` 3행 · `original-example.json` 대괄호 없는 2항목)를 씁니다. 아래 결과는 2026-09-29 빈 DB에서 순서대로 실행한 값입니다(원문은 [S07-T04 증빙 http-curl.txt](../10-delivery/evidence/S07-T04/http-curl.txt)).

> **Windows Git Bash 주의**: Git for Windows에 든 `curl`(8.6.0 mingw)은 명령줄 인자의 한글을 시스템 코드 페이지(CP949)로 바꿔 보내므로 `-F 'data=김이름,...'` · `--data-binary '김이름,...'` 같은 인라인 한글 입력이 `400` · 21022가 됩니다(2026-09-29 실측, 파일 입력은 영향 없음). Windows 내장 `/c/Windows/System32/curl.exe`(8.21.0 실측, UTF-8로 보냄)를 쓰거나 입력을 UTF-8 파일로 보냅니다. 아래 결과는 Windows 내장 curl로 실행했습니다.

#### 성공 예시

```bash
# P01 multipart 파일 필드(CSV) → 201 count 3
curl -s -i -X POST http://localhost:5180/api/employee -F "file=@tests/Services/Employee/EmergencyHub.Employee.IntegrationTests/TestData/Examples/original-example.csv"

# P02 multipart 파일 필드(JSON, 대괄호 없는 나열) → 201 count 2
curl -s -i -X POST http://localhost:5180/api/employee -F "file=@tests/Services/Employee/EmergencyHub.Employee.IntegrationTests/TestData/Examples/original-example.json"

# P03 multipart 텍스트 필드(CSV 두 줄, 동명이인 '김이름'이 입사일 1999-12-31로 하나 더) → 201 count 2
curl -s -i -X POST http://localhost:5180/api/employee -F 'data=김이름,kim.early@example.com,010-5555-0001,1999-12-31
한이름,han@example.com,010-5555-0002,2004-05-06'

# P04 multipart 텍스트 필드(JSON 배열) → 201 count 1
curl -s -i -X POST http://localhost:5180/api/employee -F 'data=[{"name":"오이름","email":"oh@example.com","tel":"010-6666-6666","joined":"2005-06-07"}]'

# P05 form-urlencoded 텍스트 필드 → 201 count 1
curl -s -i -X POST http://localhost:5180/api/employee --data-urlencode 'data=서이름,seo@example.com,010-7777-7777,2006-07-08'

# P06 raw text/csv(따옴표 안의 쉼표) → 201 count 2
curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: text/csv' --data-binary '신이름,shin@example.com,010-8888-8888,2007-08-09
"유,이름",yoo@example.com,010-9999-9999,2008-09-10'

# P07 raw application/json(대괄호 없는 나열) → 201 count 2
curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: application/json' --data-binary '{"name":"권이름","email":"kwon@example.com","tel":"010-1010-1010","joined":"2009-10-11"},{"name":"황이름","email":"hwang@example.com","tel":"010-2020-2020","joined":"2010-11-12"}'
```

- `-F 'data=...'`의 값이 `@` · `<`로 시작하면 curl이 파일로 해석합니다. `--data-urlencode`는 값을 퍼센트 인코딩하고 Content-Type `application/x-www-form-urlencoded`를 붙입니다. `--data-binary`는 줄바꿈을 그대로 보냅니다(`-d @파일`은 파일의 줄바꿈을 지움).

#### 실패 예시

P01 ~ P07 뒤에 이어서 실행한 결과입니다(모두 0건 저장).

| # | 명령 | 결과 |
|---|---|---|
| F01 | `curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: text/csv' --data-binary '가이름,ga@example.com,010-1234,2000-01-01`↵`나이름,na@example,010-1234-5678,2000-2-3'` | `400` · 1001, `rows[1].tel` 21012, `rows[2].email` 21004, `rows[2].joined` 21016 |
| F02 | `curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: application/json' --data-binary '[{"name":"다이름","email":"Dup@Example.com","tel":"010-3030-3030","joined":"2011-01-01"},{"name":"라이름","email":"dup@example.com","tel":"010-4040-4040","joined":"2012-01-01"}]'` | `400` · 1001, `rows[1].email` · `rows[2].email` 21018(대소문자만 다른 요청 안 중복) |
| F03 | P01 명령을 다시 실행 | `409` · 23001, `rows[1].email` · `rows[2].email` · `rows[3].email` 23001 |
| F04 | `curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: text/csv' --data-binary 'name,email,tel,joined`↵`마이름,ma@example.com,010-5050-5050,2013-01-01'` | `400` · 1001, 헤더 행이 데이터로 읽혀 `rows[1].email` 21004 · `rows[1].tel` 21011 · `rows[1].joined` 21016 |
| F05 | `curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: text/csv' --data-binary '바이름,"ba"@example.com,010-6060-6060,2014-01-01'` | `400` · 1001, `rows[1]` 21021(닫는 따옴표 뒤 문자) |
| F06 | `curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: application/json' --data-binary '[{"name":"사이름","email":"sa@example.com","tel":"010-7070-7070","joined":20150101},null]'` | `400` · 1001, `rows[1].joined` 21025, `rows[2]` 21024 |
| F07 | `curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: application/json' --data-binary '[{"name":"아이름"},]'` | `400` · 1001, `""` 21023(끝 쉼표) |
| F08 | `curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: application/json' --data-binary '[]'` | `400` · 1001, `""` 21028(행 0개) |
| F09 | `curl -s -i -X POST http://localhost:5180/api/employee -d '김이름,kim@gmail.com,010-0000-0000,2000-01-01'` | `400` · 1001, `""` 21028(form-urlencoded에 `data` 키 없음) |
| F10 | `curl -s -i -X POST http://localhost:5180/api/employee -F "file=@tests/Services/Employee/EmergencyHub.Employee.IntegrationTests/TestData/Examples/original-example.csv" -F 'data=자이름,ja@example.com,010-8080-8080,2016-01-01'` | `400` · 1001, `""` 21029(`file`과 `data` 함께) |
| F11 | `curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type:'` | `400` · 1001, `""` 21028(Content-Type 없는 빈 본문, 415 아님) |
| F12 | `curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: text/plain' --data-binary '차이름,cha@example.com,010-9090-9090,2017-01-01'` | `415` · 1005 |
| F13 | `head -c 1048577 /dev/zero \| curl -s -i -X POST http://localhost:5180/api/employee -H 'Content-Type: text/csv' --data-binary @-` | `413` · 1004(1 MiB + 1바이트) |

- ↵는 따옴표 안의 줄바꿈(명령에서는 실제 줄바꿈)입니다. 표 안의 `\|`는 셸 파이프 `|`입니다.

## GET /api/employee (목록 조회)

쿼리 매개변수

| 이름 | 형식 | 기본값 | 범위 | 실패 |
|---|---|---|---|---|
| `page` | integer | 1 | 1 ~ 100,000 | 숫자가 아니거나 `int` 범위 밖(`2147483648`) → `400` · 1001(`errors.page` 1001), 범위 밖(`0` · `-1`) → `400` · 1001(`errors.page` 1003) |
| `pageSize` | integer | 20 | 1 ~ 100 | 위와 같음(`errors.pageSize`) |

- 매개변수 이름은 대소문자를 무시합니다. 1003(`Common.InvalidPaging`)은 최상위 `code`가 아니라 **필드 코드**로 담깁니다(최상위는 1001).
- 정렬은 입사일(`joined`) → ID(등록 순) 고정이고 `sort`는 없습니다(ADR-0025). 마지막 쪽을 넘는 `page`는 빈 `items`와 올바른 `totalCount`의 `200`입니다.

성공 응답: `200 OK` (속성 순서 `items` · `totalCount` · `page` · `pageSize`, 항목은 `id` · `name` · `email` · `tel` · `joined`)

```json
{"items":[{"id":"01a0e9f8-0cc1-735d-858d-6ede384814b2","name":"정이름","email":"jung@gmail.com","tel":"010-4444-4444","joined":"2003-04-05"}, ...],"totalCount":13,"page":2,"pageSize":5}
```

| 속성 | 형식 | 설명 |
|---|---|---|
| `items[].id` | UUID 문자열 | 직원 ID |
| `items[].name` | string | 이름(정규화 값) |
| `items[].email` | string | 대소문자 표기를 보존한 이메일(`email` 컬럼, 소문자 정규화 값이 아님) |
| `items[].tel` | string | 입력 그대로의 전화번호 |
| `items[].joined` | string (`yyyy-MM-dd`) | 입사일 |
| `totalCount` | integer | 전체 직원 수 |
| `page` · `pageSize` | integer | 적용한 값(빠진 매개변수는 기본값) |

curl 예시(P01 ~ P07 뒤, 13건)

| # | 명령 | 결과 |
|---|---|---|
| G01 | `curl -s -i http://localhost:5180/api/employee` | `200`, 13건(`page` 1 · `pageSize` 20), 첫 항목은 입사일이 가장 빠른 `김이름`(1999-12-31) |
| G02 | `curl -s -i 'http://localhost:5180/api/employee?page=2&pageSize=5'` | `200`, 6 ~ 10번째 5건, `totalCount` 13 |
| G03 | `curl -s -i 'http://localhost:5180/api/employee?page=100&pageSize=5'` | `200`, `{"items":[],"totalCount":13,"page":100,"pageSize":5}` |
| G04 | `curl -s -i 'http://localhost:5180/api/employee?page=0&pageSize=101'` | `400` · 1001, `errors.page` · `errors.pageSize` 1003 |
| G05 | `curl -s -i 'http://localhost:5180/api/employee?page=abc'` | `400` · 1001, `errors.page` 1001 |

- URL에 `&`가 있으면 따옴표로 감쌉니다.

## GET /api/employee/{name} (이름 조회)

경로 매개변수

| 이름 | 형식 | 규칙 |
|---|---|---|
| `name` | string(퍼센트 인코딩) | 앞뒤 공백 제거 + NFC 뒤 **정확히 일치**(대소문자 구분, NFD로 보낸 이름도 조회됨). 동명이인이면 입사일이 빠른 1명(같으면 등록 순) |

- 공백만 있는 이름(`%20`)은 모델 바인딩이 `null`로 바꿔 `400` · 1001(`errors.name` 21007)입니다. 그 밖의 이름 규칙 실패는 `400` · 1001(`errors.name` 21008 길이 · 21009 문자)입니다.
- `/`가 들어간 이름(`%2F`)은 보장하지 않습니다(PRD-002 범위 밖).
- 이름은 개인정보라 `ProblemDetails.instance`, 요청 완료 로그, 추적 span `url.path`에는 요청 경로 대신 라우트 템플릿 `/api/employee/{name}`이 남습니다([ADR-0025 이름 경로 매개변수와 개인정보](../03-architecture/adr/0025-api-rule-exceptions-for-assignment-endpoints.md#이름-경로-매개변수와-개인정보)). 이 보장은 `{name}` 라우트에 일치한 요청이 대상입니다.

성공 응답: `200 OK` (`id` · `name` · `email` · `tel` · `joined`, 목록 항목과 같은 형식)

```json
{"id":"01a0e9f8-0d19-7797-9f3a-ed73bef47340","name":"김이름","email":"kim.early@example.com","tel":"010-5555-0001","joined":"1999-12-31"}
```

실패 응답

| HTTP | 최상위 `code` | 경우 |
|---|---|---|
| 400 | `1001` | 이름 규칙 실패(`errors.name` 21007 · 21008 · 21009) |
| 404 | `22001` | 일치하는 직원 없음 |
| 503 | `9003` | 일시 장애가 재시도 한도를 넘음 |
| 500 | `9001` | 예상하지 못한 오류 |

curl 예시(P01 ~ P07 뒤)

| # | 명령 | 결과 |
|---|---|---|
| G06 | `curl -s -i http://localhost:5180/api/employee/%EA%B9%80%EC%9D%B4%EB%A6%84` (`김이름`) | `200`, 동명이인 2명 중 입사일이 빠른 `kim.early@example.com`(1999-12-31) |
| G07 | `curl -s -i http://localhost:5180/api/employee/%EC%97%86%EB%8A%94%EC%9D%B4%EB%A6%84` (`없는이름`) | `404` · 22001, `instance` `/api/employee/{name}` |
| G08 | `curl -s -i http://localhost:5180/api/employee/%20` | `400` · 1001, `errors.name` 21007, `instance` `/api/employee/{name}` |

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
| 2026-09-29 | developer | PRD-002 API 3개로 다시 씀: 직원 필드 표, 일괄 등록 입력 경로 · 형식 판별 · 엄격 UTF-8 · CSV(헤더 행 실측 응답, TD-028) · JSON · 한도(413 · 연결 종료 · 전송 형식 오류 1001) · 처리 순서 · 행 0개 21028 · 실패 응답 표 · 행 오류 경로 규칙(최대 100개 + 21030 · 23002), 목록 조회(1003은 필드 코드), 이름 조회(템플릿 `instance`, `%2F` 미보장), curl 예시 28개(Aspire 실측), Windows Git Bash curl 한글 인자 주의. PRD-001 샘플 명세 삭제(옛 `ux_employees_email` 문구 포함, BL-134) (S07-T04) |
