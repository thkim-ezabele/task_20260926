---
title: "S06: 일괄 등록 POST /api/employee"
type: sprint
sprint: "S06"
status: active
prd: [PRD-002]
started: 2026-09-28
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
- 토픽 브랜치: `feature/prd-002-employee-contacts` · 스프린트 종료 태그: `sprint/S06`(PRD 종료 뒤 일괄)
- 스프린트 번호는 S05-T01에서 확정했다(PRD-001 S01~S04 다음 번호, PRD `sprints` · 파일 이름 · 작업 ID 일치).

## 목표

> CSV와 JSON(배열, 단일 객체, 대괄호 없는 나열)을 입력 경로 표의 모든 방식(multipart `file` · `data`, form-urlencoded, raw)으로 보내면, 전부 유효할 때 201과 `{ count, ids }`를 받는다. 한 행이라도 틀리면 0건 저장되고 400 / 409와 행 번호를 받는다. 413 · 415는 공통 정수 코드로 응답한다.

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S06-T01 | BuildingBlocks 오류 계약 확장 | FR-06, FR-09, NFR-04, NFR-05 | ① BuildingBlocks.Domain에 상세 목록(행 경로 · 정수 코드 · 메시지)을 가진 Conflict 오류를 추가하고, BuildingBlocks.Api가 409 ProblemDetails `errors`(ValidationError와 같은 키 형식, 값 `{code, message}`)로 바꾼다. ErrorAndResultAreNotDerived 예외 목록과 testing-strategy 표는 1:1 ② `ErrorType` PayloadTooLarge(11) · UnsupportedMediaType(12)와 팩토리 2개를 추가해 413 · 1004 / 415 · 1005로 바꾼다. `[Consumes]` 불일치(Content-Type 없음 포함)가 공통 ProblemDetails 415 · 1005로 나가는지 BuildingBlocks.Api 테스트 호스트(표본 Controller, TestServer)로 확인 ③ 전역 예외 처리는 BadHttpRequestException 413을 413 · 1004로, 그 밖은 기존대로 400 · 1001(TD-021 부분 상환, multipart InvalidDataException은 T05) ④ 기존 409(3001 · 3003 · 23001, `errors` 없음), 400 형식, 기존 ErrorType 매핑, FieldError Validation 유형 제한의 하위 호환을 단위 테스트로 확인 ⑤ 23505 detail(값 포함)이 응답 · 로그에 나가지 않음(단위 테스트) ⑥ error-codes 1004 · 1005 상태 "사용", api-guidelines · CommonErrorsTests 1:1, ProblemFieldError.cs:4 XML 주석의 폐기 코드 21001 교체 | S05-T06 | done | a9f6e78, 2704dba, b33d371 |
| S06-T02 | CSV 파서 (Application `internal`, 상태 기계) | FR-03, FR-10, NFR-04 | ① FR-03 규칙 전부(헤더 없음, 열 4개, RFC 4180 따옴표, 엄격 UTF-8 + BOM, `\n` · `\r\n`, 빈 줄 무시, trim, 물리 줄 번호) ② 단위 테스트: 원문 예시 1건, 열 부족 · 초과, 따옴표 안 쉼표 · 줄바꿈, BOM, 마지막 줄 개행 없음, 빈 줄 사이 행 번호, CP949 바이트 거부, 1,000행 초과(21027) ③ UTF-8로 인코딩된 서로게이트(ED A0 80)는 21022(경로 ""), NUL 바이트(0x00)는 값 보존(거부는 VO), CR만 있는 줄의 동작을 테스트로 고정 ④ 오류 메시지에 입력 값 없음, 새 패키지 없음 ⑤ 서로게이트 · 제어 문자 테스트 데이터는 `DisableDiscoveryEnumeration = true`와 입력 보존 단언(BL-131) | S05-T02 | done | 32dbe77, 6db4151 |
| S06-T03 | JSON 파서 (배열, 단일 객체, 대괄호 없는 나열) | FR-04, FR-10, NFR-04 | ① 배열 · 단일 객체 · 대괄호 없는 나열이 같은 행 목록을 만들고 알 수 없는 속성은 무시 ② 항목 오류: 문자열 아님(`joined` 숫자, 21025), 속성 누락, 객체 아님(21024), 대소문자만 다른 중복 속성(21026) ③ 끝 쉼표 · 주석 · `[..],[..]` · 최대 깊이 64 초과는 21023(경로 "") ④ 값 · 속성 이름의 짝 없는 서로게이트 이스케이프(`\ud800` · `\udc00`)는 500 없이 21022(경로 ""), `\u0000`은 값 보존(단위 테스트로 실측) ⑤ 오류에 `JsonException` · `InvalidOperationException` 원문과 입력 값 없음 ⑥ 서로게이트 · 제어 문자 테스트 데이터는 `DisableDiscoveryEnumeration = true`와 입력 보존 단언(BL-131) | S05-T02 | done | c36a793, 92cd63e, d2d3aad, c0ffc93 |
| S06-T04 | RegisterEmployeesCommand, Validator, Handler | FR-06, FR-01, FR-10, NFR-04 | ① Validator는 인계 메모의 판정 순서대로 겉모양만(빈 입력 21028, Sources 정의 안 된 비트 1002, 두 비트 이상 21029, Format 1002), 순서 경계마다 테스트, 21028 · 21029는 PropertyName ""이고 error-codes "요청 전체" 목록에 추가 ② Handler: 파싱 → VO 행 검증 → 요청 안 중복 → DB 사전 조회 → Aggregate 생성 · AddRange, 앞 단계 실패 시 다음 단계 Substitute 미호출 단언 ③ 요청 안 중복은 관련 행 모두 표시(3행 이상 포함), DB 사전 조회 결과는 NormalizedEmail → 행 번호 사전으로 짝짓고(순서 가정 없음) 입력 순서로 409 `rows[n].email` ④ 400 목록(파싱 · 행 검증 · 요청 안 중복 합산)과 409 목록 각각 최대 100개 + 잘림 항목(21030 · 23002, 경로 ""), 성공은 count와 입력 순서 ids ⑤ Email.Create가 Cc 제어 문자(탭 · DEL 포함) · 짝 없는 서로게이트를 21004로 거부(BL-129, Name과 공용 Domain internal 도우미, error-codes 21004 설명 갱신) ⑥ 아키텍처 "대상 대기" Validator 규칙 2개 해제(PendingTargetRules ↔ testing-strategy 표 1:1, 건너뜀 대기 1 + 격리 1) ⑦ enum 1002 증빙은 Validator 단위 테스트로 대체해 대응표 반영, coding-conventions에 "VO · Command를 로그 템플릿 인자로 넘기지 않는다"(BL-130), BL-132 예시를 새 Handler 이름으로 | T01, T02, T03 | done | fda5dfe, (verify) |
| S06-T05 | Employee.Api 바인더, Controller, 크기 제한, Swagger | FR-05, FR-09, NFR-01 | ① 전용 `IModelBinder`가 `EmployeeImportPayload` 생성, 판별 순서 Content-Type → 확장자 → 내용(바인더 단위 테스트) ② 액션 하나에 `[Consumes]` 4종, `POST /api/employee` 201, `Location` 없음, 지원하지 않는 형식 415 · 1005 ③ 1 MiB 제한(`RequestSizeLimit` · `MultipartBodyLengthLimit` · `ValueLengthLimit`), 입력 경로별(multipart `file` · multipart `data` · form-urlencoded · raw) 한도 초과 예외 도달 위치(바인더 / 값 공급자 · ModelState / 전역 처리기) 실측, 한도 초과만 413 · 1004, 잘못된 multipart(boundary 없음 등)는 400 · 1001, 판정 기준을 api-guidelines에 기록 ④ 폼 필드의 잘못된 UTF-8 바이트 치환 여부를 실측해 진행 기록에 남김(치환되어 21022 판정과 어긋나면 BLOCKED) ⑤ Swagger OperationFilter로 `file` · `data` · raw 시험 가능 ⑥ Controller는 `ISender`만, 아키텍처 테스트 통과, "대상 대기" Controller 규칙 1개 해제(목록 · 표 1:1, 건너뜀 대기 0 + 격리 1) | T04 | doing | |
| S06-T06 | POST 통합 · 인수 · 개인정보 테스트와 등록 성능 측정 | FR-05, FR-06, FR-10, NFR-01, NFR-02, NFR-04 | ① 입력 경로 표의 모든 행이 기대 상태 코드 · `code`로 응답, 실패(400, 409, 413, 1,000행 초과, NUL email 400 · 21004) 때 DB 0건 ② 동시 경합 두 시나리오: (a) 다른 요청의 같은 이메일 — 테스트 호스트 전용 hook으로 사전 조회 뒤 커밋 전 충돌 행 삽입, 409 · 23001 · 행 번호 없음 · 먼저 커밋된 쪽만 남음 (b) 같은 ID 재전송 — 고정 IIdGenerator로 409 · 3003 · 로그 202(TD-010) ③ 테스트 안 임의 포트 실제 Kestrel 호스트(fixture DB 공유)가 `MaxRequestBodySize` 초과를 413 · 1004로 응답, TestServer 테스트는 RequestFormLimits 경로만 확인함을 테스트 이름 · 진행 기록에 명시 ④ 원문 예시 fixture(CSV · JSON) 인수 시나리오 ⑤ 개인정보: 400 / 409 / 500 본문과 캡처한 로그에 이름 · 이메일 · 전화번호 값 없음, **BL-024**: `ActivityListener`로 모은 Npgsql span 태그(`db.connection_string` · `db.statement` 등)에 DB 비밀번호와 입력 값 없음 ⑥ 1,000행 CSV 측정 테스트가 4초 단언, 로컬 측정(워밍업 1회 뒤 N회 중앙값, `EnableSensitiveDataLogging` 끔) 2초 이내 여부와 EF 배치 크기(명령 분할 수)를 진행 기록에, 로컬 2초 초과면 BLOCKED ⑦ 증빙 대응표 HTTP "이전 대기: S06-T06" 행 닫기(1002 행은 T04 Validator 단위 + 21028 경로 HTTP 400 · 1001 형식), BL-133(`HttpProblem` · `LogEventText`) 재사용 또는 삭제 | T05 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S06-T01 | 해당 없음(23505 경로가 바뀌면 제약 이름 매핑 검토) | TDD, 공유 계약 변경이므로 기존 테스트 회귀 없음 확인 | ADR ④ · api-guidelines · error-codes와 일치 | ProblemDetails 형식 인수 조건 대조 |
| S06-T02 | 해당 없음 | TDD, 순수 클래스 | 표준 진입 점검, 예외 메시지 개인정보 | FR-03 인수 조건 대조 |
| S06-T03 | 해당 없음 | TDD, `JsonDocument` | 표준 진입 점검 | FR-04 인수 조건 대조 |
| S06-T04 | 해당 없음(계획 리뷰 D5: 스키마 · SQL 변경 없음, `= ANY` 사전 조회는 S05-T06 실측) | TDD | ADR-0018 범위 예외가 이 Command에만 있는지, 로깅 데코레이터가 Content를 기록하지 않는지, VO · Command 로그 인자 금지 | FR-06 인수 조건 대조(단위 수준) |
| S06-T05 | 해당 없음 | TDD(바인더 단위 테스트) | ADR ① · ②와 일치, 얇은 Controller | Swagger 수동 확인 기록(HTTP 전체 검증은 T06) |
| S06-T06 | 해당 없음(계획 리뷰 D5, 측정 조건은 인계 메모) | fixture · 로그 캡처 도구 · Kestrel 호스트 · 경합 hook 준비 | 표준 진입 점검 | 주 작성자: 통합 · 인수 · 개인정보 테스트 작성 · 실행, 실패 원인별 반려 |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다 (경고 0, 커버리지 보고 — NFR-06)
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push했다 (CI 확인 · 태그 `sprint/S06`은 PRD 종료 뒤 일괄)
- [ ] CI로만 판정할 조건 (PRD 종료 뒤 일괄): NFR-02 CI 러너에서 1,000행 측정 테스트의 4초 단언 통과, FR-10 · NFR-06 토픽 PR CI 전체 테스트 통과 · 경고 0 · 커버리지 보고 산출물

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| 2026-09-28 | - | 계획 리뷰 | 승인(대리) | developer 리뷰(high 5 · medium 4 · low 1) → orchestrator 통합: 분할 없음, T01~T06 완료 조건 수정, D1~D6 추천안 승인(emergency-hub-d2). PRD FR-01 email 규칙 변경(BL-129) |
| 2026-09-28 | S06-T01 | dba | 해당 없음 | 파이프라인 표 dba 열 "해당 없음"(호출 생략) |
| 2026-09-28 | S06-T01 | developer | PASS | ConflictError · 409 errors, ErrorType 11 · 12, 413 · 1004 / 415 · 1005(라우팅 415는 StatusCodePages, [FromBody] 무 CT는 ClientErrorFactory), TestHost 8.0.31 추가, 테스트 +69 |
| 2026-09-28 | S06-T01 | 기록 | handoff | T04 ConflictError.Create 사용법, T05 [FromBody] 없는 액션은 CT 없음이면 액션 도달(판정은 바인더), T06 UseStatusCodePages 415만 처리 |
| 2026-09-28 | S06-T01 | 결정 | 승인(대리) | 완료 조건 ② "Content-Type 없음"은 [FromBody] 경로 · [Consumes] 불일치 경로로 충족, 전용 바인더 경로의 Content-Type 없음은 T05 바인더가 판정(ADR-0026 1절) |
| 2026-09-28 | S06-T01 | reviewer | REJECT → developer | [컨벤션] ApiServiceCollectionExtensions.cs:114 `ImplementationType!` null-forgiving 금지. 테스트 주석 3곳 415 경로 정정(handoff). TD-029 new |
| 2026-09-28 | S06-T01 | tester | PASS | build 경고 0, format 0, test 통과 1,741 · 실패 0, check-docs 결함 4. StatusCodePagesPipelineTests +3(404 빈 본문 유지). 405 · 엔드포인트 415 확인은 T05로 |
| 2026-09-28 | S06-T01 | developer | PASS | 재작업 1: CreateInner `!` 제거(속성 패턴 switch + InvalidOperationException), 실패 테스트 +1, 테스트 주석 3곳 415 경로 정정. BB.Api 200/200. 후보는 TD-029에 포함 |
| 2026-09-28 | S06-T01 | reviewer | PASS | b33d371 반려 사유 해소, 새 위반 없음 |
| 2026-09-28 | S06-T01 | tester | PASS | build 경고 0, format 0, test 통과 1,737 · 건너뜀 4 · 실패 0(전체 1,741, 앞 행 1,741은 건너뜀 포함 수로 보임), check-docs 결함 4. 반려 1회 |
| 2026-09-28 | S06-T02 | dba | 해당 없음 | 파이프라인 표 dba 열 "해당 없음"(호출 생략) |
| 2026-09-28 | S06-T02 | developer | PASS | ImportTextDecoder(엄격 UTF-8) · CsvImportParser(상태 기계), 21019~21022 · 21027 사용, CR 단독 = 필드 문자, 공백만 줄 = 빈 줄. 테스트 +60 |
| 2026-09-28 | S06-T02 | 기록 | handoff | T03: Decode 뒤 Result<ImportParseResult>(ImportRow · ImportRowError) 공유, 21027 = EmployeeErrors.ImportTooManyRows. T04: 파서 실패 → 경로 "" FieldError, 행 오류 → Rows[n] |
| 2026-09-28 | S06-T02 | reviewer | PASS | 위반 없음. 제안(결과 리뷰로): 테스트 기대값의 보이지 않는 U+FEFF를 이스케이프로, "닫는 따옴표 뒤 문자 = 21021" 해석을 FR-03에도 적을지 |
| 2026-09-28 | S06-T02 | tester | PASS | build 경고 0, format 0, test 통과 1,798 · 건너뜀 4 · 실패 0, check-docs 결함 4. 여러 줄 따옴표 레코드 21021 행 번호 · 복구 +1 |
| 2026-09-28 | S06-T03 | dba | 해당 없음 | 파이프라인 표 dba 열 "해당 없음"(호출 생략) |
| 2026-09-28 | S06-T03 | developer | PASS | JsonImportParser, ImportField · ImportRowError.Field, 21023~21026 사용, 21022 서로게이트(GetString InvalidOperationException 좁은 catch), 입력 깊이 64, 테스트 +104 |
| 2026-09-28 | S06-T03 | 기록 | handoff | T04: Field None → Rows[n], 필드 있으면 Rows[n].Name 등, 속성 누락은 null → VO 필수 코드, 요청 전체(21022 · 21023 · 21027) 경로 "" |
| 2026-09-28 | S06-T03 | reviewer | REJECT → developer | [컨벤션] JsonImportParser.cs:126 `Error!` null-forgiving. [버그(문서)] error-codes.md:217 21022 예시 `\ud800` · `\udc00`이 U+FFFD 2개로 깨짐 |
| 2026-09-28 | S06-T03 | tester | REJECT → developer | error-codes.md:217 U+FFFD(사실 문장 오류). test 통과 1,902 · 건너뜀 4 · 실패 0. 보강 +3, 이스케이프 속성 이름 테스트 입력 복구. BL-136 new |
| 2026-09-28 | S06-T03 | developer | PASS | 재작업 1: 속성 패턴 switch로 `!` 제거, error-codes 217행 이스케이프 원문 복구(EF BF BD 0), 변경 파일 9개 grep 0. Application 173/173 |
| 2026-09-28 | S06-T03 | reviewer | PASS | d2d3aad 반려 2건 해소, 새 위반 없음 |
| 2026-09-28 | S06-T03 | tester | PASS | build 경고 0, format 0, test 통과 1,907 · 건너뜀 4 · 실패 0, check-docs 결함 4, U+FFFD 0. 반려 1회 |
| 2026-09-28 | S06-T04 | dba | 해당 없음 | 계획 리뷰 D5(스키마 · SQL 변경 없음), 호출 생략 |
| 2026-09-28 | S06-T04 | developer | PASS | Command · Validator(Cascade Stop, 21028 · 21029 경로 "") · Handler 5단계, 400 · 409 각 100개 잘림, EmployeeTextRules(21004 · 21009), 대상 대기 2개 해제, BL-130 · 132 문서. 테스트 +93. BL-137 · 138 new |
| 2026-09-28 | S06-T04 | 기록 | handoff | T05: EmployeeImportFormat(: short) · [Flags] EmployeeImportSources, 바인더는 출처 비트 모두 켬 · 빈 입력 Format 0. T06: 경로 규칙, 오류 있으면 21018 미보고, NUL email → 400 · 21004 · DB 0건 |
| 2026-09-28 | S06-T04 | reviewer | PASS | 위반 없음, CA1812 2건 승인 목록과 함께 승인. 제안: 테스트 빈 줄 정리 |
| 2026-09-28 | S06-T04 | tester | PASS | build 경고 0, format 0, test 통과 1,998 · 건너뜀 2 · 실패 0(순증 +89), check-docs 결함 4, U+FFFD 0. EmailTests U+200D 이스케이프 복구 |
| 2026-09-28 | S06-T05 | dba | 해당 없음 | 파이프라인 표 dba 열 "해당 없음"(호출 생략) |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모:

