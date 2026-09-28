---
title: "PRD-002: 직원 연락처 조회 · 일괄 등록 (CSV / JSON)"
type: prd
prd: "002"
status: stable
received: 2026-09-27
sprints: [S05, S06, S07]
branch: "feature/prd-002-employee-contacts"
pr: 8
release: "v0.2.0"
retro:
aliases: [PRD-002]
tags: [delivery, prd]
created: 2026-09-27
updated: 2026-09-28
---

# PRD-002: 직원 연락처 조회 · 일괄 등록 (CSV / JSON)

- 받은 날: 2026-09-27
- 스프린트: [S05](../sprints/S05-rebase-decisions-schema.md), [S06](../sprints/S06-bulk-register-api.md), [S07](../sprints/S07-query-api-docs.md) (S05-T01에서 확정)
- 토픽 브랜치: `feature/prd-002-employee-contacts` · PR: [#8](https://github.com/thkim-ezabele/task_20260926/pull/8) · 릴리스: `v0.2.0` · 회고: -
- 선행 토픽: [PRD-001 기반 구축](PRD-001-foundation.md) (병합 후 스프린트 시작)

## 원문

> 다른 세션에서 prd-001 에 대한 스프린트 진행을 하고 있고 지금 세션에서는 기본 인프라 구축뒤에 실제 개발되어야할 요구사항에 대해 서술할께
> 해당 프로젝트는 직원들의 긴급연락망이고 아래의 조건을 구현해야 함
>
> - [필수] 직원의 기본 역락정보 알수 있어야함
> - 직원정보 추가시 json, csv 두가지 형식 지원
>   - [필수] csv 업로드 또는 body 에 csv 직접 입력시 작동
>   - [필수] json 업로드 또는 body 에 json 직접 입력시 작동
>
> json ex) {"name":"...", "email":"...", "tel":"....","joined":"2000-01-01"},....
> csv ex) 김이름,kim@gmail.com,010-0000-0000,2000-01-01 .....
> 위 필드들은 필수값임
>
> 아래의 3개  endpoint 는 필수작성이며 필요한 endpoint 들은 추가 가능
>
> GET /api/employee?page={page}&pageSize={pageSize}
> - 전체 데이터를 보여주고 페이징 가능하도록 출력
> GET /api/employee/{name}
> - 요청한 name 과 일치하는 직원의 상세 연락정보를 반환
> POST /api/employee
> - response 201 (created)
> - <input type=file> 을 통해 csv, json 파일 업로드시 작동
> - <textarea></textarea> 에서 직접 데이터 입력했을 경우에도 작동됩니다.
>
> 위 3개의 api 는 필수이기 때문에 반드시 구현해야해 근데 2번째 api 의 경우 동명이인 이슈가 있을수 있으니 백로그로 남겨도 되고 아니면 v2 또는 별도의 endpoint 추가로 구현을 해도 될거 같아
> 일단 위 요구사항들 정리가 필요하고 특히 지금 병렬로 다른 세션 동작중이니 다른 세션과의 충돌 여부 확인하고 진행해야해

### 인터뷰 정리

| 항목 | 답변 |
|---|---|
| 진행 방식 | 별도 git worktree(`develop` 기준)에서 작성하고, 문서 전용 토픽 브랜치를 지금 만든다(병행 토픽, 규칙 예외). 스프린트 실행은 PRD-001 병합 뒤 |
| API 경로 | 과제 필수 경로(`/api/employee`)를 그대로 쓴다. 프로젝트 규칙(`api/v1/employees`)과 다른 점은 ADR로 기록 |
| 동명이인 | 이름 조회는 단건 반환, 동명이인 처리는 백로그 |
| 동명이인 중 반환 대상 · 목록 정렬 | 입사일이 빠른 직원. 목록은 입사일 → 등록 순 |
| 일괄 등록 실패 | 한 행이라도 잘못되면 전부 거부하고 행별 오류를 반환 |
| 입력 형식 | multipart(파일 필드 · 텍스트 필드)와 raw body(`text/csv`, `application/json`) 모두 |
| CSV 헤더 | 헤더 없음만 지원(모든 줄을 데이터로 본다). 헤더 지원은 백로그 · 기술부채로 남긴다 |
| 중복 · 전화번호 | 이메일은 유일(대소문자 무시, DB와 같은 요청 안 모두), 전화번호는 형식만 검사하고 입력 그대로 저장 |

