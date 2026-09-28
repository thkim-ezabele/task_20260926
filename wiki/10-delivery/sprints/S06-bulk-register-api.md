---
title: "S06: 일괄 등록 POST /api/employee"
type: sprint
sprint: "S06"
status: planned
prd: [PRD-002]
started:
finished:
adrs: []
worklogs: []
aliases: [S06]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-28
---

# S06: 일괄 등록 POST /api/employee

- PRD: [PRD-002](../prd/PRD-002-employee-contacts.md)
- 토픽 브랜치: `feature/prd-002-employee-contacts` · 스프린트 종료 태그: `sprint/S06`
- 스프린트 번호는 S05-T01에서 확정했다(PRD-001 S01~S04 다음 번호, PRD `sprints` · 파일 이름 · 작업 ID 일치).

## 목표

> CSV와 JSON(배열, 단일 객체, 대괄호 없는 나열)을 입력 경로 표의 모든 방식(multipart `file` · `data`, form-urlencoded, raw)으로 보내면, 전부 유효할 때 201과 `{ count, ids }`를 받는다. 한 행이라도 틀리면 0건 저장되고 400 / 409와 행 번호를 받는다. 413 · 415는 공통 정수 코드로 응답한다.

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S06-T01 | BuildingBlocks 오류 계약 확장 | FR-06, FR-09, NFR-04, NFR-05 | ① 상세 목록을 가진 Conflict 오류 타입 추가, ProblemDetails 409가 `errors` 확장(행 경로, 정수 코드)으로 변환 ② `ErrorType`에 PayloadTooLarge · UnsupportedMediaType 추가, 413 · 415와 S05-T02 공통 코드로 변환 ③ 기존 409(`errors` 없음)와 기존 계약 하위 호환을 단위 테스트로 확인 ④ 23505 detail(값 포함)이 응답 · 로그에 나가지 않음(단위 테스트) ⑤ S05-T01에서 편입을 결정한 TD-010 · BL-023 처리 반영 | S05-T06 | todo | |
| S06-T02 | CSV 파서 (Application `internal`, 상태 기계) | FR-03, FR-10, NFR-04 | FR-03 규칙 전부(헤더 없음, 열 4개, RFC 4180 따옴표, 엄격 UTF-8 + BOM, `\n` · `\r\n`, 빈 줄 무시, trim, 물리 줄 번호). 단위 테스트: 원문 예시 1건, 열 부족 · 초과, 따옴표 안 쉼표 · 줄바꿈, BOM, 마지막 줄 개행 없음, 빈 줄 사이 행 번호, CP949 바이트 거부, 1,000행 초과. 오류 메시지에 입력 값 없음. 새 패키지 없음 | S05-T02 | todo | |
| S06-T03 | JSON 파서 (배열, 단일 객체, 대괄호 없는 나열) | FR-04, FR-10, NFR-04 | 세 형태가 같은 행 목록을 만든다. 항목 오류: 문자열 아님(`joined` 숫자), 속성 누락, 객체 아님, 대소문자만 다른 중복 속성. 알 수 없는 속성 무시. 끝 쉼표 · 주석 · `[..],[..]`는 전용 문법 코드, 경로 빈 문자열. `JsonException` 원문이 오류에 없음 | S05-T02 | todo | |
| S06-T04 | RegisterEmployeesCommand, Validator, Handler | FR-06, FR-01, FR-10, NFR-04 | ① Validator는 겉모양만(Format 1002, 빈 입력, `file` · `data` 동시) ② Handler: 파싱 → Value Object 행 검증 → 요청 안 이메일 중복(두 행 표시) → DB 중복 사전 조회(409 + 행 번호) → 여러 건 추가, 앞 단계 실패 시 멈춤 ③ 행 오류 최대 100개 + 잘림 표시, 경로 `rows[n].field`(1부터) ④ 성공 결과는 count와 입력 순서 ids ⑤ 단위 테스트(NSubstitute) 단계별 성공 / 실패 / 엣지, enum 1002 증빙 이전(Validator 수준)을 대응표에 반영 | T01, T02, T03 | todo | |
| S06-T05 | Employee.Api 바인더, Controller, 크기 제한, Swagger | FR-05, FR-09, NFR-01 | ① 전용 `IModelBinder`가 `EmployeeImportPayload` 생성, 판별 순서 Content-Type → 확장자 → 내용 ② 액션 하나에 `[Consumes]` 4종, `POST /api/employee` 201, `Location` 없음 ③ 1 MiB 제한(`RequestSizeLimit`, `MultipartBodyLengthLimit`, `ValueLengthLimit`), `BadHttpRequestException` · `InvalidDataException` → 413, 지원하지 않는 형식 → 415 ④ Swagger OperationFilter로 `file` · `data` · raw 시험 가능 ⑤ Controller는 `ISender`만, 아키텍처 테스트 통과 | T04 | todo | |
| S06-T06 | POST 통합 · 인수 · 개인정보 테스트와 등록 성능 측정 | FR-05, FR-06, FR-10, NFR-01, NFR-02, NFR-04 | ① 입력 경로 표의 모든 행이 기대 상태 코드 · `code`로 응답 ② 실패 시 DB 0건(400, 409, 413, 1,000행 초과) ③ 동시 요청 23505 → 409(행 번호 없음) ④ TestServer · Kestrel `MaxRequestBodySize` 차이 확인 ⑤ 원문 예시 fixture(CSV · JSON) 인수 시나리오 ⑥ 개인정보: 400 / 409 / 500 본문과 캡처한 로그에 이름 · 이메일 · 전화번호 값 없음, **BL-024**: `ActivityListener`로 모은 Npgsql span 태그(`db.connection_string` · `db.statement` 등)에 DB 비밀번호와 등록한 입력 값 없음 ⑦ 1,000행 CSV 등록 시간 · EF 배치 크기를 진행 기록에 남김(2초 이내, CI 임계값 4초) ⑧ HTTP 수준 증빙 이전(23505 → 409, 500 형식)을 대응표에 반영 | T05 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S06-T01 | 해당 없음(23505 경로가 바뀌면 제약 이름 매핑 검토) | TDD, 공유 계약 변경이므로 기존 테스트 회귀 없음 확인 | ADR ④ · api-guidelines · error-codes와 일치 | ProblemDetails 형식 인수 조건 대조 |
| S06-T02 | 해당 없음 | TDD, 순수 클래스 | 표준 진입 점검, 예외 메시지 개인정보 | FR-03 인수 조건 대조 |
| S06-T03 | 해당 없음 | TDD, `JsonDocument` | 표준 진입 점검 | FR-04 인수 조건 대조 |
| S06-T04 | 사전 조회 쿼리와 1,000건 단일 트랜잭션(ADR ② 예외) 검토 | TDD | ADR-0018 범위 예외가 이 Command에만 있는지, 로깅 데코레이터가 Content를 기록하지 않는지 | FR-06 인수 조건 대조(단위 수준) |
| S06-T05 | 해당 없음 | TDD(바인더 단위 테스트) | ADR ① · ②와 일치, 얇은 Controller | Swagger 수동 확인 기록(HTTP 전체 검증은 T06) |
| S06-T06 | fixture DB 구성, 성능 측정 조건(`EnableSensitiveDataLogging` 끔) 검토 | fixture · 로그 캡처 도구 준비 | 표준 진입 점검 | 주 작성자: 통합 · 인수 · 개인정보 테스트 작성 · 실행, 실패 원인별 반려 |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다 (경고 0, 커버리지 보고 — NFR-06)
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S06` 태그를 붙였다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| | | | | |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모:

- T01 완료 조건은 S05-T01에서 확인하는 PRD-001 S02-T06 결과(ErrorType 매핑, `errors` 형식, CommonErrors, BL-019)에 따라 바뀐다. 이 스프린트 계획 리뷰에서 다시 확정한다.
- 파서(T02 · T03)는 S05-T02에만 의존해 T01과 독립이지만, 파이프라인은 작업 순서대로 진행한다.
- 위험을 앞에 두려고 POST(S06)를 GET(S07)보다 먼저 배치했다.
- 계획 리뷰에서 확인할 것: enum 1002 증빙을 Validator 단위 테스트로 대체할지(새 API에는 사용자가 enum을 직접 보내는 경로가 없음).
- S06-T04(Validator 규칙 2개) · S06-T05(Controller 규칙 1개) 완료 조건에 아키텍처 규칙 '대상 대기' 해제를 넣는다(S06 계획 리뷰에서 반영).

## 결과 리뷰

> 스프린트 종료 시 orchestrator의 결과 리뷰(계획 대비 실제, 완료 조건 · FR 충족, 반려 분석)를 요약합니다.

-

## 생긴 백로그 / 기술부채

| ID | 제목 | 발생 작업 | 정리 결과 |
|---|---|---|---|
| | | | open / planned:SNN / dropped |

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
| 2026-09-28 | - | BL-024 편입: T06 완료 조건 ⑥에 추적 태그 실측 추가 |
| 2026-09-28 | developer | S05-T01: 스프린트 번호 확정, T01 의존 `S05-T05` → `S05-T06`(S05 작업 재구성), 계획 메모에 '대상 대기' 해제 항목 추가 |