- T01 완료 조건은 S05-T01에서 확인하는 PRD-001 S02-T06 결과(ErrorType 매핑, `errors` 형식, CommonErrors, BL-019)에 따라 바뀐다. 이 스프린트 계획 리뷰에서 다시 확정한다.
- 파서(T02 · T03)는 S05-T02에만 의존해 T01과 독립이지만, 파이프라인은 작업 순서대로 진행한다.
- 위험을 앞에 두려고 POST(S06)를 GET(S07)보다 먼저 배치했다.
- 계획 리뷰에서 확인할 것: enum 1002 증빙을 Validator 단위 테스트로 대체할지(새 API에는 사용자가 enum을 직접 보내는 경로가 없음).
- S06-T04(Validator 규칙 2개) · S06-T05(Controller 규칙 1개) 완료 조건에 아키텍처 규칙 '대상 대기' 해제를 넣는다(S06 계획 리뷰에서 반영).

S05 인계 (S05 결과 리뷰 2026-09-28, 원문은 [S05 진행 기록](S05-rebase-decisions-schema.md#진행-기록)):

- **S06-T01**: S05-T01 기록 행의 'S06-T01 완료 조건 확정안' ①~⑦로 현재 완료 조건 ⑤('TD-010 · BL-023 처리 반영')를 대체한다(BL-023은 PRD-001에서 done, TD-010은 위험 수용). 상세 Conflict 오류 형식을 ErrorAndResultAreNotDerived 예외 목록에 넣기, `[Consumes]` 불일치 415의 공통 ProblemDetails 변환 실측, ErrorType 11 · 12 · 팩토리 2개, BadHttpRequestException 413 → 1004.
- **S06-T04**: ExistsByNormalizedEmailAsync는 삭제됨. `ListExistingNormalizedEmailsAsync`는 결과 순서를 보장하지 않으므로 Handler가 NormalizedEmail 값으로 행 번호와 짝짓고, Repository는 Distinct · 빈 목록 처리를 하지 않는다. 잘림 21030 · 23002, BOM · 공백만 입력 = 21028, 21028 · 21029의 PropertyName 결정과 error-codes 해당 목록. 대상 대기 규칙 2개 해제. BuildingBlocks.Api ProblemFieldError.cs:4 XML 주석 예시가 폐기 코드 21001 사용. BL-129 · BL-130 · BL-132.
- **S06-T05**: 폼 필드의 잘못된 UTF-8 바이트 치환 여부 실측, 바인더 InvalidDataException → 413 판정 기준, 대상 대기 Controller 규칙 1개 해제. (T01 결정) Content-Type 없는 요청은 [FromBody]가 없어 액션까지 온다. 바인더가 확장자 → 내용 판별로 처리하고, 판별 불가면 415 · 1005(Result `CommonErrors.UnsupportedMediaType`)로 거절하며 그 판정 결과를 진행 기록에 남긴다.
- **S06-T06**: 동시 경합 · 재전송 테스트 기대값은 3003 · 로그 202(TD-010, S05-T06 실측), InitialCreate를 다시 만들면 순서 재확인. S05 증빙 테스트 대응표 HTTP 행 '이전 대기: S06-T06' 닫기. BL-129 · BL-130 · BL-133. AppHost 볼륨은 새 스키마 상태이며 리셋 전 커밋 worktree에서 띄우면 42P07. 알려진 잡음 기준은 [database.md 알려진 잡음 로그](../../04-development/database.md#알려진-잡음-로그-첫-실행--재시작)가 원본(BL-117은 개수 기록, 판정은 완료 조건 기준).
- **S06-T02 · T03**: 서로게이트 · 제어 문자 테스트 데이터는 `DisableDiscoveryEnumeration = true`와 입력 보존 단언(BL-131).
- **공통**: S05 종료 CI에서 UUID v7 같은 밀리초 테스트 통과를 확인했으므로 S06에서는 결과만 참조한다.

### 사전 점검 (2026-09-28)

| 명령 | 결과 |
|---|---|
| `git pull --ff-only` | Already up to date, HEAD `ef9eb86`(스프린트 속도 튜닝) |
| 앞 스프린트 | S05 `status: done`, 태그 `sprint/S05` 있음 |
| `docker version --format '{{.Server.APIVersion}}'` | `1.43` → 로컬 통합 테스트에 `DOCKER_API_VERSION=1.43` |
| `dotnet --version` | `8.0.425` |
| `gh auth status` | Logged in (thkim-ezabele) |
| `dotnet dev-certs https --check --trust` | 종료 코드 `7`(미신뢰) → AppHost는 `--launch-profile http` |

### 계획 리뷰 결과 (2026-09-28, 대리 승인)

속도 튜닝 규칙(ef9eb86)으로 developer 1명이 리뷰하고 orchestrator가 통합했다. 작업 분할 · 추가 없이 T01~T06 완료 조건만 고쳤다(위 작업 표가 확정본).

- **정정 1 (T06 ②)**: S05 인계의 "경합 = 3003 · 로그 202"는 같은 ID 재전송(TD-010)에만 맞다. 다른 요청의 같은 이메일 경합은 ADR-0026 9절 · FR-06대로 409 · 23001(행 번호 없음)이다. S05 진행 기록 원문은 고치지 않는다.
- **정정 2 (T05 ③)**: InvalidDataException은 잘못된 multipart에서도 나므로 경로별 도달 위치를 실측하고 한도 초과만 413으로 보낸다.
- **정정 3 (T06 ③)**: .NET 8 TestServer는 `MaxRequestBodySize`를 강제하지 않으므로 테스트 안 실제 Kestrel 호스트를 쓴다.
- **결정**: D1 BL-129 PRD FR-01 email 규칙에 Cc 제어 문자 · 짝 없는 서로게이트 21004 거부 추가(PRD 문장 · 변경 이력은 이 계획 확정 커밋, 코드 · error-codes는 T04, HTTP는 T06). D2 BL-130 VO ToString 재정의 안 함, 로그 템플릿 인자 금지 규칙(T04). D3 JSON 짝 없는 서로게이트 이스케이프는 새 코드 없이 21022. D4 enum 1002 증빙은 Validator 단위로 대체. D5 T04 · T06 dba 단계 생략. D6 T06에 테스트용 실제 Kestrel 호스트.
- **위험**: T06 범위가 크다. 반려 2회에 이르면 ③ · ⑥을 T07로 나누는 안을 오케스트레이션 세션에 BLOCKED로 올린다(임의 분리 금지). T05 ④에서 폼 UTF-8 치환이 확인되면 BLOCKED 가능. NFR-02 CI 4초 판정은 PRD 종료 뒤 일괄이라 늦게 드러날 수 있다.

### 인계 메모

- **S06-T01**: TD-010은 ADR-0026 11절 위험 문구와 S05-T06 실측(pk · ux 동시 위반이면 pk_employees가 먼저 보고 → 3003)을 참조만 하고 코드는 바꾸지 않는다. BL-023은 PRD-001에서 done. 415 실측 경로는 ConsumesAttribute → UnsupportedMediaTypeResult → ClientErrorResultFilter, Content-Type 없음도 확인. 23505 매핑 경로는 바꾸지 않는다(바뀌면 dba 검토로).
- **S06-T02**: FR-03 줄 끝은 `\n` · `\r\n`뿐이므로 CR 단독은 필드 안 문자로 다루고 trim 결과를 테스트로 고정한다(FR-03과 어긋난다고 판단되면 BLOCKED). NUL 거부는 VO 몫(파서 아님). BL-131.
- **S06-T03**: 짝 없는 서로게이트는 `GetString()` 전에 확인하거나 예외를 잡아 21022로 바꾼다(방식은 developer, 속성 이름도 같은 판정). JsonDocument 최대 깊이는 기본 64 유지. error-codes 21022 설명에 "JSON 이스케이프의 짝 없는 서로게이트" 한 구절 추가. BL-131.
- **S06-T04**: 판정 순서 — 공백 집합은 BOM 뒤 0x20 · 0x09 · 0x0D · 0x0A만, Sources None → 21028, 정의 안 된 비트 → 1002, 두 비트 이상 → 21029, Format 1002는 빈 입력이 아닐 때만. `ListExistingNormalizedEmailsAsync`는 순서 미보장, Repository는 Distinct · 빈 목록 처리 안 함. reviewer는 VO · Command 로그 인자 금지와 로깅 데코레이터가 RequestName만 기록하는지 점검. ADR-0018 범위 예외는 이 Command에만. BL-130 · BL-132.
- **S06-T05**: S05-T01 기록상 multipart InvalidDataException은 현재 9001. FormValueProviderFactory의 ValueProviderException이 ModelState → 400 · 1001로 바뀔 수 있다(필요하면 이 액션에서 폼 값 공급자 미사용). 한도 초과 판정은 예외 형식 · 한도 초과 메시지 기준, api-guidelines에는 한 문단만.
- **S06-T06**: 알려진 잡음 · 제외 기준 — [database.md 알려진 잡음 로그](../../04-development/database.md#알려진-잡음-로그-첫-실행--재시작) N1~N5는 AppHost 로컬 실행 증빙용이라 Testcontainers 판정에는 적용하지 않는다. 테스트가 일부러 낸 로그(500 형식 테스트의 전역 예외, 경합 (a)의 23505 · EF 실패 로그(Debug), 재전송 (b)의 로그 202)는 이벤트 ID를 단언하고 제외한다. 그 밖의 ERROR · FATAL · Error · Critical은 제외하지 않고 기록해 판정받는다. AppHost를 띄우면 BL-117(첫 `/health/ready` Error 2건)은 개수만 기록(판정은 완료 조건 기준), 리셋 전 커밋 worktree에서 띄우면 42P07. 실행 — `DOCKER_API_VERSION=1.43`, 성능 측정은 `EnableSensitiveDataLogging` 끔. 재현 — 경합 hook은 테스트 호스트(ConfigureTestServices)에만, 제품 코드에 테스트 분기 없음. InitialCreate를 다시 만들면 pk · ux 보고 순서 재확인. 1,000행 × 9열(약 9,000 매개변수)은 Npgsql 한도 안, 재시도 배치 재전송은 TD-010 수용. BL-129 · BL-130 · BL-133. 대시보드 수동 확인(BL-024 나머지)은 S07-T04.

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
| 2026-09-28 | ① 계획 리뷰 | T01~T06 완료 조건 정정안 전체, D1(BL-129 PRD FR-01 email 규칙 변경)~D6 추천안, T06 반려 2회 시 분리안 BLOCKED 보고 조건 | 오케스트레이션 `emergency-hub-d2` | 2026-09-28 계획 리뷰 행 | 대기 |
| 2026-09-28 | 결정 (S06-T01) | 완료 조건 ② Content-Type 없음 해석: [FromBody] · [Consumes] 불일치 두 경로로 충족, 전용 바인더 경로는 T05 판정 | 오케스트레이션 `emergency-hub-d2` | 2026-09-28 S06-T01 결정 행 | 대기 |

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
| 2026-09-28 | orchestrator | S05 결과 리뷰: 계획 메모에 "S05 인계" 소절 추가 |
| 2026-09-28 | orchestrator | S06 계획 확정: 완료 조건 T01~T06 수정, T04 · T06 dba 생략, DoD CI · 태그 일괄, 사전 점검 · 인계 메모 · 대리 승인 절 추가, `active` |