## 분석

### 목적

긴급 상황에 연락할 직원의 **기본 연락정보(이름, 이메일, 전화번호, 입사일)** 를 시스템에 넣고 조회할 수 있게 한다. PRD-001이 만든 뼈대(BuildingBlocks, Employee 샘플, Aspire) 위에 첫 실제 기능을 올린다.

- 인사 담당자가 가진 명단(CSV 파일 · JSON)을 파일 업로드 또는 직접 붙여넣기로 한 번에 등록한다.
- 전체 명단을 페이지 단위로 보고, 이름으로 한 사람의 연락정보를 바로 찾는다.
- 과제 필수 API 3개를 명세 그대로 제공한다.

### 선행 조건

스프린트(S05~, 가번호)는 아래가 끝난 뒤 시작한다. 첫 작업(재기준화)에서 확인한다.

- PRD-001이 `develop`에 병합되고(`v0.1.0`), `develop`을 이 토픽 브랜치에 병합했다.
- PRD-001 S02-T06(공통 API 처리 · ProblemDetails 계약)이 확정되었다. FR-05 · FR-04의 BuildingBlocks 확장은 그 결과 위에서 설계한다.
- PRD-001 결과 리뷰에서 다음 항목의 처리 결과를 확인했다: TD-010(커밋 응답 중 연결 끊김 재시도 → 23505 오보고, 일괄 등록의 정상 요청이 409로 보일 위험), BL-019(매핑 없는 23505 공통 Conflict 코드), BL-023(정상 경합 23505에도 EF Error 로그), BL-024(로그 · 추적에 파라미터 값 없음 실측, NFR-04 전제).

### 기능 요구사항

