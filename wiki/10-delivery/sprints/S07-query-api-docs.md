---
title: "S07: 조회 API, 조회 성능, 문서 마무리"
type: sprint
sprint: "S07"
status: active
prd: [PRD-002]
started: 2026-09-29
finished:
adrs: []
worklogs: []
aliases: [S07]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-29
---

# S07: 조회 API, 조회 성능, 문서 마무리

- PRD: [PRD-002](../prd/PRD-002-employee-contacts.md)
- 토픽 브랜치: `feature/prd-002-employee-contacts` · 스프린트 종료 태그: `sprint/S07`
- 스프린트 번호는 S05-T01에서 확정했다(PRD-001 S01~S04 다음 번호, PRD `sprints` · 파일 이름 · 작업 ID 일치).

## 목표

> POST로 등록한 직원을 `GET /api/employee`(페이징)와 `GET /api/employee/{name}`(동명이인이면 입사일이 빠른 1명)으로 조회한다. 10,000건에서 두 조회가 200ms 안에 끝나고, Aspire로 띄운 상태에서 문서의 curl 예시가 모두 동작한다.

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S07-T01 | 목록 조회 Query와 `GET /api/employee` | FR-07, FR-10 | `page` 기본 1 · 상한 100,000, `pageSize` 기본 20 · 1~100. 숫자 아님 1001, 범위 밖 1003. 목록 · 개수를 따로 조회해 Handler가 합침. 응답 `{ items, totalCount, page, pageSize }`. 단위 테스트(Validator, Handler) 통과 | S06-T06 | done | 5fcc647 |
| S07-T02 | 이름 조회 Query와 `GET /api/employee/{name}`, 라우트 템플릿 세 곳(ADR-0025) | FR-08, FR-10, NFR-04 | Query Validator는 공백을 뗀 뒤 빈 이름이면 400 · 21007, 그 밖의 Name 규칙 실패면 400 · 21008 / 21009를 낸다(Name Value Object 판정). Handler는 trim + NFC 뒤 정확히 일치하는(대소문자 구분) 이름을 찾고, 동명이인이면 `joined_on` · `id` 순으로 첫 1명을 돌려주며, 없으면 404 · 22001을 낸다. 요청 완료 로그 `RequestPath` · `ProblemDetails.instance` · 추적 span `url.path` 세 곳은 모든 엔드포인트에서 라우트 템플릿이 있으면 템플릿(instance는 PathBase + 템플릿)을, 없으면 지금처럼 요청 경로를 쓴다. 500 예외 경로에서도 세 곳이 템플릿을 쓴다. 요청 경로를 단언하던 기존 `instance` 계약 테스트를 새 계약으로 고치고, 세 곳마다 템플릿 있음(성공) · 템플릿 없음(요청 경로 유지) · 500 경로 테스트를 둔다. ADR-0025 "이름 경로 매개변수와 개인정보" 결정 표와 API 설계 가이드 `instance` 행을 구현과 대조해 진행 기록에 남긴다. 단위 테스트가 통과하고, 세 곳과 로그에 이름 값이 없다 | S06-T06 | todo | |
| S07-T03 | 조회 통합 테스트와 10,000건 조회 성능 측정 | FR-07, FR-08, FR-10, NFR-03, NFR-04 | ① 페이징 경계: 25건에서 `page=2&pageSize=10` → 11~20번째, 마지막 페이지를 넘으면 빈 `items` · 200 · 올바른 `totalCount`, `page=0` · `pageSize=101` → 1003, `page=abc` → 1001 ② 한글 URL 200, NFD 조회, 없는 이름 404, 공백 이름 400, 동명이인 3명 중 가장 빠른 1명 ③ 10,000건 fixture 시더(마이그레이션 시드 아님)로 두 조회를 로컬에서 측정해 200ms 이내 측정값과 EXPLAIN 인덱스 사용 결과를 진행 기록에 남김 ④ `{name}` 라우트에 일치한 요청(200 · 400 · 404 · 500)의 응답 본문 · `instance` · 요청 완료 로그 · 추적 span `url.path`에 이름 값 없음 ⑤ S05 증빙 대응표의 나머지 행(1001 HTTP, GET 행)을 닫아 "이전 대기" 0건 | T01, T02 | todo | |
| S07-T05 | 행 0개 일괄 등록 입력 21028 거부(BL-137, 도메인 로직) | FR-06, FR-10 | 파싱 결과 행이 0개면 Handler가 파싱 직후, DB 조회와 `AddRange` 전에 400 · 21028(경로 `""`)을 돌려주고 아무것도 저장하지 않는다. 행이 1개 이상이면 지금 동작 그대로다. 0행을 201로 고정하던 Handler · Controller 단위 테스트를 새 동작으로 고친다(파서 테스트는 유지). 입력 경로 통합 테스트에 0행 JSON(`[]`)과 0행 CSV를 넣어 400 · 21028 · DB 0건을 확인한다. PRD FR-06과 error-codes 21028 행에 한 줄씩(판정 원본에 Handler 0행 추가) 반영하고 BL-137을 닫는다. 단위 · 통합 테스트가 통과한다 | S06-T06 | todo | |
| S07-T04 | 문서 마무리와 Aspire curl 실행 기록 (문서 작업) | FR-11 | API 명세(3개 엔드포인트, 행 오류 경로 규칙, `curl -F file=@` · `-F data=` · `--data-binary` + Content-Type 예시), error-codes · database(일괄 트랜잭션 예외 링크, 리셋 이력) · local-setup 등록 · 조회 예시가 draft 이상. Aspire로 띄운 상태에서 curl 예시를 모두 실행하고, **BL-024** 대시보드 추적 화면에서 `db.connection_string`에 비밀번호가 없고 span 태그에 등록 값이 없음을 확인해(마스킹 캡처를 `evidence/S07-T04/`에) 진행 기록에 명령 · 출력을 남기고, worklog 반영은 사용자에게 안내(`/worklog`) | T03, T05 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S07-T01 | 해당 없음(S05-T06 인덱스 사용 확인만) | TDD | ADR ① 경로 예외, CQRS 읽기 경로 | 인수 조건 대조(HTTP는 T03) |
| S07-T02 | 해당 없음 | TDD | 개인정보(경로 매개변수 로깅, 요청 로깅의 path 기록 여부), ADR-0025 세 곳 대조, 기존 계약 테스트 변경 목록 대조(6곳), 500 경로 템플릿 | 인수 조건 대조(HTTP는 T03) |
| S07-T03 | 시더 구성, EXPLAIN으로 인덱스 사용 확인 | 시더 · fixture 준비 | 표준 진입 점검 | 주 작성자 |
| S07-T05 | 해당 없음 | TDD | 표준(Result 경계: 요청 값 판정을 Handler에서 하는 이유, 개인정보) | 인수 조건 대조(0행 JSON · CSV 400 · 0건 저장) |
| S07-T04 | database 문서 검토 | 문서 작업 | 문서 규칙, 문서의 코드 번호와 error-codes 일치 | 명령 점검표: curl 예시 실행 출력(상태 코드 · `code`), 0행 curl 예시 400 · 21028, U+FFFD(EF BF BD) grep 0건(BL-136 해결 전 임시) |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다 (경고 0, 커버리지 보고 — NFR-06)
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] CI로만 판정할 조건: 토픽 PR CI에서 S07-T03 조회 측정이 CI 임계값(200ms × 2) 안이고 CI가 통과했다(NFR-03)
- [ ] 토픽 브랜치를 push하고 `sprint/S07` 태그를 붙였다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| 2026-09-29 | 계획 | 사용자 결정 | 결정 | BL-137 = A(행 0개 입력은 21028 400), S07-T04 앞 코드 작업 S07-T05로 추가, PRD FR-06 · error-codes 한 줄씩. 사용자 직접 결정(오케스트레이션 세션 전달, 대리 승인 아님) |
| 2026-09-29 | - | 계획 리뷰 | 승인(대리) | developer 리뷰(high 3 · medium 5 · low 1, 차단 0) → orchestrator 통합: T05 추가, T02 · T03 완료 조건 수정, CI 임계값 DoD로, Q1~Q3 추천안 승인(emergency-hub-d2) |
| 2026-09-29 | S07-T01 | dba | 해당 없음 | 스키마 · 매핑 · 쿼리 변경 없음(S05-T06 Repository · 인덱스 사용) |
| 2026-09-29 | S07-T01 | developer | PASS | TDD 테스트 68 추가 · 통과, build 경고 0. 공용 EmployeeResponse(Employees/), 1003은 필드 코드(최상위 1001, errors.page[0].code) 해석 → T03 확인 |
| 2026-09-29 | S07-T01 | reviewer | PASS | 1003 필드 코드 해석 모순 없음(ADR-0018), CA1812 억제 2건 승인(ListEmployeesQueryHandler · Validator), record 위치 규칙 충족 |
| 2026-09-29 | S07-T01 | tester | PASS | 파이프라인 테스트 4 보강, build 경고 0 · format 0, test 2,349 통과 · 1 건너뜀 · 실패 0, check-docs 6(BL-018) |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모:

