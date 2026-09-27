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
> 결정 근거: [ADR-0005 PostgreSQL](../03-architecture/adr/0005-use-postgresql.md), [ADR-0002 MSA](../03-architecture/adr/0002-adopt-msa.md)
>
> [위키 홈](../README.md)

## Database per Service 원칙

- 서비스마다 **자기 Database**를 갖는다: `emergency_hub_<service>` (예: `emergency_hub_employee`). 로컬에서는 PostgreSQL 인스턴스 하나에 Database를 나눠 둔다.
- 서비스는 **다른 서비스의 Database에 접근하지 않는다**(조인, 조회 모두 금지). 필요한 데이터는 API나 통합 이벤트로 받아 자기 DB에 복제한다.
- 서비스별 DB 계정을 따로 두고, 계정은 자기 Database에만 권한을 갖는다.
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

- 읽기 연결은 `default_transaction_read_only=on`으로 열어, 읽기 DbContext로 실수로 쓰기를 하면 DB가 거부하도록 한다. 복제본을 도입하면 읽기 전용 계정으로 바꾼다.
- 두 DbContext는 같은 엔티티 매핑(`IEntityTypeConfiguration<T>`)을 공유한다(`ApplyConfigurationsFromAssembly`).
- 읽기 DbContext에는 `SaveChanges`를 쓰지 않는다. BuildingBlocks의 읽기 전용 기반 클래스가 `SaveChanges` 호출 시 예외를 던지게 한다.
- 복제 지연이 생기면 "쓰고 바로 읽기"가 틀릴 수 있다. Command 직후 결과가 필요하면 Command가 필요한 값(ID 등)을 반환하게 한다.

## 테이블 / 컬럼 네이밍 규칙 (snake_case)

모든 식별자는 **소문자 snake_case**로 합니다. 따옴표가 필요한 이름은 만들지 않습니다. EF Core에서는 `EFCore.NamingConventions`의 `UseSnakeCaseNamingConvention()`으로 자동 변환합니다.

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
| 기본 키 | `uuid` | **UUID v7**(시간 순서 정렬)을 애플리케이션에서 생성한다. 인덱스 단편화가 적고 서비스 간에 충돌하지 않는다. |
| 코드 | `smallint` | [코드값 규칙](#코드값-규칙) |
| 비트 플래그 | `integer` / `bigint` | [비트 마스킹 규칙](#비트-마스킹-규칙) |
| 문자열 | `text` | 길이 제한이 도메인 규칙이면 `varchar(n)` 또는 체크 제약. `char(n)` 금지 |
| 시각 | `timestamptz` | **항상 UTC로 저장**한다. `timestamp`(time zone 없음) 금지 |
| 날짜 | `date` | 시각 정보가 없는 날짜 |
| 금액 / 정밀 수치 | `numeric(p, s)` | `money`, `float` 금지 |
| 반정형 데이터 | `jsonb` | Outbox payload, 외부 응답 원문 등. 조회 조건으로 자주 쓰면 컬럼으로 뺀다 |
| 불리언 | `boolean` | NULL 허용하지 않는 것을 기본으로 한다 |

> 🟡 .NET 8에는 `Guid.CreateVersion7()`이 없습니다(.NET 9부터). UUID v7 생성 방식(라이브러리 또는 직접 구현)은 기반 구축 토픽에서 정합니다.

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
- 매핑은 Infrastructure의 `Persistence/Configurations/`에 **엔티티마다 `IEntityTypeConfiguration<T>` 하나**로 둔다. Domain에 데이터 어노테이션을 쓰지 않는다.
- 강타입 ID는 값 변환기(`HasConversion`)로 `uuid`에 매핑한다.
- Value Object는 Owned Type 또는 Complex Type(EF Core 8)으로 매핑한다.
- Lazy Loading은 쓰지 않는다. 필요한 연관은 `Include`로 명시한다.
- 데이터 접근은 **Repository**로만 한다. Repository에는 람다식 LINQ 쿼리만 두고 분기 · 로직을 넣지 않는다([코딩 컨벤션 · Repository 규칙](coding-conventions.md#repository-규칙-ef-core)).
- **Query(CQRS)는 Read Repository가 읽기 DbContext에서 `Select` 프로젝션**으로 응답 `record`를 바로 만든다. 엔티티 전체를 불러와 변환하지 않는다.
- Command는 Write Repository로 Aggregate를 불러오고, `SaveChangesAsync`는 Unit of Work(파이프라인)에서 한 번만 호출한다.
- DbContext는 `AddDbContext`로 `Scoped` 등록한다.
- 연결 문자열은 설정 / 시크릿으로 주입한다([설정 & 시크릿 관리](../06-deployment/configuration.md)).

## 마이그레이션 규칙

- 서비스마다 EF Core 마이그레이션을 따로 둔다(Infrastructure 프로젝트의 `Persistence/Migrations/`).
- 마이그레이션 이름은 PascalCase로 변경 의도를 적는다: `AddEmployeeNotificationChannels`
- **이미 적용(push)된 마이그레이션은 고치지 않는다.** 수정은 새 마이그레이션으로 한다.
- 생성한 SQL을 확인한다: `dotnet ef migrations script --idempotent`. dba는 작업마다 SQL을 검토한다.
- 파괴적 변경(컬럼 삭제 / 이름 변경 / 타입 변경)은 **확장 → 이전 → 축소** 2단계 이상으로 나눈다.
- 운영 환경에서는 애플리케이션 시작 시 자동 적용하지 않는다(마이그레이션 번들 / 스크립트로 적용). 로컬 개발 환경만 시작 시 적용을 허용한다.

## 트랜잭션 & 동시성 제어

- **Command 하나 = 트랜잭션 하나 = Aggregate 하나.** 여러 Aggregate를 바꿔야 하면 도메인 이벤트로 나눈다.
- 격리 수준은 기본값(Read Committed)을 쓴다. 더 높은 수준이 필요하면 이유를 작업 문서에 남긴다.
- 동시성은 **낙관적 잠금**으로 제어한다. PostgreSQL 시스템 컬럼 `xmin`을 동시성 토큰으로 매핑한다(`uint Version` 속성 + `IsRowVersion()`).
- 동시성 충돌(`DbUpdateConcurrencyException`)은 `Result` 충돌 오류로 변환한다.

## Outbox 테이블

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

> 🟡 메시지 브로커와 MassTransit 도입이 확정되면 MassTransit의 EF Core Outbox 테이블로 대체할 수 있습니다.

## 시드 데이터

- 코드값은 C# enum이 원본이므로 DB 시드 대상이 아니다.
- 운영에 필요한 기준 데이터는 마이그레이션(`HasData`)으로 넣는다.
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