| ID | 요구사항 | 우선순위 | 인수 조건 |
|---|---|---|---|
| FR-01 | **Employee 연락정보 모델 재설계.** 테이블 `employees`: `id` uuid(v7, ADR-0013), `name` varchar(100), `email` varchar(254)(입력 표기 보존), `normalized_email` varchar(254)(Domain이 `ToLowerInvariant`로 정규화) + **`ux_employees_normalized_email`**, `phone_number` varchar(20), `joined_on` date(`DateOnly`), `employee_status` smallint + `ck_employees_employee_status`(**유지**, 등록 시 Active=1 고정, API 비노출), `created_at` / `updated_at` timestamptz, `xmin`. 모두 NOT NULL. API 필드 이름은 `name` · `email` · `tel` · `joined`. 필드 규칙(Domain Value Object): name은 앞뒤 공백 제거 + NFC 정규화, 제어 문자 거부, 1~100자(UTF-16 단위) / email은 `local@domain` 하나, 공백 없음, domain에 `.` 포함, 254자 이하 / tel은 숫자와 하이픈만(`+` 불허, 맨 앞 · 맨 뒤 · 연속 하이픈 불허), 숫자 8~15자리, 전체 20자 이하, 입력 그대로 저장, 전화번호 중복 허용 / joined는 `yyyy-MM-dd` 정확 파싱(`2000-2-3` 거부), 1900-01-01 이상, 미래 날짜 허용. PRD-001 샘플의 `display_name`은 `name`으로 바꾸고, **샘플 API(`api/v1/employees`)는 제거**한다 | 상 | 도메인 단위 테스트가 필드별 성공 / 실패 / 엣지(공백만, 최대 길이, `2000-02-30`, 전화 하이픈 경계, 숫자 자리 경계, NFD 이름, 대소문자만 다른 이메일, joined 하한 · 미래)를 다룬다. PRD-001 증빙 테스트(ck 위반, 23505 변환, xmin 충돌, 읽기 연결 쓰기 거부, UUID v7 정렬, enum 1002)가 새 스키마 · API로 옮겨져 통과하고, 옮긴 **대응표**(이전 테스트 → 새 테스트)가 스프린트 문서에 남는다 |
| FR-02 | **스키마 리셋.** 운영 전 리셋 정책(ADR-0012)으로 `InitialCreate`를 다시 만든다. **리셋 전용 작업**으로 분리해 PRD-001 병합 뒤 한 번만 하고, 이 worktree에서 마이그레이션을 미리 만들지 않는다. 23505 제약 이름 매핑을 `ux_employees_normalized_email`로 교체한다. 인덱스: `ix_employees_joined_on_id (joined_on, id)`, `ix_employees_name_joined_on_id (name, joined_on, id)` | 상 | ADR-0012 절차대로 리셋 전용 커밋 1개, 진행 기록, database 변경 이력 한 줄, local-setup 볼륨 삭제 안내가 있다. dba가 `--idempotent` SQL(명명 · 타입 · ck · 인덱스)을 검토한 기록이 있다 |
| FR-03 | **CSV 파싱.** 직접 구현(상태 기계, 패키지 추가 없음), Application `internal` 순수 클래스. 헤더 없음, 열 순서 고정 `name,email,tel,joined`, 쉼표 구분, RFC 4180 따옴표 규칙, **엄격 UTF-8**(BOM 허용, 잘못된 바이트는 400 전용 코드), `\n` · `\r\n`, 빈 줄 무시, 모든 필드 앞뒤 공백 제거. 열 개수가 4가 아니거나, 닫히지 않은 따옴표, 따옴표 없는 필드 안의 `"`는 그 행의 오류. 행 번호는 **레코드가 시작하는 물리 줄 번호**(1부터, 빈 줄 포함) | 상 | 원문 예시 행(`김이름,kim@gmail.com,010-0000-0000,2000-01-01`)이 1건으로 등록된다. 열 부족 · 초과, 따옴표 안의 쉼표 · 줄바꿈, BOM, 마지막 줄 개행 없음, 빈 줄 사이 행 번호, CP949 바이트 거부를 단위 테스트로 검증한다 |
| FR-04 | **JSON 파싱.** 배열 `[{...}]`, 단일 객체 `{...}`, **대괄호 없는 나열** `{...},{...}`를 받는다(첫 공백 아닌 문자가 `{`이면 `[]`로 감싼 뒤 `JsonDocument`로 요소마다 검사, .NET 8에는 `AllowMultipleValues` 없음). 속성 이름은 대소문자 무시, 알 수 없는 속성 무시. 값이 문자열이 아니거나 없거나, 요소가 객체가 아니거나(`null` · 숫자), 대소문자만 다른 중복 속성이면 그 항목의 오류. 끝 쉼표, 주석, `[..],[..]`는 문법 오류(전용 정수 코드, 경로는 빈 문자열). 항목 번호는 1부터. 오류 메시지에 `JsonException` 원문을 넣지 않는다 | 상 | 세 형태가 같은 결과로 등록된다. 한 항목의 `"joined": 20000101`(숫자)은 그 항목 오류로만 보고된다. 문법 오류는 전용 코드 400이다 |
| FR-05 | **POST 입력 경로와 형식 판별.** 액션 하나에 `[Consumes]` 4종(multipart/form-data, application/x-www-form-urlencoded, text/csv, application/json). **Employee.Api 전용 `IModelBinder`** 가 전송 형식에서 텍스트와 형식만 꺼내 `EmployeeImportPayload(Format, Content)`를 만든다(파일 필드 `file`, 텍스트 필드 `data`, raw body). 형식 판별 순서: Content-Type → 파일 확장자(`.csv` · `.json`) → 내용 추정(`[` · `{`로 시작하면 JSON). Controller는 Command로 바꿔 `ISender`만 호출한다(ADR-0016). `file` · `data`는 nullable이고 조합 규칙은 Validator가 본다. Swagger OperationFilter로 파일 · 텍스트 · raw 입력을 시험할 수 있게 한다 | 상 | 아래 **입력 경로 표**의 모든 행이 기대 상태 코드와 정수 `code`로 응답한다 |
| FR-06 | **일괄 등록 Command.** `RegisterEmployeesCommand(EmployeeImportFormat Format, string Content)`(`Csv = 1`, `Json = 2`, 0 예약). Validator는 겉모양만(Format 1002, 빈 입력, `file`과 `data` 동시 전송). Handler가 **파싱 → 행 검증(Domain Value Object `Create` 결과 모음) → 요청 안 이메일 중복 → DB 이메일 중복 사전 조회**(Write Repository 람다 `= ANY`, 쓰기 연결) → `AddRange` 순서로 처리하고, 앞 단계가 실패하면 멈춘다. 행 검증을 Handler · Domain에서 하는 것은 ADR-0018 범위 예외, 1,000개 Aggregate 한 트랜잭션 저장은 이 Command에 한정한 예외(상한 1,000)로 ADR에 적는다. **한 행이라도 실패하면 아무것도 저장하지 않는다.** 오류: 파싱 · 필드 · **요청 안 중복**은 400 `ValidationError`(경로 `rows[3].email`, 1부터, Handler가 생성, 요청 안 중복은 두 행 모두 표시), **DB에 이미 있는 이메일**은 409 + 충돌 행 번호(BuildingBlocks에 상세 목록을 가진 Conflict 오류와 ProblemDetails `errors` 409 확장 추가), 동시 경합 23505는 제약 이름 매핑으로 409(**행 번호 없음**). 행 오류는 최대 100개까지 담고 잘렸으면 표시한다. 성공하면 **201** + `{ "count": N, "ids": [...] }`(ids는 입력 순서) | 상 | 전부 유효하면 N건 저장 · 201. 한 행 오류가 섞이면 0건 저장 · 400 · 해당 행 번호. 요청 안 중복(대소문자만 다른 경우 포함)은 400, DB 기존 이메일은 409 + 행 번호, 동시 요청 경합은 409(행 번호 없음). 통합 테스트로 "실패 시 0건 저장"을 DB에서 확인한다 |
| FR-07 | **`GET /api/employee?page={page}&pageSize={pageSize}`** 전체 목록 페이징. `page` 1부터(기본 1, 상한 100,000), `pageSize` 기본 20 · 1~100. 숫자가 아니면 1001(바인딩), 범위 밖이면 **1003**(`Common.InvalidPaging`). 정렬은 `joined_on` → `id`(등록 순) 고정. 응답 `{ "items": [...], "totalCount": N, "page": p, "pageSize": s }`, 항목은 `id` · `name` · `email` · `tel` · `joined`. Read Repository의 목록 · 개수 메서드를 나눠 Handler가 합친다(`COUNT(*) OVER()` 미사용) | 상 | 25건에서 `page=2&pageSize=10`이 11~20번째를, 마지막 페이지를 넘는 `page`는 빈 `items` · 200 · 올바른 `totalCount`를 반환한다. `page=0`, `pageSize=101`은 1003, `page=abc`는 1001이다 |
| FR-08 | **`GET /api/employee/{name}`** 이름이 정확히 일치(앞뒤 공백 제거 + NFC, 대소문자 구분)하는 직원의 상세 연락정보(`id` · `name` · `email` · `tel` · `joined`)를 반환한다. **동명이인이면 입사일이 빠른 1명**(같으면 등록 순). 없으면 404(Employee 대상 없음 코드), 공백 제거 뒤 빈 이름이면 400. `/`가 들어간 이름(`%2F`)은 보장하지 않는다(백로그) | 상 | 한글 이름(URL 인코딩) 200, NFD로 보낸 이름도 조회됨, 없는 이름 404, 동명이인 3명 중 입사일이 가장 빠른 1명 반환을 통합 테스트로 확인한다 |
| FR-09 | **결정 기록(ADR).** 구현 작업보다 먼저, 사용자 확인 후 `accepted`로 커밋한다. ① 과제 API 명세에 따른 규칙 예외: 버전 없는 단수 경로 `/api/employee`, `page` / `pageSize`, `name` 경로 매개변수, 일괄 201의 `Location` 생략, 적용 범위(이 3개 엔드포인트만), 413 / 415 처리 ② 직원 일괄 가져오기 입력 처리: 전용 바인더, 형식 판별 순서, 파서 레이어(Application), CSV 직접 구현, 대괄호 없는 JSON, 행 번호 규칙, ADR-0018 범위 예외, 다중 Aggregate 단일 트랜잭션 예외 ③ 이메일 대소문자 무시 유일(정규화 컬럼 + 일반 유니크 인덱스, citext · `lower()` 식 인덱스 · ICU collation을 쓰지 않는 이유) ④ BuildingBlocks 오류 계약 확장: 상세 Conflict 오류와 409 `errors`, `ErrorType`(PayloadTooLarge, UnsupportedMediaType)과 공통 코드. 번호는 PRD-001 병합 뒤 할당 | 상 | ADR이 사용자 확인 후 `accepted`로 커밋되고 구현보다 먼저 커밋된다. api-guidelines에 예외 절과 링크, error-codes에 새 공통 · Employee 코드가 추가된다 |
| FR-10 | **테스트.** 단위(CSV · JSON 파서, Value Object, Validator, Handler — NSubstitute), 통합(Testcontainers + `WebApplicationFactory`: 입력 경로 표 전체, 전부 거부 시 0건 저장, 413 / 415, 페이징 경계, 동명이인, 한글 URL, 동시 요청 23505), 원문 예시를 fixture 파일로 둔 인수 시나리오, **개인정보 테스트**(400 / 409 / 500 응답 본문과 캡처한 로그에 입력한 이름 · 이메일 · 전화번호 값이 없음 — 로깅 데코레이터, FluentValidation `{PropertyValue}`, 파서 예외, 23505 detail 경로) + Npgsql 추적 span 태그에 비밀번호 · 입력 값 없음(BL-024). 성공 / 실패 / 엣지 필수 | 상 | `dotnet test`와 CI가 통과하고, FR-01~08 인수 조건마다 테스트가 1개 이상 연결된다 |
| FR-11 | **문서.** API 명세(3개 엔드포인트, 행 오류 경로 규칙, 형식별 curl 예시 `-F file=@` · `-F data=` · `--data-binary` + Content-Type), 에러 코드 표(공통 413 / 415, Employee 파싱 · 필드 · 중복 코드), database(`employees` ERD · 인덱스 · 코드 표 · 일괄 트랜잭션 예외 링크 · 리셋 이력), local-setup 등록 · 조회 예시 | 중 | 해당 문서가 `draft` 이상이고, curl 예시를 Aspire로 띄운 상태에서 실행한 기록(worklog)이 있다 |

