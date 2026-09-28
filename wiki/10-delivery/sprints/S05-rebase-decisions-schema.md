---
title: "S05: 재기준화, 결정 기록, 새 직원 모델과 스키마"
type: sprint
sprint: "S05"
status: active
prd: [PRD-002]
started: 2026-09-28
finished:
adrs: [ADR-0025, ADR-0026, ADR-0027, ADR-0028]
worklogs: []
aliases: [S05]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-28
---

# S05: 재기준화, 결정 기록, 새 직원 모델과 스키마

- PRD: [PRD-002](../prd/PRD-002-employee-contacts.md)
- 토픽 브랜치: `feature/prd-002-employee-contacts` · 스프린트 종료 태그: `sprint/S05`
- 스프린트 번호는 S05-T01에서 확정했다(PRD-001 S01~S04 다음 번호, PRD `sprints` · 파일 이름 · 작업 ID 일치).

## 목표

> PRD-001 결과 위로 토픽을 다시 맞추고 ADR 4건을 `accepted`로 확정한다. 새 `employees` 스키마(`InitialCreate` 재생성)가 Aspire(MigrationService)와 Testcontainers에 적용되고, Domain Value Object 규칙과 DB 규칙(PRD-001 증빙 테스트 이전분)이 테스트로 통과한다. 이 스프린트 동안 HTTP API는 없다(샘플 API 제거).

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S05-T01 | 재기준화 (문서 작업) | NFR-05, NFR-06 | ① `develop`(`v0.1.0` 포함)이 토픽 브랜치에 병합되었음을 `git merge-base --is-ancestor`로 확인하고 병합 커밋 해시(`7160967`)를 진행 기록에 남겼다 ② 스프린트 번호(PRD `sprints` · 파일 이름 · ID 일치), ADR 번호 0025~0028, 에러 코드 범위를 확정했다 ③ 범위 밖 항목을 `BL` / `TD`로 등록했고 BL-024는 `planned:S06`이다 ④ `10-delivery/README` 목록 · roadmap에 PRD-002 행이 있고, S06 · S07 문서의 S05 작업 참조가 새 번호와 맞으며, check-docs 결함이 기준선(4, BL-018) 이하다 ⑤ TD-010(위험 수용 · open) · BL-019 · BL-023 · BL-024 처리 결과와 S02-T06 ProblemDetails 계약 확인 결과(S06-T01 완료 조건 확정안)를 진행 기록에 남겼다 ⑥ `v0.1.0` 기준 전체 테스트 이름 기준선을 저장하고, 이를 바탕으로 증빙 테스트 대응표 틀을 이 문서에 만들었다 ⑦ `dotnet build` 경고 0, `dotnet test` 통과(`DOCKER_API_VERSION=1.43`)를 확인했다 | PRD-001 병합 | done | 456b872 · 4256a4c · f00663a |
| S05-T02 | ADR 4건과 기준 문서 반영 (문서 작업, 사용자 확인) | FR-09, FR-11, NFR-05 | ① ADR 4건(0025~0028)을 1차 초안 → 확인(대리) → 2차 작성으로 `accepted` 커밋했고, 이 커밋이 T03 이후 모든 구현 커밋보다 앞선다 ② ADR ②에 ADR-0018 범위 예외(행 검증은 `RegisterEmployeesCommand` Handler 한정), VO `Create` → Result를 Employee 필드 규칙 판정 원본으로 삼는 적용 범위, 다중 Aggregate 단일 트랜잭션 예외(상한 1,000), TD-010 위험 문구가 있다 ③ api-guidelines에 예외 절(적용 범위 3개 엔드포인트)과 ADR 링크가 있다 ④ error-codes Employee 표에서 21001 · 21002 · 21006을 `폐기`로 표시하고, 21003~21005 · 22001 · 23001은 뜻을 유지한 채 설명만 갱신했으며(23001은 `ux_employees_normalized_email`), 새 Employee 코드와 공통 413 · 415를 정수로 할당하고 행마다 구현 작업 ID(예약)를 표시했다 ⑤ coding-conventions(실패 처리 경계, 이메일 string 문구, Register 예시), database.md VO 매핑 원칙(단일 값 VO는 값 변환기), clean-architecture 트리 주석(EmployeeEmail), testing-strategy 아키텍처 절("대상 대기" 목록)이 ADR과 일치한다 ⑥ ADR 목록과 09-memory/design 갱신 대상을 표시했고 check-docs 새 결함이 0이다 | T01 | done | 6ee9956 · bd0cbc7 · 585cc05 |
| S05-T03 | Employee Value Object 4개 (Domain 로직, 기존 Aggregate 미변경) | FR-01, FR-10, NFR-05 | ① Name · Email(입력 표기 + NormalizedEmail) · PhoneNumber · JoinedOn은 sealed record이고 `Create(string?)`가 T02에서 할당한 필드 코드 Error를 담은 Result를 반환한다 ② FR-01 필드 규칙(인계 메모의 세부 기본값)을 모두 구현했다 ③ 단위 테스트가 FR-01 인수 조건 엣지 목록 전부와 규칙마다 성공 / 실패 / 엣지를 다룬다 ④ 길이 · 자리 수 상수는 VO 한 곳에 public const로 두어 EF 설정이 참조할 수 있다 ⑤ EmployeeErrors에 새 코드 상수를 추가하고 EmployeeErrorsTests 표를 error-codes와 1:1로 갱신했다(폐기 상수 삭제는 T04) ⑥ Employee Aggregate · Application · Infrastructure는 바뀌지 않았고 빌드 경고 0, 전체 테스트(단위 · 아키텍처 · 통합)가 통과한다 | T02 | done | f4f4117 · 824a0a6 |
| S05-T04 | Employee Aggregate 재설계, EF 매핑 · 23505 교체, 샘플 API 제거 (마이그레이션 제외) | FR-01, FR-02, FR-10, NFR-06 | ① `Employee.Register`는 검증된 VO만 받고 `employee_status`를 Active=1로 고정하며, `display_name` → `name`, EmployeeRegisteredDomainEvent는 유지한다 ② EmployeeConfiguration이 dba 명세(database.md, "실측 전" 표시)의 컬럼 · 타입 · NOT NULL · `ck_employees_employee_status` · `ux_employees_normalized_email` · `ix_` 2개와 일치하고, 23505 매핑은 `ux_employees_normalized_email` → 23001이다 ③ 샘플 API · Command · Query · Validator · Handler와 폐기 코드 상수(21001 · 21002 · 21006)를 제거했고, 샘플 의존 테스트는 대응표 "처리" 열대로 삭제 · 수정했으며(Skip 금지) 샘플 잔존 grep이 0이다 ④ 대상이 0개가 되는 아키텍처 규칙 3개는 해제 작업 ID(S06-T04 · S06-T05)를 적은 "대상 대기" 목록으로만 건너뛰고, 대기 규칙에 대상이 생기면 실패하는 안전장치 테스트가 있다 ⑤ EmployeeModelMetadataTests · DI 등록 테스트는 새 스키마 기대값으로 통과하고, EmployeeMigrationsTests 2건은 새 스키마 기대값으로 먼저 바꿨다 ⑥ 빌드 경고 0, 컴파일 실패 0이고, 실패 테스트는 진행 기록에 확정한 허용 목록(trx 이름)과 1:1이다 | T03 | done | 1571a83 · 9b4ff4f · e46baea |
| S05-T05 | 스키마 리셋 전용 작업 (`InitialCreate` 재생성, ADR-0012) | FR-02, FR-11 | ① 커밋 R 하나에 `Persistence/Migrations/` 변경만 있다(옛 파일 삭제, 새 `InitialCreate` 생성 3개 + 손으로 쓴 Sealed 2개), footer 없음 ② 커밋 D(`Stage: dba`)에 `--idempotent` SQL 검토 기록(명명 · 타입 · ck · 인덱스 3개 생성 SQL 원문)과 진행 기록(사유, 로컬 볼륨 삭제 필요)이 있다 ③ 커밋 D에서 database.md(변경 이력 한 줄, 적용 범위 실측 표, ERD · 인덱스 · 코드 표 draft, InitialCreate 대조 표 새 ID, Sealed 문구)와 local-setup 볼륨 삭제 안내를 갱신했다 ④ `emergency-hub-postgres-data` 볼륨 삭제 뒤 AppHost(http 프로필)에서 MigrationService가 종료 코드 0, Controller 없는 Api의 `/health/ready`가 200이다 ⑤ Testcontainers `MigrateAsync` 적용, `dotnet test` 전체 통과, T04 허용 목록이 모두 green이고 건너뜀은 "대상 대기" 3 + 기존 1이다 ⑥ developer 단계 코드 변경 0(필요하면 T04로 반려) | T04 | doing | |
| S05-T06 | Repository 구현과 PRD-001 DB 증빙 테스트 이전 | FR-01, FR-06, FR-07, FR-08, FR-10, FR-11 | ① `IEmployeeRepository`에 정규화 이메일 목록 중 이미 있는 값을 반환하는 조회(람다 `Contains` → `= ANY`, 쓰기 연결)와 AddRange가 있고 BuildingBlocks는 바뀌지 않았다 ② Read Repository는 목록(`joined_on`, `id` 정렬, Skip / Take), 개수, 이름 단건 프로젝션(동명이인이면 `joined_on`, `id` 순 첫 1명)을 람다 LINQ로만 제공한다 ③ 새 스키마로 옮겨 통과: ck 위반 거부, 23505 → 23001 Conflict(대소문자만 다른 이메일, 입력 표기 보존 포함), xmin 충돌, 읽기 연결 쓰기 거부, UUID v7 DB 정렬, 감사 컬럼 UTC, NFD 이름의 NFC 저장 ④ 목록 · 이름 · `= ANY` 쿼리 3개의 EXPLAIN 인덱스 사용을 진행 기록에 남겼다 ⑤ 대응표의 DB 수준 행이 모두 이전 완료, HTTP 수준 행은 "이전 대기: S06-T06 / S07-T03"이고, 대응표가 T01 기준선과 1:1이다 | T05 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S05-T01 | 마이그레이션 · 23505 매핑 현황 확인(진행 기록에만, database.md 변경 없음) | 문서 작업: 번호 확정, 공유 문서 반영, 백로그 등록, 계약 확인 기록, 테스트 기준선 · 대응표 틀, S06 · S07 참조 수정 | 문서 점검표 D1~D5, 번호 충돌 없음, 기존 ADR 불변 | 명령 점검표: 병합 확인, 링크 점검, build / test 출력, 기준선 개수 |
| S05-T02 | ADR ③(정규화 컬럼) · ② 다중 Aggregate 트랜잭션 · TD-010 위험 문구의 DB 내용 검토 | 1차 초안 반환 → 확인(대리) → 2차 파일 작성, 기준 문서 5종 반영 | ADR 템플릿, PRD FR-05 · 06 · 09 · Q14 · Q15와 일치, 코드 중복 없음, D1~D5 | 명령 점검표: ADR 커밋 순서, 코드 번호 중복 0(예약 행 별도 집계), 링크 |
| S05-T03 | 해당 없음 | TDD: Value Object 4개, EmployeeErrors 새 상수 | 표준 진입 점검, Domain 프레임워크 비의존, NFC · 소문자화 위치 | FR-01 인수 조건 대조 |
| S05-T04 | 스키마 명세(컬럼 · 인덱스 · 제약 이름 표)를 database.md 초안에 기록("명세(실측 전, S05-T05에서 확정)") | TDD: Aggregate, EF 설정, 23505 매핑, 샘플 제거, 대상 대기, 대응표 처리 | 표준 진입 점검(마이그레이션 적용 점검은 T05로), 샘플 잔존 grep 0, 대상 대기 목록 · 안전장치 | 허용 목록(trx 이름)과 실제 실패 1:1 대조 |
| S05-T05 | 주 작성자: 커밋 R(리셋만, footer 없음) → 커밋 D(기록 · 문서, `Stage: dba`) | 코드 변경 없음. 빌드 · 테스트 재실행 결과만 기록. 코드 수정이 필요하면 T04로 반려 | `git show --stat R`로 Migrations 경로만 · footer 없음 · 명세 일치 확인 | 볼륨 삭제 후 AppHost 실행 기록, Testcontainers 적용, `\d employees` 대조, 허용 목록 green 확인 |
| S05-T06 | 쿼리 인덱스 사용 EXPLAIN 검토, TD-010 제약 검사 순서 1회 실측, 테스트 DB 구성(Respawn, read-only 연결) | TDD(Testcontainers), 람다 LINQ만 | 표준 진입 점검, 대응표가 T01 기준선과 1:1 | 대응표 대조, 통합 테스트 실행 |

### 인계 메모 (handoff)

> 계획 리뷰(2026-09-28)에서 확정한 작업별 세부 단언입니다. 단계 호출 때 해당 작업 항목만 넘깁니다.

**공통**

- 로컬 `dotnet test`는 `DOCKER_API_VERSION=1.43`을 붙인다(BL-102). AppHost는 `--launch-profile http`(개발 인증서 미신뢰, BL-099).
- 볼륨은 `emergency-hub-postgres-data`만 지운다(다른 프로젝트 · 익명 볼륨 금지).
- 세부 기본값의 원본은 이 절이다. PRD가 정하지 않은 규칙을 작업자가 새로 정하지 않는다(애매하면 BLOCKED).

**S05-T01**

- 대응표 머리말: "이전 테스트 이름은 `v0.1.0` 기준". 열: 이전 테스트(전체 이름) | PRD-001 FR Trait | 처리 | 새 테스트 또는 작업 ID | 상태.
- "처리" 계획값
  - 삭제(HTTP 샘플, 이전 대기 S06-T04 / S06-T05 / S06-T06 / S07-T01~T03): Http/EmployeeRegistrationHttpTests, ProblemDetailsHttpTests, OpenApiContractHttpTests, ReadOnlyWriteRejectionHttpTests(HTTP 부분), RequestCompletionLogTests, Api.UnitTests EmployeesControllerTests, Application.UnitTests RegisterEmployee* · GetEmployeeById* · ContractShapeTests.
  - 수정(T04, T05 뒤 green): Schema/EmployeeSchemaTests, FaultInjection/TestTriggersTests, Persistence/EmployeePersistenceRoundTripTests, PersistenceLogExposureTests(샘플 의존 부분은 삭제 후 S06-T06), Fixtures/EmployeeApiFactoryTests(샘플 의존 부분), Api.UnitTests ProgramTests(라우트 의존 부분).
  - 이전(T06): ck · 23505 · xmin · 읽기 연결 쓰기 거부 · UUID v7 · 감사 UTC.
  - 변경 없음(경로 리터럴): ServiceDefaults HealthEndpointsTests InlineData, BuildingBlocks 테스트의 리터럴 표본.
