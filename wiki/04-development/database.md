---
title: "데이터베이스 (PostgreSQL)"
type: doc
status: draft
tags: [development]
created: 2026-09-27
updated: 2026-09-27
---

# 데이터베이스 (PostgreSQL)

> PostgreSQL 설계 규칙과 EF Core(Npgsql) 사용 가이드입니다. dba 에이전트가 작업 기준으로, developer 에이전트가 진입 점검 기준으로 사용합니다.
> 결정 근거: [ADR-0005 PostgreSQL](../03-architecture/adr/0005-use-postgresql.md), [ADR-0002 MSA](../03-architecture/adr/0002-adopt-msa.md), [ADR-0009 읽기 / 쓰기 분리](../03-architecture/adr/0009-separate-read-write-db-context.md), [ADR-0011 Aspire 로컬 오케스트레이션](../03-architecture/adr/0011-use-aspire-local-orchestration.md), [ADR-0012 마이그레이션 적용 · 리셋](../03-architecture/adr/0012-migration-apply-and-pre-production-reset.md), [ADR-0013 UUID v7](../03-architecture/adr/0013-uuid-v7-with-uuidnext.md), [ADR-0014 트랜잭션 경계 · UoW](../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md), [ADR-0022 Respawn](../03-architecture/adr/0022-respawn-and-coverage-tooling.md)
>
> [위키 홈](../README.md)

## Database per Service 원칙

- 서비스마다 **자기 Database**를 갖는다: `emergency_hub_<service>` (예: `emergency_hub_employee`). 로컬에서는 PostgreSQL 인스턴스 하나에 Database를 나눠 둔다.
- 서비스는 **다른 서비스의 Database에 접근하지 않는다**(조인, 조회 모두 금지). 필요한 데이터는 API나 통합 이벤트로 받아 자기 DB에 복제한다.
- 서비스별 DB 계정을 따로 두고, 계정은 자기 Database에만 권한을 갖는다([ADR-0011](../03-architecture/adr/0011-use-aspire-local-orchestration.md) 롤 모델).
  - 앱 롤은 `<service>_app`(예: `employee_app`)이다. `LOGIN` 롤이고 자기 DB의 **소유자**이며 `CREATEDB` · `SUPERUSER`가 없다. MigrationService와 Api가 모두 이 롤로 접속한다.
  - 슈퍼유저(`postgres`)는 서버 초기화와 DB 생성 스크립트에만 쓰고 애플리케이션에 주입하지 않는다(Aspire `WithReference(db)` 금지: 슈퍼유저 자격 증명이 주입된다).
  - PostgreSQL 15+에서 `public` 스키마 소유자는 `pg_database_owner`이므로 DB 소유자인 앱 롤은 별도 `GRANT` 없이 `public`에 객체를 만든다.
  - 마이그레이션 전용 롤 분리는 TD-001, 서비스 DB가 2개 이상이 되면 `REVOKE CONNECT, TEMPORARY ON DATABASE ... FROM PUBLIC`을 적용한다(BL-015).
- 테이블은 기본 스키마 `public`을 쓴다.

## 읽기 / 쓰기 연결 분리

**읽기 전용 DB(복제본)는 지금 두지 않지만, 설정과 코드에서는 처음부터 읽기와 쓰기를 나눕니다.** 나중에 복제본을 붙일 때 연결 문자열만 바꾸면 되도록 하기 위해서입니다.

| 구분 | 쓰기 (Command) | 읽기 (Query) |
|---|---|---|
| 연결 문자열 키 | `ConnectionStrings:Write` | `ConnectionStrings:Read` |
| 현재 대상 | 서비스 Database | **같은 서비스 Database** (복제본 도입 시 교체) |
| DbContext | `<Service>DbContext` | `<Service>ReadDbContext` |
| 추적 | 기본(변경 추적) | `QueryTrackingBehavior.NoTracking` 기본값 |
| 사용처 | Write Repository, Unit of Work, Outbox | Read Repository |
| 마이그레이션 | **여기서만** 생성 · 적용 | 만들지 않음 |

```json
// appsettings.json (서비스별)
{
  "ConnectionStrings": {
    "Write": "Host=localhost;Database=emergency_hub_employee;Username=employee_app",
    "Read":  "Host=localhost;Database=emergency_hub_employee;Username=employee_app;Options=-c default_transaction_read_only=on"
  }
}
```