#### 입력 경로 표 (FR-05 인수 조건)

| 요청 | 기대 |
|---|---|
| multipart `file` = CSV 파일 / JSON 파일 | 201 |
| multipart `data` = CSV 텍스트 / JSON 텍스트 | 201 |
| form-urlencoded `data` = CSV 텍스트 / JSON 텍스트 | 201 |
| raw `text/csv` / raw `application/json` (대괄호 없는 나열 포함) | 201 |
| form-urlencoded, `data` 키 없음(`curl -d '김이름,...'`) | 400 (빈 입력 코드) |
| multipart `file`과 `data` 둘 다 / 둘 다 없음 | 400 |
| `text/plain` 등 지원하지 않는 Content-Type | 415 (공통 정수 코드) |
| 본문 1 MiB 초과(Kestrel `BadHttpRequestException`, multipart `InvalidDataException`) | 413 (공통 정수 코드), 0건 저장 |
| 1,000행 초과 | 400 (전용 코드), 0건 저장 |

### 비기능 요구사항

| ID | 요구사항 | 측정 기준 |
|---|---|---|
| NFR-01 | 입력 크기 제한 | 요청 본문 **전체 기준** 1 MiB(`RequestSizeLimit`, `RequestFormLimits.MultipartBodyLengthLimit` · `ValueLengthLimit`), 최대 1,000행. TestServer와 Kestrel의 `MaxRequestBodySize` 차이를 통합 테스트로 확인한다 |
| NFR-02 | 등록 성능 | 1,000행 CSV 등록 2초 이내. 통합 테스트(Testcontainers)에서 Stopwatch로 측정해 스프린트 진행 기록에 남기고, CI는 느슨한 임계값(2배)으로 확인한다. EF 배치 크기를 측정 · 기록한다 |
| NFR-03 | 조회 성능 | 10,000건(테스트 fixture 시더, 마이그레이션 시드 아님)에서 목록 조회 · 이름 조회 200ms 이내. 측정 방식은 NFR-02와 같다 |
| NFR-04 | 개인정보 | 로그 · 에러 `detail`에 이름 · 이메일 · 전화번호 **값**을 남기지 않는다(행 번호 · 필드 · 정수 코드로만 식별). SQL 파라미터 값 미기록(ADR-0020), 테스트 · 측정에서 `EnableSensitiveDataLogging` 끔. FR-10 개인정보 테스트로 검증한다. **BL-024 편입**(2026-09-28 사용자 결정): Npgsql 추적 span 태그(`db.connection_string` · `db.statement` 등)에 DB 비밀번호와 SQL 파라미터 값이 없음을 자동 테스트(`ActivityListener`)와 Aspire 대시보드 수동 확인 1회로 실측한다 |
| NFR-05 | 정수 코드 | 새 에러 코드 · 필드 오류 코드 · enum은 정수(ADR-0008). 번호는 PRD-001 병합 후 에러 코드 표 기준으로 할당 |
| NFR-06 | PRD-001 품질 기준 유지 | 경고 0, 아키텍처 테스트 통과, Employee Domain / Application 커버리지 80% 보고. 새 패키지 없음(추가하면 라이선스 기록) |