- 테스트 이름 기준선은 trx 또는 `--list-tests`로 뽑아 스크래치에 저장하고, 개수(통과 · 건너뜀)를 진행 기록에 적는다.
- 명령 점검표: `git merge-base --is-ancestor v0.1.0 HEAD`, 병합 해시, check-docs 기준선 4, build 경고 0, `DOCKER_API_VERSION=1.43 dotnet test`.
- dba 현황(진행 기록에만): 마이그레이션 `20260927134235_InitialCreate` 1건(product_version 8.0.31), 23505 매핑 `ux_employees_email` → 23001 1건, Migrations 폴더 5개.
- S06 · S07 참조 수정(T01 커밋에 포함): S06 문서 31행 S06-T01 의존 `S05-T05` → `S05-T06`, S07 문서 42행 `S05-T05` → `S05-T06`. S06 문서 계획 메모에 "S06-T04(Validator 규칙 2개) · S06-T05(Controller 규칙 1개) 완료 조건에 아키텍처 규칙 '대상 대기' 해제를 넣는다(S06 계획 리뷰에서 반영)"를 적는다.
- TD-010은 위험 수용 · `open` 유지(사용자 사전 합의). 행에 "S05-T06에서 pk · ux 검사 순서 1회 실측, ADR ② 위험 항목"을 적는다.

**S05-T02**

- 새 Employee 코드는 21007부터(`S T NNN`) 규칙마다 하나. name: 필수 · 길이 · 제어 문자. tel: 필수 · 허용 문자 · 숫자 자리 수 · 전체 길이 · 하이픈 위치. joined: 필수 · 형식 · 하한. 요청 안 중복(400). 파싱 · UTF-8 · JSON 문법 · 1,000행 초과는 구현 작업 ID(S06-T02~T04)로 예약. DB 중복(409 + 행 번호)과 경합 23505(409, 행 번호 없음)는 모두 23001.
- email 정의(ADR ③): `email`은 Trim만 한 입력 표기, `normalized_email` = `ToLowerInvariant(email)`(NFC 없음). 비ASCII 한계, `CHECK (normalized_email = lower(email))`를 두지 않는 이유(판정 원본은 Domain 한 곳). citext 미사용 근거로 권한을 들려면 `employee_app` 롤로 Docker 실측, 실측하지 않으면 권한은 근거에서 뺀다.
- ADR ② DB 전제: 1,000행 `INSERT ... RETURNING xmin` 한 트랜잭션, 재시도 시 배치 전체 재전송(`acceptAllChangesOnSuccess: false`), `= ANY` 배열 파라미터 1개. EF 배치 크기는 S06 NFR-02에서 실측.
- TD-010 위험 문구: "정상 요청이 409(3003, 행 번호 없음)로 보일 수 있음. pk와 ux 제약의 검사 순서는 S05-T06 실측 뒤 확정."
- database.md 한 줄: 길이는 Domain(UTF-16)이 varchar(코드 포인트)보다 엄격해 22001이 생기지 않는다. `joined_on` 하한, 이름 · 전화 형식은 DB ck 없는 Domain 규칙.
- error-codes 표 ↔ 코드 대조(S04-T05 방식)에서 예약 행은 따로 센다.
- (T02 dba) ADR ② · ③ DB 조항 문안과 실측 근거는 dba 반환(스크래치 `s05/t02-db-clauses.md`)을 그대로 쓴다. citext 권한 근거는 실측 결과 성립하지 않아 넣지 않는다. database.md 512행 CHECK 문구는 `ux_employees_normalized_email` 기준으로 고치며 근거를 "실측(S05-T02): U+0130이 .NET에서는 그대로, PG libc `lower()`에서는 `i`라 23514"로 확정한다.
- (T01 reviewer) PRD-002 78행(선행 조건) · 153행(병행 진행 메모)의 '가번호' 문장을 확정 문장으로 바꾸고 PRD 변경 이력에 한 줄 남긴다. Q12 답변 · 변경 이력의 당시 기록은 그대로 둔다.
- (T01 developer) ADR 번호: 0025 ① · 0026 ② · 0027 ③ · 0028 ④. ADR ④ 근거 계약 사실: FieldError.Create는 Validation 유형만, ValidationError는 sealed · 1001 고정, ErrorProblemDetails의 `errors`는 ValidationError에만, ErrorStatusCodes 9종(그 밖 500), BadHttpRequestException은 전부 400 · 1001(TD-021). 413 · 415의 ErrorType 값과 코드 유형 자리(T=1의 1004 · 1005 후보 또는 예비 T=6~8)는 1차 초안의 확인 항목으로 올린다.

**S05-T03**

- (T02 tester) ADR 커밋 순서 재확인: 구현 커밋마다 `git merge-base --is-ancestor bd0cbc7 <구현 커밋>`. 스프린트 끝에 `git log --oneline 393e3d7..bd0cbc7 -- src tests`(0줄)로 한 번 더. 21007~21017 추가 뒤 error-codes 상태를 '사용'으로 바꾸고 표 ↔ 상수 대조를 다시 한다(기준선: 공통 사용 12 · 예약 2, Employee 사용 5 · 폐기 3 · 예약 25).
- (T02 developer) EmployeeErrors 새 상수 이름 · 코드 · 유형의 원본은 error-codes Employee 표(21007 NameRequired ~ 21017 JoinedOnTooEarly, 예약: S05-T03). 21009는 제어 문자와 짝 없는 서로게이트를 함께 담는다. EmployeeErrorsTests는 '사용' 행과 1:1로 맞추고 예약 · 폐기 행은 따로 센다. 추가한 상수의 행 상태는 '사용'으로 바꾼다.
- name: Trim 뒤 NFC, 길이는 NFC 뒤 UTF-16 1~100. 제어 문자는 `char.IsControl`(Cc, 탭 포함)만 거부, Cf(ZWJ 등)는 허용. 짝 없는 서로게이트는 Normalize 전에 검사해 Result 오류로(예외 아님).
- email: Trim 뒤 254자 이하, `@` 정확히 하나, 공백 없음, domain에 `.` 포함, 첫 · 끝 `.`과 연속 `.` 거부(`a@.com` · `a@com.` · `a@b..c` 거부). 대소문자만 다른 두 입력은 NormalizedEmail이 같다.
- tel: `char.IsAsciiDigit`와 `-`만, 숫자 8~15자리, 전체 20자 이하, 맨 앞 · 맨 뒤 · 연속 하이픈 거부, 입력 그대로 보존.
- joined: `Create(string?)`에서 `DateOnly.TryParseExact("yyyy-MM-dd", InvariantCulture)`, 1900-01-01 이상, 미래 허용(`2000-2-3` · `2000-02-30` · `1899-12-31` 거부).
- VO는 get-only sealed record. Email은 Value와 NormalizedEmail을 가진다.

**S05-T04**

- EF 매핑: Name · PhoneNumber · JoinedOn · Email(입력 표기)은 EmployeeConfiguration 안의 HasConversion으로 스칼라 컬럼 매핑(공통 규약으로 넓히지 않음). Aggregate는 NormalizedEmail을 string 속성으로 가지고, 유니크 인덱스와 `= ANY`는 이 속성에 건다.
- Owned · Complex Type을 쓰지 않는 근거(`(name, joined_on, id)` 복합 인덱스, 번역 안정성)는 developer 첫 단계에서 실측해 진행 기록에 남긴다. 값 변환기 속성의 Where · OrderBy · 최상위 프로젝션 번역도 확인하고, 막히면 BLOCKED.
- `ix_` 이름은 규칙 생성(EFCore.NamingConventions) + EmployeeModelMetadataTests가 `GetDatabaseName`과 열 순서를 고정. `ux_`만 `EmployeeDbNames.NormalizedEmailUniqueIndex` 상수. `email` 컬럼 인덱스 없음.
- dba 명세: 컬럼 8개(+xmin), 모두 NOT NULL, DEFAULT 없음, 제약 3개(`pk_employees`, `ck_employees_employee_status IN (1, 2)`, `ux_employees_normalized_email`), `ix_` 2개, 최장 식별자 29바이트. EmployeeStatus(Active=1, Inactive=2) · Deactivate 유지, EmployeeImportFormat은 DB 미저장.
- 대상 대기 목록: ValidatorsDeriveFromRequestValidator · ValidatorsDoNotInjectRepositoriesOrServices → S06-T04 해제, ControllersDoNotUseInfrastructureOrRepositories → S06-T05 해제. 건너뜀 메시지에 해제 작업 ID. 안전장치: "대기 규칙의 대상이 1개 이상이면 실패(목록에서 빼라)". 표본 테스트(ShouldFlagExactly)는 그대로.
- EmployeeRegisteredDomainEvent를 지우면 RequestAndResponseModelsAreRecords도 대상 0개가 되므로 유지.
- IEmployeeReadRepository.GetById · EmployeeResponse는 샘플 Query와 함께 제거(인터페이스는 T06까지 비어도 됨). EmployeeLogs 20001은 남기고, 경고가 나면 BLOCKED.
- EmployeeMigrationsTests 기대 SQL은 메타데이터 이름과 기존 문자열 형식으로(예: `CREATE UNIQUE INDEX ux_employees_normalized_email ON employees (normalized_email);`).
- (T01 tester) 기준선 TSV에 표시 이름이 같은 Theory 사례가 11종 있다. trx 이름으로 허용 목록 · 대응표를 대조할 때 이름 집합이 아니라 개수로 비교한다.
- (T04 reviewer → T05) testing-strategy Q2 행 기대값(옛 pk_employees · ux_employees_email)을 리셋 뒤 실측값(pk_employees, ux_employees_normalized_email, ix_ 2개)으로 바꾼다(커밋 D). idempotent 스크립트 테스트의 '다른 문장 없음' 보장은 Migrations_OfWriteContext_AreOnlyInitialCreate에 기대므로 리셋 뒤 그 테스트 green을 함께 본다. 대응표 '수정(T04, T05 뒤 green)' 행 상태 칸은 green 갱신 때 '수정 완료(T04) · …' 형식으로 맞춘다.
- (T04 developer → T05) 옛 스냅샷 대비 모델 차이는 ModelSnapshot 테스트 실패 메시지(DropIndexOperation ux_employees_email 등)에 나온다. 기대 SQL 문자열은 `TestDoubles/EmployeeCreateScript.cs` 한 곳이니 새 InitialCreate 생성 SQL과 형식이 다르면 여기만 실측값으로 맞춘다. tester는 허용 목록 59건이 리셋 뒤 전부 green인지, 건너뜀이 대상 대기 3 + 기존 1인지 확인한다(trx 원본: 스크래치 `s05/t04-failures.tsv`).
- 실패 허용 목록: 단위는 `EmployeeMigrationsTests.ModelSnapshot_HasNoDifferencesFromCurrentModel` · `IdempotentScript_CreatesEmployeesAndHistoryWithoutSchemaXminOrDefault` 2건, 통합은 대응표 "수정(T05 뒤 green)" 행만. developer 단계에서 trx 이름으로 확정해 진행 기록에 남긴다. 스키마 불일치(옛 마이그레이션) 원인으로 trx에서 확인된 실패는 대응표에 "수정(T04, T05 뒤 green)" 또는 "이전(T06, T05 뒤 green)" 행으로 추가해 허용하고 행마다 실패 원인 한 줄(예: 42703 column does not exist)을 적는다(2026-09-28 결정). **스키마 불일치로 설명되지 않는 실패와 T05 리셋 뒤에도 남는 실패는 BLOCKED**.
- reviewer grep: 패턴 `api/v1/employees|RegisterEmployee|GetEmployeeById|DisplayName|display_name|ux_employees_email`, 범위 `src/` · `tests/Services` · `tests/Aspire`. 제외: `Persistence/Migrations`(T05까지), `wiki`, `tests/BuildingBlocks`(리터럴 표본), HealthEndpointsTests InlineData, EF의 `DisplayName()` 호출.
- EmployeeBuilder(Domain · Integration)는 새 필드로, 기본 이메일은 순번으로 고유하게.
- (T02 dba) `normalized_email`은 DB가 강제하지 않으므로(ck · 식 인덱스 없음) 테스트 시드와 원시 SQL INSERT도 `ToLowerInvariant` 값을 넣는다. EmployeeBuilder(Integration)는 Email VO를 거쳐 값을 만든다.
- (T01 developer) 대응표 "처리" 열이 원본이다. EmployeePersistenceRoundTripTests.GetByIdAsync_* 2건은 IEmployeeReadRepository.GetById 제거와 부딪히므로 수정 방식을 T04에서 정한다. PersistenceLogExposureTests는 샘플 의존 메서드가 0개라 2건 모두 "수정".
- (T04 dba) EmployeeConfiguration 규칙, EmployeeModelMetadataTests 사양 15항목, EmployeeMigrationsTests 기대값, 원시 SQL · 시드 규칙은 스크래치 `s05/t04-dba-handoff.md`가 원본이다(요지: VO 4개 HasConversion은 `Create(...).Value` 경유, NormalizedEmail은 변환기 없는 string, ix_는 HasIndex만 · HasDatabaseName 없음, HasDefaultValue · HasColumnOrder 금지, 속성 선언 순서 Name · Email · NormalizedEmail · PhoneNumber · JoinedOn · EmployeeStatus, 열 순서는 CREATE TABLE 블록 Trim 줄 Equal, 인덱스 3개 · 최장 식별자 30, NotContain("ix_")는 개수 단언으로). 기대 SQL 형식이 T05 실측과 다르면 기대 문자열을 실측값에 맞춘다.
- (T03 tester) Email Value는 제어 문자 · 짝 없는 서로게이트를 거르지 않는다(BL-129). NUL이 든 email 저장 시 PG 오류(22021 추정)를 실측할지는 T04 developer가 판단하고, 실측하면 진행 기록에 남긴다. 규칙 추가는 이 스프린트 범위 밖이다.
- (T03 reviewer) EF 설정은 Name.MaxLength · Email.MaxLength · PhoneNumber.MaxLength const를 참조하고 숫자를 다시 쓰지 않는다. NormalizedEmail은 Email VO에서 가져온다. BL-130(record ToString이 값 출력)과 관련해 Aggregate가 VO를 로그 인자로 넘기지 않는지 함께 본다.
- (T03 developer) EF HasConversion은 VO private 생성자를 쓸 수 없으므로 변환 식은 `v => Name.Create(v).Value`처럼 Create를 거친다(DB 값이 규칙을 어기면 Value 접근 예외). 참조 상수는 Name.MaxLength · Email.MaxLength · PhoneNumber.MaxLength, JoinedOn.MinValue는 static readonly.
- (T03 developer) 폐기 상수 삭제 때 EmployeeErrorsTests.DeprecatedRows를 비우고 DocumentedRows_CountByStatus의 폐기 기대 개수 3을 고친다. `Employee.Email`(string)과 새 `Email` 형식이 같은 네임스페이스라 'Color Color' 형태가 되지만 컴파일 문제는 없다. EmployeeEmail 도우미를 지우면 IEmployeeRepository · Employee의 cref도 고친다.
- (T02 developer) 폐기 상수 21001 · 21002 · 21006을 지우면 error-codes 해당 행 상태의 '상수 삭제 S05-T04'를 완료로 표시한다. 23001 설명의 '그 전까지는 ux_employees_email' 문구는 매핑 교체 뒤 지운다. clean-architecture 트리(EmployeeEmail.cs, Commands/Queries 샘플)와 coding-conventions 기능 폴더 구조 · 예시 안내를 샘플 제거 뒤 실제 구성으로 갱신한다(예시 코드 교체는 S06-T04). 대상 대기 목록의 원본 표는 testing-strategy '대상 대기 목록'.
- (T01 dba) 23505 매핑 교체 대상은 `EmployeeDbNames.EmailUniqueIndex`(EmployeeDbNames.cs:20)와 EmployeeInfrastructureServiceCollectionExtensions.cs:47의 `errors.Map` 한 곳. 옛 이름 `ux_employees_email`이 EmployeeEmail.cs:9, EmployeeErrors.cs:10, EmployeeRepository.cs:9, RegisterEmployeeCommandHandler.cs:16(샘플, 삭제 대상) 주석에 남아 있다. BuildingBlocks UniqueIndexName.cs:22 · UniqueConstraintErrorsBuilder.cs:10의 주석 예시는 동작 영향이 없으므로 두어도 된다(grep 제외 범위).