- 예시에 `Password`가 없는 이유: 비밀번호는 커밋하지 않고 user-secrets · AppHost 매개변수로 주입한다(NFR-06). 연결 문자열에 들어가므로 `;`를 쓰지 않는다. 로컬에서는 AppHost가 `ConnectionStrings__Write` / `ConnectionStrings__Read`를 `ReferenceExpression`으로 조립해 환경 변수로 넣는다(Api에는 둘 다, MigrationService에는 Write만, [ADR-0011](../03-architecture/adr/0011-use-aspire-local-orchestration.md)).
- 읽기 연결은 `default_transaction_read_only=on`으로 열어, 읽기 DbContext로 실수로 쓰기를 하면 DB가 거부하도록 한다(`25006`). 이것은 실수를 막는 **안전장치이지 보안 경계가 아니다**(세션에서 `SET`으로 풀 수 있다). 복제본을 도입하면 읽기 전용 계정으로 바꾼다.
- 연결 문자열에 `Include Error Detail=true` · `Persist Security Info=true`를 쓰지 않는다([ADR-0020](../03-architecture/adr/0020-logging-with-serilog-and-otlp.md)).
- 두 DbContext는 같은 엔티티 매핑(`IEntityTypeConfiguration<T>`)을 공유한다(`ApplyConfigurationsFromAssembly`).
- 읽기 DbContext에는 `SaveChanges`를 쓰지 않는다. BuildingBlocks의 읽기 전용 기반 클래스가 `SaveChanges` 호출 시 예외를 던지게 한다.
- 복제 지연이 생기면 "쓰고 바로 읽기"가 틀릴 수 있다. Command 직후 결과가 필요하면 Command가 필요한 값(ID 등)을 반환하게 한다.

## 테이블 / 컬럼 네이밍 규칙 (snake_case)