### 범위 밖 (Out of Scope)

아래 백로그 후보는 번호 충돌을 피하려고 재기준화 작업에서 `BL-NNN` / `TD-NNN`으로 등록한다.

- 동명이인 전체 조회 · 식별(목록 반환, ID 조회 엔드포인트, v2) → 백로그
- CSV 헤더 행 지원(현재 헤더를 넣으면 날짜 형식 오류로 거부) → 백로그 · 기술부채
- CP949(Excel 기본 저장) CSV 지원 → 백로그
- `/`가 들어간 이름의 경로 조회 → 백로그
- 직원 개인정보 보존 기간 · 삭제, 직원 수정 · 삭제, 조직 · 부서 정보 → 백로그
- 인증 / 권한(Identity 토픽). 이 토픽의 API는 인증 없이 열려 있다
- 프론트엔드 화면(HTML form 페이지). 검증은 Swagger · curl · 통합 테스트로 한다
- `Idempotency-Key`, 통합 이벤트 발행(브로커 보류, ADR-0023)
- Employee 외 서비스

### 영향 범위

| 대상 | 내용 |
|---|---|
| 서비스 / 바운디드 컨텍스트 | Employee(Domain · Application · Infrastructure · Api), BuildingBlocks(Domain: 상세 Conflict 오류 · `ErrorType` 확장, Api: ProblemDetails 409 `errors` · 413 / 415 변환, Infrastructure: `IRepository` 여러 건 추가) |
| 데이터 (DB, 스키마) | `emergency_hub_employee.employees` 재설계(FR-01), 인덱스 3개(`ux_employees_normalized_email`, `ix_employees_joined_on_id`, `ix_employees_name_joined_on_id`), `InitialCreate` 재생성(리셋 전용 커밋, ADR-0012), 23505 매핑 교체 |
| 이벤트 / API | 이벤트 없음. 추가: `GET /api/employee`, `GET /api/employee/{name}`, `POST /api/employee`. 제거: PRD-001 샘플 `api/v1/employees` |
| ADR 후보 | FR-09의 4건(API 규칙 예외, 일괄 가져오기 입력 처리, 이메일 대소문자 무시 유일, BuildingBlocks 오류 계약 확장) |