**S05-T05**

- Sealed 2개: 새 `<ID>_InitialCreate.Sealed.cs`는 새로 쓰고 `EmployeeDbContextModelSnapshot.Sealed.cs`는 유지. ClassesAreSealed · MigrationAndSnapshotTypes_AreAllSealed 통과.
- tester 점검: `\d employees`로 컬럼 · 타입 · NOT NULL · 기본값 없음 · ck 1 · 인덱스 3 + PK 이름 대조, 볼륨 삭제 전후 `docker volume ls`(익명 볼륨 2개 유지), R 이후 T04 허용 목록 전부 통과 대조. MigrationReapplyTests · RespawnHistoryTableTests는 수정 없이 새 ID로 통과.
- local-setup · 진행 기록: 볼륨을 다른 worktree와 공유하면 42P07 가능. psql 점검 8번 유효 확인.
- (T02 developer) database.md 명명 규칙 예시(176행 `ux_employees_email`), 적용 범위 실측 표(187행), ERD · InitialCreate 대조 표, 인덱스 표의 '인덱스는 2개'와 첫 열 `ux_employees_email`, 이름 상수 문단을 새 스키마 실측값으로 갱신하고, 인덱스 표 비고의 'S05-T04 · T05부터' 문구를 현재형으로 바꾼다.
- (T04 dba) 커밋 D: '새 스키마 명세'의 '실측 전' 표시와 기준 구분 인용 블록 제거, 옛 ERD · 대조 표를 새 실측값으로 교체, 점검표 a~g(a 최장 30바이트, e 인덱스 3개), 적용 범위 실측 표 ix_ · 날짜 `_on` '적용됨', 열 순서 규칙이 생성 SQL과 같은지 확인.
- (T01 dba) 리셋 전 기준: Migrations 폴더 파일 5개, ProductVersion 8.0.31(Designer · Snapshot). 커밋 D에서 database.md 122 · 183 · 187 · 371행의 InitialCreate ID와 컬럼 목록을 새 실측값으로 갱신.
- 알려진 잡음 · 제외 기준: PRD-001에서 확정한 첫 실행 잡음(3D000, BL-117 첫 `/health/ready` Unhealthy)은 개수를 기록하고 판정에서 뺀다. 그 밖의 새 오류는 제외하지 않고 기록한 뒤 판정받는다.

**S05-T06**

- (T05 dba) testing-strategy P4 행(148행)의 'ux_employees_email → 23001 · 로그 201 (실측)'을 23505 경로 이전 테스트(UnitOfWorkConflictTests) green 확인 뒤 `ux_employees_normalized_email`로 고친다.
- EXPLAIN: 10,000건 + ANALYZE(또는 `enable_seqscan=off`). 대상은 목록(`ix_employees_joined_on_id`), 이름(`ix_employees_name_joined_on_id`, Sort 없음), `list.Contains`가 만든 `= ANY`(ux). COUNT(*) 전체 스캔은 정상. 생성 SQL 원문을 함께 기록.
- (T02 developer) ADR-0026 11절의 TD-010 pk · ux 검사 순서는 실측 뒤 진행 기록과 TD-010 행에 남긴다(ADR 본문은 고치지 않음).
- (T04 developer) 값 변환기 VO는 조건 · 정렬에서 VO끼리 비교(`e.Name == name`), `e.Name.Value`는 Where에서 번역 안 됨. 최상위 Select의 `.Value`, `list.Contains(e.NormalizedEmail)` → `= ANY`는 번역됨. IEmployeeRepository는 ExistsByNormalizedEmailAsync · Add 유지(목록 조회 추가 시 대체 여부는 T06 판단). EmployeePersistenceRoundTripTests.ReadContext_* 2건은 읽기 DbContext 직접 프로젝션이므로 Read Repository 테스트로 옮기거나 유지 판단. Integration EmployeeBuilder.WithStatus(Inactive)는 등록 뒤 Deactivate.
- (T02 dba) S05-T02의 1,000행 · 전 행 일치 Seq Scan은 EXPLAIN 판정 근거가 아니다. TD-010 pk · ux 검사 순서 실측은 T06 몫.
- UUID v7 정렬: 같은 밀리초 안 1,000건 생성 → `ORDER BY id`가 생성 순서와 같은지.
- TD-010 실측: pk · ux 동시 위반 1회로 어느 제약이 먼저 검사되는지(3003 / 23001)만 보고 진행 기록과 TD-010 행에 남긴다. 재현 테스트는 만들지 않고 S06 동시 경합 테스트에 인계.
- (T01 developer) 이전(T06) 행은 UnitOfWorkConflictTests 6건(23505 → 23001 2 · pk → 3003 1 · xmin 3). EmployeeSchemaTests(ck · 읽기 연결 · UUID v7)와 RoundTrip 감사 UTC 1건은 "수정" 행이며 "이전 확인 S05-T06"이 붙어 있다. ⑤의 1:1 대조 기준선은 스크래치 `test-baseline-v0.1.0.tsv`(없으면 `v0.1.0`에서 trx로 재생성).
- tester 보강 후보: name 100자 · 서로게이트 왕복, `joined_on` 1900-01-01 · 미래 왕복, `employee_status` 1 저장 · ck 위반, phone 20자 경계.

## 증빙 테스트 대응표

> 이전 테스트 이름은 `v0.1.0` 기준입니다.

- 기준선(S05-T01): `v0.1.0` 전체 테스트 1,538개(통과 1,537 · 건너뜀 1, 테스트 프로젝트 13개). `v0.1.0`과 HEAD의 코드 차이가 없어(`git diff --stat v0.1.0 HEAD -- src tests` 출력 없음) HEAD에서 `dotnet test --logger trx`로 뽑았습니다.
- 대상: 인계 메모 S05-T01의 "처리" 계획값에 든 클래스와, 이전 항목(ck · 23505 · xmin · 읽기 연결 쓰기 거부 · UUID v7 · 감사 UTC)이 든 테스트입니다. 95행, 테스트 226개(삭제 96 · 수정 35 · 이전 6 · 변경 없음 89).
- 표기: `클래스.*`는 그 클래스 전체, `클래스.메서드`는 그 메서드의 모든 사례(`[Theory]` 데이터 포함)입니다. 괄호 안 수는 기준선의 사례 수입니다. "PRD-001 FR Trait"는 클래스 · 메서드의 `[Trait]` 값이고, 없으면 `-`입니다.
- 처리: `삭제`(샘플 제거, 새 테스트는 "새 테스트 또는 작업 ID"의 작업에서) · `수정(T04, T05 뒤 green)` · `이전(T06)` · `변경 없음`(경로 · 이름 리터럴 표본). 상태는 `계획`으로 시작하고, 각 작업이 처리한 뒤 새 테스트 이름과 함께 갱신합니다.
- S05-T04 갱신: 기준선 95행의 상태를 채우고, 샘플 의존인데 계획값에 없던 삭제 · 수정 3행과 옛 스키마(`20260927134235_InitialCreate`) 때문에 실패하는 테스트 16행을 추가했습니다(2026-09-28 결정). 추가 19행 · 테스트 43개. `수정(T04)`은 단위 테스트라 T04에서 바로 통과합니다.
- "T04 실패 허용"은 S05-T05(`InitialCreate` 재생성) 전까지 실패하는 사례와 원인입니다. 통합 57개(기존 행 37 · 추가 행 20)이고, 단위 2개(`EmployeeMigrationsTests.ModelSnapshot_HasNoDifferencesFromCurrentModel` · `IdempotentScript_CreatesEmployeesAndHistoryWithoutSchemaXminOrDefault`)는 표 밖 허용 목록입니다. T05 뒤 모두 green이어야 합니다.

