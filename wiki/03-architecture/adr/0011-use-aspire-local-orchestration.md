---
title: "ADR-0011: .NET Aspire 로컬 오케스트레이션"
type: adr
adr: "0011"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0011]
tags: [adr, architecture, database, infrastructure]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0011: .NET Aspire 로컬 오케스트레이션

## 배경 (Context)

- 로컬 개발에서 PostgreSQL → 마이그레이션 → Employee Api를 **명령 하나로** 순서대로 띄워야 한다([PRD-001](../../10-delivery/prd/PRD-001-foundation.md) FR-03). 인프라 범위는 로컬만이고 docker compose는 쓰지 않는다([PRD-001 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q2 · Q3).
- 연결은 [ADR-0009](0009-separate-read-write-db-context.md)의 `ConnectionStrings:Write` / `ConnectionStrings:Read`로 받아야 하고, 읽기 연결은 `default_transaction_read_only=on`이어야 한다. Aspire 기본 방식(`WithReference`)은 `ConnectionStrings:<리소스 이름>` 키 하나만 만든다.
- Aspire 9.5.2 소스를 확인한 결과(S01-T02 dba), DB 리소스의 연결 식은 **부모 서버의 연결 문자열(Username=postgres, 슈퍼유저 비밀번호) + Database**다. `WithReference(employeeDb)`를 걸면 슈퍼유저 자격 증명이 애플리케이션에 주입되어, "슈퍼유저는 생성 스크립트에만"이라는 로컬 계정 방침(S01 계획 리뷰 Q4, PRD Q12)을 어긴다.
- `AddDatabase`의 생성 스크립트(`WithCreationScript`)는 서버가 준비되면 `postgres` DB에 **명령 1개**로 실행되고, `42P04`(이미 존재)만 무시하며 다른 오류는 로그만 남긴다. 여러 문장은 암묵 트랜잭션이 되어 `CREATE DATABASE`가 거부되고, `CREATE ROLE`을 섞으면 재시작 때 `42710`(롤 존재)로 실패한다. DB가 만들어지지 않으면 DB 헬스체크가 Unhealthy로 남아 `WaitFor(db)` 리소스가 대기 상태로 멈춘다.
- 사용 버전은 Aspire 9.5.2로 확정했다([패키지 버전 · 라이선스 · Aspire](../package-versions.md#aspire)). Aspire 9.5는 2025-11-11에 지원이 끝났고 9.x NuGet은 모두 폐기 표시되어 있다. .NET 8 지원 종료는 2026-11-10이다([ADR-0001](0001-use-dotnet8.md)).

## 검토한 대안 (Options)

1. **docker compose**: 장점: 널리 쓰이고 Aspire 의존이 없다. 단점: 기동 순서 · 연결 주입 · 대시보드를 직접 만들어야 하고, PRD 방향 결정(Aspire AppHost)과 맞지 않는다.
2. **Aspire + `WithReference(db)` 기본 연결 주입**: 장점: 코드가 가장 짧다. 단점: 슈퍼유저 자격 증명이 주입되고, ADR-0009 연결 키 · 읽기 전용 연결을 만들 수 없다.
3. **Aspire + 직접 조립한 Write / Read 연결 문자열 주입**: 장점: ADR-0009 키와 로컬 계정 방침을 그대로 지킨다. 단점: 연결 식을 직접 조립해야 한다.

롤 · DB 생성 위치: (a) **롤과 DB 모두 컨테이너 초기화 스크립트**: 볼륨이 남은 상태에서 DB만 지우면 다시 만들 수 없다. (b) **롤은 초기화 스크립트, DB는 `WithCreationScript` 한 문장**: 재시작 때 `42P04`만 발생해 무시된다.

EF Core 등록: (가) **Aspire 클라이언트 통합 `AddNpgsqlDbContext`**: 등록이 가장 짧고 헬스체크 · 추적 · 재시도가 자동이다. 그러나 항상 `AddDbContextPool`을 쓰고 옵션 콜백에 `IServiceProvider`가 없어 DI의 `SaveChangesInterceptor`(`TimeProvider`)를 붙일 수 없으며, [데이터베이스 규칙](../../04-development/database.md#ef-core-구성-npgsql)의 "`AddDbContext`로 Scoped"와 충돌한다. 폐기 패키지가 하나 더 늘고, 단독 사용 시 OpenTelemetry.Api 1.9.0 전이로 NU1902가 난다(OTel 1.19.x 고정으로만 해소, BL-004 · BL-008). Aspire 없이 도는 통합 테스트와 등록 경로도 달라진다. (나) **`AddDbContext` + `EnrichNpgsqlDbContext`**: Scoped는 유지되지만 net8.0에서 EF 내부 API(EF1001)를 쓰고 폐기 패키지가 필요하다. (다) **클라이언트 통합 미사용, `AddDbContext` + `UseNpgsql` 직접 등록**: 등록 코드를 직접 쓰지만 위 결함이 없다.

## 결정 (Decision)

**대안 3, 롤 · DB 생성은 (b), EF Core 등록은 (다)를 채택한다.**

리소스 구성 (`EmergencyHub.AppHost`, `Aspire.AppHost.Sdk` 9.5.2):

| 리소스 | 이름 | 내용 |
|---|---|---|
| PostgreSQL 서버 | `postgres` | 이미지 태그 `17`(`WithImageTag`, 값은 Testcontainers와 한 곳에서 관리, [PostgreSQL 이미지](../package-versions.md#postgresql-이미지)), 이름 있는 데이터 볼륨(예: `emergency-hub-postgres-data`) |
| Database | `employee-db` | `AddDatabase("employee-db", databaseName: "emergency_hub_employee")`. 리소스 이름은 하이픈만 허용(ASPIRE006, BL-012)이라 DB 이름과 분리 |
| MigrationService | `employee-migrations` | [ADR-0012](0012-migration-apply-and-pre-production-reset.md) |
| Api | `employee-api` | Employee Api |
| 매개변수 | `postgres-password`, `employee-app-password` | `AddParameter(..., secret: true)`, 값은 AppHost user-secrets(`Parameters:postgres-password`, `Parameters:employee-app-password`) |

- **롤 모델**: 슈퍼유저 `postgres`는 서버 초기화와 생성 스크립트에만 쓴다. `employee_app`은 `LOGIN` 롤이고 `emergency_hub_employee`의 소유자이며, MigrationService와 Api가 모두 이 롤로 접속한다(`CREATEDB` · `SUPERUSER` 없음).
- **롤 생성**: `WithInitFiles`로 넣는 초기화 셸 스크립트(`docker-entrypoint-initdb.d`, 빈 볼륨에서 한 번 실행)가 `employee_app`을 만든다. 비밀번호는 `WithEnvironment("EMPLOYEE_APP_PASSWORD", 매개변수)`로 넘기고, 스크립트는 `psql -v`로 받은 변수를 `:'pw'` 형태로 인용한다. 스크립트는 LF 줄바꿈이어야 한다(S01-T05 `.gitattributes`). 9.5.2의 `WithInitBindMount`는 `Obsolete`이고 경고를 오류로 다루므로 쓰지 않는다.
- **DB 생성**: `WithCreationScript("CREATE DATABASE emergency_hub_employee OWNER employee_app")` 한 문장만 둔다. 재시작 때 나는 `42P04`는 Aspire가 무시한다. PRD Q12 · FR-03의 "생성 스크립트로 롤 생성"은 이 두 스크립트(롤: 초기화 스크립트, DB: 생성 스크립트)로 구체화한다.
- **권한(PostgreSQL 15+)**: `public` 스키마의 소유자는 `pg_database_owner`이므로 DB 소유자인 `employee_app`이 별도 `GRANT` · `ALTER SCHEMA` 없이 `public`에 객체를 만들 수 있다. 이것이 "`employee_app`이 스키마 소유자"를 실현하는 방식이다. PostgreSQL 15부터 `PUBLIC`에는 `public` 스키마 `CREATE` 권한이 기본으로 없다.
- **연결 주입**: `WithReference(employeeDb)`는 **쓰지 않는다**(슈퍼유저 자격 증명 주입). 서버의 `PrimaryEndpoint("tcp")` Host · Port와 `employee_app`, `employee-app-password` 매개변수로 `ReferenceExpression`을 조립해 `WithEnvironment`로 넣는다.
  - `ConnectionStrings__Write` = `Host=…;Port=…;Database=emergency_hub_employee;Username=employee_app;Password=…`
  - `ConnectionStrings__Read` = Write + `;Options=-c default_transaction_read_only=on`
  - Api에는 Write · Read 둘 다, MigrationService에는 Write만 넣는다.
- **시작 순서**: `migrations.WaitFor(employeeDb)`(DB 생성 뒤 Healthy), `api.WaitForCompletion(migrations)`(종료 코드 0이어야 Api 시작).
- **비밀번호**: 저장소에 커밋하지 않는다(NFR-06). 두 값은 볼륨을 처음 초기화할 때 저장되므로, 바꾸려면 볼륨을 지워야 한다. 연결 문자열에 들어가므로 `;`를 쓰지 않는다.
- **EF Core 등록**: Aspire 클라이언트 통합(`Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`)은 쓰지 않는다. BuildingBlocks.Infrastructure의 공용 확장 메서드가 쓰기 · 읽기 DbContext를 `AddDbContext`(Scoped) + `UseNpgsql(연결, o => o.EnableRetryOnFailure())` + `UseSnakeCaseNamingConvention()`으로 등록하고, 두 DbContext는 같은 실행 전략을 쓴다. Api · MigrationService · 통합 테스트가 같은 등록 코드를 쓴다(FR-09). 추적은 Npgsql.OpenTelemetry의 `AddNpgsql()`, 헬스체크는 `AddDbContextCheck<EmployeeDbContext>()`로 직접 붙인다.
- **ServiceDefaults의 서비스 디스커버리 · 복원력**: Aspire 템플릿대로 `AddServiceDiscovery()`와 `ConfigureHttpClientDefaults`(`AddStandardResilienceHandler()` + `AddServiceDiscovery()`)를 포함한다(Microsoft.Extensions.ServiceDiscovery 9.5.2, Microsoft.Extensions.Http.Resilience 9.9.0). FR-03이 ServiceDefaults 구성으로 명시한 항목이고, 템플릿과의 차이를 줄인다. 지금은 나가는 HTTP 호출이 없어 실제로 동작하는 곳은 없다. DB 복원력은 이것이 아니라 `EnableRetryOnFailure`가 맡는다(BL-009 해소).
- **지원 기간**: Aspire 9.5.2를 지원 종료 상태로 유지한다(사용자 결정, PRD Q3 · Q19). Aspire 13은 C# AppHost에 .NET 10 SDK가 필요하므로 .NET 8을 유지하는 동안은 옮길 수 없다([지원 기간](../package-versions.md#지원-기간)).

## 결과 (Consequences)

- 긍정: `dotnet run --project <AppHost>` 한 번으로 DB → 마이그레이션 → Api가 순서대로 뜬다. ADR-0009의 연결 키와 읽기 전용 연결을 Aspire에서도 그대로 쓰고, 애플리케이션에는 슈퍼유저 자격 증명이 전달되지 않는다. EF 등록 경로가 Aspire 유무와 관계없이 하나라 통합 테스트가 실제 등록 코드를 검증한다. DI 인터셉터와 Scoped 규칙을 유지한다.
- 부정: 연결 식 조립, 헬스체크 · 추적 등록을 직접 관리한다. 비밀번호를 바꾸거나 롤 스크립트를 고치면 볼륨을 지워야 한다. 생성 스크립트가 실패하면 MigrationService가 대기 상태로 멈추며 원인이 리소스 로그에만 남는다(해결: 이름 있는 볼륨 삭제, BL-017).
- 부정: 읽기 연결의 `default_transaction_read_only=on`은 실수를 막는 **안전장치이지 보안 경계가 아니다**(세션에서 `SET`으로 풀 수 있다). 복제본을 도입하면 읽기 전용 롤로 바꾼다(ADR-0009 결과와 같음).
- 부정: `employee_app`이 DDL 권한을 가지므로 Api도 스키마를 바꿀 수 있다. 마이그레이션 전용 롤 분리는 TD-001로 관리한다. 서비스 DB가 2개 이상이 되면 `REVOKE CONNECT, TEMPORARY ... FROM PUBLIC`을 적용한다(BL-015).
- 부정: 보안 패치가 없는 Aspire 9.5.2를 쓴다(TD-005). AppHost 전이 MessagePack 취약점은 수동 고정으로 막는다(TD-007). .NET 8 · Aspire 메이저 업그레이드는 TD-002, 전환 재검토는 BL-002.
- 검증: 롤 · 생성 스크립트 동작은 소스 분석 결론이며, Docker로 새 볼륨 → 시작 → 중지 → 재시작을 실측하는 일은 S03 Aspire 작업 완료 조건에 넣는다(BL-003 · BL-014: 리소스 로그 오류 0, DB 소유자 `employee_app`, `public` 소유자 `pg_database_owner`).
- 후속: [패키지 버전 · 라이선스](../package-versions.md)에 클라이언트 통합 미사용, `WithInitFiles`, Npgsql.OpenTelemetry · HealthChecks.EntityFrameworkCore 행을 반영한다(BL-010 · BL-013 · BL-016). 연결 · 비밀 설정 절차는 [로컬 개발 환경 구성](../../01-getting-started/local-setup.md)과 [설정 & 시크릿 관리](../../06-deployment/configuration.md)(S04)에 쓴다. 로컬 계정 규칙은 [데이터베이스](../../04-development/database.md)에 반영한다(S01-T04 · FR-11).