### 병행 진행 메모 (PRD-001과의 충돌 관리)

- PRD-001 스프린트가 같은 저장소 폴더에서 진행 중이라, 이 PRD는 **별도 git worktree**에서 작성한다. PRD-001 작업 트리와 브랜치는 건드리지 않는다.
- **토픽은 한 번에 하나** 규칙([개발 관리](../README.md))의 예외다. 이 토픽 브랜치에는 **새 파일(PRD, 스프린트 계획)만** 커밋하고, 공유 문서(`10-delivery/README.md` 목록, `backlog.md`, `tech-debt.md`, `roadmap.md`, 기준 문서, error-codes, database, api-guidelines)는 고치지 않는다. 공유 문서 반영은 재기준화 작업에서 한다.
- `/prd` 스킬에서 건너뛴 단계: ⓪-2 `develop` 위 작업(worktree로 대체), ⓪-3 진행 중 토픽 차단(사용자 승인으로 예외), ⓪-4 번호 자동 계산(`develop`에 PRD-001 문서가 없어 수동 지정), ⑦-4 README · roadmap 행 추가(재기준화로 연기).
- 번호: 스프린트 **S05~는 가번호**이고 PRD-001 병합 때 다시 확인한다. `BL` · `TD` · ADR · 에러 코드 번호는 이 토픽에서 미리 잡지 않고 재기준화 작업에서 할당한다.
- Draft PR은 지금 만들되, `.github/workflows/ci.yml`이 PRD-001 브랜치에만 있어 병합 전에는 CI가 돌지 않는다. PR 본문에 "문서 전용, PRD-001 병합 전 스프린트 금지"를 적는다.
- 링크 주의: ADR 0011~0023, PRD-001 등 PRD-001에서 만든 문서는 `develop`에 병합되기 전까지 이 브랜치에서 열리지 않는다.
- 회고 개선안 후보: `/prd` 스킬에 "병행 문서 토픽" 절차를 넣을지.

