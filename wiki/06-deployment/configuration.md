---
title: "설정 & 시크릿 관리"
type: doc
status: draft
tags: [deployment]
created: 2026-09-27
updated: 2026-09-28
---

# 설정 & 시크릿 관리

> 애플리케이션 설정 파일 구조, 환경 변수 · 주입 경로, 시크릿 관리 방법과 서비스별 설정 키 목록입니다. 키 목록은 저장소의 `appsettings*.json` · `launchSettings.json` · AppHost 주입 코드와 일치해야 합니다(바꾸면 이 문서도 고칩니다).
>
> [위키 홈](../README.md)

## appsettings 구조

| 프로젝트 | 파일 | 내용 |
|---|---|---|
| Employee Api | `appsettings.json` | `Serilog` 절만: `MinimumLevel`(기본 `Information`, `Microsoft` · `System` · `Microsoft.AspNetCore` · `Microsoft.EntityFrameworkCore` · `Microsoft.EntityFrameworkCore.Database.Command` = `Warning`, `Microsoft.Hosting.Lifetime` = `Information`), `WriteTo`(콘솔 텍스트, 비동기 파일 JSON `logs/employee-.json` 일 단위 · 100MB · 14개), `Properties.ServiceName` = `employee` |
| Employee Api | `appsettings.Development.json` | `Serilog:MinimumLevel`: 기본 `Debug`, `Microsoft.EntityFrameworkCore.Database.Command` = `Information` |
| Employee MigrationService | `appsettings.json` | Api와 같은 구성(`Microsoft.AspNetCore` 재정의 없음), 파일 `logs/employee-migration-.json`, `ServiceName` = `employee-migration` |
| Employee MigrationService | `appsettings.Development.json` | Api Development와 같음 |
| AppHost | 없음 | 설정 파일을 두지 않는다. 값은 코드 상수와 `launchSettings.json`, user-secrets |

