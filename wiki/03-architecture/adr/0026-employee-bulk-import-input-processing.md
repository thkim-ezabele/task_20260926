---
title: "ADR-0026: 직원 일괄 가져오기 입력 처리 (바인더, 파서, 행 검증, 단일 트랜잭션 예외)"
type: adr
adr: "0026"
status: accepted
date: 2026-09-28
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0026]
tags: [adr, architecture, api, database]
created: 2026-09-28
updated: 2026-09-28
---

# ADR-0026: 직원 일괄 가져오기 입력 처리 (바인더, 파서, 행 검증, 단일 트랜잭션 예외)

## 배경 (Context)

- `POST /api/employee`는 CSV · JSON을 파일 업로드와 직접 입력으로 받고(FR-05, [ADR-0025](0025-api-rule-exceptions-for-assignment-endpoints.md)), 한 요청에 최대 1,000행을 등록한다(NFR-01). **한 행이라도 실패하면 아무것도 저장하지 않고** 행별 오류를 돌려준다(FR-06, Q5).
- 입력 형식 규칙은 PRD가 정했다: CSV는 헤더 없음 · 열 순서 `name,email,tel,joined` · RFC 4180 따옴표 · 엄격 UTF-8(BOM 허용) · 물리 줄 번호(FR-03), JSON은 배열 · 단일 객체 · 대괄호 없는 나열(FR-04), 처리 순서는 파싱 → 행 검증 → 요청 안 이메일 중복 → DB 이메일 중복 사전 조회 → 여러 건 추가(FR-06, Q14). 필드 규칙은 Domain Value Object가 갖는다(FR-01, Q15).
- 기존 결정과 부딪히는 지점이 셋 있다.
  - [ADR-0018](0018-use-fluentvalidation.md): 검증은 Mediator 검증 데코레이터(Validator)에서만 실행하고, Validator는 DB에 접근하지 않는다.
  - [코딩 컨벤션 · 실패 처리 경계](../../04-development/coding-conventions.md#ddd-구현-규칙-aggregate--value-object)(S04): 요청 값만으로 판정하는 규칙은 Validator가 `Result`로, Aggregate는 같은 규칙을 불변식으로 다시 검사해 예외로 막는다.
  - [데이터베이스 · 트랜잭션 & 동시성 제어](../../04-development/database.md#트랜잭션--동시성-제어): Command 하나 = 트랜잭션 하나 = Aggregate 하나([ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md) 배경).
- Controller는 얇게 두고 `ISender`만 쓴다([ADR-0016](0016-use-controllers-for-api.md)). 파이프라인은 로깅 → 검증 → 트랜잭션(Command만) → Handler이고, 트랜잭션 데코레이터는 Handler를 트랜잭션 밖에서 한 번 실행한 뒤 성공이면 `IUnitOfWork.CommitAsync`를 부른다([ADR-0015](0015-custom-mediator-pipeline.md), ADR-0014, `TransactionCommandHandlerDecorator`).
- 새 패키지를 넣지 않는다(NFR-06, 넣으면 라이선스 기록). .NET 8의 `System.Text.Json`에는 여러 최상위 값을 읽는 옵션이 없다(PRD FR-04).

## 검토한 대안 (Options)

### 입력을 꺼내는 위치

1. **Controller 액션 매개변수 여러 개**(`IFormFile? file`, `[FromForm] string? data`, raw body): 장점: 프레임워크 기본 바인딩. 단점: raw body(`text/csv` · `application/json`)와 form 필드를 한 액션에서 함께 받기 어렵고, 판별 로직이 Controller에 들어가 얇은 Controller 규칙을 어긴다.
2. **Content-Type마다 액션을 나눔**: 장점: 액션마다 단순하다. 단점: 같은 경로 · 메서드에 액션이 4개가 되고 판별 · 변환 코드가 흩어진다.
3. **Employee.Api 전용 `IModelBinder`**: 장점: 전송 형식 처리가 한 곳에 모이고 Controller는 Command 변환과 `ISender` 호출만 한다. 바인더를 단위 테스트할 수 있다. 단점: 바인더와 Swagger OperationFilter를 직접 써야 한다.

### 행 검증 위치

1. **Validator에서 파싱 · 행 검증**: 장점: ADR-0018을 그대로 따른다. 단점: Validator가 파싱한 결과를 Handler에 넘길 방법이 없어 파싱이 두 번 일어나고(최대 1 MiB), "앞 단계가 실패하면 멈춤" 순서의 마지막 단계(DB 중복)는 Validator가 할 수 없어 행 오류가 두 곳에서 만들어진다.
2. **Api(바인더)에서 파싱**: 장점: Command가 파싱된 행을 받는다. 단점: 파서가 HTTP 레이어에 묶여 Application 밖에서 재사용 · 단위 테스트하기 어렵고, 파싱 오류가 `Result`가 아니라 모델 상태 오류(`1001`)로 뭉개진다.
3. **Handler에서 파싱 → 행 검증 → 중복 판정, 필드 규칙은 Domain Value Object**: 장점: 처리 순서와 "앞 단계 실패 시 멈춤"이 한 곳에 있고, 필드 규칙이 Domain 한 곳에 모인다. 단점: ADR-0018의 범위 예외가 필요하다.

### CSV 파서

1. **CsvHelper 등 외부 패키지**: 장점: 검증된 구현. 단점: 새 패키지(NFR-06)와 라이선스 기록, 행 번호(레코드가 시작하는 물리 줄) · 엄격 UTF-8 · 행별 오류 모음 규칙에 맞추려면 설정 · 감싸기가 필요하다.
2. **`Microsoft.VisualBasic.FileIO.TextFieldParser`(공유 프레임워크)**: 장점: 패키지 추가 없음. 단점: 형식 오류를 예외(`MalformedLineException`)로 알려 행별 오류 모음과 맞추려면 감싸야 하고, 행 번호 규칙과 맞는지 실측이 필요하다.
3. **직접 구현(상태 기계)**: 장점: 패키지 없음, 규칙(따옴표 · 줄 번호 · 오류 모음)을 그대로 코드로 옮기고 단위 테스트로 고정한다. 단점: 구현 · 테스트 비용.

### 여러 Aggregate 저장

1. **요청마다 Command 하나 · Aggregate 하나로 나눔**(행마다 트랜잭션): 장점: 기존 규칙 유지. 단점: 일부만 저장될 수 있어 FR-06(전부 거부)을 어긴다.
2. **도메인 이벤트 · 보상 처리로 나눔**: 장점: 규칙 유지. 단점: 중간 상태(일부 저장)가 드러나고, 이벤트 디스패치는 아직 없다(수집만).
3. **이 Command에 한해 여러 Aggregate를 트랜잭션 하나에 저장**: 장점: 원자성을 DB 트랜잭션이 보장한다. 단점: 규칙 예외, 트랜잭션 크기 상한이 필요하다.

## 결정 (Decision)

**입력은 Employee.Api 전용 바인더가 꺼내고, 파서는 Application, 필드 규칙은 Domain Value Object, 행 검증과 중복 판정은 `RegisterEmployeesCommand` Handler가 한다. ADR-0018 범위 예외와 다중 Aggregate 단일 트랜잭션 예외는 이 Command 하나에 한정한다.**

### 1. 전용 바인더 (Employee.Api)

- 액션 하나에 `[Consumes]` 4종(ADR-0025)을 두고, 매개변수는 Employee.Api 전용 `IModelBinder`가 만드는 `EmployeeImportPayload` 하나다.
- 바인더는 전송 형식에서 **입력 바이트 · 형식 · 입력 출처**만 꺼낸다. 파일 필드 `file`, 텍스트 필드 `data`(multipart · form-urlencoded), raw body(`text/csv` · `application/json`). 내용을 해석(파싱 · 검증)하지 않는다.
- **Command 모양**: `RegisterEmployeesCommand(EmployeeImportFormat Format, EmployeeImportSources Sources, ReadOnlyMemory<byte> Content)`로 둔다. PRD 초안 FR-06의 `(Format, string Content)`에서 바꾼 이유는 두 가지다(PRD FR-06은 S05-T02에서 이 모양으로 고쳤다).
  - 엄격 UTF-8 판정(잘못된 바이트 → 전용 코드, CP949 거부)은 바이트가 있어야 할 수 있고, FR-03은 이를 Application 파서 규칙으로 두었다. 문자열로 받으면 판정이 Api(바인더)로 가야 한다.
  - "`file`과 `data` 동시 전송 · 둘 다 없음"은 Validator가 판정한다(FR-05 · 06). Validator가 판정하려면 출처 정보가 Command에 있어야 한다. `EmployeeImportSources`는 `[Flags]` `int` enum(`None = 0`, `File = 1`, `Data = 2`, `Body = 4`, [ADR-0008](0008-integer-codes-and-bitmask.md))이고 DB에 저장하지 않는다.
- `EmployeeImportFormat`은 `short` enum(`Csv = 1`, `Json = 2`, `0` 예약)이고 DB에 저장하지 않는다.
- 폼 필드(`data`)는 프레임워크가 이미 문자열로 해독한 값을 받으므로, 바인더가 UTF-8로 다시 인코딩해 넘긴다. 이 경로의 잘못된 바이트는 전용 코드로 판정되지 않을 수 있다(아래 결과의 전제).
- 폼 한도 초과(`InvalidDataException`)는 `413`으로 응답한다([ADR-0028](0028-building-blocks-error-contract-extension.md)). 변환 위치(바인더)와 판정 기준은 S06-T05에서 실측해 정한다.

### 2. 형식 판별 순서

1. **Content-Type**: raw body는 요청 Content-Type, multipart 파일 필드는 그 파트의 Content-Type. `text/csv` → Csv, `application/json` → Json(매개변수 무시). 그 밖이거나 없으면 다음 단계.
2. **파일 확장자**: 파일 필드의 파일 이름이 `.csv` · `.json`(대소문자 무시)이면 그 형식. 그 밖이면 다음 단계.
3. **내용 추정**: BOM과 앞 공백을 건너뛴 첫 문자가 `[` 또는 `{`이면 Json, 그 밖이면 Csv.

- 요청의 `multipart/form-data` · `application/x-www-form-urlencoded`는 전송 형식이지 내용 형식이 아니므로 1단계에서 보지 않는다. 텍스트 필드 `data`는 3단계로 판별한다.
- 입력이 비어 있으면 형식을 정하지 않는다(`Format = 0`). Validator는 빈 입력을 먼저 보고하고, 입력이 있는데 형식이 정의되지 않은 값이면 `1002`로 거부한다.

### 3. 파서 레이어 (Application)

- CSV 파서와 JSON 파서는 Employee.Application의 `internal` 순수 클래스다(DI 등록 · I/O 없음). 입력은 바이트, 출력은 "행 번호 + 필드 문자열 4개" 목록 또는 행 오류 목록이다.
- **엄격 UTF-8 해독은 파서 앞 공통 단계**다. BOM(`EF BB BF`)은 허용하고 떼며, 잘못된 바이트는 요청 전체 오류(경로 `""`, `21022`)다.
- 행 수가 1,000을 넘으면 요청 전체 오류(경로 `""`, `21027`)로 멈춘다.
- 파서 오류 메시지와 예외에 입력 값을 넣지 않는다(NFR-04). `JsonException` 원문을 오류에 넣지 않는다(FR-04).

### 4. CSV 직접 구현

- 상태 기계로 직접 구현한다(패키지 추가 없음). 헤더 없음(모든 줄이 데이터, 헤더 지원은 BL-122 · TD-028), 열 순서 고정 `name,email,tel,joined`, 쉼표 구분, RFC 4180 따옴표 규칙(따옴표 안의 쉼표 · 줄바꿈 · `""`), `\n` · `\r\n`, 빈 줄 무시, 모든 필드 앞뒤 공백 제거.
- 행 오류: 열 개수가 4가 아님, 닫히지 않은 따옴표, 따옴표 없는 필드 안의 `"`.

### 5. JSON (대괄호 없는 나열)

- 받는 형태: 배열 `[{...}]`, 단일 객체 `{...}`, 대괄호 없는 나열 `{...},{...}`. **첫 공백 아닌 문자가 `{`이면 앞뒤를 `[` · `]`로 감싼 뒤** `JsonDocument`로 읽고 요소마다 검사한다. 세 형태는 같은 행 목록을 만든다.
- 속성 이름은 대소문자 무시, 알 수 없는 속성은 무시한다. 항목 오류: 값이 문자열이 아님, 요소가 객체가 아님(`null` · 숫자), 대소문자만 다른 중복 속성. 속성이 없으면 그 필드의 필수 코드다.
- 문법 오류(끝 쉼표, 주석, `[..],[..]`)는 요청 전체 오류(경로 `""`, `21023`)다. `JsonDocumentOptions` 기본값(끝 쉼표 · 주석 불허)을 쓴다.

### 6. 행 번호와 오류 경로

- 행 번호 n은 1부터다. **CSV는 레코드가 시작하는 물리 줄 번호**(빈 줄도 센다, 따옴표 안 줄바꿈이 있으면 첫 줄), **JSON은 항목 순서**(감싼 뒤 배열 순서)다.
- 경로(`FieldError.PropertyName`): 필드 오류 `Rows[n].Name` · `Rows[n].Email` · `Rows[n].Tel` · `Rows[n].Joined`, 행 전체 오류 `Rows[n]`, 요청 전체 오류 `""`. API 키는 기존 규칙대로 `rows[n].email`처럼 바뀐다(`FieldErrorKeys`, S05-T01 확인).
- 필드마다 첫 실패 하나만 보고한다(Value Object `Create`가 오류 하나를 돌려줌). 행이 여러 개 틀리면 모두 모은다.
- 요청 안 이메일 중복은 **두 행 모두** 표시한다(`rows[2].email`, `rows[5].email`, 같은 코드).
- **행 오류는 최대 100개까지 담는다.** 잘렸다는 표시는 101번째 항목으로 경로 `""`에 전용 코드 하나를 넣는다. `400` 목록은 Validation 유형 `21030`(`Employee.RowErrorsTruncated`), `409` 목록은 Conflict 유형 `23002`(`Employee.RowConflictsTruncated`)다([에러 코드 · Employee](../../05-api/error-codes.md#employee-에러-코드)).

### 7. 검증 위치와 ADR-0018 범위 예외

- **Validator(`RegisterEmployeesCommand`)는 겉모양만 본다**: 입력 출처(`Sources`가 `None`이면 빈 입력 `21028`, 둘 이상이면 동시 전송 `21029`), 빈 입력(`Content` 길이 0, 그리고 BOM이나 공백만 있는 입력도 빈 입력으로 본다. 코드 `21028`), 형식(`MustBeDefinedEnum` `1002`). Validator는 지금처럼 DB에 접근하지 않는다.
- **Handler가 다음 순서로 처리하고, 앞 단계가 실패하면 멈춘다**: ① 파싱 → ② 행 검증(행마다 Value Object `Create` 결과를 모음) → ③ 요청 안 이메일 중복(`NormalizedEmail` 서수 비교) → ④ DB 이메일 중복 사전 조회 → ⑤ Aggregate 생성 · 여러 건 추가.
- ①~③의 실패는 Handler가 `ValidationError`(`400`, `1001` + 행별 `errors`)를 만들어 돌려준다. ④의 실패는 상세 Conflict 오류(`409`, `23001` + 충돌 행 번호, ADR-0028)다.
- **ADR-0018 범위 예외: `ValidationError`를 검증 데코레이터 밖(Handler)에서 만드는 것은 `RegisterEmployeesCommand` Handler에만 허용한다.** 다른 Command · Query는 ADR-0018을 그대로 따른다. reviewer는 S06-T04에서 이 예외가 이 Handler에만 있는지 확인한다.

### 8. Value Object `Create` → `Result`의 적용 범위

- **Employee 필드 규칙(name · email · tel · joined)의 판정 원본은 각 Value Object(`Name` · `Email` · `PhoneNumber` · `JoinedOn`)의 `Create(string?)`가 돌려주는 `Result`다.** 실패는 필드 코드(Validation 유형) `Error` 하나를 담는다. 규칙과 길이 · 자리 수 상수는 Value Object 한 곳에만 두고, Handler · Validator · EF 설정은 그것을 호출하거나 참조한다(규칙을 다시 쓰지 않는다).
- **S04 실패 처리 경계의 세 분류는 유지한다.** 바뀌는 것은 첫 분류("요청 값만으로 판정할 수 있는 규칙 → `Result`")의 판정 위치다: Employee 필드 규칙은 Validator가 아니라 Value Object `Create`가 판정하고, 호출한 쪽(이 ADR에서는 Handler)이 결과를 `FieldError`로 옮긴다.
- `Employee.Register`는 검증된 Value Object만 받으므로 문자열 규칙을 다시 검사하지 않는다. `null` 인자 · 빈 ID처럼 요청 값이 아닌 불변식 위반은 지금처럼 예외(`ArgumentException` 계열)다. 저장 데이터에 따른 실패(DB 이메일 중복)는 `Result`다.
- **적용 범위는 Employee Value Object 4개다.** 다른 Aggregate의 Value Object가 같은 방식을 쓰려면 그 작업에서 코딩 컨벤션에 기준을 추가한다. Handler가 `ValidationError`를 만드는 것(7절의 범위 예외)은 이 적용 범위와 따로, `RegisterEmployeesCommand`에만 한정된다. 다른 Command가 Employee 필드를 받으면 그 Validator가 Value Object `Create`를 호출해 결과를 옮긴다.

### 9. 다중 Aggregate 단일 트랜잭션 예외

- 기준 규칙은 "Command 하나 = 트랜잭션 하나 = Aggregate 하나"다([데이터베이스 · 트랜잭션 & 동시성 제어](../../04-development/database.md#트랜잭션--동시성-제어), ADR-0014 배경). **`RegisterEmployeesCommand`에 한해 여러 Employee Aggregate를 한 트랜잭션에 저장하고, 상한은 1,000개다**(NFR-01).
- 근거: "한 행이라도 실패하면 아무것도 저장하지 않는다"(FR-06). 이벤트나 보상 처리로 나누면 부분 저장이 드러나므로 원자성을 DB 트랜잭션에 맡긴다. 새 Aggregate INSERT뿐이고 기존 행 UPDATE가 없어 `xmin` 충돌 경로가 없다.
- 적용 범위: 이 Command 하나. 다른 Command가 여러 Aggregate를 저장하려면 새 ADR.
- 사전 조회(Read Committed)는 경합을 막지 못한다. 동시에 같은 이메일을 등록하면 `ux_employees_normalized_email`의 `23505`가 최종 방어선이고, 결과는 `409` · `23001`(행 번호 없음, FR-06)이다.

### 10. DB 전제 (dba 검토, S05-T02 실측)

- `RegisterEmployeesCommand` 한 번 = 트랜잭션 하나(Read Committed, ADR-0014 UnitOfWork). 최대 1,000개 Employee Aggregate를 `INSERT ... RETURNING xmin`으로 저장한다. 실측(`postgres:17`): 한 트랜잭션에서 1,000행 반환, 모든 행의 `xmin`이 트랜잭션 ID 하나.
- 한 행이라도 제약을 위반하면 트랜잭션 전체가 롤백되어 0건 저장이다. 실측: 499행 INSERT 뒤 `23505` → 커밋 시도 ROLLBACK, 0건.
- DB 중복 사전 조회는 Write Repository 람다 LINQ 하나(쓰기 연결, UnitOfWork 트랜잭션 밖 — Handler 실행 시점, ADR-0014)이고, `normalized_email = ANY(@p)` 배열 파라미터 1개다(1,000개 원소 실측).
- 실행 전략 재시도 때 배치 전체를 다시 보낸다: `SaveChangesAsync(acceptAllChangesOnSuccess: false)`로 엔트리가 Added로 남고, ID는 Handler가 만든 값(`ValueGeneratedNever`)이라 같은 INSERT가 다시 나간다.
- EF 배치 크기(명령 분할 수)와 1,000행 처리 시간은 S06-T06(NFR-02)에서 실측 · 기록한다. 이 ADR은 배치 크기를 정하지 않는다.

### 11. 위험 (TD-010, 위험 수용 · open)

- 커밋 응답 중 연결이 끊기면 실제로 커밋된 배치를 실행 전략이 다시 보내 **정상 요청이 `409`(`3003`, 행 번호 없음)로 보일 수 있다.**
- `pk_employees`와 `ux_employees_normalized_email`의 검사 순서(같은 행이 둘을 함께 위반할 때 `3003`인지 `23001`인지)는 **S05-T06 실측 뒤 확정**한다.
- 해소는 Phase 4 전 `verifySucceeded` 또는 `Idempotency-Key`다([기술부채](../../10-delivery/tech-debt.md) TD-010).

## 결과 (Consequences)

- 긍정: 전송 형식(Api) · 내용 해석(Application 파서) · 필드 규칙(Domain) · 처리 순서(Handler)가 레이어마다 한 곳에 있다. 필드 규칙이 Value Object 한 곳이라 Validator와 Aggregate가 같은 규칙을 두 번 쓰지 않는다. 한 요청은 전부 저장되거나 0건이다. 새 패키지가 없다.
- 부정: ADR-0018(검증은 데코레이터에서만)과 "트랜잭션 하나 = Aggregate 하나"에 예외가 하나씩 생긴다. 로깅 데코레이터의 실패 로그(102)는 행 검증 실패도 `1001`로만 남긴다. 1,000행 트랜잭션은 단건보다 잠금 · WAL이 크고, 재시도 때 배치 전체를 다시 보낸다. CSV 파서의 구현 · 테스트 비용이 든다.
- 부정: Command가 PRD 초안 FR-06 문구(`string Content`)와 달리 바이트와 입력 출처를 가진다(1절). PRD FR-06 본문과 변경 이력은 S05-T02에서 고쳤다.
- 전제와 실측 작업:
  - 폼 필드(`data`)의 잘못된 UTF-8 바이트를 프레임워크가 어떻게 해독하는지(치환 문자 여부)와 그때 응답 코드(S06-T05 실측, 치환되면 한계로 기록).
  - 폼 한도 초과(`InvalidDataException`)를 `413`으로 바꾸는 지점과 판정 기준(S06-T05).
  - 람다 `Contains`가 `normalized_email = ANY(@p)` 배열 파라미터 하나로 번역되고 `ux_employees_normalized_email`을 쓰는지(S05-T06 EXPLAIN).
  - 값 변환기로 매핑한 Value Object 속성의 Where · OrderBy · 프로젝션 번역(S05-T04 첫 단계, 막히면 BLOCKED).
  - `pk` · `ux` 제약 검사 순서(S05-T06, TD-010).
  - 1,000행 등록 시간 2초 이내와 EF 배치 크기(S06-T06, NFR-02).
- 후속: Value Object 4개는 S05-T03, Aggregate · EF 매핑은 S05-T04, 사전 조회 Repository는 S05-T06, CSV 파서 S06-T02, JSON 파서 S06-T03, Command · Validator · Handler S06-T04, 바인더 · Controller · Swagger S06-T05, 통합 검증 S06-T06. 기준 문서(코딩 컨벤션 실패 처리 경계, 데이터베이스 트랜잭션 규칙의 예외 링크, 테스트 전략)와 에러 코드 표(`21007` ~ `21030`, `23002`)는 S05-T02에서 이 ADR에 맞췄다.
- 확인: 2026-09-28 오케스트레이션 세션 대리 확인((1) 적용 범위는 Employee Value Object 4개 (2) Command `(Format, [Flags] Sources, ReadOnlyMemory<byte> Content)` (3) 잘림 코드 `21030` · `23002`, 경로 `""` (4) BOM · 공백만 있는 입력은 빈 입력 `21028`). `/retro` ④ 추인 대상이다([S05 대리 승인](../../10-delivery/sprints/S05-rebase-decisions-schema.md#대리-승인)).
