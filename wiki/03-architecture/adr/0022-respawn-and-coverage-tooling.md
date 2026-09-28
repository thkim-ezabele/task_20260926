---
title: "ADR-0022: 통합 테스트 DB 초기화(Respawn)와 커버리지 도구(coverlet + ReportGenerator)"
type: adr
adr: "0022"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0022]
tags: [adr, architecture, testing, database]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0022: 통합 테스트 DB 초기화(Respawn)와 커버리지 도구(coverlet + ReportGenerator)

## 배경 (Context)

- 통합 테스트는 Testcontainers로 실제 PostgreSQL을 띄우고, 마이그레이션을 실제로 적용한 스키마에서 `WebApplicationFactory<Program>`으로 Api를 호출한다([테스트 전략 · 통합 테스트](../../04-development/testing-strategy.md#통합-테스트-testcontainers), [ADR-0012](0012-migration-apply-and-pre-production-reset.md)). 컨테이너는 컬렉션 단위로 공유하므로 테스트 사이에 데이터를 지울 방법이 필요하다.
- Api는 자기 연결 · 재시도 실행 전략 · 자체 커밋으로 동작한다([ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md)). 테스트 쪽에서 연 트랜잭션을 롤백하는 방식으로는 Api가 커밋한 데이터를 되돌릴 수 없다.
- DB 제약: 앱 롤 `employee_app`은 DB 소유자이지만 `CREATEDB` · `SUPERUSER`가 없다([ADR-0011](0011-use-aspire-local-orchestration.md)). 읽기 연결은 `default_transaction_read_only=on`이다. `__EFMigrationsHistory`는 snake_case의 유일한 예외로 EF 기본 이름을 유지하고 초기화 대상에서 제외하기로 했다(ADR-0012). PK는 UUID v7(`ValueGeneratedNever`)이라 시퀀스가 없다([ADR-0013](0013-uuid-v7-with-uuidnext.md)).
- 커버리지: [PRD-001](../../10-delivery/prd/PRD-001-foundation.md) NFR-03은 BuildingBlocks · Employee Domain / Application 라인 커버리지 80%를 목표로 CI에서 **측정 · 보고만** 하고 필수 체크는 걸지 않는다. FR-10은 CI에서 커버리지 보고를 요구한다. [PRD 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q11에서 Respawn과 커버리지 도구를 확정 범위에 넣었다.
- 버전 · 라이선스는 S01-T01에서 확인했다([패키지 버전 · 라이선스 · 테스트](../package-versions.md#테스트), [도구](../package-versions.md#도구)). Respawn 7.0.0 API는 S01-T04 dba가 DLL 리플렉션과 v7.0.0 소스로 확인했다(Docker 실측은 S03-T05).

## 검토한 대안 (Options)

통합 테스트 DB 초기화:

1. **테스트마다 트랜잭션을 열고 롤백**: 장점: 빠르다. 단점: Api가 다른 연결에서 실행 전략 안에서 직접 커밋하므로 `WebApplicationFactory` 통합 테스트에 맞지 않는다.
2. **테스트마다 DB 재생성 또는 `MigrateAsync` 반복**: 장점: 완전히 격리된다. 단점: 느리고, `employee_app`에 `CREATEDB`가 없다(ADR-0012).
3. **직접 작성한 `TRUNCATE` 스크립트**: 장점: 의존이 없다. 단점: 테이블이 늘 때마다 수동으로 고쳐야 하고 FK 순서를 직접 처리한다.
4. **Respawn 7.0.0(Apache-2.0, 의존 없음)**: 장점: 생성 시 `information_schema`로 대상 테이블 · FK를 읽어 삭제문을 캐시하고, 한 트랜잭션에서 `TRUNCATE ... CASCADE`로 지운다. 테이블이 늘어도 코드를 고치지 않는다. 단점: 설정 값(제외 테이블 이름 등)을 틀리면 조용히 잘못 동작한다(아래 결정에서 고정).

커버리지:

1. **coverlet.collector 10.0.1(MIT) + ReportGenerator 5.5.11(Apache-2.0)**: 장점: VSTest `--collect "XPlat Code Coverage"`로 추가 빌드 설정 없이 cobertura를 만들고, ReportGenerator가 여러 테스트 프로젝트 결과를 합쳐 요약 · HTML을 만든다. xUnit v3 VSTest 경로에서 동작을 확인했다. 단점: 도구가 둘이다.
2. **coverlet.msbuild**: 장점: 빌드 속성만으로 수집한다. 단점: 테스트 프로세스 안에서 계측해 VSTest 수집기 방식보다 취약하고, coverlet 문서도 collector를 권장한다.
3. **Microsoft.CodeCoverage(`--collect "Code Coverage"`, MIT, Test.Sdk 전이 포함)**: 장점: 추가 패키지가 없다. 단점: 기본 출력이 이진 `.coverage`라 형식 · 필터를 따로 설정해야 하고, 보고서 도구는 여전히 필요하다.

## 결정 (Decision)

**초기화는 대안 4(Respawn 7.0.0), 커버리지는 대안 1(coverlet.collector 10.0.1 + ReportGenerator 5.5.11)을 채택한다.**

### Respawn 구성

```csharp
new RespawnerOptions
{
    DbAdapter = DbAdapter.Postgres,
    SchemasToInclude = ["public"],
    TablesToIgnore = [new Table("public", "__EFMigrationsHistory")],
    WithReseed = false,
}
```

- **어댑터**: `DbAdapter.Postgres`를 명시한다(연결 형식 이름으로 자동 추론되지만 명시해 의도를 드러낸다).
- **대상 스키마**: `SchemasToInclude = public`. 서비스 테이블은 `public`만 쓴다([데이터베이스](../../04-development/database.md#database-per-service-원칙)).
- **제외 테이블**: `__EFMigrationsHistory`를 **스키마를 붙이고 큰따옴표 없이 대소문자 그대로** 적는다. Respawn은 이 값을 SQL 문자열 리터럴로 `information_schema`와 비교하므로(대소문자 구분), 따옴표를 넣으면 일치하지 않아 이력 테이블까지 `TRUNCATE`되고, 다음 `MigrateAsync`가 `InitialCreate`를 다시 적용하다 실패한다. 이 이름은 ADR-0012의 snake_case 예외 이름과 같은 값이다.
- **연결**: 쓰기 연결(`ConnectionStrings:Write`와 같은 값, `employee_app`)로 만든 **별도 `NpgsqlConnection`**을 쓴다. 읽기 연결은 `default_transaction_read_only=on`이라 `TRUNCATE`가 `25006`으로 거부된다. DbContext의 연결은 쓰지 않는다. Respawn은 연결을 열지 않으므로 호출자가 `OpenAsync` 뒤에 넘긴다.
- **TRUNCATE 권한**: `TRUNCATE ... CASCADE`는 대상 테이블 전부의 `TRUNCATE` 권한 또는 소유권이 필요하다. `employee_app`으로 마이그레이션하므로 모든 테이블의 소유자라 `GRANT`가 필요 없다. `information_schema.tables`는 권한이 있는 테이블만 보여 주므로, 소유자가 다른 테이블은 **오류 없이 대상에서 빠진다**(소유자를 섞지 않는다).
- **`WithReseed = false`**: PK가 UUID v7이라 시퀀스가 없다. identity · serial 컬럼을 도입하면 다시 검토한다.
- **초기화 시점**: 컨테이너 시작 → DB 준비 → `MigrateAsync` → `Respawner.CreateAsync`는 컬렉션 fixture에서 **한 번** 한다. `CreateAsync`는 대상 테이블 목록을 캐시하므로 반드시 `MigrateAsync` 뒤에 부른다(대상이 0개면 `InvalidOperationException`). `ResetAsync`는 **테스트마다 시작 전**(`IAsyncLifetime.InitializeAsync`)에 부른다. 실패한 테스트의 데이터가 다음 테스트 시작 때 지워지므로 정리 누락이 전파되지 않는다.
- **병렬 실행**: `TRUNCATE`는 `ACCESS EXCLUSIVE` 잠금을 잡으므로 같은 DB를 쓰는 테스트는 한 컬렉션에서 순차 실행한다. 컬렉션을 나누면 컨테이너도 따로 둔다.
- **순환 FK 금지**: Respawn은 순환 FK가 있으면 `ALTER TABLE ... DISABLE TRIGGER ALL`을 쓰는데, 내부 제약 트리거 때문에 슈퍼유저가 필요해 `employee_app`에서는 실패한다. 스키마에 순환 FK를 만들지 않는다(dba 스키마 리뷰 항목, BL-035).
- **기준 데이터**: `HasData` 기준 데이터 테이블이 생기면 `TablesToIgnore`에 추가하거나 초기화 뒤 다시 넣는다(현재 없음, BL-034).

### 통합 테스트 DB 준비

- **이미지**: `new PostgreSqlBuilder("postgres:17")`. 값은 AppHost의 `WithImageTag("17")`와 한 곳에서 관리한다([PostgreSQL 이미지](../package-versions.md#postgresql-이미지)). 매개변수 없는 생성자는 `Obsolete`(기본 15.1)라 이미지 인자가 필수다(TD-004).
- **접속 롤(`employee_app` 재현)**: 컨테이너의 `POSTGRES_USER`(Testcontainers `WithUsername`)는 슈퍼유저다. 컨테이너가 뜨면 **슈퍼유저 연결로** ① `CREATE ROLE employee_app LOGIN PASSWORD '<테스트 전용 값>'` ② `CREATE DATABASE emergency_hub_employee OWNER employee_app`을 명령 하나씩 실행한다(`CREATE DATABASE`는 트랜잭션 블록 안에서 실행할 수 없다). 이후 마이그레이션 · Respawn · Api는 모두 `employee_app` 연결을 쓴다. 로컬 · 운영과 같은 권한에서 마이그레이션 · `TRUNCATE` · PostgreSQL 15+ `public` `CREATE` 동작을 검증하기 위해서다. 슈퍼유저로 테스트하면 권한 문제가 가려진다. 이 준비 단계가 AppHost의 롤 · DB 생성 역할을 대신한다.
- **준비 명령 실행 방식**: 롤 · DB 생성은 Npgsql 명령으로 실행해 SQL 오류가 예외로 올라오게 한다. `ExecScriptAsync`는 `psql --file`이라 `ON_ERROR_STOP`이 없으면 SQL 오류에도 종료 코드 0이 날 수 있다.
- **마이그레이션**: 운영 등록 코드(`AddDbContext` + `UseNpgsql(EnableRetryOnFailure)` + `UseSnakeCaseNamingConvention`, ADR-0011)로 만든 쓰기 DbContext에서 `CreateExecutionStrategy().ExecuteAsync(() => MigrateAsync())`를 fixture당 한 번 실행한다. `EnsureCreated`와 앱 코드의 `CREATE DATABASE`는 금지한다(ADR-0012). EF Core 8 `MigrateAsync`에는 잠금이 없으므로 `WebApplicationFactory` 호스트나 여러 fixture가 같은 DB에 마이그레이션하지 않는다(TD-011).
- **연결 문자열**: Write = 컨테이너 Host · Port + `Database=emergency_hub_employee` + `Username=employee_app`, Read = Write + `Options=-c default_transaction_read_only=on`. `NpgsqlConnectionStringBuilder`로 조립해 `WebApplicationFactory`의 `ConnectionStrings:Write` / `ConnectionStrings:Read`로 주입한다. `Include Error Detail` · `Persist Security Info`는 테스트 연결에도 쓰지 않는다([ADR-0020](0020-logging-with-serilog-and-otlp.md)).

### 커버리지

- **수집**: 모든 테스트 프로젝트가 `coverlet.collector` 10.0.1(`PrivateAssets=all`, 중앙 등록)을 참조하고, `dotnet test --collect "XPlat Code Coverage" --settings coverlet.runsettings`로 테스트 프로젝트마다 cobertura XML을 만든다. 8.0.0부터 .NET 8 SDK · 런타임 이상이 필요하며 조건을 충족한다.
- **`coverlet.runsettings`(저장소 루트, S01-T07에서 작성) 방향**:
  - 형식: `cobertura`
  - 포함(`Include`): NFR-03 대상 어셈블리만 — `EmergencyHub.BuildingBlocks.Domain`, `EmergencyHub.BuildingBlocks.Application`, `EmergencyHub.Employee.Domain`, `EmergencyHub.Employee.Application`. Api · Infrastructure는 커버리지 대신 통합 테스트로 확인한다([테스트 전략 · 커버리지 기준](../../04-development/testing-strategy.md#커버리지-기준)).
  - 제외: 테스트 어셈블리, `**/Migrations/**`(`ExcludeByFile`), `GeneratedCodeAttribute` · `CompilerGeneratedAttribute` · `ExcludeFromCodeCoverageAttribute`(`ExcludeByAttribute`, `[LoggerMessage]` 소스 생성 코드 포함)
  - 자동 속성 제외(`SkipAutoProps = true`)
- **보고**: ReportGenerator는 로컬 도구 매니페스트(`.config/dotnet-tools.json`, `dotnet-reportgenerator-globaltool` 5.5.11)로 고정하고 `dotnet tool restore` 뒤 실행한다. 모든 테스트 프로젝트의 cobertura 결과를 합쳐 텍스트 요약(`TextSummary`), GitHub 잡 요약용 Markdown, HTML을 만든다. CI는 요약을 잡 요약에 싣고 보고서를 아티팩트로 올린다(S01-T07).
- **판정**: 80%는 **목표이며 CI 필수 체크(임계값 실패)로 걸지 않는다**(NFR-03). 안정되면 필수 체크로 바꿀지 새로 정한다. 숫자보다 필수 테스트 케이스(성공 / 실패 / 엣지) 충족을 먼저 판정한다.

## 결과 (Consequences)

- 긍정: 테이블이 늘어도 초기화 코드를 고치지 않고, 이력 테이블이 보존되어 fixture당 한 번의 마이그레이션으로 끝난다. 통합 테스트가 앱과 같은 롤 · 권한 · 등록 코드로 돈다. 커버리지는 설정 파일 하나와 도구 매니페스트로 로컬 · CI에서 같은 명령으로 재현된다.
- 부정: 같은 DB를 쓰는 통합 테스트는 순차 실행이라 테스트가 늘면 느려진다(필요하면 컬렉션 · 컨테이너를 나눈다). fixture가 슈퍼유저 준비 단계를 따로 가져 AppHost 초기화 스크립트와 내용이 중복된다.
- 위험: 제외 테이블 이름 표기, 소유자가 다른 테이블의 조용한 누락, 읽기 연결 사용은 모두 오류 없이 잘못 동작할 수 있다. 이 ADR에 표기를 고정하고 아래 검증으로 막는다.
- 검증: Respawn 뒤 이력 테이블 보존 · `TRUNCATE` 권한 · 읽기 연결 쓰기 거부(`25006`) · `employee_app` fixture 재현은 S03-T05에서 실측한다(BL-033). 이력 테이블 이름이 `TablesToIgnore` 값과 같은지는 S03-T02에서 확인한다(BL-036). 커버리지 수집 · 보고는 S01-T07에서 로컬과 CI로 확인한다.
- 후속: [테스트 전략](../../04-development/testing-strategy.md)의 통합 테스트 · 커버리지 절과 [데이터베이스](../../04-development/database.md)의 시드 데이터 규칙에 반영한다(S01-T04). 도구 매니페스트는 S01-T05, runsettings · CI 보고는 S01-T07, 통합 테스트 fixture는 S03-T05에서 만든다.