| 이전 테스트(전체 이름) | PRD-001 FR Trait | 처리 | 새 테스트 또는 작업 ID | 상태 |
|---|---|---|---|---|
| `EmergencyHub.Employee.IntegrationTests.Http.EmployeeRegistrationHttpTests.Get_UnknownOrEmptyId_Returns404With22001ProblemDetails` (2) | FR-08 | 삭제 | 이전 대기: S07-T03 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.EmployeeRegistrationHttpTests.PostThenGet_ValidRequest_Returns201WithLocationOfGetRouteAnd200WithNormalizedEmail` (1) | FR-08 | 삭제 | 이전 대기: S06-T06 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.EmployeeRegistrationHttpTests.Post_ConcurrentInsertBetweenPreCheckAndInsert_Returns409With23001WithoutEfErrorAnd102After20001` (1) | FR-08 | 삭제 | 이전 대기: S06-T06 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.EmployeeRegistrationHttpTests.Post_DisplayNameOfEmojis_Accepts50AndRejects51With21002` (2) | FR-08 | 삭제 | 이전 대기: S06-T06 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.EmployeeRegistrationHttpTests.Post_Email254CharsAfterTrimWithSurroundingSpacesAndUppercase_Returns201AndStoresNormalized` (1) | FR-08 | 삭제 | 이전 대기: S06-T06 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.EmployeeRegistrationHttpTests.Post_SameEmailAfterTrimAndLowercase_Returns409With23001AndKeepsFirstRowOnly` (1) | FR-08 | 삭제 | 이전 대기: S06-T06 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.ProblemDetailsHttpTests.Get_NonGuidId_Returns400With1001UnderIdKeyInsteadOf404` (1) | FR-07 | 삭제 | 이전 대기: S07-T03(1001 HTTP) | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.ProblemDetailsHttpTests.Get_UnhandledExceptionInReadRepository_Returns500With9001WithoutOriginalMessageAndLogsOnceAtError` (1) | FR-07 | 삭제 | 이전 대기: S06-T06(500 형식) | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.ProblemDetailsHttpTests.Post_EmployeeStatusAsString_Returns400With1001UnderEmployeeStatusKey` (1) | FR-07 | 삭제 | 이전 대기: S07-T03(1001 HTTP) | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.ProblemDetailsHttpTests.Post_EmptyBody_Returns400With21001And21003And21006InFieldOrder` (1) | FR-07 | 삭제 | 이전 대기: S06-T06 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.ProblemDetailsHttpTests.Post_MalformedJson_Returns400With1001AndFieldCode1001` (1) | FR-07 | 삭제 | 이전 대기: S07-T03(1001 HTTP) | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.ProblemDetailsHttpTests.Post_MissingOrNullEmployeeStatus_Returns400With21006` (2) | FR-07 | 삭제 | 대체 없음(21006 폐기, 계획 결정 3) | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.ProblemDetailsHttpTests.Post_TraceparentHeader_ProblemTraceIdEqualsIncomingTraceId` (1) | FR-07 | 삭제 | 이전 대기: S06-T06 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.ProblemDetailsHttpTests.Post_UndefinedEmployeeStatus_Returns400With1001AndField1002` (2) | FR-07 | 삭제 | 이전 대기: S06-T04(enum 1002, Validator 수준) | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.OpenApiContractHttpTests.*` (2) | FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.ReadOnlyWriteRejectionHttpTests.*` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T06(HTTP 500, DB 수준은 EmployeeSchemaTests 행) | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Http.RequestCompletionLogTests.*` (4) | FR-03 · FR-09 | 삭제 | 이전 대기: S06-T06 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.Constructor_DependsOnlyOnSender` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.GetByIdAsync_EmptyGuid_StillSendsQuery` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S07-T02 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.GetByIdAsync_Found_Returns200WithResponse` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S07-T02 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.GetByIdAsync_NotFound_ReturnsProblemResultWith22001` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S07-T02 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.RegisterAsync_CommandSucceeds_Returns201AtGetRouteWithIdBody` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.RegisterAsync_DuplicateEmail_ReturnsProblemResultWith23001` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.RegisterAsync_NullFields_SendsEmptyStringsAndNullStatus` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.RegisterAsync_NullRequest_ThrowsArgumentNullException` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.RegisterAsync_Request_SendsCommandWithSameValuesAndToken` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.RegisterAsync_UndefinedStatus_PassesValueUnchangedForValidator` (2) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.RegisterAsync_ValidationFailed_ReturnsProblemResultWithValidationError` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Api.UnitTests.Controllers.EmployeesControllerTests.Routes_AreApiV1EmployeesWithoutIdConstraint` (1) | FR-08 · FR-07 | 삭제 | 이전 대기: S06-T05 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandHandlerTests.*` (13) | - | 삭제 | 이전 대기: S06-T04 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_AllPropertiesInvalid_CollectsOneFailurePerPropertyInOrder` (1) | - | 삭제 | 이전 대기: S06-T04 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_DisplayNameOverMaxLength_Reports21002` (2) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_DisplayNameWithinMaxLengthAfterTrim_HasNoFailures` (5) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_EmailAtMaxLengthWithSurroundingWhitespace_HasNoFailures` (1) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_EmailOverMaxLength_Reports21005` (1) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_EmailWithCaseOrSurroundingWhitespace_HasNoFailures` (3) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_LongMalformedEmail_ReportsLengthFirstAndStops` (1) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_MalformedEmail_Reports21004` (4) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_MissingDisplayName_Reports21001Only` (4) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_MissingEmail_Reports21003Only` (4) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_MissingEmployeeStatus_Reports21006Only` (1) | - | 삭제 | 대체 없음(21006 폐기, 계획 결정 3) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_ReservedOrUndefinedEmployeeStatus_Reports1002OnEmployeeStatus` (4) | - | 삭제 | 이전 대기: S06-T04(enum 1002) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_UnicodeDisplayNameOverMaxLength_Reports21002` (3) | - | 삭제 | S05-T03(Value Object 필드 규칙) | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee.RegisterEmployeeCommandValidatorTests.Validate_ValidCommand_HasNoFailures` (2) | - | 삭제 | 이전 대기: S06-T04 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.Queries.GetEmployeeById.GetEmployeeByIdQueryHandlerTests.*` (4) | - | 삭제 | 이전 대기: S07-T02 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.ContractShapeTests.ApplicationAssemblyMarker_PointsToApplicationAssembly` (1) | - | 삭제 | 이전 대기: S06-T04 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.ContractShapeTests.EmployeeReadRepository_InheritsReadMarkerOnly` (1) | - | 삭제 | S05-T06 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.ContractShapeTests.EmployeeResponse_HasDocumentedMembersInOrder` (1) | - | 삭제 | 이전 대기: S07-T01 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.ContractShapeTests.GetEmployeeByIdQuery_IsQueryOfEmployeeResponseByGuid` (1) | - | 삭제 | 이전 대기: S07-T02 | 삭제 완료(T04) |
| `EmergencyHub.Employee.Application.UnitTests.Employees.ContractShapeTests.RegisterEmployeeCommand_IsRecordCommandReturningEmployeeIdWithNullableStatus` (1) | - | 삭제 | 이전 대기: S06-T04 | 삭제 완료(T04) |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.Columns_AfterMigration_MatchSampleTableTypesWithoutDefaults` (1) | FR-08 | 수정(T04, T05 뒤 green) | S05-T04 | 수정 완료(T04) · T04 실패 허용: 옛 InitialCreate 컬럼(display_name, normalized_email · phone_number · joined_on 없음)과 불일치 |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.CommitAsync_TrackedEntryWithUndefinedStatus_RethrowsDbUpdateExceptionWithCheckConstraintAndRedactedDetail` (4) | FR-08 | 수정(T04, T05 뒤 green) | S05-T04, 이전 확인 S05-T06(ck) | 수정 완료(T04) · T04 실패 허용: EF INSERT가 23514 대신 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.ConstraintAndIndexNames_AfterMigration_EqualEfModelNamesAndConstants` (1) | FR-08 | 수정(T04, T05 뒤 green) | S05-T04 | 수정 완료(T04) · T04 실패 허용: DB 인덱스가 옛 2개(pk · 옛 유니크 인덱스)라 ix_ 2개 · ux_employees_normalized_email 없음 |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.OrderById_IdsGeneratedWithinSameMillisecondInsertedShuffled_ReturnsGenerationOrder` (1) | FR-08 · FR-06 | 수정(T04, T05 뒤 green) | S05-T04, 이전 확인 S05-T06(UUID v7) | 수정 완료(T04) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_DefinedStatus_IsAccepted` (2) | FR-08 | 수정(T04, T05 뒤 green) | S05-T04, 이전 확인 S05-T06(ck) | 수정 완료(T04) · T04 실패 허용: 42703 column "name" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_StatusOutsideSmallintRange_IsRejectedByColumnType22003` (1) | FR-08 | 수정(T04, T05 뒤 green) | S05-T04, 이전 확인 S05-T06(ck) | 수정 완료(T04) · T04 실패 허용: 22003 대신 42703 column "name" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_UndefinedOrReservedStatus_IsRejectedByCheckConstraint23514` (4) | FR-08 | 수정(T04, T05 뒤 green) | S05-T04, 이전 확인 S05-T06(ck) | 수정 완료(T04) · T04 실패 허용: 23514 대신 42703 column "name" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.ReadConnection_InsertInsideExplicitReadCommittedTransaction_IsStillRejectedWith25006` (1) | FR-08 | 수정(T04, T05 뒤 green) | S05-T04, 이전 확인 S05-T06(읽기 연결 쓰기 거부) | 수정 완료(T04) · T04 실패 허용: 25006 대신 42703 column "name" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.ReadConnection_WriteStatement_IsRejectedWith25006` (3) | FR-08 | 수정(T04, T05 뒤 green) | S05-T04, 이전 확인 S05-T06(읽기 연결 쓰기 거부) | 수정 완료(T04) · T04 실패 허용: 3건 중 UPDATE 1건, 25006 대신 42703 column "name" does not exist(DELETE · TRUNCATE 통과) |
| `EmergencyHub.Employee.IntegrationTests.Schema.EmployeeSchemaTests.ReadDbContext_RawInsertThroughProductionRegistration_IsRejectedWith25006` (1) | FR-08 | 수정(T04, T05 뒤 green) | S05-T04, 이전 확인 S05-T06(읽기 연결 쓰기 거부) | 수정 완료(T04) · T04 실패 허용: 25006 대신 42703 column "name" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.FaultInjection.TestTriggersTests.*` (4) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04 | 수정 완료(T04) · T04 실패 허용: 4건 중 원시 INSERT 3건, 42703 column "name" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.CommitThenRead_MaxLengthAndUnicodeValues_RoundTripUnchanged` (3) | FR-06 | 수정(T04, T05 뒤 green) | S05-T04(name · email · phone 경계, 읽기 DbContext 프로젝션) | 수정 완료(T04) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.CommitThenReload_NewWriteDbContext_MaterializesThroughPrivateConstructorWithAuditAndVersion` (1) | FR-06 | 수정(T04, T05 뒤 green) | S05-T04 | 수정 완료(T04) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.Commit_TimeProviderAtPlusNineAndSeoulSession_StoresSameInstantAsUtc` (1) | FR-06 | 수정(T04, T05 뒤 green) | S05-T04, 이전 확인 S05-T06(감사 UTC) | 수정 완료(T04) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.GetByIdAsync_StoredEmployee_ProjectsIdValueAndShadowAuditColumnsOnReadConnection` (1) | FR-06 | 수정(T04, T05 뒤 green) | `ReadContext_StoredEmployee_ProjectsIdValueValueObjectsAndShadowAuditColumns`(읽기 DbContext 직접 프로젝션, Read Repository 조회는 S05-T06) | 수정 완료(T04) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.GetByIdAsync_UnknownOrEmptyId_ReturnsNull` (1) | FR-06 | 수정(T04, T05 뒤 green) | `ReadContext_UnknownOrEmptyId_ProjectsNothing`(읽기 DbContext 직접 프로젝션, Read Repository 조회는 S05-T06) | 수정 완료(T04) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.PersistenceLogExposureTests.*` (2) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(DETAIL 조각 `Key (normalized_email)`, 전화번호 비노출 추가) | 수정 완료(T04) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeApiFactoryTests.Configuration_ContentRootCopy_HasNoSerilogSinksButKeepsLevelsAndNoOtlpEndpoint` (1) | FR-09 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeApiFactoryTests.Constructor_NullDatabase_Throws` (1) | FR-09 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeApiFactoryTests.CreateClient_FixtureConnections_ReadyHealthReturns200Healthy` (1) | FR-09 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeApiFactoryTests.CreateClient_WrongWritePassword_ReadyHealthReturns503` (1) | FR-09 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeApiFactoryTests.Dispose_Factory_DeletesContentRootCopy` (1) | FR-09 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeApiFactoryTests.Logs_HostLogger_CollectsEventWithServiceNameFromSettings` (1) | FR-09 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeApiFactoryTests.TimeProvider_Replaced_AuditTimestampsUseFakeTime` (1) | FR-09 | 수정(T04, T05 뒤 green) | S05-T04(HTTP 대신 Api 호스트 DI의 Repository · UnitOfWork 커밋, HTTP 경로는 S06-T06) | 수정 완료(T04) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeApiFactoryTests.WriteInterceptors_Registered_ObserveApiCommandCommit` (1) | FR-09 | 수정(T04, T05 뒤 green) | `WriteInterceptors_Registered_ObserveApiHostUnitOfWorkCommit`(HTTP 대신 Api 호스트 UnitOfWork 커밋, HTTP 경로는 S06-T06) | 수정 완료(T04) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.ConfigurePipeline_Development_ServesOpenApiWithEmployeeRoutes` (1) | FR-08 · FR-11 | 수정(T04, T05 뒤 green) | `ConfigurePipeline_DevelopmentWithoutControllers_ServesOpenApiDocumentWithNoPaths`(경로 0개, Controller는 S06-T05) | 수정 완료(T04) · 통과 |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.ConfigurePipeline_NonDevelopment_DoesNotServeOpenApi` (3) | FR-08 · FR-11 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.ConfigureServices_CalledTwice_Throws` (1) | FR-08 · FR-11 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.ConfigureServices_Development_DoesNotEnableSensitiveDataLogging` (1) | FR-08 · FR-11 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.ConfigureServices_HealthChecks_AreExactlyTwoDbContextChecksTaggedReady` (1) | FR-08 · FR-11 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.ConfigureServices_ReadMissing_ThrowsWithoutConnectionValues` (1) | FR-08 · FR-11 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.ConfigureServices_WriteAndRead_BuildsWithScopeValidationAndResolvesSenderAndBothContexts` (1) | FR-08 · FR-11 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.ConfigureServices_WriteMissing_ThrowsWithoutConnectionValues` (1) | FR-08 · FR-11 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.ConfigureServices_WriteWhitespace_Throws` (1) | FR-08 · FR-11 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.Api.UnitTests.ProgramTests.DbRetry_IsThreeRetriesWithFiveSecondMaxDelay` (1) | FR-08 · FR-11 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_DeactivateAlreadyInactiveEmployee_WritesNothingAndKeepsXminAndUpdatedAt` (1) | FR-08 | 이전(T06, T05 뒤 green) | S05-T06(xmin) | 계획 · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_SameIdDifferentEmail_Returns3003FromPrimaryKeyAndLogs202AtWarning` (1) | FR-08 | 이전(T06, T05 뒤 green) | S05-T06(23505 pk → 3003) | 계획 · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_SameNormalizedEmailWithoutPreCheck_Returns23001FromEmailUniqueIndex` (2) | FR-08 | 이전(T06, T05 뒤 green) | S05-T06(23505 → 23001) | 매핑 상수만 교체(T04, NormalizedEmailUniqueIndex) · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_SecondScopeDeactivatesWithStaleXmin_Returns3001AndLogs203AtDebug` (1) | FR-08 | 이전(T06, T05 뒤 green) | S05-T06(xmin) | 계획 · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_TwoScopesDeactivateSameEmployee_FirstSucceedsAndUpdatesXminAndUpdatedAtOnly` (1) | FR-08 | 이전(T06, T05 뒤 green) | S05-T06(xmin) | 계획 · T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.ServiceDefaults.UnitTests.HealthEndpointsTests.IsHealthPath_Path_ReturnsWhetherUnderHealthSegment(path: "/api/v1/employees", expected: False)` | FR-03 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.BuildingBlocks.Api.UnitTests.Acceptance.ExceptionResponseAcceptanceTests.*` (9) | FR-07 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.BuildingBlocks.Api.UnitTests.Acceptance.ResultResponseAcceptanceTests.*` (25) | FR-07 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.BuildingBlocks.Api.UnitTests.Errors.ErrorProblemDetailsTests.*` (20) | - | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence.UniqueIndexNameTests.*` (17) | FR-06 | 변경 없음 | - | 변경 없음 · 통과(T04) |
| `EmergencyHub.Employee.Infrastructure.UnitTests.Persistence.EmployeeReadRepositoryTests.*` (4) | FR-08 | 삭제 | S05-T06(Read Repository 목록 · 개수 · 이름 조회) | 삭제 완료(T04): 샘플 조회 GetByIdAsync · EmployeeResponse 제거 |
| `EmergencyHub.Employee.Domain.UnitTests.Employees.EmployeeEmailTests.*` (18) | - | 삭제 | S05-T03(`EmailTests`, Email Value Object) | 삭제 완료(T04): EmployeeEmail 도우미 제거 |
| `EmergencyHub.Employee.Infrastructure.UnitTests.DependencyInjection.EmployeeInfrastructureServiceCollectionExtensionsTests.AddEmployeeInfrastructure_Called_RegistersApplicationHandlers` (1) | FR-06 · FR-08 | 수정(T04) | `AddEmployeeInfrastructure_EmployeeApplicationWithoutHandlers_RegistersNoHandlersAndStillBuilds`(Handler 등록 확인은 S06-T04에서 복원) | 수정 완료(T04) · 통과 |
| `EmergencyHub.Employee.IntegrationTests.FaultInjection.CommandFaultInterceptorTests.CommitAsync_OneTransientFailure_RetriesToSuccess` (1) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.FaultInjection.CommandFaultInterceptorTests.CommitAsync_ZeroFailures_PassesThroughAndCountsAttempt` (1) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.FaultInjection.TransactionProbeInterceptorTests.*` (3) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.DbContextInterceptorRegistrationTests.AddWriteDbContextInterceptors_AfterProductionRegistration_KeepsAuditInterceptor` (1) | FR-06 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.DbContextInterceptorRegistrationTests.AddWriteDbContextInterceptors_CalledTwice_AppendsBothInterceptors` (1) | FR-06 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeDatabaseFixtureTests.CreateServices_AddAndCommit_StoresRowThroughProductionRegistration` (1) | FR-09 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeDatabaseFixtureTests.ReadConnection_Insert_IsRejectedWithReadOnlyTransaction25006` (1) | FR-09 | 수정(T04, T05 뒤 green) | S05-T04(원시 INSERT 새 컬럼) | T04 실패 허용: 25006 대신 42703 column "name" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Fixtures.EmployeeDatabaseFixtureTests.ResetAsync_AfterRowsWereAdded_EmptiesTablesButKeepsMigrationHistory` (1) | FR-09 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Migrations.MigrationReapplyTests.ApplyMigrationsAsync_RunTwiceMoreOnMigratedDatabase_KeepsOneHistoryRowAndExistingData` (1) | FR-08 · FR-09 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Migrations.MigrationReapplyTests.IdempotentScript_RunTwiceWithPsqlOnEmptyDatabaseThenMigrateAsync_CreatesSchemaOnceOwnedByAppRole` (1) | FR-08 · FR-09 | 수정(T04, T05 뒤 green) | S05-T04(인덱스 상수 NormalizedEmailUniqueIndex) | T04 실패 허용: 옛 InitialCreate 스크립트에 ux_employees_normalized_email이 없어 pg_indexes 0건 |
| `EmergencyHub.Employee.IntegrationTests.Persistence.EfCoreErrorLogLevelTests.*` (3) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist(CheckViolation 1건은 23514 대신 42703) |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_CommitTimeSerializationFailureOnce_RetriesWholeUnitAndStoresExactlyOneRow` (1) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_DeletedAggregateWithDomainEvents_DeletesRowAndClearsEvents` (1) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_PreCommitHookThrows_RollsBackSavedRowAndPropagatesWithoutRetry` (1) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_SerializationFailureOnEveryAttempt_ThrowsRetryLimitExceededClassifiedAs9003` (1) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |
| `EmergencyHub.Employee.IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_ServerDefaultIsolationSerializable_RunsInReadCommittedTransaction` (1) | FR-05 | 수정(T04, T05 뒤 green) | S05-T04(EmployeeBuilder 새 필드) | T04 실패 허용: 42703 column "joined_on" of relation "employees" does not exist |


### S05-T04 실패 허용 목록 (확정)

> 2026-09-28 S05-T04 developer 단계 trx(`DOCKER_API_VERSION=1.43 dotnet test --logger trx`)에서 뽑은 실패 전체 59건(단위 2 + 통합 57, Theory 사례는 행마다 1건). 모두 옛 마이그레이션(`20260927134235_InitialCreate`) 원인이며 S05-T05 리셋 뒤 전부 green이어야 한다. 이 목록 밖 실패 · 리셋 뒤에도 남는 실패는 BLOCKED(2026-09-28 결정). 접두사 `EmergencyHub.Employee.` 생략.

| # | 테스트(trx 이름) | 실패 원인(첫 줄) |
|---|---|---|
| 1 | `Infrastructure.UnitTests.Persistence.EmployeeMigrationsTests.IdempotentScript_CreatesEmployeesAndHistoryWithoutSchemaXminOrDefault` | Expected script "CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" ( |
| 2 | `Infrastructure.UnitTests.Persistence.EmployeeMigrationsTests.ModelSnapshot_HasNoDifferencesFromCurrentModel` | Expected differences to be empty because 매핑이 바뀌었으면 새 마이그레이션이 필요하다, but found at least one item |
| 3 | `IntegrationTests.FaultInjection.CommandFaultInterceptorTests.CommitAsync_OneTransientFailure_RetriesToSuccess` | 42703: column "joined_on" of relation "employees" does not exist |
| 4 | `IntegrationTests.FaultInjection.CommandFaultInterceptorTests.CommitAsync_ZeroFailures_PassesThroughAndCountsAttempt` | 42703: column "joined_on" of relation "employees" does not exist |
| 5 | `IntegrationTests.FaultInjection.TestTriggersTests.CreateAlwaysFailAsync_Insert_FailsWith40001UntilDisposed` | Expected (act to be the same string, but they differ at index 1: |
| 6 | `IntegrationTests.FaultInjection.TestTriggersTests.CreateCommitFailureOnceAsync_TwoAutocommitInserts_FailsOnlyFirstCommitCountedBySequence` | Expected (first to be the same string, but they differ at index 1: |
| 7 | `IntegrationTests.FaultInjection.TestTriggersTests.CreateIsolationProbeAsync_Insert_RecordsServerTransactionSettingsAndCleansUp` | 42703: column "name" of relation "employees" does not exist |
| 8 | `IntegrationTests.FaultInjection.TransactionProbeInterceptorTests.CommitAsync_EveryCommitFails_ExceedsReducedRetryLimitWithoutCommit` | 42703: column "joined_on" of relation "employees" does not exist |
| 9 | `IntegrationTests.FaultInjection.TransactionProbeInterceptorTests.CommitAsync_ObserveOnly_RecordsRequestedReadCommittedAndOneCommit` | 42703: column "joined_on" of relation "employees" does not exist |
| 10 | `IntegrationTests.FaultInjection.TransactionProbeInterceptorTests.CommitAsync_OneCommitFailure_RetriesWholeTransactionAndCommitsOnce` | 42703: column "joined_on" of relation "employees" does not exist |
| 11 | `IntegrationTests.Fixtures.DbContextInterceptorRegistrationTests.AddWriteDbContextInterceptors_AfterProductionRegistration_KeepsAuditInterceptor` | 42703: column "joined_on" of relation "employees" does not exist |
| 12 | `IntegrationTests.Fixtures.DbContextInterceptorRegistrationTests.AddWriteDbContextInterceptors_CalledTwice_AppendsBothInterceptors` | 42703: column "joined_on" of relation "employees" does not exist |
| 13 | `IntegrationTests.Fixtures.EmployeeApiFactoryTests.TimeProvider_Replaced_AuditTimestampsUseFakeTime` | 42703: column "joined_on" of relation "employees" does not exist |
| 14 | `IntegrationTests.Fixtures.EmployeeApiFactoryTests.WriteInterceptors_Registered_ObserveApiHostUnitOfWorkCommit` | 42703: column "joined_on" of relation "employees" does not exist |
| 15 | `IntegrationTests.Fixtures.EmployeeDatabaseFixtureTests.CreateServices_AddAndCommit_StoresRowThroughProductionRegistration` | 42703: column "joined_on" of relation "employees" does not exist |
| 16 | `IntegrationTests.Fixtures.EmployeeDatabaseFixtureTests.ReadConnection_Insert_IsRejectedWithReadOnlyTransaction25006` | Expected (act to be the same string, but they differ at index 0: |
| 17 | `IntegrationTests.Fixtures.EmployeeDatabaseFixtureTests.ResetAsync_AfterRowsWereAdded_EmptiesTablesButKeepsMigrationHistory` | 42703: column "joined_on" of relation "employees" does not exist |
| 18 | `IntegrationTests.Migrations.MigrationReapplyTests.ApplyMigrationsAsync_RunTwiceMoreOnMigratedDatabase_KeepsOneHistoryRowAndExistingData` | 42703: column "joined_on" of relation "employees" does not exist |
| 19 | `IntegrationTests.Migrations.MigrationReapplyTests.IdempotentScript_RunTwiceWithPsqlOnEmptyDatabaseThenMigrateAsync_CreatesSchemaOnceOwnedByAppRole` | Expected (verify.ScalarAsync<long>("SELECT count(*) FROM pg_indexes WHERE schemaname = 'public' AND tablename  |
| 20 | `IntegrationTests.Persistence.EfCoreErrorLogLevelTests.CommitAsync_CheckViolation_PropagatesDbUpdateExceptionWithoutEfErrorLogs` | Expected (act to be the same string, but they differ at index 0: |
| 21 | `IntegrationTests.Persistence.EfCoreErrorLogLevelTests.CommitAsync_CommitTimeSerializationFailureOnce_RetriesToSuccessWithoutEfErrorLogs` | 42703: column "joined_on" of relation "employees" does not exist |
| 22 | `IntegrationTests.Persistence.EfCoreErrorLogLevelTests.CommitAsync_DuplicateEmail_Returns23001WithEfFailureLogsAtDebugOnly` | 42703: column "joined_on" of relation "employees" does not exist |
| 23 | `IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.Commit_TimeProviderAtPlusNineAndSeoulSession_StoresSameInstantAsUtc` | 42703: column "joined_on" of relation "employees" does not exist |
| 24 | `IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.CommitThenRead_MaxLengthAndUnicodeValues_RoundTripUnchanged(name: "\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00\ud83d\ude00"···, email: "Emoji@Example.com", phoneNumber: "01012345678")` | 42703: column "joined_on" of relation "employees" does not exist |
| 25 | `IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.CommitThenRead_MaxLengthAndUnicodeValues_RoundTripUnchanged(name: "가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가가"···, email: "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"···, phoneNumber: "0-1-2-3-4-5-6-7-8-90")` | 42703: column "joined_on" of relation "employees" does not exist |
| 26 | `IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.CommitThenRead_MaxLengthAndUnicodeValues_RoundTripUnchanged(name: "한글 English 混合 \ud83d\ude00", email: "mixed@example.com", phoneNumber: "010-1234-5678")` | 42703: column "joined_on" of relation "employees" does not exist |
| 27 | `IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.CommitThenReload_NewWriteDbContext_MaterializesThroughPrivateConstructorWithAuditAndVersion` | 42703: column "joined_on" of relation "employees" does not exist |
| 28 | `IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.ReadContext_StoredEmployee_ProjectsIdValueValueObjectsAndShadowAuditColumns` | 42703: column "joined_on" of relation "employees" does not exist |
| 29 | `IntegrationTests.Persistence.EmployeePersistenceRoundTripTests.ReadContext_UnknownOrEmptyId_ProjectsNothing` | 42703: column "joined_on" of relation "employees" does not exist |
| 30 | `IntegrationTests.Persistence.PersistenceLogExposureTests.CommitAsync_DuplicateEmail_NoLogRecordOrResultExposesEmailOrServerDetail` | 42703: column "joined_on" of relation "employees" does not exist |
| 31 | `IntegrationTests.Persistence.PersistenceLogExposureTests.CommitAsync_OneDuplicateEmail_LogsExactlyOneCommandErrorAndOneSaveChangesFailed` | 42703: column "joined_on" of relation "employees" does not exist |
| 32 | `IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_DeactivateAlreadyInactiveEmployee_WritesNothingAndKeepsXminAndUpdatedAt` | 42703: column "joined_on" of relation "employees" does not exist |
| 33 | `IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_SameIdDifferentEmail_Returns3003FromPrimaryKeyAndLogs202AtWarning` | 42703: column "joined_on" of relation "employees" does not exist |
| 34 | `IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_SameNormalizedEmailWithoutPreCheck_Returns23001FromEmailUniqueIndex(storedEmail: "dup@example.com", newEmail: "  DUP@Example.COM ")` | 42703: column "joined_on" of relation "employees" does not exist |
| 35 | `IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_SameNormalizedEmailWithoutPreCheck_Returns23001FromEmailUniqueIndex(storedEmail: "dup@example.com", newEmail: "dup@example.com")` | 42703: column "joined_on" of relation "employees" does not exist |
| 36 | `IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_SecondScopeDeactivatesWithStaleXmin_Returns3001AndLogs203AtDebug` | 42703: column "joined_on" of relation "employees" does not exist |
| 37 | `IntegrationTests.Persistence.UnitOfWorkConflictTests.CommitAsync_TwoScopesDeactivateSameEmployee_FirstSucceedsAndUpdatesXminAndUpdatedAtOnly` | 42703: column "joined_on" of relation "employees" does not exist |
| 38 | `IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_CommitTimeSerializationFailureOnce_RetriesWholeUnitAndStoresExactlyOneRow` | 42703: column "joined_on" of relation "employees" does not exist |
| 39 | `IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_DeletedAggregateWithDomainEvents_DeletesRowAndClearsEvents` | 42703: column "joined_on" of relation "employees" does not exist |
| 40 | `IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_PreCommitHookThrows_RollsBackSavedRowAndPropagatesWithoutRetry` | 42703: column "joined_on" of relation "employees" does not exist |
| 41 | `IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_SerializationFailureOnEveryAttempt_ThrowsRetryLimitExceededClassifiedAs9003` | 42703: column "joined_on" of relation "employees" does not exist |
| 42 | `IntegrationTests.Persistence.UnitOfWorkTransactionTests.CommitAsync_ServerDefaultIsolationSerializable_RunsInReadCommittedTransaction` | 42703: column "joined_on" of relation "employees" does not exist |
| 43 | `IntegrationTests.Schema.EmployeeSchemaTests.Columns_AfterMigration_MatchSampleTableTypesWithoutDefaults` | Expected columns to be equal to |
| 44 | `IntegrationTests.Schema.EmployeeSchemaTests.CommitAsync_TrackedEntryWithUndefinedStatus_RethrowsDbUpdateExceptionWithCheckConstraintAndRedactedDetail(status: -1)` | Expected (inner.SqlState, inner.ConstraintName) to be equal to |
| 45 | `IntegrationTests.Schema.EmployeeSchemaTests.CommitAsync_TrackedEntryWithUndefinedStatus_RethrowsDbUpdateExceptionWithCheckConstraintAndRedactedDetail(status: 0)` | Expected (inner.SqlState, inner.ConstraintName) to be equal to |
| 46 | `IntegrationTests.Schema.EmployeeSchemaTests.CommitAsync_TrackedEntryWithUndefinedStatus_RethrowsDbUpdateExceptionWithCheckConstraintAndRedactedDetail(status: 32767)` | Expected (inner.SqlState, inner.ConstraintName) to be equal to |
| 47 | `IntegrationTests.Schema.EmployeeSchemaTests.CommitAsync_TrackedEntryWithUndefinedStatus_RethrowsDbUpdateExceptionWithCheckConstraintAndRedactedDetail(status: 9)` | Expected (inner.SqlState, inner.ConstraintName) to be equal to |
| 48 | `IntegrationTests.Schema.EmployeeSchemaTests.ConstraintAndIndexNames_AfterMigration_EqualEfModelNamesAndConstants` | Expected indexes to be equal to {"ix_employees_joined_on_id", "ix_employees_name_joined_on_id", "pk_employees" |
| 49 | `IntegrationTests.Schema.EmployeeSchemaTests.OrderById_IdsGeneratedWithinSameMillisecondInsertedShuffled_ReturnsGenerationOrder` | 42703: column "joined_on" of relation "employees" does not exist |
| 50 | `IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_DefinedStatus_IsAccepted(status: Active)` | 42703: column "name" of relation "employees" does not exist |
| 51 | `IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_DefinedStatus_IsAccepted(status: Inactive)` | 42703: column "name" of relation "employees" does not exist |
| 52 | `IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_StatusOutsideSmallintRange_IsRejectedByColumnType22003` | Expected (act to be the same string, but they differ at index 0: |
| 53 | `IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_UndefinedOrReservedStatus_IsRejectedByCheckConstraint23514(status: -1)` | Expected (exception.SqlState, exception.ConstraintName, exception.TableName) to be equal to |
| 54 | `IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_UndefinedOrReservedStatus_IsRejectedByCheckConstraint23514(status: 0)` | Expected (exception.SqlState, exception.ConstraintName, exception.TableName) to be equal to |
| 55 | `IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_UndefinedOrReservedStatus_IsRejectedByCheckConstraint23514(status: 3)` | Expected (exception.SqlState, exception.ConstraintName, exception.TableName) to be equal to |
| 56 | `IntegrationTests.Schema.EmployeeSchemaTests.RawInsert_UndefinedOrReservedStatus_IsRejectedByCheckConstraint23514(status: 9)` | Expected (exception.SqlState, exception.ConstraintName, exception.TableName) to be equal to |
| 57 | `IntegrationTests.Schema.EmployeeSchemaTests.ReadConnection_InsertInsideExplicitReadCommittedTransaction_IsStillRejectedWith25006` | Expected (act to be the same string, but they differ at index 0: |
| 58 | `IntegrationTests.Schema.EmployeeSchemaTests.ReadConnection_WriteStatement_IsRejectedWith25006(sql: "UPDATE employees SET name = 'changed'")` | Expected (exception.SqlState, exception.ConstraintName) to be equal to |
| 59 | `IntegrationTests.Schema.EmployeeSchemaTests.ReadDbContext_RawInsertThroughProductionRegistration_IsRejectedWith25006` | Expected (act to be the same string, but they differ at index 0: |

### S05-T04 tester 대조표

| 완료 조건 | 성공 | 실패 | 엣지 |
|---|---|---|---|
| ① Register는 VO만, Active 고정, name, 이벤트 유지 | `EmployeeTests.Register_WithValidValueObjects_SetsIdAndEachValueObject` · `_FixesStatusToActive` · `_RaisesSingleRegisteredDomainEventWithIdOnly` | `Register_EmptyId_ThrowsArgumentException`, `Register_NullValueObject_ThrowsArgumentNullException` | `Register_Signature_TakesOnlyIdAndValueObjectsWithoutStatus`, `Register_EmailsDifferingOnlyInCase_*`, `Register_NfdName_*`, `Deactivate_CalledTwice_*` |
| ② EF 매핑 = dba 명세, 23505 → 23001 | `EmployeeModelMetadataTests.Columns_*` · `Indexes_*` · `CheckConstraints_*` · `ValueObjectConverters_RoundTripThroughCreate`, `UniqueConstraintRegistry_MapsOnlyNormalizedEmailIndexToDuplicateEmailInstance` | `ValueObjectConverters_ProviderValueBreakingRule_Throw`, `WriteContext_StoredValueBreakingValueObjectRule_ThrowsOnMaterialization`, `UniqueConstraintRegistry_OtherOrNonExactNames_AreNotMapped` | `Columns_HaveNoDefaultValues`, `Indexes_DoNotIncludeEmailInputColumn`, `Email_HasNoLowerCaseCheckConstraint`, `Identifiers_*_AtMost63Bytes` |
| ③ 샘플 · 폐기 상수 제거, 대응표 처리, grep 0 | grep 제외 범위 밖 0, `EmployeeErrorsTests.Fields_MatchDocumentedUsedRows` | `EmployeeErrorsTests.DeprecatedCodes_HaveNoConstants` | `AddEmployeeInfrastructure_EmployeeApplicationWithoutHandlers_RegistersNoHandlersAndStillBuilds`, `DocumentedRows_CountByStatus` |
| ④ 대상 대기 3규칙과 안전장치 | `PendingTargetRuleTests.All_IsExactlyTheDocumentedThreeRulesWithReleaseTaskIds` · `ShouldPassOnProduct_PendingRuleWithoutProductTargets_SkipsWithReleaseTaskId` | `ShouldPassOnProduct_PendingRuleWithTargets_FailsAskingToRemoveItFromList` | `ShouldPassOnProduct_SameRuleOutsideListWithoutTargets_FailsAsVacuousPass`, `Find_RuleNotInList_ReturnsNull` |
| ⑤ 메타데이터 · DI 통과, Migrations 테스트 2건 새 기대값 | `EmployeeModelMetadataTests` · `EmployeeInfrastructureServiceCollectionExtensionsTests` 전체 통과 | `EmployeeMigrationsTests` 2건(T05 전까지 허용 목록) | `Migrations_OfWriteContext_AreOnlyInitialCreate`, `MigrationAndSnapshotTypes_*_AreAllSealed` |
| ⑥ 경고 0, 실패 = 허용 목록 1:1 | build 경고 0 · 오류 0 | 실패 59 = TSV 59, 불일치 0 | Theory 사례는 개수로 대조, 실패 메시지 59건 모두 스키마 원인 |

### S05-T03 tester 대조표

| 항목 | 성공 | 실패 | 엣지 |
|---|---|---|---|
| ① sealed record + `Create` → 필드 코드 Result | `*Tests.Create_Valid*` 4개 클래스 | 코드마다 `BeSameAs(EmployeeErrors.X)` | `*Tests.Create_Same*_AreEqual` |
| 공백만 (4개 필드) | - | `*Tests.Create_NullEmptyOrWhitespace_Returns*Required` | `NameTests.Create_ControlCharacterOnlyThatIsNotWhitespace_ReturnsNameInvalidCharacter`, 추가 `NonWhitespaceInvisibleInputTests.*` |
| name 최대 길이 · NFD | `NameTests.Create_AtMaxLengthAfterTrimAndNfc_Succeeds`, `Create_NfdName_NormalizesToNfc` | `Create_OverMaxLengthAfterTrimAndNfc_ReturnsNameTooLong` | `Create_NfcExpandsOverMaxLength_ReturnsNameTooLong`, `Create_NfdAndNfcOfSameName_AreEqual` |
| name 제어 문자 · 서로게이트 | `Create_FormatCharacter_IsAllowed`, `Create_PairedSurrogate_IsAllowed` | `Create_ControlCharacterInside_*`, `Create_UnpairedSurrogate_*WithoutThrowing` | `Create_ControlCharacterOnlyAtEdges_IsTrimmedAndSucceeds`, `Create_OverMaxLengthWithControlCharacter_ReportsInvalidCharacterFirst` |
| email 형식 · 길이 | `EmailTests.Create_ValidEmail_KeepsTrimmedInputAndLowercasesNormalized`, `Create_AtMaxLengthAfterTrim_Succeeds` | `Create_Malformed_ReturnsEmailInvalid`, `Create_OverMaxLengthAfterTrim_ReturnsEmailTooLong` | `Create_LongMalformedEmail_ReportsLengthFirst`, `Create_DottedLocalPart_IsNotCheckedForDots` |
| 대소문자만 다른 이메일 | `EmailTests.Create_EmailsDifferingOnlyInCase_HaveSameNormalizedEmail` | - (같은 NormalizedEmail이 맞는 동작, 중복 거부는 S06 Handler) | 같은 테스트에서 Value는 다름 |
| 전화 하이픈 경계 | `PhoneNumberTests.Create_ValidPhoneNumber_KeepsInputAsIs` | `Create_LeadingTrailingOrConsecutiveHyphen_*` | `Create_TooFewDigitsWithLeadingHyphen_ReportsInvalidHyphenFirst` |
| 숫자 자리 · 전체 길이 · 허용 문자 | 8 · 15자리, `Create_AtMaxLengthWithMaxDigits_Succeeds` | `Create_DigitCountOutOfRange_*`, `Create_OverMaxLength_*`, `Create_CharacterOtherThanAsciiDigitOrHyphen_*` | `Create_OverMaxLengthWithPlus_ReportsInvalidCharacterFirst` |
| `2000-02-30` · 형식 | `JoinedOnTests.Create_ValidDate_ReturnsDate` | `Create_NotExactFormatOrNonexistentDate_*` | `Create_UnderNonInvariantCurrentCulture_ParsesSameDate` |
| joined 하한 · 미래 | `Create_MinValue_Succeeds`, `Create_FutureDate_IsAllowed` | `Create_BeforeMinValue_ReturnsJoinedOnTooEarly` | `Constants_MatchFieldRules` |
| ④ public const | `Name/Email.MaxLength_Is*`, `Constants_MatchFieldRules` | - (상수 값 단언) | - |
| ⑤ EmployeeErrors ↔ error-codes | `EmployeeErrorsTests.Fields_MatchDocumentedUsedAndDeprecatedRows` | `ReservedCodes_AreNotDefinedYet` | `DocumentedRows_CountByStatus` |
| ⑥ 다른 레이어 무변경 · 경고 0 · 전체 통과 | `git show --stat f4f4117 -- src`(Domain 5개), build 경고 0, test 1,730 | - | - |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다 (경고 0, 커버리지 보고 — NFR-06. S05는 샘플 제거로 분모가 작아진 수치임을 적는다)
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S05` 태그를 붙였다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| 2026-09-28 | - | 계획 리뷰 | 승인(대리) | 작업 5 → 6(옛 T03을 T03 VO · T04 Aggregate · 매핑 · 샘플 제거로 분할, 옛 T04 · T05는 T05 · T06). 막히는 질문 6개 추천안 채택, 보완 조건 3개(대상 대기 해제 ID와 S06 계획 메모, T04 실패 허용 목록 확정 · 목록 밖 실패 BLOCKED, S06 · S07 참조 수정은 T01 커밋) |
| 2026-09-28 | S05-T01 | dba | PASS | 현황 확인: 마이그레이션 `20260927134235_InitialCreate` 1건(product_version 8.0.31), 23505 매핑 `ux_employees_email` → 23001 1건(EmployeeInfrastructureServiceCollectionExtensions.cs:47), Migrations 폴더 5개(생성 3 + Sealed 2). database.md 변경 없음. handoff → T04(옛 이름 주석 5곳) · T05(리셋 전 기준) |
| 2026-09-28 | S05-T01 | developer | PASS | 문서 작업. ① `git merge-base --is-ancestor v0.1.0 HEAD` 0, `origin/develop`(210b128) 0, 병합 커밋 `7160967`(앞선 `1f671da`와 함께 v0.1.0 포함) ② 스프린트 S05~S07 확정, ADR 0025 ① API 규칙 예외 · 0026 ② 일괄 가져오기 입력 처리 · 0027 ③ 이메일 대소문자 무시 유일 · 0028 ④ BuildingBlocks 오류 계약 확장, Employee 새 검증 코드 21007~, 공통 413 · 415는 T02에서 할당. 사용 중 코드 Common 12 · Employee 8이 error-codes 표와 일치 ③ BL-121~127 · TD-028 new, BL-024 planned:S06 ④ README PRD-002 · S05~S07 행, roadmap Phase 2 행, S06-T01 의존 · S07-T01 dba 칸 S05-T05 → S05-T06, S06 계획 메모 '대상 대기' 해제, check-docs 94개 · 결함 4 ⑤ TD-010 위험 수용 · open(S05-T06 1회 실측), BL-019 done(3003), BL-023 done(d2263d6), BL-024 planned:S06 ⑥ 기준선 1,538개(통과 1,537 · 건너뜀 1, 13개 프로젝트, trx), 대응표 95행 · 226개(삭제 96 · 수정 35 · 이전 6 · 변경 없음 89) ⑦ build 경고 0 · 오류 0, test 실패 0 · 건너뜀 1. 새 BL-128(roadmap PRD-001 행 미갱신) |
| 2026-09-28 | S05-T01 | developer | 기록 | S02-T06 계약 확인: ProblemDetails는 code · traceId 항상, `errors`는 ValidationError(400, 1001 고정, sealed)에만. FieldError는 Validation 유형만 받아 409 행 오류 표현 불가. ErrorType → HTTP 9종, 413 · 415 없음. BadHttpRequestException은 400 · 1001(TD-021), multipart InvalidDataException은 9001. `Rows[3].Email` → `rows[3].email`. **S06-T01 완료 조건 확정안**(S06 계획 리뷰 입력): ① ErrorType.Conflict + 행 경로 · 정수 코드 · 메시지 목록을 가진 상세 Conflict 오류(BuildingBlocks.Domain), 409 `errors`(ValidationError와 같은 키 · `{code, message}`) ② ErrorType PayloadTooLarge · UnsupportedMediaType → 413 · 415 + ADR-0028 공통 코드 ③ BadHttpRequestException 413은 413 · 새 코드, 그 밖은 400 · 1001 유지(TD-021 부분 상환, multipart InvalidDataException은 S06-T05) ④ 기존 409(3001 · 3003 · 23001) · 400 형식 · ErrorType 9종 · FieldError 제한 하위 호환 단위 테스트 ⑤ 23505 detail 비노출 ⑥ TD-010은 ADR ② 위험 문구 · S05-T06 실측만, BL-023 처리됨 ⑦ error-codes · api-guidelines · CommonErrorsTests 1:1 |
| 2026-09-28 | S05-T01 | developer | 기록 | 비차단 질문 판단(추천안): 범위 밖 중 '→ 백로그' 표시 없는 4개(인증 / 권한, 프론트엔드, Idempotency-Key · 통합 이벤트, Employee 외 서비스)는 이미 roadmap · ADR-0023 · api-guidelines에 있어 등록하지 않음(완료 조건 ③ 해석). RETRO-PRD-001 ADR 후보는 0029부터 또는 PRD-002 병합 뒤(→ `/retro` 입력). 413 · 415 ErrorType 값 · 코드 자리는 T02 1차 초안에서 확인. T04 실패 허용 목록 범위는 오케스트레이션에 확인 요청 |
| 2026-09-28 | S05-T04 | 결정 | 승인(대리) | 실패 허용 범위 변경: 보완 조건 (b)를 "스키마 불일치(옛 마이그레이션) 원인으로 trx에서 확인된 실패는 developer 단계에서 대응표에 추가해 허용(행마다 실패 원인 한 줄, 예: 42703 column does not exist)"으로. 스키마 불일치로 설명되지 않는 실패와 T05 리셋 뒤에도 남는 실패는 BLOCKED |
| 2026-09-28 | S05-T01 | reviewer | PASS | D1 check-docs 94개 · 결함 4(기준선), D2 ADR 변경 0, D3 대응표 95행 · 226개를 기준선 TSV와 스크립트 대조해 행별 불일치 0, D4 ①~⑦ 대응 칸 있음, D5 roadmap PRD-001 행은 BL-128로. 번호 충돌 없음(BL-121 · TD-028 · ADR 0025 · 21007). handoff → T02(PRD 78 · 153행 '가번호' 문장), 범위 밖 미등록 4개는 오케스트레이션이 수용(T04 결정 메시지) |
| 2026-09-28 | S05-T01 | tester | PASS | 명령 점검: 병합 확인 v0.1.0 · origin/develop 조상 0(7160967, v0.1.0은 1f671da 경유), check-docs 94개 · 결함 4(기준선), ADR 변경 0, 코드 Common 12 · Employee 8, 대응표 95행 · 226개 기준선 TSV와 따로 대조해 불일치 0, build 경고 0 · 오류 0, test 1,538(통과 1,537 · 건너뜀 1 · 실패 0, 13개 프로젝트). 파일 변경 없음. handoff → T04(표시 이름이 같은 Theory 사례 11종, 대조는 개수로) |
| 2026-09-28 | S05-T02 | dba | PASS | ADR ② · ③ DB 조항 문안 작성(파일 변경 없음). 실측(임시 postgres:17 17.11, tmpfs, 볼륨 전후 4개 동일): `employee_app`(DB 소유자, NOSUPERUSER)로 `CREATE EXTENSION citext` 성공 → 권한은 citext 미사용 근거에서 제외. U+0130이 .NET `ToLowerInvariant`는 그대로 · PG libc `lower()`는 `i` · ICU는 `i`+U+0307, CHECK(`normalized_email = lower(email)`)는 23514, `lower()` 식 유니크는 `İ@x.com`/`i@x.com` 23505, 일반 유니크는 2행 저장, ICU nondeterministic는 LIKE 불가 · `ﬀ`=`ff`. 1,000행 `INSERT ... RETURNING xmin` 한 트랜잭션(distinct xmin 1), `= ANY` 배열 1,000개 1건 파라미터, 499행 뒤 23505 → ROLLBACK 0행 |
| 2026-09-28 | S05-T02 | developer 1차 | PASS | ADR 0025~0028 초안(파일 없음, 스크래치). 확인 항목: 0025 이름 값의 instance · 요청 로그 노출(A 템플릿화 / B 이 엔드포인트만 / C 백로그), 0026 (1) VO Create → Result 적용 범위 (2) Command 모양 바이트 + Sources (3) 행 오류 잘림 코드 (4) BOM · 공백만 입력, 0028 413 · 415 값(A 11 · 12 / 1004 · 1005, B 61 · 62 / 6001 · 6002). dba 문안의 사전 조회 '같은 트랜잭션'을 ADR-0014 기준 '트랜잭션 밖'으로 정정 |
| 2026-09-28 | S05-T02 | ADR 확인 | 승인(대리) | 전부 추천안: 0025 A(요청 로그 RequestPath · ProblemDetails instance(ErrorProblemDetails.cs:55) · 추적 url.path 세 곳 라우트 템플릿화, S07-T02 범위와 같음), 0026 (1) Employee VO 4개 한정 (2) `(Format, [Flags] Sources, ReadOnlyMemory<byte> Content)`, PRD FR-06 변경 이력에 기록 (3) 21030 · 23002 (4) 빈 입력 코드, 0027 원안, 0028 A(1004 · 1005), design.md T02에서 반영. ADR 4건은 `/retro` ④ 추인 대상으로 따로 표시 |
| 2026-09-28 | S05-T02 | developer 2차 | PASS | ADR 0025~0028 `accepted` 파일, adr/README 4행, api-guidelines 예외 절 · 공통 변환 규칙(413 · 415 · 409 `errors` · instance), error-codes(공통 1004 · 1005 예약 S06-T01, Employee 33행 = 사용 5 · 폐기 3(21001 · 21002 · 21006) · 예약 25(21007~21030 · 23002, 행마다 작업 ID), 번호 중복 0), coding-conventions 실패 처리 경계 · 이메일 문구, database.md VO 값 변환기 · 트랜잭션 예외 링크 · CHECK 문구(U+0130 실측), clean-architecture 155행, testing-strategy 대상 대기 목록, PRD-002 '가번호' 문장 · FR-05 · FR-06 Command 모양 · 변경 이력(S05-T02 대리 승인), 09-memory/design 4행, S07 문서 S07-T02 항목(ADR-0025 A 세 곳). check-docs 98개 · 결함 4. 코드 변경 0. 스킬 전달 누락(1차 코드 표 미전달)으로 21023~21027 순서가 승인안과 달라 커밋 전에 승인안(JSON 21023~21026, 1,000행 초과 21027)으로 정정. FR-05 문구는 FR-06과 어긋나 함께 맞춤. RequestPath 위치는 BuildingBlocks가 아니라 Employee.Api Program.cs:85 · ServiceDefaults로 정정 기재 |
| 2026-09-28 | S05-T02 | reviewer | PASS | ADR 템플릿(frontmatter 13키 · 절 4개), FR-09 ①~④ ↔ ADR 0025~0028 1:1, Q14 · Q15 반영, FR-06 Command 모양 승인안 일치, 코드 중복 0(Employee 33행 = 사용 5 · 폐기 3 · 예약 25, 배정 승인안 일치), D1 check-docs 98개 · 결함 4, D2 기존 ADR 수정 0, D3 코드 대조(ErrorProblemDetails.cs:55, Program.cs:85, EmployeeEmail static, ErrorType 9종, EnsureValidCode), D4 ①~⑥ 대응, D5 S07 메모 · 400 예시 21004는 승인 결정에 따른 정합 수정 |
| 2026-09-28 | S05-T02 | 기록 | 다음 스프린트 인계 | S06 계획 리뷰 입력(developer 1차 · reviewer handoff): S06-T01 — 상세 Conflict 오류 형식을 ErrorAndResultAreNotDerived 예외 목록에, `[Consumes]` 불일치 415의 공통 ProblemDetails 변환 방법 실측, ErrorType 11 · 12 · 팩토리 2개, BadHttpRequestException 413 → 1004. S06-T04 — 잘림 21030 · 23002, BOM · 공백만 입력 = 21028, Validator가 만드는 21028 · 21029의 경로(PropertyName) 결정과 error-codes 259행 목록 반영 판단. S06-T05 — 폼 필드의 잘못된 UTF-8 바이트 치환 여부 실측, 바인더의 InvalidDataException → 413 판정 기준. S07-T02 — ADR-0025 A 세 곳(S07 문서 계획 메모에 반영됨), 추적 span 경로 실측 |
| 2026-09-28 | S05-T02 | tester | PASS | 명령 점검표: ADR 0025~0028은 bd0cbc7에서 처음 추가(A 4, 기존 ADR 수정 0), bd0cbc7 이후 · S05 계획 확정 이후 src/tests 커밋 0. frontmatter accepted 4. error-codes 47행 중복 0(공통 사용 12 · 예약 2, Employee 사용 5 · 폐기 3 · 예약 25, 예약 행 모두 작업 ID), 사용 · 폐기 20행 ↔ CommonErrors · EmployeeErrors 상수 20개 1:1. check-docs 98개 · 결함 4(bd0cbc7 트리도 같음, 새 결함 0), 새 ADR 앵커 링크 정상. 사실 문장 재실측 일치. 테스트 추가 0 |
| 2026-09-28 | S05-T03 | dba | 해당 없음 | 파이프라인 표 dba 열 "해당 없음"(호출 생략) |
| 2026-09-28 | S05-T03 | developer | PASS | TDD(Red: CS0103 · CS0117 확인 → Green). VO 4개(Name · Email · PhoneNumber · JoinedOn, `public sealed record`, private 생성자 · get-only, `Create(string?)` → Result, Employees 폴더), EmployeeErrors 21007~21017 11개, 상수 Name.MaxLength 100 · Email.MaxLength 254 · PhoneNumber.MaxLength 20 · Min/MaxDigitCount 8/15 · JoinedOn.Format · MinValue(static readonly). 필드별 첫 실패만 보고, 판정 순서 name 21007 → 21009 → 21008, email 21003 → 21005 → 21004, tel 21010 → 21011 → 21013 → 21014 → 21012, joined 21015 → 21016 → 21017. 명세 밖 결정 2개(tel 판정 순서, email '.' 규칙은 domain에만)는 주석 · 테스트 · error-codes에 기록. xUnit v3 MemberData 직렬화가 짝 없는 서로게이트를 바꿔 `DisableDiscoveryEnumeration = true`로 수정. error-codes 21007~21017 '사용'(Employee 사용 16 · 예약 14 · 폐기 3). Aggregate · Application · Infrastructure · EmployeeEmail 변경 0. build 경고 0 · 오류 0, `dotnet format --verify-no-changes` 0, test 통과 1,720 · 건너뜀 1 · 실패 0(추가 183), check-docs 98개 · 결함 4. 새 BL-129~131 |
| 2026-09-28 | S05-T03 | reviewer | PASS | 표준 진입 점검 16항목 통과: build 경고 0, test 1,720 · 건너뜀 1 · 실패 0(직접 실행), format 0, FR-01 엣지 목록 전부 대조(NFD 리터럴은 hex로 확인), 규칙 코드 11개마다 실패 테스트, Domain 프레임워크 비의존, NFC · 소문자화는 VO 한 곳, 억제 0. ADR-0026 8절 · ADR-0027 대조 일치, ADR 커밋 순서 OK. 명세 밖 결정 2개는 PRD · 인계 메모와 충돌 없음 |
| 2026-09-28 | S05-T03 | tester | PASS | FR-01 엣지 목록 · 규칙 11개 대조(대조표 아래), 빈 곳 1개(공백이 아닌 보이지 않는 문자 U+0000 · U+200B의 tel · joined · email 분류) 보강 +10(NonWhitespaceInvisibleInputTests). build 경고 0, test 1,730 통과 · 건너뜀 1 · 실패 0. error-codes 사용 16 · 폐기 3 · 예약 14 대조 일치, ADR 순서 OK. Email이 NUL(U+0000)을 허용해 PG text 저장 시 500 가능성(미실측) → BL-129에 병합 |
| 2026-09-28 | S05-T04 | dba | PASS | database.md '새 스키마 명세(실측 전, S05-T05에서 생성 SQL로 확정)' 작성: 컬럼 9개 + xmin 열 순서 · 타입 · NOT NULL · 기본값 없음, 제약 · 인덱스 5개 기대 SQL · 이름 결정 방식, 이름 상수 위치, EmployeeImportFormat DB 미저장. 옛 ERD · InitialCreate 대조 표는 '옛 스키마'로 옮기고 기준 구분 문장(EF 모델 = 새 명세, 마이그레이션 · DB = 옛 InitialCreate, T05 전까지). 인계 메모 '최장 식별자 29바이트' 정정: 최장은 `ix_employees_name_joined_on_id` 30바이트(메타데이터 테스트 기대 28 → 30). 열 순서는 EF Core 8 선언 순서 동작에 기댄 것이라 실측 전 표시. check-docs 결함 4. 메타데이터 · 마이그레이션 테스트 사양은 스크래치 `s05/t04-dba-handoff.md` |
| 2026-09-28 | S05-T04 | developer | PASS | 첫 단계 실측: 값 변환기 VO의 `Where(e.Name == name)` · `OrderBy(JoinedOn).ThenBy(Id)` · Skip/Take · 최상위 Select(.Value) · `list.Contains(e.NormalizedEmail)` → `= ANY` 번역됨, `Where(e.Name.Value == ...)`는 번역 안 됨(VO끼리 비교 규칙을 coding-conventions에). Owned · ComplexProperty는 `(name, joined_on, id)` 복합 인덱스 선언 불가(ArgumentException / InvalidOperationException), Complex는 열 순서도 깨짐 → 값 변환기. ① Register(EmployeeId, Name, Email, PhoneNumber, JoinedOn), Active 고정, EmployeeEmail.cs · DisplayName 삭제 ② EmployeeConfiguration 명세 일치, 메타데이터 사양 15항목 green(#10 · #14 옛 이름 부정 리터럴은 grep 0을 위해 정확 집합 단언으로), 23505 매핑 `ux_employees_normalized_email` → 23001 ③ 샘플 API · Command · Query · 폐기 상수 삭제, Skip 0, 샘플 잔존 grep은 제외 범위 4줄만 ④ PendingTargetRules(3규칙 → S06-T04 · S06-T05) + 안전장치 PendingTargetRuleTests 9개, 아키텍처 통과 105 · 건너뜀 4 ⑤ DI 레지스트리 키 1개 ⑥ build 경고 0 · 오류 0, format 0, test 1,608 = 통과 1,545 · 실패 59 · 건너뜀 4(실패 = 허용 목록 59건 1:1, 원인 42703 등 스키마 불일치, 목록 밖 0). 대응표에 19행 · 43개 추가(샘플 의존 3행, 옛 스키마 실패 16행). 스키마와 무관한 테스트 데이터 결함 1건(전화 21자) 수정. 기준 문서 정합(error-codes · clean-architecture · coding-conventions · testing-strategy · employee-api · api-reference · local-setup), 억제 승인 목록 20 → 17. check-docs 결함 4. 새 BL-132 · 133 |
| 2026-09-28 | S05-T04 | 기록 | 허용 목록 확정 | 실패 허용 목록 59건(단위 2 + 통합 57)을 아래 'S05-T04 실패 허용 목록 (확정)' 표로 확정. 목록 밖 실패 0 |
| 2026-09-28 | S05-T04 | reviewer | PASS | 직접 재실행: build 경고 0, format 0, test 1,608 = 통과 1,545 · 실패 59 · 건너뜀 4, 실패 59건이 허용 TSV · 문서 표와 1:1(목록 밖 0), trx 메시지 59건 모두 스키마 불일치(42703 등). 메타데이터 #10 · #14 대체 단언은 사양보다 약하지 않음. 대상 대기 3규칙 testing-strategy와 1:1, 공허 통과 방지 유지(목록 밖 사본은 대상 0개면 실패, 대기 규칙에 대상이 생기면 실패). 대응표 114행 · 269개가 기준선과 행별 불일치 0. ADR-0026 8절 · 0027 일치, Migrations 변경 0. 억제 승인 목록 20 → 17 정당(삭제된 샘플 CA1812 3건) |
| 2026-09-28 | S05-T04 | tester | PASS | 직접 재실행: build 경고 0 · 오류 0, test 1,608 = 통과 1,545 · 실패 59 · 건너뜀 4. 실패 59건이 허용 TSV와 이름별 개수 1:1(목록 밖 0), 원인 42703 54 · 옛 인덱스 3 · 옛 컬럼 1 · ModelSnapshot 1. 건너뜀 = 대상 대기 3 + 기존 1. 샘플 잔존 grep 제외 범위 밖 0. 완료 조건 ①~⑥ 대조표(아래) 빈 곳 없음, 테스트 추가 0. S06-T04 인계: BuildingBlocks.Api ProblemFieldError.cs:4 XML 주석 예시가 폐기 코드 21001 사용 |
| 2026-09-28 | S05-T05 | dba | PASS | 커밋 R `5ee04a4`(Persistence/Migrations/만 4 files, 이름 변경 감지 포함 옛 3 삭제 · 새 3 + Snapshot 수정, footer · trailer 없음 — ADR-0012 '커밋 footer는 따로 두지 않는다'). 리셋 사유: S05-T04 새 스키마(name · normalized_email · phone_number · joined_on, ux 교체, ix_ 2개)를 옛 마이그레이션이 담지 못함. 20260927134235 → `20260928090646_InitialCreate`, 폴더 5개(생성 3 + Sealed 2), ProductVersion 8.0.31. **로컬은 AppHost 실행 전 `emergency-hub-postgres-data` 볼륨 삭제 필요**(옛 이력만 있으면 42P07 예상, 다른 worktree와 볼륨 공유). 스냅샷 없는 `migrations add`에는 `--output-dir Persistence/Migrations`가 필요(없으면 `Migrations/`에 `...Infrastructure.Migrations`로 생성됨, 실측). 점검표(--idempotent): a 통과(최장 30바이트, 스키마 한정자 0, 따옴표 `"__EFMigrationsHistory"` 7 · `"migration_id"` 5), b 통과(uuid · varchar(100) · varchar(254)×2 · varchar(20) · date · smallint · timestamptz×2), c 통과(9개 NOT NULL, DEFAULT 0, xmin 0), d 통과(pk_employees, `ck_employees_employee_status CHECK (employee_status IN (1, 2))`, ux = EmployeeDbNames 상수, fk_ 없음, DEFERRABLE 0), e 통과(3개, CONCURRENTLY 0), f 통과(migration_id varchar(150) · product_version varchar(32), 8.0.31), g 통과(DO 블록 5개, 임시 postgres:17에 두 번 적용 rc 0 · 이력 1행). 인덱스 생성 SQL: `CREATE INDEX ix_employees_joined_on_id ON employees (joined_on, id);` · `CREATE INDEX ix_employees_name_joined_on_id ON employees (name, joined_on, id);` · `CREATE UNIQUE INDEX ux_employees_normalized_email ON employees (normalized_email);`. has-pending-model-changes 차이 없음, build 경고 0, EmployeeCreateScript 기대 문자열과 차이 0 |
| 2026-09-28 | S05-T05 | dba | 기록(커밋 D) | database.md(변경 이력, 새 스키마 명세 '실측 전' 해제 · 새 ERD · 대조, 점검표 실측 열, 적용 범위 표, 명명 예시, --output-dir), local-setup('운영 전 리셋 뒤 (볼륨 삭제 필요)' 절, 42P07은 예상 · 미재현), testing-strategy Q2(인덱스 4 · 제약 2, 임시 DB 실측). check-docs 결함 4. testing-strategy P4 행(옛 `ux_employees_email → 23001 (실측)`)은 새 이름 재실측 뒤 고침(→ T06) |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

**사전 점검** (2026-09-28)

| 항목 | 명령 | 결과 |
|---|---|---|
| 브랜치 | `git branch --show-current` | `feature/prd-002-employee-contacts` |
| 원격 차이 | `git rev-list --count origin/feature/prd-002-employee-contacts..HEAD` | 0 (HEAD `d125bee`) |
| Docker | `docker info --format '{{.ServerVersion}}'` · `docker version --format '{{.Server.APIVersion}}'` | 24.0.7 · 1.43 → `DOCKER_API_VERSION=1.43` |
| SDK | `dotnet --version` | 8.0.425 |
| gh | `gh auth status` | 로그인됨 |
| 개발 인증서 | `dotnet dev-certs https --check --trust` | 종료 코드 7(미신뢰) → AppHost는 `--launch-profile http` |
| check-docs 기준선 | `node scripts/check-docs.js` | 94개 파일, 결함 4(raw frontmatter, BL-018) |

**사용자 사전 합의** (세션 시작 시): T02 ADR 확인은 오케스트레이션 세션 대리 확인, TD-010은 위험 수용 · `open`(ADR ② 위험 항목), 샘플 API 제거와 커밋 시점 통합 테스트 실패 허용.

**에이전트 리뷰 요약**

| 관점 | 주요 발견 |
|---|---|
| dba | 옛 T03 단위 테스트 통과 불가(EmployeeMigrationsTests), 원시 SQL 통합 테스트가 대응표 밖, Sealed 2개 재작성, `ix_` · `date` · `_on` 첫 사례 실측, TD-010은 3003으로 보일 가능성 |
| developer | 아키텍처 규칙 대상 0개, 샘플 HTTP 테스트 삭제 필요, VO EF 매핑 미정, T03 분할 제안, 배포 코드 처리, BuildingBlocks `IRepository` 변경 부적합 |
| reviewer | 아키텍처 대상 0개 · 마이그레이션 테스트 충돌, VO Result가 실패 처리 경계와 어긋남(기준 문서 반영 대상 누락), 배포된 21001~21006 처리, FR-01 모호점 |
| tester | 예상 실패 기준(삭제 vs 실패), HTTP 증빙 공백, 예약 코드 대조, FR-01 DB 엣지, UUID v7 같은 밀리초 정렬, T04(옛) 점검 기준 |

**orchestrator 통합 · 결정** (모두 추천안 채택)

| # | 질문 | 결정 |
|---|---|---|
| 1 | 샘플 제거로 대상 0개가 되는 아키텍처 규칙 3개 | 해제 작업 ID(S06-T04 · S06-T05)를 적은 "대상 대기" 목록으로 명시 건너뜀 + 대상이 생기면 실패하는 안전장치. testing-strategy는 T02에서 |
| 2 | T04 커밋 시점 예상 실패 범위 | 단위는 EmployeeMigrationsTests 2건, 통합은 대응표 "수정(T05 뒤 green)" 행만. 컴파일 실패 0, trx 이름 1:1, 목록 밖 실패는 BLOCKED |
| 3 | 배포된 Employee 코드 | 21001 · 21002 · 21006 폐기, name은 21007부터 새로. 21003~21005 · 22001 · 23001 유지(23001은 DB 중복 · 경합 23505 공용) |
| 4 | VO EF 매핑과 기준 문서 | EmployeeConfiguration HasConversion, NormalizedEmail은 string 속성. 기준 문서(database · coding-conventions · clean-architecture · testing-strategy)는 T02에서 먼저 |
| 5 | S05 동안 HTTP 수준 증빙 공백 | 대응표 "이전 대기: S06-T06 / S07-T03" + 위험 기록. 테스트 전용 Controller 없음 |
| 6 | T03 분할 | 분할(작업 5 → 6) |

**위험**

- (상) ADR ②(VO Create → Result 적용 범위, ADR-0018 범위 예외)가 T03 이후 전체의 전제다. T02 1차 초안에서 적용 범위 문장을 먼저 확정한다.
- (중) EmployeeMigrationsTests 기대 SQL과 T05 실제 생성 SQL이 다르면 T05 → T04 반려. `ix_` 이름은 메타데이터 테스트로 먼저 고정한다.
- (중) 값 변환기 속성의 LINQ 번역이 T06에서 막힐 수 있다. T04 첫 단계에서 실측, 막히면 BLOCKED.
- (중) S06에서 대상 대기 해제 누락 → 안전장치 테스트와 S06 계획 메모.
- (중) TD-010(위험 수용): 경합에서 pk가 먼저 검사되면 3003으로 보일 수 있다. T06 1회 실측, S06 인계.
- (저) HTTP 증빙 공백(S05~S06-T06), 볼륨 공유 시 42P07, 커버리지 분모 축소.
- (저) 문서 작업(T01 · T02)은 PRD-001 반려 7회 유형이다. 세부 단언은 인계 메모에 두고 완료 조건은 6~7문장.

**계획 메모** (분할 시 메모를 새 번호로 갱신)

- **develop 병합 커밋**(`1f671da`, `7160967`)은 `Stage` footer가 없는 예외 커밋이다.
- **리셋 전용 작업**(T05): 커밋 R(Migrations만, footer 없음)과 커밋 D(기록, `Stage: dba`). developer 단계는 코드를 바꾸지 않는다.
- **T04 → T05 공백**: T04 커밋 시점에는 허용 목록의 테스트만 실패한다. push는 스프린트 종료 때라 CI 영향은 없다. T04 developer 진입 점검의 "마이그레이션 적용" 항목은 T05로 넘긴다.
- 스키마 변경은 T04에 모아 리셋은 T05 한 번으로 끝낸다. 이후 스키마를 바꾸면 추가 마이그레이션이 필요하다.
- T03은 ADR ②(VO 적용 범위) · ③(이메일 정규화)이 `accepted`된 뒤 시작한다.
- BL-024는 편입 확정(S06-T06 · S07-T04), T01에서 `planned:S06`. BL-023은 PRD-001 S03에서 처리됨. .NET 10 전환(BL-002)은 진행하지 않음.

## 대리 승인

> 사용자 부재 등으로 승인 지점(계획 리뷰 · 결정 · BLOCKED · 결과 리뷰 · ADR 확인)을 다른 세션이 승인하면 그때마다 한 행을 추가합니다. 추인은 `/retro` ④에서 사용자가 일괄로 합니다.

| 날짜 | 승인 지점 | 승인 내용 | 승인한 세션 | 근거 (진행 기록) | 추인 |
|---|---|---|---|---|---|
| 2026-09-28 | ① 계획 리뷰 | 작업 5 → 6 분할, 막히는 질문 1~6 추천안, 세부 기본값 · 위험 기록, 보완 조건 3개 | 오케스트레이션 `emergency-hub-d2` | 2026-09-28 계획 리뷰 행 | 대기 |
| 2026-09-28 | 결정(S05-T04 실패 허용 범위) | 스키마 불일치 원인 실패는 developer 단계에서 대응표에 추가해 허용(원인 한 줄), 그 밖은 BLOCKED | 오케스트레이션 `emergency-hub-d2` | 2026-09-28 S05-T04 결정 행 | 대기 |
| 2026-09-28 | ADR 확인(S05-T02) | **ADR 0025~0028 결정 내용(`/retro` ④ 추인 대상 ADR)**: 0025 A, 0026 (1)~(4) 추천안, 0027 원안, 0028 A, design.md 즉시 반영 | 오케스트레이션 `emergency-hub-d2` | 2026-09-28 S05-T02 ADR 확인 행 | 대기 |

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
| 2026-09-28 | - | BL-024 편입 확정, .NET 10 미진행 메모 |
| 2026-09-28 | orchestrator | 계획 리뷰 확정(작업 5 → 6, 인계 메모, 대리 승인) |
| 2026-09-28 | developer | S05-T01: 스프린트 번호 확정, 증빙 테스트 대응표 틀(`v0.1.0` 기준선) 추가 |