## 리뷰 요약

> `/prd`의 병렬 리뷰(orchestrator / dba / developer) 결과와 반영 내용을 요약합니다.

| 관점 | 주요 발견 | 반영 |
|---|---|---|
| orchestrator | 병행 토픽이 `/prd` 절차 · 번호 체계 · 공유 문서와 충돌, PRD-001 샘플과 증빙 테스트 처리 미정, 행 단위 오류를 현재 계약으로 표현 불가, 페이징 코드 1003 존재, `curl -d` · `text/plain` 기대 응답 누락, 기존 TD-010 · BL-019 · 023 · 024 위험, 개인정보 검증 수단 없음, 인수 조건 경계 모호 | B1 → 병행 진행 메모 · 선행 조건, B2 → FR-01 · 02, B3 → FR-06, FR-05 입력 경로 표, FR-07(1003), FR-10 개인정보 테스트, NFR-02 · 03 증빙 방식 |
| dba | 일괄 저장이 "트랜잭션 하나 = Aggregate 하나" 규칙의 예외, 경합 23505는 행 번호 불가, 이메일 유일은 정규화 컬럼 권장, 리셋은 전용 작업으로 분리, `employee_status` 유지, 컬럼명 `joined_on` · `phone_number`, 복합 인덱스, 한글 NFC, 목록 · 개수 쿼리 분리 | FR-01 · 02 · 06 · 07 · 08, FR-09 ③, NFR-03 |
| developer | 파서는 Application + Domain Value Object((C)안), 409 행 번호는 BuildingBlocks 확장 필요, 요청 안 중복은 400, 다중 Content-Type은 전용 바인더 + `[Consumes]` + OperationFilter, 413 / 415 코드 없음, .NET 8 JSON 나열 처리, CSV 직접 구현 · 행 번호 · UTF-8, 페이징 오버플로 | B3 · B4 → FR-05 · 06, FR-03 · 04, FR-07, FR-09 ② · ④ |

## 질문과 답변