- POST(S06)를 먼저 끝냈으므로, POST로 넣은 데이터로 curl 조회를 시연할 수 있다.
- T03에서 PRD-001 증빙 대응표의 "이전 대기"를 모두 닫는다.
- S07-T02: ADR-0025 A — RequestPath · instance(ErrorProblemDetails.cs:55) · url.path 세 곳 라우트 템플릿화, 기존 instance 계약 테스트 수정 포함([ADR-0025](../../03-architecture/adr/0025-api-rule-exceptions-for-assignment-endpoints.md#이름-경로-매개변수와-개인정보), S05-T02 대리 확인).

S05 인계 (S05 결과 리뷰 2026-09-28, 원문은 [S05 진행 기록](S05-rebase-decisions-schema.md#진행-기록)):

- **S07-T01 · T02**: `ListOrderedByJoinedOnAsync(skip, take)` · `CountAsync` · `FindFirstByNameAsync(Name)` · `EmployeeContactResponse`(S05-T06). skip 계산과 JSON 이름(tel · joined)은 Query 몫. S07-T02는 추적 span 경로 실측. 프로젝션 `record` 위치 규칙(여러 Query가 함께 쓰면 기능 폴더 밖, 하나면 안)을 reviewer가 확인.
- **S07-T03**: S05 증빙 테스트 대응표 HTTP 행 '이전 대기: S07-T03' 닫기. EXPLAIN 도우미(IntegrationTests QueryPlans)는 S05-T06 것을 재사용. 알려진 잡음 기준은 database.md 알려진 잡음 로그가 원본.
- **S07-T04**: BL-128(roadmap PRD-001 · PRD-002 행) · BL-134(employee-api.md 83행), TD-028 명세 문구('헤더 행을 넣으면 날짜 형식 오류로 거부').

S06 인계 (S06 결과 리뷰 2026-09-29, 원문은 [S06 진행 기록](S06-bulk-register-api.md#진행-기록)):

- **계획 리뷰**: BL-137(내용은 있지만 행이 0개인 일괄 등록 입력) 결정 대기. 추천 A = 21028 400(Handler 파싱 직후 한 곳, PRD FR-06 · error-codes 한 줄, S07-T04 앞에 작은 코드 작업 추가), B = 현행 201 `{count:0, ids:[]}` 유지 + 명세 명시. 오케스트레이션 세션이 사용자에게 확인해 입력으로 넘긴다.
- **S07-T03**: S06-T06 도구 재사용(EmployeeApiFactoryOptions UseKestrel · IdGenerator · AfterEmailLookup, HttpProblem · LogEventText, Http/ImportContent, PerformanceMeasurement 워밍업 1회 + 5회 중앙값 · SensitiveDataLogging 끔). BL-139: UnexpectedErrors 제외는 이벤트 ID + SourceContext로(TestServer RequestSizeLimitFilter도 ID 1). 로그 판정 기준은 S06 인계 메모 T06 항목과 같음. S05 증빙 대응표 GET 행 '이전 대기: S07-T03'. `DOCKER_API_VERSION=1.43`.
- **S07-T04**: API 명세 · error-codes에 S06 실측 동작 — (1) Content-Type 없음은 415가 아니라 raw로 보고 내용 판별(빈 본문 21028) (2) 폼 · multipart는 원래 바이트 기준 엄격 UTF-8(21022) (3) 413은 한도 바이트 수 기준, 잘못된 multipart는 400 · 1001 (4) Kestrel 1 MiB 초과 때 클라이언트는 413 또는 연결 종료로 보일 수 있음(판정은 서버 로그) (5) 400 · 409 목록 최대 100개 + 잘림 21030 · 23002, 행 경로 rows[n].field · 요청 전체 "" (6) CSV 닫는 따옴표 뒤 문자 = 21021 (7) 0행 입력 동작(BL-137 결정 결과). local-setup에 Api 단독 실행 시 `ConnectionStrings__Write` · `Read` 필요. tester 명령 점검표에 U+FFFD(EF BF BD) grep 0(BL-136 해결 전 임시). BL-024 대시보드 수동 확인(자동 부분은 S06-T06 완료).

### 사전 점검 (2026-09-29)

| 명령 | 결과 |
|---|---|
| `git pull --ff-only` | Already up to date, HEAD `08613a9`(S06 worklog) |
| 앞 스프린트 | S05 · S06 `status: done`, S06 종료 커밋 `f5e0877` origin에 포함 |
| `docker version --format '{{.Server.APIVersion}}'` | `1.43` → 로컬 통합 테스트에 `DOCKER_API_VERSION=1.43` |
| `dotnet --version` | `8.0.425` |
| `gh auth status` | Logged in (thkim-ezabele) |
| `dotnet dev-certs https --check --trust` | 종료 코드 `7`(미신뢰) → AppHost는 `--launch-profile http` |

### 계획 리뷰 결과 (2026-09-29, 대리 승인)

developer 1명이 리뷰하고 orchestrator가 통합했다. 작업은 S07-T05(BL-137) 하나만 늘렸다(위 작업 표가 확정본). 번호를 끼워 넣으면 기존 S07-T04 참조(ADR-0025, 테스트 파일, evidence 경로)가 깨지므로 T05로 추가하고 표에서 T04 앞에 두었다.

- **T02 보강**: ADR-0025 세 곳 라우트 템플릿화를 계획 메모에서 완료 조건으로 옮김. 500 예외 경로는 ExceptionHandlerMiddleware가 endpoint를 지우므로 `IExceptionHandlerFeature.Endpoint`로 읽는다.
- **T03 조정**: CI 임계값(×2) 판정은 DoD로. ④ 판정 대상은 `{name}` 라우트에 일치한 요청(405 · 일치 없음 404는 ADR-0025 fallback).
- **결정**: Q1 공백 외 Name 규칙 실패는 400 · 21008 / 21009(Validator, S04 실패 처리 경계). Q2 템플릿 도우미는 ServiceDefaults와 BuildingBlocks.Api에 internal로 중복 + 같은 테스트 표 + TD(트리거: 두 번째 서비스가 생길 때 공통 위치 재검토). Q3 instance는 PathBase + 템플릿.
- **위험**: T02가 세 프로젝트 · 모든 엔드포인트의 instance 계약을 바꾼다. 반려 2회에 이르면 분할안(ServiceDefaults 쪽 / BuildingBlocks 쪽)을 오케스트레이션 세션에 BLOCKED로 올린다(임의 분리 금지).

### 인계 메모

- **공통**: 로컬 통합 테스트 `DOCKER_API_VERSION=1.43`, AppHost는 `--launch-profile http`.
- **S07-T01**: `page` · `pageSize`는 `int?` 바인딩. `page=2147483648` → 1001, `page=-1` → 1003(T03 ①에도 추가). skip은 `(page - 1) * pageSize`를 checked로 계산해 단언. JSON 이름 tel · joined는 Query 몫. 프로젝션 `record`는 T01 · T02가 함께 쓰면 기능 폴더 밖(reviewer 확인).
- **S07-T02**: 템플릿은 `GetEndpoint()` ?? `IExceptionHandlerFeature.Endpoint`의 `RouteEndpoint.RoutePattern.RawText`. "/" 보정, PathBase는 instance만, 대소문자 유지를 같은 테스트 표로 고정. 도우미는 두 프로젝트에 internal 중복, developer가 TD `new` 기록(트리거: 두 번째 서비스가 생길 때 공통 위치 재검토). RequestPath는 ServiceDefaults `RequestLoggingOptions.GetMessageTemplateProperties`(EnrichDiagnosticContext 안 씀), url.path는 계측 `EnrichWithHttpResponse`에서 `SetTag`, 검증은 `ActivityListener`(새 패키지 없음). 고칠 instance 테스트 6곳: ErrorProblemDetailsTests 31 · 68 · 261(PathBase), ResultResponseAcceptanceTests:80, ExceptionResponseAcceptanceTests:154, UnsupportedMediaTypeAcceptanceTests:63, UnsupportedMediaTypeStatusCodeResponsesTests:28. 템플릿 있음 성공 테스트는 새로, 415는 요청 경로 유지 엣지. 빈 이름 400은 `%20`(끝 슬래시는 목록 라우트). 추적 span 경로 실측을 진행 기록에.
- **S07-T03**: 알려진 잡음 · 제외 기준 — ④에서 405 · 일치 없음 404는 판정 대상 아님(테스트 이름 · 주석에 이유). database.md 알려진 잡음 N1~N5는 AppHost 실행용이라 Testcontainers 판정에 적용하지 않는다. 테스트가 일부러 낸 로그(500 형식 테스트의 전역 예외)는 이벤트 ID를 단언하고 제외, UnexpectedErrors 제외는 이벤트 ID + SourceContext(BL-139). 그 밖의 ERROR · FATAL은 기록해 판정받는다. Microsoft.AspNetCore Warning 재정의를 테스트 하나로 고정. 측정은 S06-T06 PerformanceMeasurement(워밍업 1 + 5회 중앙값, SensitiveDataLogging 끔). 재사용: EmployeeApiFactoryOptions, HttpProblem · LogEventText, 고정 IIdGenerator, QueryPlans. `{name}` 500 경로에서 instance · RequestPath · url.path 이름 없음 HTTP 단언.
- **S07-T05**: 뒤집을 테스트 — Handler 단위 :416, Controller 단위 :145, 통합 RegisterEmployeesInputPathTests:14 주석. 파서 테스트는 유지. error-codes 21028 판정 원본을 "일괄 등록 Validator, 0행은 Handler(S07-T05)"로. 0행 CSV 예: NBSP만 있는 줄. reviewer: 파싱 뒤에야 알 수 있어 Handler 판정(S04 실패 처리 경계, ADR-0018 범위 예외와 같은 근거인지).
- **S07-T04**: 0행 동작은 T05 결과(400 · 21028)로 명세. S06 인계 (1)~(7), BL-128 · BL-134 · TD-028 문구, local-setup `ConnectionStrings__Write` · `Read`, BL-024 대시보드 확인(Edge headless 자동 캡처 + 마스킹, 실패하면 span 태그 텍스트 증빙 + 캡처는 BL). 명세에서 "이름 값 없음"을 405 · 일치 없음 404까지 넓게 주장하지 않는다.

## 결과 리뷰

> 스프린트 종료 시 orchestrator의 결과 리뷰(계획 대비 실제, 완료 조건 · FR 충족, 반려 분석)를 요약합니다.

-

## 생긴 백로그 / 기술부채

| ID | 제목 | 발생 작업 | 정리 결과 |
|---|---|---|---|
| | | | open / planned:SNN / dropped |

## 대리 승인

> 사용자 부재 등으로 승인 지점(계획 리뷰 · 결정 · BLOCKED · 결과 리뷰 · ADR 확인)을 다른 세션이 승인하면 그때마다 한 행을 추가합니다. 추인은 `/retro` ④에서 사용자가 일괄로 합니다.

| 날짜 | 승인 지점 | 승인 내용 | 승인한 세션 | 근거 (진행 기록) | 추인 |
|---|---|---|---|---|---|
| 2026-09-29 | ① 계획 리뷰 | T05 추가 · T02 · T03 완료 조건 수정 · CI 임계값 DoD 이동, Q1(400 · 21008/21009) · Q2(도우미 중복 + TD, 트리거 두 번째 서비스) · Q3(PathBase 붙임), T02 반려 2회 시 분할안 BLOCKED 보고 조건 | 오케스트레이션 `emergency-hub-d2` | 2026-09-29 계획 리뷰 행 | 대기 |

## 회고

### 잘된 점

-

### 문제

-

### 다음에 바꿀 것

-

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-002 분할, 가번호) |
| 2026-09-28 | - | BL-024 편입: T04에 대시보드 추적 수동 확인 추가 |
| 2026-09-28 | developer | S05-T01: 스프린트 번호 확정, T01 dba 참조 `S05-T05` → `S05-T06`(S05 작업 재구성) |
| 2026-09-28 | developer | S05-T02: 계획 메모에 S07-T02 라우트 템플릿 세 곳(ADR-0025) 항목 추가 |
| 2026-09-28 | orchestrator | S05 결과 리뷰: 계획 메모에 "S05 인계" 소절 추가 |
| 2026-09-29 | orchestrator | S06 결과 리뷰: 계획 메모에 "S06 인계" 소절 추가 |
| 2026-09-29 | orchestrator | S07 계획 확정: T05(BL-137) 추가, T02 · T03 완료 조건 수정, T04 의존, DoD CI 조건, 사전 점검 · 인계 메모 · 대리 승인 절 추가, `active` |
