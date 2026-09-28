---
title: "ADR-0025: 과제 API 명세에 따른 API 규칙 예외 (`/api/employee` 3개 엔드포인트)"
type: adr
adr: "0025"
status: accepted
date: 2026-09-28
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0025]
tags: [adr, architecture, api]
created: 2026-09-28
updated: 2026-09-28
---

# ADR-0025: 과제 API 명세에 따른 API 규칙 예외 (`/api/employee` 3개 엔드포인트)

## 배경 (Context)

- 과제 원문은 필수 엔드포인트 3개를 정해 두었다: `GET /api/employee?page={page}&pageSize={pageSize}`(전체 목록 페이징), `GET /api/employee/{name}`(이름이 일치하는 직원의 상세 연락정보), `POST /api/employee`(`201`, 파일 업로드와 직접 입력 모두)([PRD-002 원문](../../10-delivery/prd/PRD-002-employee-contacts.md#원문)).
- 인터뷰 Q2에서 "과제 필수 경로를 그대로 쓰고, 프로젝트 규칙과 다른 점은 ADR로 기록"하기로 했다([PRD-002 질문과 답변](../../10-delivery/prd/PRD-002-employee-contacts.md#질문과-답변)).
- 프로젝트 규칙([API 설계 가이드](../../04-development/api-guidelines.md))과 과제 명세가 다른 지점은 다음과 같다.
  - 경로: 규칙은 `/api/v{major}/{resources}`(주 버전, 복수형 명사)이고 Controller는 `[Route("api/v1/employees")]`이다([ADR-0016](0016-use-controllers-for-api.md)). 과제는 버전 없는 단수형 `/api/employee`다.
  - 경로 매개변수: 규칙은 리소스 ID(UUID 문자열)만 쓴다. 과제는 `{name}`이다.
  - 페이징: 규칙은 `offset` / `limit`(기본 20, 최대 100)과 응답 `{ items, totalCount, offset, limit }`이다. 과제는 `page` / `pageSize`다.
  - 생성 응답: 규칙은 `POST` 생성에 `201` + `Location` + `{ "id": ... }`이다. 과제의 `POST`는 여러 직원을 한 번에 등록한다.
  - 요청 형식: 규칙은 `application/json`이다. 과제는 파일 업로드(`<input type=file>`)와 직접 입력(`<textarea>`)을 모두 받으라고 한다(FR-05: multipart · form-urlencoded · `text/csv` · `application/json`).
- 입력 크기는 본문 전체 1 MiB · 최대 1,000행이다(NFR-01). 입력 경로 표는 지원하지 않는 Content-Type을 `415`, 1 MiB 초과를 `413`으로 정했고 둘 다 공통 정수 코드를 요구한다(FR-05). S05-T01 계약 확인 결과, 지금 BuildingBlocks에는 `413` · `415`에 대응하는 `ErrorType`이 없고 `BadHttpRequestException`은 원래 상태 코드와 관계없이 `400` · `1001`(TD-021), multipart `InvalidDataException`은 `9001`로 응답한다([S05 진행 기록](../../10-delivery/sprints/S05-rebase-decisions-schema.md#진행-기록)).

## 검토한 대안 (Options)

1. **프로젝트 규칙만 따름**(`/api/v1/employees`, `offset` / `limit`, `201` + `Location`): 장점: 규칙 예외가 없다. 단점: 과제 필수 요구(경로 · 매개변수 이름)를 채우지 못한다.
2. **두 경로를 모두 제공**(규칙 경로 + 과제 경로 별칭): 장점: 규칙 경로도 남는다. 단점: 같은 동작의 공개 표면 · 테스트 · 문서가 두 배가 되고, 과제는 하나만 요구한다. 규칙 경로의 소비자가 없다.
3. **API 설계 가이드 자체를 과제 형식으로 바꿈**(전 서비스 단수형 · 버전 없음 · `page`): 장점: 예외가 없어진다. 단점: 확정한 규칙과 ADR-0016 라우팅을 과제 하나 때문에 모든 서비스에서 뒤집는다. 다른 서비스에는 근거가 없다.
4. **과제 명세를 이 3개 엔드포인트에 한정한 예외로 둠**: 장점: 과제 명세를 그대로 채우고, 예외가 한 곳의 목록으로 드러난다. 단점: 한 서비스 안에 두 경로 체계가 생길 수 있고, 예외 목록을 따로 관리해야 한다.

## 결정 (Decision)

**대안 4: 과제 필수 3개 엔드포인트에 한해 아래 표의 규칙 예외를 둔다. 표에 없는 규칙은 모두 [API 설계 가이드](../../04-development/api-guidelines.md)를 따른다.**

### 예외 목록

| 항목 | 프로젝트 규칙 | 이 3개 엔드포인트 |
|---|---|---|
| 경로 | `/api/v{major}/{resources}`, 복수형 | `/api/employee`(버전 없음, 단수형). Controller는 `[Route("api/employee")]` |
| 목록 페이징 매개변수 | `offset` / `limit`(기본 20, 최대 100) | `page`(1부터, 기본 1, 1 ~ 100,000) / `pageSize`(기본 20, 1 ~ 100). 숫자가 아니면 바인딩 오류 `1001`, 범위 밖이면 `1003`(`Common.InvalidPaging`) |
| 목록 응답 | `{ items, totalCount, offset, limit }` | `{ items, totalCount, page, pageSize }`. 항목은 `id` · `name` · `email` · `tel` · `joined` |
| 목록 정렬 | `sort` 매개변수, 허용 필드는 엔드포인트마다 | `sort` 매개변수 없음. `joined_on` → `id`(등록 순) 고정 |
| 경로 매개변수 | 리소스 ID(UUID)만 | `{name}` 문자열. 앞뒤 공백 제거 + NFC 뒤 정확히 일치(대소문자 구분). 동명이인이면 입사일이 빠른 1명(같으면 등록 순). 없으면 `404`, 공백 제거 뒤 빈 값이면 `400`. `/`가 들어간 이름(`%2F`)은 보장하지 않는다(BL-124) |
| 생성 응답 | `201` + `Location` + `{ "id": ... }` | `201`, `Location` 없음, 본문 `{ "count": N, "ids": [...] }`(ids는 입력 순서) |
| 요청 형식 | `application/json` | `POST`만 `[Consumes]` 4종: `multipart/form-data`(파일 필드 `file`, 텍스트 필드 `data`), `application/x-www-form-urlencoded`(`data`), `text/csv`, `application/json`. 처리 방식은 [ADR-0026](0026-employee-bulk-import-input-processing.md) |
| 실패 상태 코드 | `400` · `401` · `403` · `404` · `409` · `422` · `500` · `502` · `503` | 위에 더해 `POST`에서 `413`(본문 1 MiB 초과, 0건 저장)과 `415`(지원하지 않는 Content-Type). 둘 다 `ProblemDetails`(`code`, `traceId`)이고 `ErrorType` · 공통 코드는 [ADR-0028](0028-building-blocks-error-contract-extension.md) |

- `Location`을 생략하는 이유: 한 요청이 여러 리소스를 만들어 가리킬 URI가 하나가 아니고, ID로 조회하는 엔드포인트가 없다(동명이인 · ID 조회는 BL-121). 만든 ID는 본문 `ids`로 돌려준다.
- 크기 한도는 `POST` 액션에 본문 전체 기준 1 MiB(`RequestSizeLimit`, `RequestFormLimits.MultipartBodyLengthLimit` · `ValueLengthLimit`)로 둔다(NFR-01). 1,000행 초과는 `413`이 아니라 `400`(Employee 전용 코드)이다.
- 그 밖의 규칙은 바꾸지 않는다: camelCase, 코드값 정수, `null` 속성 생략 안 함, 빈 컬렉션 `[]`, 최상위 응답 객체, 실패 응답 RFC 9457 `ProblemDetails`(`code` · `traceId` 항상), OpenAPI 문서 `v1`([ADR-0019](0019-use-swashbuckle-openapi.md)).

### 적용 범위

- **이 3개 엔드포인트(Employee.Api의 Controller 하나)에만 적용한다.** 과제 명세 밖에서 새로 만드는 엔드포인트(예: ID 조회, 동명이인 전체 조회 BL-121)는 이 예외를 쓰지 않고 API 설계 가이드의 기본 규칙을 따른다.
- 버전 없는 경로라 "주 버전을 올려 하위 호환을 깨는 변경" 방식을 쓸 수 없다. 이 3개 엔드포인트의 호환을 깨는 변경은 과제 명세가 바뀔 때만 하고, 그때 이 ADR을 새 ADR로 대체한다.

### 이름 경로 매개변수와 개인정보

`{name}`은 개인정보(이름)가 URL 경로에 들어간다. NFR-04는 로그와 에러 `detail`에 이름 값을 남기지 않도록 하고, S07-T03 완료 조건 ④는 "404 본문과 로그에 이름 값 없음"이다. 지금 구현에서 요청 경로가 남는 곳은 세 군데다(S05-T02에서 코드 확인).

**결정: 세 곳 모두 요청 경로 대신 라우트 템플릿(`/api/employee/{name}`)을 쓴다.** 라우트 템플릿이 없는 응답(라우팅 전 오류, 일치하는 엔드포인트 없음)은 지금처럼 요청 경로를 쓴다. 이 3개 엔드포인트에 한정하지 않고 모든 엔드포인트에 적용한다.

| # | 위치 | 지금 동작 | 바뀐 뒤 |
|---|---|---|---|
| 1 | 요청 완료 로그의 `RequestPath`(`UseSerilogRequestLogging`, Employee.Api `Program.cs` · ServiceDefaults) | 요청 경로를 기록 | 라우트 템플릿을 기록 |
| 2 | `ProblemDetails.instance`(BuildingBlocks.Api `ErrorProblemDetails.cs:55`, `PathBase + Path`) | 요청 경로 | 라우트 템플릿 |
| 3 | 추적 span의 `url.path`(ASP.NET Core 계측) | 요청 경로가 span 속성에 들어간다(OpenTelemetry ASP.NET Core 계측 기본 동작, S07-T02에서 실측 확인) | 라우트 템플릿 |

- 구현 방법(템플릿을 읽는 위치, 계측 보강 방식)은 S07-T02에서 실측해 정한다. 기존 `instance` 계약 테스트(요청 경로를 단언하는 테스트)의 수정도 S07-T02에 포함한다.
- 쿼리 문자열은 지금처럼 어디에도 넣지 않는다.

## 결과 (Consequences)

- 긍정: 과제 필수 3개 엔드포인트를 명세 그대로 제공한다. 규칙과 다른 점이 이 ADR의 표 하나에 모여 reviewer가 대조할 수 있다. 다른 엔드포인트의 규칙은 바뀌지 않는다.
- 부정: Employee 서비스에 `/api/employee`(예외)와 이후의 `/api/v1/...`(기본 규칙) 두 경로 체계가 함께 있을 수 있다. 목록 응답 형식이 서비스 안에서 둘(`page` / `offset`)이 된다. 버전 없는 경로라 호환을 깨는 변경의 이행 경로가 없다.
- 부정: 요청 완료 로그 `RequestPath` · `ProblemDetails.instance` · 추적 span `url.path` 세 곳이 요청 경로에서 라우트 템플릿으로 바뀌어, 기존 계약("요청 경로")이 모든 엔드포인트에서 바뀐다(운영 전이라 소비자 없음). 로그 · 추적으로 실제 요청 경로를 찾을 수 없고 `traceId`로 찾는다. [API 설계 가이드 · 공통 변환 규칙](../../04-development/api-guidelines.md#에러-응답-포맷-problemdetails)의 `instance` 행을 함께 고친다(S05-T02).
- 전제와 실측 작업:
  - `[Consumes]` 4종을 둔 액션 하나가 네 형식을 모두 받고, 그 밖의 Content-Type을 `415`로 거부한다(S06-T05 바인더 · Controller 단위 테스트, S06-T06 통합 테스트).
  - TestServer와 Kestrel의 `MaxRequestBodySize` 동작 차이를 확인한다(NFR-01, S06-T06).
  - 한글 이름의 URL 인코딩 조회 · NFD 입력 조회가 된다(FR-08, S07-T03).
  - 세 곳을 라우트 템플릿으로 바꾸는 방법과, 바꾼 뒤 요청 완료 로그 · `instance` · 추적 span에 이름 값이 없는지(S07-T02 단위 테스트, S07-T03 통합 테스트).
- 후속: [API 설계 가이드](../../04-development/api-guidelines.md)에 이 ADR로 가는 예외 절을 추가한다(S05-T02). 구현은 S06-T05(`POST`, 413 · 415), S07-T01(목록), S07-T02(이름 조회, 라우트 템플릿 세 곳과 기존 `instance` 계약 테스트 수정), 통합 검증은 S06-T06 · S07-T03, API 명세 문서는 S07-T04.
- 확인: 2026-09-28 오케스트레이션 세션 대리 확인(대안 4, 개인정보 세 곳은 라우트 템플릿안). `/retro` ④ 추인 대상이다([S05 대리 승인](../../10-delivery/sprints/S05-rebase-decisions-schema.md#대리-승인)).
