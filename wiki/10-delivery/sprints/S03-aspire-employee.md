---
title: "S03: Aspire와 Employee 샘플 서비스 전 구간"
type: sprint
sprint: "S03"
status: active
prd: [PRD-001]
started: 2026-09-27
finished:
adrs: []
worklogs: []
aliases: [S03]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-27
---

# S03: Aspire와 Employee 샘플 서비스 전 구간

- PRD: [PRD-001](../prd/PRD-001-foundation.md)
- 토픽 브랜치: `feature/prd-001-foundation` · 스프린트 종료 태그: `sprint/S03`

## 목표

> `dotnet run --project <AppHost>` 한 번으로 PostgreSQL → MigrationService → Employee Api가 순서대로 뜨고, HTTP로 직원을 등록 · 조회할 수 있으며, 통합 테스트가 DB 규칙을 증명한다.

## 작업

실행 순서: T01 → T02 → T03 → T04 → T05 → T06 → T07 (계획 리뷰에서 5개 → 7개로 재구성: 기존 T04를 T03 · T05로, 기존 T05를 T06 · T07로 나누고, Api를 ServiceDefaults · MigrationService 뒤로 옮김)

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S03-T01 | Employee Domain · Application: 직원 등록 Command, 단건 조회 Query | FR-08, FR-09, FR-11 | Employee Aggregate(sealed, `EmployeeId` record struct)는 팩토리 `Register(id, displayName, email, status)`에서 이름 Trim과 이메일 Trim + ToLowerInvariant 정규화를 하고, 불변식 위반이면 예외를 던지며, 등록 도메인 이벤트를 수집하고, 변경 메서드 `Deactivate()` 1개를 가진다. `EmployeeStatus : short`는 Active=1, Inactive=2이며 0은 쓰지 않는다. 등록 Command + Validator(21001~21006, employeeStatus 정의값 1002) + Handler는 `IIdGenerator`로 ID를 만들고, 정규화된 이메일로 사전 중복 검사를 해 중복이면 23001을 반환하며, SaveChanges를 호출하지 않는다. 단건 조회 Query + `IEmployeeReadRepository` + `EmployeeResponse`는 없는 ID에 22001을 반환한다. EmployeeErrors와 로그 이벤트 20001을 정의하고 error-codes.md Employee 절에 등록했다. 단위 테스트가 성공 / 실패 / 엣지(인계 메모)를 다루고 모두 통과하며 빌드 경고 0이다 | S02-T02, S02-T06 | done | 7830001, c007120 |
| S03-T02 | Employee Infrastructure: 쓰기 / 읽기 DbContext, 매핑, Repository, 설계 시점 팩터리, 초기 마이그레이션 | FR-08, FR-06, FR-02, FR-11, NFR-01 | 두 DbContext가 공유하는 매핑이 계획 리뷰의 `employees` 확정안을 따른다. 테이블 · 인덱스 이름 상수는 Infrastructure 한 곳에 두고, `HasUniqueIndex`와 UoW 23505 매핑(`ux_employees_email` → 23001)이 같은 상수를 쓴다. Repository · ReadRepository와 `AddEmployeeInfrastructure`(실행 전략 설정 한 곳, 재시도 설정 인자)를 구현했다. 쓰기 DbContext용 `IDesignTimeDbContextFactory`와 EF Design 참조(Infrastructure에만, PrivateAssets=all)를 두고, 도구 매니페스트 dotnet-ef로 `InitialCreate` 1건을 생성했다. DB 없이 `DbContext.Model` 메타데이터를 검사하는 매핑 테스트가 통과한다. dba가 `--idempotent` SQL을 기준 SQL · BL-084 · BL-036 항목과 대조한 결과를 진행 기록에 남기고, database.md 코드 정의 표와 ERD를 갱신했다. 생성 코드를 포함해 `dotnet build` 경고 0이고 `dotnet format --verify-no-changes`를 통과한다(BL-045) | T01, S02-T04 | doing | |
| S03-T03 | ServiceDefaults · MigrationService | FR-03, FR-08, FR-09 | `AddServiceDefaults`는 OTel 트레이스 · 메트릭(OTLP 엔드포인트가 없으면 exporter 미등록), 서비스 디스커버리, Http.Resilience를 등록한다. `MapDefaultEndpoints`는 `/health/live`(DB 검사 없음)와 `/health/ready`를 모든 환경에 매핑한다(BL-030). Serilog는 OTLP 싱크 단일 경로(ADR-0020)이고, `ReadFrom.Services`로 DI의 `ILogEventSink`를 받으며, ExceptionHandlerMiddleware 범주를 `MinimumLevel.Override`로 끈다(BL-075). MigrationService Worker는 Infrastructure · ServiceDefaults만 참조하고 Write 연결만 쓰며, 실행 전략 안에서 `MigrateAsync`를 실행한다. 성공하면 종료 코드 0, 실패하면 0이 아닌 종료 코드와 Error 로그를 남긴다. 단위 테스트(헬스 경로 매핑, Serilog Override와 DI sink 수집, Worker 성공 · 실패 종료 코드)가 통과하고 빌드 경고 0이다. logging-observability 문서에 헬스체크 노출 환경과 응답 본문 방침을 반영했다 | T02 | todo | |
| S03-T04 | Employee Api와 아키텍처 테스트 Employee 편입 | FR-08, FR-07, FR-09, NFR-02, FR-11 | `ISender`만 쓰는 Controller가 `POST api/v1/employees`에 201 + Location + `{ id }`로, `GET api/v1/employees/{id}`(경로 제약 없음)에 200 또는 404 · 22001로 응답하고, 실패 Result는 BuildingBlocks.Api가 ProblemDetails로 바꾼다. Program은 `AddServiceDefaults` · `MapDefaultEndpoints`, `ConnectionStrings` Write / Read → 쓰기 / 읽기 DbContext 바인딩, `/health/ready`의 두 DbContext 검사, Development 한정 Swagger UI를 구성한다. `ArchitectureAssemblies.All`에 Employee 5개 레이어(Domain · Application · Infrastructure · Api · MigrationService)를 추가해 서비스 전용 규칙이 건너뜀 0으로 모두 통과한다(BL-085). src의 제품 프로젝트가 AppHost · ServiceDefaults를 빼고 모두 목록에 있는지 검사하는 안전장치 테스트와 ProductNames 비서비스 제외(BL-086)가 통과한다. 제품 코드 위반 3종을 커밋하지 않는 임시 변경으로 재현한 결과를 진행 기록에 남겼다. 05-api 직원 API 명세(요청 · 응답 · 에러 코드)를 작성했다 | T03 | todo | |
| S03-T05 | Aspire AppHost와 로컬 실행 증빙 | FR-03, FR-08, NFR-04, NFR-06 | AppHost는 다음을 구성한다: PostgreSQL(postgres:17, 이름 있는 볼륨 상수), 첫 실행 때 자동 생성 · 저장되는 secret 매개변수 2개, Database `emergency_hub_employee`(리소스 `employee-db`), `employee_app` 초기화 스크립트, `WithReference` 없이 조립한 Write / Read 연결(Read에만 `default_transaction_read_only=on`), `WaitFor` · `WaitForCompletion(migration)` · `WithHttpHealthCheck("/health/ready")`. 새 볼륨과 빈 user-secrets 상태에서 `dotnet run --project <AppHost>` 한 번으로 postgres → migration(종료 코드 0) → api(Healthy) 순서로 뜬다(NFR-04). 텍스트 증빙 최소 구성과 Edge 헤드리스 캡처로 HTTP 등록 → 조회, 대시보드 로그 · 트레이스를 기록했고, 캡처가 실패하면 수동 캡처 목록을 남긴다. 중지 → 재시작을 2회 해도 리소스 로그에 오류가 없고, psql로 DB 소유자 employee_app, public 소유자 pg_database_owner, 이력 테이블 1개를 확인했다(BL-014). 실제 Kestrel에 curl로 경로 파싱 오류와 본문 초과를 보냈을 때의 현행 동작(BL-083 H1 · H2 Kestrel 부분)과, 데코레이터 이벤트 101로 확인한 Scrutor Decorate 동작(TD-020)을 기록했다. 비밀 점검 명령 6개가 모두 0건이고, 증빙에 나온 토큰 · 비밀번호는 가렸다(NFR-06) | T04 | todo | |
| S03-T06 | 통합 테스트 기반과 영속성 · 스키마 실측 | FR-09, FR-05, FR-06, FR-08, NFR-07 | `EmergencyHub.Employee.IntegrationTests`의 컬렉션 fixture는 Testcontainers postgres:17(이미지 인자 명시, AppHost 태그와 대조, TD-004)에 AppHost 초기화 스크립트를 공유 마운트해 employee_app과 DB 소유자를 재현한다. 또 Write / Read(read-only) 연결, 운영 등록 코드의 `MigrateAsync`(`EnsureCreated` 금지), 이력 테이블을 보존하는 Respawn을 쓴다(BL-033). 장애 주입 도우미(인터셉터, fixture 안에서 만들고 지우는 테스트 전용 트리거, 재시도 한도 축소 등록)를 제공하고, testing-strategy에 그 방식을 적었다. 트랜잭션 · UoW 실측 P1~P8(BL-081, P9 제외)과, 스키마 · 연결 실측 S1, S2(DB 수준), S3, S4(smallint), S6, 마이그레이션 재적용 멱등(BL-082)이 통과한다. BL-023 · BL-073의 측정값과 결정을 진행 기록에 남겼고, 설정을 바꾸기로 했으면 이 작업에서 반영했다. CI(ubuntu, 기존 build-test 잡, 이미지 선 pull, 실패 시 컨테이너 로그 아티팩트)에서 통과하고 PR 워크플로가 10분 이내다(BL-057, NFR-07) | T05 | todo | |
| S03-T07 | HTTP · 로그 인수 통합 테스트 | FR-08, FR-07, FR-09, NFR-03 | WebApplicationFactory 호스트는 T06 fixture를 공유하고, 쓰기 DbContext 재등록 · TimeProvider 교체 · 로그 수집 sink 주입 도우미를 쓴다. 등록 → 조회 인수(201 + Location, GET 200에 정규화된 이메일, 없는 ID 404 · 22001)와 이메일 중복 409 · 23001이 통과한다. 1001(JSON 파싱, 형식 불일치, Guid 경로 바인딩 → 필드 키), 1002(0 · 99), 21006(누락), 9001(처리되지 않은 예외) 응답이 ProblemDetails 형식이고, traceId가 traceparent와 같다(BL-083 H1 TestServer 부분 · H3). 읽기 연결 쓰기 거부(25006)가 500 · 9001로 응답되고, 본문과 수집 로그에 SQL · 제약 이름 · 25006 · ExceptionHandlerMiddleware 원본 메시지가 없다(BL-082 S2 응답, BL-083 H4). CI에서 통과하고, 커버리지 보고에 Employee Domain · Application이 포함된다(NFR-03) | T04, T06 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S03-T01 | 해당 없음(생략. `employees` 확정안은 계획 리뷰 결과) | TDD | 표준 진입 점검. 아키텍처 규칙은 커밋하지 않는 임시 목록 확장으로 대상이 있는 규칙만 판정 | 단위 테스트 실행, 인수 조건 대조 |
| S03-T02 | 기준 SQL · 이름 상수 · 코드 표 입력, 생성 뒤 idempotent SQL 검토 · ERD | 매핑 · Repository 구현, 메타데이터 매핑 테스트 | 표준 진입 점검(아키텍처 규칙은 T01과 같은 방식) | 빌드 경고 0 · format · 매핑 테스트. 실제 적용은 T05 · T06 |
| S03-T03 | MigrationService의 Write 연결 · MigrateAsync 적용 방식이 ADR-0011 · 0012와 같은지 검토 | 구현, 단위 테스트 | 표준 진입 점검 | 단위 테스트 실행, 인수 조건 대조 |
| S03-T04 | 연결 문자열 키 · ready 검사가 ADR-0011 매핑과 같은지 확인 | TDD, Controller는 얇게, 아키텍처 목록 편입 | 표준 진입 점검(이 작업부터 아키텍처 규칙 자동 판정), 위반 재현 기록 확인 | 아키텍처 테스트(Skipped 0), 인수 조건 대조. HTTP 인수는 T07 |
| S03-T05 | DB 리소스 · 롤 스크립트 · 연결 식 작성 / 검토, psql 확인 항목 | AppHost 구현 | 비밀 점검 명령 실행, 설정 키 일치 | 볼륨 초기화 후 실행 기록, 재시작 2회, curl, Edge 캡처 · 마스킹 |
| S03-T06 | 테스트 DB 구성(롤, read-only 연결, Respawn 제외 테이블), 테스트 전용 트리거 · 검증 쿼리 검토 | fixture · 장애 주입 도우미 · CI 변경 | 표준 진입 점검 | 주 작성자: P · S 시나리오 작성 · 실행, 실패 시 원인별 반려 |
| S03-T07 | 해당 없음(생략. S2 대역 방식은 인계 메모) | WebApplicationFactory 도우미 | 표준 진입 점검 | 주 작성자: HTTP · 로그 시나리오 작성 · 실행 |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S03` 태그를 붙였다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| 2026-09-27 | S03-T01 | dba | 해당 없음 | 호출 생략(파이프라인 표 dba 열 '해당 없음'). employees 확정안은 계획 리뷰 결과 |
| 2026-09-27 | S03-T01 | developer | PASS | Employee Domain · Application TDD(Red 확인 후 구현), 테스트 144건 추가 · 전체 통과, Release 경고 0, format 통과. Deactivate()는 멱등(이벤트 없음). EmployeeStatus에 Unknown=0 예약 멤버(ck_는 IN (1, 2)). EmployeeEmail이 정규화 · 형식 판정을 한곳에서 담당. 아키텍처 임시 점검: 대상 0개 실패 3건 외 위반 0(커밋 안 함). handoff → T02 · T04 · T06 · T07. 후보 BL-089 |
| 2026-09-27 | S03-T01 | reviewer | PASS | 진입 점검 15항목 통과. 아키텍처 임시 확장은 scratchpad 복사본에서 실행(대상 0개 실패 3건 외 위반 0, 저장소 변경 없음). handoff → T02(ck_ IN (1, 2) 메타데이터 고정) · T04(대상 0개 3건 해소 확인) · T07(20001 → 102 순서) |
| 2026-09-27 | S03-T01 | tester | PASS | 인수 조건 대조 결과 빈 곳 없음(추가 0). 전체 1,188건(통과 1,177 · 건너뜀 11 · 실패 0), 경고 0. FR-08 · 09 · 11의 T01 범위 확인. handoff → T02(EF private 생성자 구체화) · T04(건너뜀 11 → 0) · T07(경계값 1~2건 파이프라인 확인) |
| 2026-09-27 | S03-T02 | dba | PASS | database.md 갱신: Employee 코드 정의 표(1 Active · 2 Inactive), Employee ERD · 인덱스 · 제약 표, 롤 이름 스키마 금지 규칙, 이력 테이블 PK · public 설명, 재시도 설정은 공통 옵션 구성 선택 인자(UseNpgsql 재호출 금지), UUID 정렬 검증 작업 번호 수정. developer 구현 사양 7개(이름 상수 EmployeeDbNames, EmployeeConfiguration, 모델 정의, Repository, 재시도 인자, 설계 시점 팩터리, 메타데이터 테스트) 전달. handoff → T02(dba 재확인 idempotent SQL 판정 a~g) · T05 · T06 |
| 2026-09-27 | S03-T02 | developer | PASS | Employee.Infrastructure(매핑 · Repository · 등록 확장 · 설계 시점 팩터리) · InitialCreate 생성, BuildingBlocks DbRetryOptions 재시도 인자 추가. 테스트 68건 추가 · 전체 1,251 통과, 경고 0(생성 코드 포함), format 통과. 이력 테이블 컬럼 · PK가 snake_case(migration_id · product_version · pk___ef_migrations_history)로 생성되어 ADR-0012(EF 기본 이름 유지)와 불일치 → 결정 요청. 후보 TD(Configuration.Abstractions 전이 참조). handoff → T03 · T04 · T06 |
| 2026-09-27 | S03-T02 | dba 재확인 | PASS | idempotent SQL 재생성 · 대조(a~g): (a) CREATE TABLE employees 기준 SQL과 일치 (b) ux_employees_email 1개 · ix_ 없음 (c) xmin · DEFAULT 없음, 따옴표 식별자는 "__EFMigrationsHistory"뿐 (d) 스키마 한정자 없음(BL-036) (e) 최장 28바이트(BL-071) (f) Flags 대상 없음(BL-088) (g) 스냅샷 · Designer xmin 동시성 토큰, CREATE에는 없음. 이력 테이블 실측: CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (migration_id character varying(150) NOT NULL, product_version character varying(32) NOT NULL, CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)). 원인 EFCore.NamingConventions 8.0.3(테이블 이름에는 미적용). 결정 A(오케스트레이션 세션): 예외는 테이블 이름만, database.md 수정, ADR-0012 48행 · ADR-0022 22행 불일치는 결과 리뷰 ADR 후보 'ADR-0012 이력 컬럼 조항 대체' |
| 2026-09-27 | S03-T02 | reviewer | REJECT → developer | 반려 1회. 아키텍처 임시 확장(복사본, Employee Domain · Application · Infrastructure)에서 대상이 있는 규칙 ClassesAreSealed가 생성 형식 InitialCreate(public partial, sealed 아님)로 실패. T04 편입 때 실패가 확정되고 RuleCheck 완화는 금지(결정 2). 조치 방식 (a) 직접 쓴 sealed partial 선언 추가 / (b) EF 생성 형식 예외 규칙화 중 메인 세션 판단: (a) 채택(기존 규칙 유지, 규칙 변경 없음). 진행 기록 테스트 수 정정: 전체 1,256 = 통과 1,245 · 건너뜀 11 · 실패 0. 후보 BL-090(ClassesAreSealed 가시성 범위 · 생성 형식 처리 기준) |
| 2026-09-27 | S03-T02 | developer | PASS | 재작업 1: InitialCreate · 스냅샷에 직접 작성한 sealed partial 선언(*.Sealed.cs) 추가, .editorconfig에서 *.Sealed.cs를 생성 코드 분류에서 제외, database.md · coding-conventions에 선언 규칙 문서화. 테스트 2건 추가(sealed 대상 개수 2 단언, sealed 마이그레이션 발견 · 생성). idempotent SQL sealed 전후 동일, 스냅샷 변경 없음. 복사본 아키텍처 점검 ClassesAreSealed 통과(대상 0개 실패 2건만). 전체 1,258 = 통과 1,247 · 건너뜀 11 · 실패 0, 경고 0, format 통과 |
| 2026-09-27 | S03-T02 | reviewer | PASS | 재검토: 1차 반려 사유 해소(복사본 아키텍처 점검 ClassesAreSealed · ImplementationsAreInternalSealed · EntityDerivedTypesAreSealed 통과, 남은 대상 0개 실패 2건은 T03 · T04 뒤 해소). 전체 1,258 = 통과 1,247 · 건너뜀 11 · 실패 0, -warnaserror 통과. BL-090 비고 보강(Migration · ModelSnapshot 파생 sealed 공통 규칙 검토) |
| 2026-09-27 | S03-T02 | tester | PASS | 인수 조건 대조 결과 빈 곳 없음(추가 0). -warnaserror 경고 0, format 통과, has-pending-model-changes 변경 없음, migrations list InitialCreate 1건, EF Design은 Infrastructure에만(BL-046). 전체 1,258 = 통과 1,247 · 건너뜀 11 · 실패 0. FR-08 · 06 · 02 · 11 · NFR-01의 T02 범위 확인. handoff → T06(실제 DB 왕복 · e.Id.Value 번역, ck_ · ux_ 실제 이름 = 상수) |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 때 남긴 메모 (계획 리뷰에서 확정):

- 샘플 `employees` 테이블의 세부 구성(PRD Q16)은 아래 "employees 확정안"으로 확정했다.
- S02에서 이관한 DB 동작 검증 항목은 T05 · T06 · T07에 나눠 닫는다. P9 · Flags · owned는 제외한다(아래 편입 표).

### 사전 사용자 결정 (스프린트 시작 전)

- 스프린트 종료 때 push → 토픽 PR CI 통과 확인 → 태그 `sprint/S03`까지 확인 없이 진행한다.
- 실행 증빙은 Edge 헤드리스 캡처와 텍스트로 남긴다. 캡처가 실패하면 텍스트로 대체하고 수동 캡처 목록만 남긴다(진행을 막지 않음).
- 이메일은 도메인에서 Trim + 소문자(Invariant) 정규화해 저장하고, 일반 컬럼에 `ux_` 유니크 인덱스를 둔다(citext · `lower()` 식 인덱스는 쓰지 않음).
- 커밋 작성자 이메일은 현행대로 유지한다.

### 에이전트 리뷰 요약

| 에이전트 | 핵심 지적 |
|---|---|
| dba | DbSet 없이 `Set<T>()`를 쓰면 테이블이 `employee`(단수)가 되므로 `ToTable` 상수 필요. 속성 이름이 `Status`면 컬럼 이름이 어긋남. 순차 HTTP 중복 요청은 23505 경로를 지나지 않음. 결정적 장애 주입(테스트 전용 트리거) 필요. `employees` 확정안 · 기준 SQL 제시 |
| developer | RuleCheck는 서비스 어셈블리가 하나라도 있으면 대상 0개 규칙이 실패하므로 5개 레이어를 한 번에 편입해야 함. ServiceDefaults · MigrationService를 Api 앞으로. `{id:guid}` 제약을 두면 1001을 검증할 수 없음. 7개 작업 재구성안 제시 |
| reviewer | T04 · T05 완료 조건이 규칙 위반(항목 15개 · 25개 이상). 비밀 값 판정 명령이 정해지지 않았고 대시보드 · 로그인 토큰으로 새는 경로가 있음. NFR-04와 ADR-0011을 함께 충족하는 조건이 불분명. 텍스트 증빙 최소 구성 제시 |
| tester | WebApplicationFactory(TestServer)로는 Kestrel H1 · H2를 재현할 수 없음. Serilog 경로 로그 수집 지점(`ReadFrom.Services`) 필요. 장애 주입 인터셉터 · 재시도 한도 축소 제안. fixture와 AppHost SQL이 갈라질 위험. Flags · owned는 검증 대상 없음 |

### 결정 (사용자 승인 2026-09-27, 오케스트레이션 세션 경유)

- **작업 재구성**: 5개 → 7개(위 작업 표). 통합 테스트는 영속성 · 스키마(T06)와 HTTP · 로그(T07)로 나눈다. T06 반려가 2회에 이르면 T06a(기반 · CI) / T06b(영속성 · 스키마 실측) 분할안을 오케스트레이션 세션에 올려 판단을 받는다.
- **BL-085**: T04에서 Employee 5개 레이어를 한 번에 편입하고 안전장치 테스트를 둔다. RuleCheck 완화는 하지 않는다. T01~T03 reviewer는 로컬에서만 목록을 임시로 늘려 대상이 있는 규칙만 판정한다(커밋 금지).
- **비밀번호 공급**: `GenerateParameterDefault` + `persist`로 첫 실행 때 비밀번호를 만들어 AppHost user-secrets에 저장한다. NFR-04 · NFR-06 · ADR-0011을 모두 충족하므로 새 ADR은 두지 않는다. 9.5.2에 해당 API가 없으면 대시보드 입력으로 대체하고 진행 기록에 남긴다.
- **입력 · API**: `employeeStatus`는 nullable이다(누락은 21006, 0 · 99는 1002). Email은 팩토리에서 정규화한 string 속성으로 둔다(값 객체 아님). `GET api/v1/employees/{id}`에는 경로 제약을 두지 않는다. 등록은 201 + Location + `{ id }`로 응답한다. Aggregate 불변식 위반은 예외로 처리한다. 동시성 재현용 `Deactivate()` 1개를 둔다(Command · API 없음).
- **코드**: 21001 displayName 필수, 21002 displayName 길이, 21003 email 필수, 21004 email 형식, 21005 email 길이, 21006 employeeStatus 필수, 22001 직원 없음, 23001 이메일 중복, 로그 이벤트 20001 EmployeeRegistered(EmployeeId만 기록).
- **헬스체크**: `/health/live`(검사 없음)와 `/health/ready`(두 DbContext)를 모든 환경에 매핑하고, 응답 본문에는 상태만 담는다.
- **통합 테스트 프로젝트**: `tests/Services/Employee/EmergencyHub.Employee.IntegrationTests` 하나만 두고, T06 · T07이 컬렉션 fixture 1개를 공유한다.
- **BL-073 기준**: Api는 재시도 총 대기가 요청 제한 시간보다 짧아야 한다(값은 실측 뒤 확정). MigrationService는 기본값을 쓴다. 재시도 설정은 등록 확장의 인자로 받는다(T02에서 인자 자리를 만들고 T06에서 값을 정함).
- **BL-023 기준**: 23505 한 건당 EF Error 로그 건수와 이메일 값 노출을 실측한다. 경합 경로에서 Error가 남거나 이메일이 노출되면 같은 작업에서 조정하고, 그렇지 않으면 유지 결정을 기록한다.
- ADR 후보는 없다. 장애 주입 방식은 testing-strategy(T06)에, 헬스체크 노출 환경은 logging-observability(T03)에 적는다.

### employees 확정안 (PRD Q16)

| 컬럼 | 타입 | NULL | 기본값 | 제약 / 비고 |
|---|---|---|---|---|
| id | uuid | NOT NULL | 없음 | `pk_employees`, ValueGeneratedNever, Handler가 `IIdGenerator`로 생성 |
| display_name | varchar(100) | NOT NULL | 없음 | Domain에서 Trim, 빈 값 거부 |
| email | varchar(254) | NOT NULL | 없음 | Trim + ToLowerInvariant 정규화 값, `ux_employees_email` |
| employee_status | smallint | NOT NULL | 없음 | Active=1, Inactive=2, `ck_employees_employee_status` (`IN (1, 2)`) |
| created_at / updated_at | timestamptz | NOT NULL | 없음 | 공통 shadow property, 감사 인터셉터(UTC) |
| xmin | 시스템 | - | - | 공통 동시성 토큰, CREATE TABLE에 나오지 않음 |

인덱스는 `pk_employees`와 `ux_employees_email` 두 개다. `CHECK (email = lower(email))`는 두지 않는다. DB collation과 .NET Invariant 소문자 변환 결과가 다를 수 있기 때문이다.

### 백로그 / 기술부채 편입

| 항목 | 처리 |
|---|---|
| BL-036 · BL-045 · BL-046 · BL-084 | S03-T02 |
| BL-030 · BL-075 | S03-T03 |
| BL-085 · BL-086 | S03-T04 |
| BL-014 · TD-020 · BL-083(H1 Kestrel 부분 · H2) | S03-T05 |
| TD-004 · BL-033 · BL-057 · BL-081(P1~P8) · BL-082(S1 · S2 DB 수준 · S3 · S4 smallint · S6) · BL-023 · BL-073 | S03-T06 |
| BL-083(H1 TestServer 부분 · H3 · H4) · BL-082(S2 응답 수준) | S03-T07 |
| BL-065 | open으로 되돌림: S03에는 도메인 서비스가 없다. 재검토 트리거는 첫 도메인 서비스, 우선안은 `IService`를 BuildingBlocks.Domain으로 옮기는 것 |
| BL-081 P9 | 제외하고 TD-010은 open 유지(TransactionCommitted 인터셉터 재현법을 상환 계획에 기록) |
| BL-082 S5 · S4 Flags · BL-081 P5 owned | BL-088로 이관(Employee에 대상 없음, 재검토 트리거: 제품 첫 `[Flags]` · owned) |

세부 단언(P1~P8, S1~S6, H1~H4, BL-084 SQL 항목, 증빙 절차, 비밀 점검 명령)은 작업별 인계 메모로 넘긴다.

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
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-001 분할) |
| 2026-09-27 | orchestrator | 계획 리뷰 반영: 작업 7개로 재구성, employees 확정안, 백로그 / 기술부채 편입, `active` |