모든 식별자는 **소문자 snake_case**로 합니다. 따옴표가 필요한 이름은 만들지 않습니다(예외: `__EFMigrationsHistory`, [마이그레이션 규칙](#마이그레이션-규칙) 참고). EF Core에서는 `EFCore.NamingConventions`의 `UseSnakeCaseNamingConvention()`으로 자동 변환합니다.

| 대상 | 규칙 | 예 |
|---|---|---|
| 테이블 | 복수형 | `employees`, `contact_groups` |
| 기본 키 컬럼 | `id` | `employees.id` |
| 외래 키 컬럼 | 단수 대상 + `_id` | `employee_id` |
| 일반 컬럼 | 의미 단위 snake_case | `display_name`, `hired_on` |
| 불리언 | `is_` / `has_` 접두사 | `is_primary`, `has_consent` |
| 시각 | `_at` 접미사 (timestamptz) | `created_at`, `acknowledged_at` |
| 날짜 | `_on` 접미사 (date) | `hired_on` |
| 코드 | 대상 + `_status` / `_type` / `_code` (정수) | `employee_status`, `contact_type` |
| 비트 플래그 | 복수형 명사 (정수) | `permissions`, `notification_channels` |
| 기본 키 제약 | `pk_<table>` | `pk_employees` |
| 외래 키 제약 | `fk_<table>_<ref_table>_<column>` | `fk_contacts_employees_employee_id` |
| 인덱스 | `ix_<table>_<columns>` | `ix_employees_department_id` |
| 유니크 인덱스 | `ux_<table>_<columns>` | `ux_employees_email` |
| 체크 제약 | `ck_<table>_<rule>` | `ck_employees_employee_status` |

## 데이터 타입 규칙

| 용도 | 타입 | 규칙 |
|---|---|---|
| 기본 키 | `uuid` | **UUID v7**(시간 순서 정렬)을 애플리케이션에서 생성한다. 인덱스 단편화가 적고 서비스 간에 충돌하지 않는다([ADR-0013](../03-architecture/adr/0013-uuid-v7-with-uuidnext.md)). |
| 코드 | `smallint` | [코드값 규칙](#코드값-규칙) |
| 비트 플래그 | `integer` / `bigint` | [비트 마스킹 규칙](#비트-마스킹-규칙) |
| 문자열 | `text` | 길이 제한이 도메인 규칙이면 `varchar(n)` 또는 체크 제약. `char(n)` 금지 |
| 시각 | `timestamptz` | **항상 UTC로 저장**한다. `timestamp`(time zone 없음) 금지 |
| 날짜 | `date` | 시각 정보가 없는 날짜 |
| 금액 / 정밀 수치 | `numeric(p, s)` | `money`, `float` 금지 |
| 반정형 데이터 | `jsonb` | Outbox payload, 외부 응답 원문 등. 조회 조건으로 자주 쓰면 컬럼으로 뺀다 |
| 불리언 | `boolean` | NULL 허용하지 않는 것을 기본으로 한다 |

UUID v7 생성([ADR-0013](../03-architecture/adr/0013-uuid-v7-with-uuidnext.md)):

- `IIdGenerator`(BuildingBlocks.Application)의 구현(BuildingBlocks.Infrastructure)이 UUIDNext의 `Uuid.NewDatabaseFriendly(Database.PostgreSql)`로 만든다(.NET 8에는 `Guid.CreateVersion7()`이 없다).
- **Handler가 생성**해 Aggregate 팩토리에 넘긴다. EF 매핑은 `ValueGeneratedNever`다.
- DB 기본값(`gen_random_uuid()`는 v4, `uuidv7()`는 PostgreSQL 18부터)은 쓰지 않는다.
- PostgreSQL `uuid` 정렬 = 생성 순서다(Npgsql이 RFC 바이트 순서로 기록, S03-T05 통합 테스트로 검증). 메모리 안 정렬 검증은 `Guid.CompareTo`가 아니라 문자열 기준으로 한다.

- 컬럼은 기본으로 `NOT NULL`이다. NULL은 "값이 없음"이 업무적으로 의미 있을 때만 허용한다.
- 모든 테이블에 `created_at`, `updated_at`(timestamptz, NOT NULL)을 둔다. 값은 애플리케이션(`TimeProvider`)이 채운다.
- 삭제는 기본적으로 물리 삭제한다. 이력이 필요한 테이블만 `deleted_at`(soft delete)을 두고, 그 테이블은 쿼리 필터를 건다.

## 코드값 규칙

> **코드값을 문자열로 저장하지 않는다.** `varchar` 코드(`'ACTIVE'`), PostgreSQL `enum` 타입 모두 금지합니다.

- 코드는 C# `enum`(기반 형식 `short`)으로 정의하고 **`smallint` 컬럼**에 정수로 저장한다. C# 규칙은 [코딩 컨벤션 · 코드값](coding-conventions.md#코드값-enum-규칙)을 따른다.
- **C# enum이 코드 정의의 원본**이다. DB에는 코드 테이블을 두지 않고, 코드 값과 의미는 서비스별 [코드 정의](#코드-정의) 표에 기록한다.
- `0`은 예약 값(`Unknown` / `None`)이다. 저장 값으로 쓰지 않는다.
- 배포된 값은 바꾸거나 재사용하지 않는다. 폐기한 값은 코드 정의 표에 `폐기`로 남긴다.
- 허용 값은 체크 제약으로 막는다: `ck_employees_employee_status CHECK (employee_status IN (1, 2, 3))`. 값을 추가하면 마이그레이션으로 제약을 함께 바꾼다.
- EF Core는 enum을 기반 정수 형식으로 저장하므로 별도 변환이 필요 없다. `HasConversion<string>()`은 쓰지 않는다.

## 비트 마스킹 규칙

권한, 알림 채널처럼 **여러 코드를 조합**해야 하면 C# `[Flags]` enum을 정수 컬럼 하나에 저장합니다.

| 플래그 수 | C# 기반 형식 | 컬럼 타입 |
|---|---|---|
| 31개 이하 | `int` | `integer` |
| 63개 이하 | `long` | `bigint` |

- 각 플래그는 `1 << n` 값 하나를 차지한다. **비트 자리는 재사용하지 않는다.**
- `0`은 "없음"이다. 조합 별칭(`All`)은 코드에서만 정의하고 저장 값으로 따로 두지 않는다.
- 정의된 비트 밖의 값은 체크 제약으로 막는다: `ck_employees_notification_channels CHECK (notification_channels >= 0 AND notification_channels <= 7)`
- 조회는 비트 연산으로 한다.

```sql
-- 특정 플래그 포함 (SMS = 1)
SELECT id FROM employees WHERE notification_channels & 1 = 1;
-- 여러 플래그 모두 포함 (SMS | EMAIL = 5)
SELECT id FROM employees WHERE notification_channels & 5 = 5;
-- 여러 플래그 중 하나라도 포함
SELECT id FROM employees WHERE notification_channels & 5 <> 0;
```

```csharp
// EF Core: 비트 연산식은 SQL로 번역된다
var smsEnabled = await db.Employees
    .Where(e => (e.NotificationChannels & NotificationChannels.Sms) != 0)
    .ToListAsync(cancellationToken);
```

- 비트 연산 조건은 일반 B-tree 인덱스를 쓰지 못한다. 큰 테이블에서 특정 플래그로 자주 거르면 부분 인덱스(`WHERE notification_channels & 1 = 1`)를 만들거나, 조합이 아닌 별도 테이블로 설계를 바꾼다.

## 코드 정의

서비스별 코드 값과 의미를 기록합니다. 코드를 추가 / 변경하는 작업은 이 표를 함께 갱신합니다.

> TODO: 서비스 코드가 생기면 서비스별로 표를 추가합니다.

| 서비스 | enum (컬럼) | 값 | 이름 | 의미 | 상태 |
|---|---|---|---|---|---|
| (예) Employee | `EmployeeStatus` (`employee_status`) | 1 | `Active` | 재직 | 사용 |

## EF Core 구성 (Npgsql)

- Provider: `Npgsql.EntityFrameworkCore.PostgreSQL`, 명명 규칙: `EFCore.NamingConventions`(`UseSnakeCaseNamingConvention()`)
- 등록: BuildingBlocks.Infrastructure의 공용 확장 메서드가 쓰기 · 읽기 DbContext를 `AddDbContext` + `UseNpgsql(연결, o => o.EnableRetryOnFailure())` + `UseSnakeCaseNamingConvention()`으로 등록한다. 두 DbContext는 같은 실행 전략을 쓰고, Api · MigrationService · 통합 테스트가 같은 등록 코드를 쓴다. 추적은 Npgsql.OpenTelemetry `AddNpgsql()`, 헬스체크는 `AddDbContextCheck<TDbContext>()`로 붙인다([ADR-0011](../03-architecture/adr/0011-use-aspire-local-orchestration.md)).
- 매핑은 Infrastructure의 `Persistence/Configurations/`에 **엔티티마다 `IEntityTypeConfiguration<T>` 하나**로 둔다. Domain에 데이터 어노테이션을 쓰지 않는다.
- 강타입 ID는 값 변환기(`HasConversion`)로 `uuid`에 매핑한다.
- Value Object는 Owned Type 또는 Complex Type(EF Core 8)으로 매핑한다.
- Lazy Loading은 쓰지 않는다. 필요한 연관은 `Include`로 명시한다.
- 데이터 접근은 **Repository**로만 한다. Repository에는 람다식 LINQ 쿼리만 두고 분기 · 로직을 넣지 않는다([코딩 컨벤션 · Repository 규칙](coding-conventions.md#repository-규칙-ef-core)).
- **Query(CQRS)는 Read Repository가 읽기 DbContext에서 `Select` 프로젝션**으로 응답 `record`를 바로 만든다. 엔티티 전체를 불러와 변환하지 않는다.
- Command는 Write Repository로 Aggregate를 불러온다. **Handler · Repository는 `SaveChanges`를 부르지 않는다**([ADR-0014](../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md)).
  - 트랜잭션 데코레이터(Command 전용)가 Handler 성공 뒤 `IUnitOfWork.CommitAsync`를 부른다. Handler는 실행 전략 밖에서 한 번만 실행된다.
  - UnitOfWork(Infrastructure)가 실행 전략 안에서 `BeginTransactionAsync(IsolationLevel.ReadCommitted)` → `SaveChangesAsync(acceptAllChangesOnSuccess: false)` → 커밋을 하고, 커밋이 성공한 뒤 전략 밖에서 `ChangeTracker.AcceptAllChanges()`를 부른다. 재시도 범위는 SaveChanges · 커밋뿐이다.
- DbContext는 `AddDbContext`로 `Scoped` 등록한다. `AddDbContextPool`과 Aspire 클라이언트 통합(`AddNpgsqlDbContext`)은 쓰지 않는다(풀 강제, DI의 `SaveChangesInterceptor`를 붙일 수 없음, [ADR-0011](../03-architecture/adr/0011-use-aspire-local-orchestration.md)).
- 연결 문자열은 설정 / 시크릿으로 주입한다([설정 & 시크릿 관리](../06-deployment/configuration.md)).

## 마이그레이션 규칙

- 서비스마다 EF Core 마이그레이션을 따로 둔다(Infrastructure 프로젝트의 `Persistence/Migrations/`).
- 마이그레이션 이름은 PascalCase로 변경 의도를 적는다: `AddEmployeeNotificationChannels`
- **이미 적용(push)된 마이그레이션은 고치지 않는다.** 수정은 새 마이그레이션으로 한다. 운영 전 리셋(아래)은 전체를 `InitialCreate` 하나로 다시 만드는 예외이며, 리셋 기간에도 개별 마이그레이션의 부분 수정은 금지한다. push 전 토픽 브랜치 안에만 있는 공유되지 않은 마이그레이션을 다시 만드는 것은 이 규칙의 대상이 아니다.
- 생성한 SQL을 확인한다: `dotnet ef migrations script --idempotent`. dba는 작업마다 SQL을 검토한다.
- 파괴적 변경(컬럼 삭제 / 이름 변경 / 타입 변경)은 **확장 → 이전 → 축소** 2단계 이상으로 나눈다.
- 운영 환경에서는 애플리케이션 시작 시 자동 적용하지 않는다(마이그레이션 번들 / 스크립트로 적용).
- **로컬 적용**: MigrationService 하나가 Write 연결로 실행 전략 안에서 `MigrateAsync`만 하고 종료한다(실패 시 0이 아닌 종료 코드, Api는 `WaitForCompletion`으로 완료를 기다림). **Api는 시작할 때 마이그레이션하지 않는다.** DB 생성은 AppHost가 하고 `EnsureCreated`는 금지한다. EF Core 8 `MigrateAsync`에는 잠금이 없으므로 적용 주체는 하나다(TD-011, [ADR-0012](../03-architecture/adr/0012-migration-apply-and-pre-production-reset.md)).
- **설계 시점 팩터리**: `IDesignTimeDbContextFactory`는 쓰기 DbContext만 만들고, 연결 문자열은 환경 변수 또는 더미 값을 쓴다(비밀 없음). 한 어셈블리에 DbContext가 2개라 `dotnet ef`에는 `--context <Service>DbContext`가 필수다. `Migrations/**`는 생성 코드(`generated_code`)로 분석에서 뺀다.
- **`__EFMigrationsHistory`**: snake_case 규칙의 유일한 예외로 EF 기본 이름을 유지한다. 컬럼 `"MigrationId"` · `"ProductVersion"`은 SQL에서 따옴표가 필요하다. 통합 테스트 Respawn 초기화 대상에서 제외한다([ADR-0022](../03-architecture/adr/0022-respawn-and-coverage-tooling.md)).
- **운영 전 리셋 정책**: 운영 배포(Phase 4) 전까지(그보다 먼저 로컬 밖 지속 공유 DB가 생기면 그때까지) 마이그레이션 전체 리셋을 허용한다. 절차 ①~⑤와 기록 방법(리셋만 담은 커밋, 스프린트 기록, 이 문서 변경 이력 한 줄)은 [ADR-0012](../03-architecture/adr/0012-migration-apply-and-pre-production-reset.md)를 따른다.

## 트랜잭션 & 동시성 제어

- **Command 하나 = 트랜잭션 하나 = Aggregate 하나.** 여러 Aggregate를 바꿔야 하면 도메인 이벤트로 나눈다.
- 격리 수준은 **Read Committed를 명시한다**(UnitOfWork가 `BeginTransactionAsync(IsolationLevel.ReadCommitted)`, 서버 기본값에 기대지 않음, ADR-0014). 더 높은 수준이 필요하면 이유를 작업 문서에 남긴다.
- 동시성은 **낙관적 잠금**으로 제어한다. PostgreSQL 시스템 컬럼 `xmin`을 동시성 토큰으로 매핑한다(`uint Version` 속성 + `IsRowVersion()`).
- 영속성 예외의 `Result` 변환은 Infrastructure(UnitOfWork)에서 한다([ADR-0014](../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md)). Application은 EF · Npgsql 형식을 참조하지 않는다.
  - `DbUpdateConcurrencyException` → `Common.ConcurrencyConflict`([에러 코드](../05-api/error-codes.md))
  - `23505`(유니크 위반) → `ConstraintName`으로 서비스별 매핑(예: `ux_employees_email` → 이메일 중복). 매핑이 없으면 공통 Conflict(BL-019)
  - `23514`(체크 제약 위반)는 변환하지 않는다(프로그래밍 오류 → 전역 예외 처리)
  - 변환 로그 · 메시지에는 제약 이름만 남기고 값은 남기지 않는다. 커밋 결과를 모르는 상태의 재시도 오판 한계는 TD-010

## Outbox 테이블

> **도입 보류**([ADR-0023](../03-architecture/adr/0023-deferred-adoptions.md)). 지금은 테이블과 처리기를 만들지 않는다. 확장 지점은 UnitOfWork의 커밋 전 · 같은 트랜잭션이다([ADR-0014](../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md)). ADR-0004는 유지하며, 아래 표는 도입할 때의 설계안이다.

- 통합 이벤트는 비즈니스 데이터와 **같은 트랜잭션**으로 `outbox_messages`에 저장하고, 별도 처리기가 브로커로 발행한다([ADR-0004](../03-architecture/adr/0004-adopt-event-driven-architecture.md)).
- 소비 측은 멱등 처리를 위해 `inbox_messages`(처리한 메시지 ID)를 둔다.

| 컬럼 | 타입 | 설명 |
|---|---|---|
| `id` | `uuid` | 메시지 ID (UUID v7) |
| `message_type` | `text` | 이벤트 타입 이름 (코드값이 아니라 역직렬화용 타입 식별자) |
| `payload` | `jsonb` | 이벤트 본문 |
| `occurred_at` | `timestamptz` | 발생 시각 |
| `processed_at` | `timestamptz` NULL | 발행 완료 시각 |
| `retry_count` | `integer` | 발행 재시도 횟수 |
| `last_error` | `text` NULL | 마지막 오류 |

- 브로커 도입을 재검토할 때 이 설계안을 MassTransit EF Core Outbox로 대체할지 함께 결정한다(ADR-0023 재검토 시점).

## 시드 데이터

- 코드값은 C# enum이 원본이므로 DB 시드 대상이 아니다.
- 운영에 필요한 기준 데이터는 마이그레이션(`HasData`)으로 넣는다. `HasData` 기준 데이터 테이블은 통합 테스트 Respawn의 `TablesToIgnore`에 추가한다(또는 초기화 뒤 재시드, [ADR-0022](../03-architecture/adr/0022-respawn-and-coverage-tooling.md)).
- 통합 테스트 DB 규칙(`employee_app` 재현, `postgres:17`, 쓰기 연결로 Respawn)은 [테스트 전략 · 통합 테스트](testing-strategy.md#통합-테스트-testcontainers)에 있다.
- 개발 / 테스트용 샘플 데이터는 마이그레이션에 넣지 않고 별도 시더(개발 환경 전용)로 넣는다.

## 서비스별 ERD

> TODO: 서비스 스키마가 생기면 서비스별 Mermaid `erDiagram`을 추가합니다.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 기본 규칙 초안: 네이밍, 타입(UUID v7, timestamptz), 정수 코드값(문자열 금지), 비트 마스킹, EF Core, 마이그레이션, 동시성(xmin), Outbox |
| 2026-09-27 | - | 읽기 / 쓰기 연결 분리(연결 문자열 · DbContext 분리, 현재는 같은 DB), Repository 경유 원칙 추가 |
| 2026-09-27 | developer | ADR 0011~0014 · 0022 · 0023 반영: 롤 모델, UUID v7 확정, 마이그레이션 적용 · 리셋 · 이력 테이블 예외, 트랜잭션 격리 명시, EF 등록, 예외 변환, Outbox 보류 (S01-T04) |