- **어떤 `appsettings*.json`에도 `ConnectionStrings` 절이 없습니다.** 비밀번호 없는 예시 값도 두지 않습니다. 연결 문자열은 AppHost가 환경 변수로 주입합니다([데이터베이스 · Api 등록 사양](../04-development/database.md#ef-core-구성-npgsql)).
- OTLP 엔드포인트도 설정 파일에 적지 않습니다(Aspire가 주입, [로깅 & 관측성 · Aspire 연동](../04-development/logging-observability.md#aspire-연동-serilog--otlp-adr-0020)).
- 로그 설정 규칙(콘솔 · 파일 형식, 수준)의 원본은 [로깅 & 관측성](../04-development/logging-observability.md)입니다.

`launchSettings.json`:

| 프로젝트 | 프로필 | 값 |
|---|---|---|
| AppHost | `https`(첫 프로필 = 기본) | `applicationUrl` `https://localhost:17180;http://localhost:15180`, `ASPNETCORE_ENVIRONMENT` · `DOTNET_ENVIRONMENT` = `Development`, `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` = `https://localhost:21180`, `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL` = `https://localhost:22180` |
| AppHost | `http` | `applicationUrl` `http://localhost:15180`, 환경 두 변수 `Development`, OTLP `http://localhost:19180`, 리소스 서비스 `http://localhost:20180`, `ASPIRE_ALLOW_UNSECURED_TRANSPORT` = `true` |
| Employee Api | `http`(유일) | `applicationUrl` `http://localhost:5180`, `ASPNETCORE_ENVIRONMENT` = `Development` |
| Employee MigrationService | 없음 | launchSettings가 없다. 환경은 AppHost가 주입한다([환경 구성](environments.md#환경별-차이점)) |

## 환경별 설정 오버라이드

- .NET 기본 호스트 순서를 그대로 씁니다(뒤가 앞을 덮음): `appsettings.json` → `appsettings.{환경}.json` → user-secrets(Development이고 `UserSecretsId`가 있는 프로젝트만) → 환경 변수 → 명령줄 인자. 별도 설정 공급자는 추가하지 않았습니다.
- `UserSecretsId`는 **AppHost에만** 있습니다(`EmergencyHub.AppHost.csproj`). Api · MigrationService는 user-secrets를 읽지 않습니다.
- 환경 파일은 `appsettings.Development.json`만 있습니다. Development가 아닌 환경(Production 등)은 `appsettings.json`만 적용됩니다. 환경 이름은 [환경 구성](environments.md)에 있습니다.

## 환경 변수 규칙

- 계층 키의 `:`는 환경 변수에서 `__`(밑줄 두 개)로 씁니다. 예: `ConnectionStrings:Write` ↔ `ConnectionStrings__Write`.
- 서비스(Api · MigrationService)가 받는 환경 변수는 **AppHost가 주입**합니다. 셸에 직접 설정하는 것은 아래 "셸에서만 주는 변수"뿐입니다.
- 비밀번호가 든 값을 셸 환경 변수로 설정하지 않습니다(셸 기록 · 프로세스 환경에 남음, [DB 마이그레이션 금지 명령](../01-getting-started/local-setup.md#금지-명령)).

AppHost가 주입하는 변수(코드: `src/Aspire/EmergencyHub.AppHost/EmergencyHubApplication.cs`):

| 대상 리소스 | 변수 | 값 | 코드 상수 |
|---|---|---|---|
| `employee-migrations` | `ConnectionStrings__Write` | 쓰기 연결 식(`Application Name=employee-migration`) | `EmployeeConnectionStrings.WriteVariable` |
| `employee-migrations` | `DOTNET_ENVIRONMENT` | `Development`(고정) | `EmergencyHubApplication.DotnetEnvironmentVariable` · `MigrationEnvironmentName` |
| `employee-api` | `ConnectionStrings__Write` | 쓰기 연결 식(`Application Name=employee-api-write`) | `EmployeeConnectionStrings.WriteVariable` |
| `employee-api` | `ConnectionStrings__Read` | 읽기 연결 식(`Application Name=employee-api-read;Options=-c default_transaction_read_only=on`) | `EmployeeConnectionStrings.ReadVariable` |
| `postgres`(컨테이너) | `EMPLOYEE_APP_PASSWORD` | 매개변수 `employee-app-password`(초기화 스크립트가 롤 비밀번호로 사용) | `EmployeeDatabaseSettings.AppRolePasswordVariable` |

- 연결 식 전문(`Host` · `Port`는 서버 엔드포인트, `Username=employee_app`, `Password`는 매개변수)은 [데이터베이스 로컬 DB 구성](../04-development/database.md#로컬-db-구성-apphost)의 연결 식 표가 원본입니다. `WithReference`는 쓰지 않습니다([ADR-0011](../03-architecture/adr/0011-use-aspire-local-orchestration.md)).
- 위 표 밖에 Aspire가 프로젝트 리소스에 넣는 변수(`OTEL_EXPORTER_OTLP_ENDPOINT` 등 `OTEL_*`)가 있습니다. 코드가 직접 읽는 것은 `OTEL_EXPORTER_OTLP_ENDPOINT`(`OtlpEndpoint.ConfigurationKey`)뿐이고, 값이 있을 때만 OTLP exporter · Serilog OTLP 싱크를 붙입니다. `postgres` 컨테이너에는 Aspire가 슈퍼유저 `POSTGRES_USER` · `POSTGRES_PASSWORD`를 넣습니다(초기화 스크립트 · psql 확인 항목이 사용).

셸에서만 주는 변수(코드 · 설정 파일에 두지 않음):

| 변수 | 쓰는 곳 | 값 | 비고 |
|---|---|---|---|
| `DOCKER_API_VERSION` | 통합 테스트(Testcontainers) 실행 셸 | `1.43` | Docker Engine API 1.43 이하일 때만(BL-102) |
| `ASPIRE_VERSION_CHECK_DISABLED` | AppHost 실행 셸 | `true` | 선택. 버전 확인과 `Aspire:VersionCheck:*` 기록을 끔(BL-097, 저장소 기본은 켜짐) |
| `EMERGENCYHUB_CONTAINER_LOG_DIRECTORY` | 통합 테스트 fixture | 폴더 경로 | 컨테이너 로그를 남길 폴더(없으면 남기지 않음). CI Test 단계가 `${{ runner.temp }}/container-logs`로 준다([CI/CD](ci-cd.md#ci-빌드--테스트--정적-분석)) |
| `ConnectionStrings__Write` | `dotnet ef` 설계 시점 팩터리(`EmployeeDbContextFactory`) | 설정하지 않음 | 없으면 비밀 없는 더미 연결 문자열을 쓴다. 허용 명령은 DB 연결이 필요 없다([DB 마이그레이션](../01-getting-started/local-setup.md#db-마이그레이션)) |

## 시크릿 관리 (User Secrets / GitHub Secrets)

**저장소에 비밀 값을 커밋하지 않습니다**(PRD-001 NFR-06). 설정 파일 · launchSettings · 코드 · 문서 · 테스트 픽스처에 실제 비밀번호를 두지 않고, 문서에는 자리표시자(`<비밀번호>`, `<토큰>`)만 씁니다.

| 비밀 | 저장 위치 | 만드는 주체 | 비고 |
|---|---|---|---|
| `Parameters:postgres-password` | AppHost user-secrets | AppHost 첫 실행(생성 매개변수, `MinLength = 32` · 특수 문자 없음, persist) | 슈퍼유저 `postgres`. 서버 초기화 · 생성 스크립트 전용, 서비스 연결에 넣지 않음 |
| `Parameters:employee-app-password` | AppHost user-secrets | 위와 같음 | 롤 `employee_app`. 연결 식과 초기화 스크립트(`EMPLOYEE_APP_PASSWORD`)에 들어감 |
| `AppHost:OtlpApiKey` | AppHost user-secrets | Aspire(첫 실행 때 대시보드 OTLP 키) | BL-100 |
| 대시보드 로그인 토큰 | 저장하지 않음(실행마다 콘솔 출력) | Aspire | `login?t=<토큰>`. 공유 · 기록하지 않음 |

- 비밀이 아닌 기록 키 `Aspire:VersionCheck:*`도 같은 user-secrets에 저장됩니다(BL-097).
- 사전 설정(`dotnet user-secrets set`)은 쓰지 않습니다. 확인은 키 이름만, 초기화는 볼륨과 함께 전부 지웁니다: [로컬 설정 (User Secrets)](../01-getting-started/local-setup.md#로컬-설정-user-secrets), [초기화 (볼륨 · user-secrets)](../01-getting-started/local-setup.md#초기화-볼륨--user-secrets).
- `dotnet user-secrets list`는 값을 평문으로 출력하므로 키 이름만 남겨 확인합니다.
- **GitHub Secrets: 현재 쓰지 않습니다.** CI 워크플로는 `permissions: contents: read`이고 비밀을 참조하지 않습니다. 통합 테스트 DB는 테스트 실행 때 Testcontainers로 띄우는 일회용 컨테이너입니다([CI/CD](ci-cd.md), [테스트 전략 · 테스트 DB 구성](../04-development/testing-strategy.md#테스트-db-구성-fixture)). 배포(CD)를 도입할 때 정합니다.
- 로그에 설정 · 연결 문자열을 통째로 쓰지 않습니다([로깅 & 관측성 · 개인정보 · 보안](../04-development/logging-observability.md#개인정보--보안)).

## 서비스별 설정 항목

키마다 값의 출처와 코드에서 읽는 위치입니다. "필수"는 없으면 시작 시 예외로 멈춘다는 뜻입니다.

### Employee Api

| 키 | 출처 | 필수 | 읽는 코드 |
|---|---|---|---|
| `ConnectionStrings:Write` | AppHost 환경 변수 `ConnectionStrings__Write` | 예 | `EmployeeInfrastructureServiceCollectionExtensions`(`GetConnectionString("Write")`) |
| `ConnectionStrings:Read` | AppHost 환경 변수 `ConnectionStrings__Read` | 예 | 같은 곳(`GetConnectionString("Read")`) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Aspire 주입 | 아니오(없으면 OTLP 내보내기 안 함) | ServiceDefaults `OtlpEndpoint.IsConfigured` |
| `Serilog:*` | `appsettings*.json` | 아니오 | ServiceDefaults `SerilogDefaults.Configure`(`ReadFrom.Configuration`) |
| 환경 이름(`ASPNETCORE_ENVIRONMENT`) | Api `launchSettings.json` `http` 프로필 | 아니오(없으면 Production) | 호스트 |

- DB 재시도(최대 3회 · 최대 지연 5초)는 설정 키가 아니라 코드 값입니다(`Program.DbRetry`, [데이터베이스 · Api 등록 사양](../04-development/database.md#ef-core-구성-npgsql)).

### Employee MigrationService

| 키 | 출처 | 필수 | 읽는 코드 |
|---|---|---|---|
| `ConnectionStrings:Write` | AppHost 환경 변수 `ConnectionStrings__Write` | 예 | `EmployeeInfrastructureServiceCollectionExtensions`(`AddEmployeeWriteDbContext`) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Aspire 주입 | 아니오 | ServiceDefaults `OtlpEndpoint.IsConfigured` |
| `Serilog:*` | `appsettings*.json` | 아니오 | ServiceDefaults `SerilogDefaults.Configure` |
| 환경 이름(`DOTNET_ENVIRONMENT`) | AppHost 주입 `Development` | 아니오(없으면 Production) | 호스트 |

- `ConnectionStrings:Read`는 쓰지 않습니다(쓰기만 등록).

### AppHost

| 키 | 출처 | 비고 |
|---|---|---|
| `Parameters:postgres-password` | user-secrets(첫 실행 때 생성) | 매개변수 이름 `EmergencyHubResourceNames.PostgresPassword` |
| `Parameters:employee-app-password` | user-secrets(첫 실행 때 생성) | `EmergencyHubResourceNames.EmployeeAppPassword` |
| `AppHost:OtlpApiKey` | user-secrets(Aspire가 저장) | |
| `Aspire:VersionCheck:*` | user-secrets(Aspire가 기록) | `LastCheckDate` · `KnownLatestVersion`, 무시를 고르면 `IgnoreVersion` |
| `ASPNETCORE_ENVIRONMENT` · `DOTNET_ENVIRONMENT` · `ASPIRE_*` | AppHost `launchSettings.json` 프로필 | 위 [appsettings 구조](#appsettings-구조)의 launchSettings 표 |

### 빌드 설정 (런타임 설정 아님)

| 속성 | 위치 | 값 | 쓰는 곳 |
|---|---|---|---|
| `EmergencyHubPostgresImageTag` | `Directory.Build.props` | `17` | AppHost(`PostgresImageTag.Read`) · 통합 테스트 fixture(어셈블리 메타데이터), CI 이미지 pull 단계(`dotnet msbuild -getProperty`) |

### 정해졌으나 구현되지 않은 키

| 키 | 결정 | 현재 |
|---|---|---|
| `Database:EnableSensitiveDataLogging` | [ADR-0020](../03-architecture/adr/0020-logging-with-serilog-and-otlp.md): Development에서만 이 플래그로 켤 수 있고 기본 `false` | 코드가 읽지 않고 모든 환경에서 꺼져 있다. 설정 파일 · AppHost에도 없다(BL-094, [환경 구성](environments.md#환경별-차이점)) |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-28 | developer | `draft`로 작성: appsettings · launchSettings 구조, 오버라이드 순서(user-secrets는 AppHost만), 환경 변수 규칙 · AppHost 주입 표 · 셸 전용 변수, 시크릿 관리(user-secrets 키, GitHub Secrets 미사용), 서비스별 설정 항목(Api · MigrationService · AppHost · 빌드 속성 · 미구현 `Database:EnableSensitiveDataLogging`). 키는 `appsettings*.json` · `launchSettings.json` · AppHost 코드와 grep 대조 (S04-T03) |