| # | 질문 | 답변 | 반영 |
|---|---|---|---|
| Q1 | 병행 세션과 충돌 없이 어떻게 진행하나 | 별도 worktree에서 작성, 문서 전용 토픽 브랜치를 지금 생성(규칙 예외), 스프린트는 PRD-001 병합 후. 건너뛴 스킬 단계는 병행 진행 메모에 기록 | 병행 진행 메모 |
| Q2 | 필수 경로가 프로젝트 규칙과 다름 | 요구 경로 그대로, ADR로 기록 | FR-09 ① |
| Q3 | 동명이인 처리 | 단건 반환 + 동명이인은 백로그 | FR-08, 범위 밖 |
| Q4 | 동명이인 중 반환 대상 · 목록 정렬 | 입사일 빠른 직원, 같으면 등록 순 | FR-07, FR-08 |
| Q5 | 일괄 등록 중 일부 실패 | 전부 거부, 행별 오류 반환 | FR-06 |
| Q6 | POST 요청 형식 | multipart(파일 · 텍스트 필드) + raw body 모두 | FR-05 |
| Q7 | CSV 헤더 | 헤더 없음만. 헤더 지원은 백로그 · 기술부채 | FR-03, 범위 밖 |
| Q8 | 중복 · 전화번호 규칙 | 이메일 유일(대소문자 무시), 전화는 형식만 검사하고 그대로 저장 | FR-01, FR-06 |
| Q9 | 대괄호 없는 JSON 나열(`{...},{...}`)을 받는가 | 확정: 받는다(배열 · 단일 객체와 함께). | FR-04 |
| Q10 | 페이징 기본값 · 크기 제한 | 확정: `page` 1부터(상한 100,000), `pageSize` 기본 20 · 최대 100, 본문 전체 1 MiB · 1,000행. | FR-07, NFR-01 |
| Q11 | `joined` 날짜 범위 | 확정: 미래 허용, 하한 1900-01-01. | FR-01 |
| Q12 | 병행 토픽 예외 범위 (B1) | 새 파일(PRD, 스프린트 계획)만 커밋, 공유 문서 수정 안 함, Draft PR 지금 생성, S05~ 가번호, 첫 작업은 재기준화 | 병행 진행 메모, 선행 조건 |
| Q13 | PRD-001 샘플 · 마이그레이션 처리 (B2) | 샘플 API 제거, `employee_status` 유지(Active 고정 · 비노출), `display_name` → `name`, `InitialCreate` 리셋은 전용 작업, 증빙 테스트 이전 + 대응표 | FR-01, FR-02 |
| Q14 | 행 단위 오류 계약 (B3) | 요청 안 중복 400(두 행 표시), DB 중복 409 + 행 번호(BuildingBlocks 상세 Conflict 추가), 경합 23505 409는 행 번호 없음, 처리 순서 파싱 → 행 검증 → 요청 안 중복 → DB 중복 | FR-06, FR-09 ④ |
| Q15 | POST 입력 구조 · 413 / 415 (B4) | Employee.Api 전용 바인더, 파서는 Application, 필드 규칙은 Domain Value Object(ADR-0018 범위 예외), 413 / 415는 `ErrorType`과 공통 정수 코드 추가 | FR-05, FR-06, FR-09 ② · ④ |
| Q16 | 세부 기본값(이메일 두 컬럼, 컬럼명 `joined_on` · `phone_number`, tel `+` 불허 · 20자 · 중복 허용, name trim · NFC · 제어 문자 거부, CSV 직접 구현 · 물리 줄 번호 · 전 필드 trim · 엄격 UTF-8, JSON 끝 쉼표 · 주석 거부, `data` 없는 form 400 · `text/plain` 415, 행 오류 최대 100개, NFR 증빙 방식) | 확정: 리뷰 통합 추천안. | FR-01 · 03 · 04 · 05 · 06, NFR-02 · 03 |
| Q17 | BL-024(추적 · 로그의 비밀번호 · 파라미터 값 미노출 실측)를 편입하는가 | 편입. 자동 테스트는 S06-T06, 대시보드 수동 확인은 S07-T04. .NET 10 전환(BL-002)은 진행하지 않음 | NFR-04, FR-10 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | PRD 접수, 인터뷰 정리, 초안 작성 (별도 worktree) |
| 2026-09-27 | - | 병렬 리뷰(orchestrator / dba / developer) 통합 반영, 차단 질문 4건(B1~B4) 답변 반영, 선행 조건 · 입력 경로 표 추가, FR 11개로 재구성 |
| 2026-09-27 | - | 스프린트 분할(S05~S07 가번호, 작업 15개) 승인, 세부 기본값(Q9~Q11, Q16) 확정, `stable` |
| 2026-09-27 | - | Draft PR #8 연결 |
| 2026-09-28 | - | develop(v0.1.0) 병합, BL-024 편입(NFR-04 · FR-10, Q17), .NET 10 전환 미진행 기록 |
| 2026-09-28 | developer | S05-T01 재기준화: 스프린트 번호 S05~S07 확정 |
