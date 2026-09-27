---
title: "로컬 개발 환경 구성"
type: doc
status: draft
tags: [getting-started]
created: 2026-09-27
updated: 2026-09-28
---

# 로컬 개발 환경 구성

> 새로 clone한 저장소에서 **AppHost 명령 하나**로 PostgreSQL → 마이그레이션 → Employee Api를 띄우고, 직원을 등록 · 조회하기까지의 절차입니다. 도구 설치는 [사전 준비 사항](prerequisites.md), 문제가 생기면 [트러블슈팅](troubleshooting.md)을 봅니다.
>
> [위키 홈](../README.md)

- 기준 환경은 Windows 11입니다. 명령마다 셸을 표시합니다. **PowerShell**은 Windows PowerShell 5.1(2026-09-28 확인 5.1.26100), **Git Bash**는 Git for Windows의 bash입니다. 두 셸의 명령 문자열이 같으면 "두 셸 공통"으로 적습니다.
- 명령은 따로 적지 않으면 **저장소 루트**에서 실행합니다.
- `<...>`는 자리표시자입니다. 비밀 값(비밀번호, 대시보드 로그인 토큰, OTLP 키)은 문서 · 로그 · 이슈에 붙이지 않습니다(PRD-001 NFR-06).

## 사전 준비

설치 방법은 [사전 준비 사항](prerequisites.md)에 있습니다. 이 절은 실행에 필요한 버전 조건과 확인 명령입니다.

| 항목 | 조건 | 확인 |
|---|---|---|
| .NET SDK | `global.json`: `8.0.400` + `rollForward: latestFeature`. 8.0의 **8.0.400 이상** SDK가 있어야 한다(8.0.2xx 등만 있으면 모든 `dotnet` 명령이 실패). xUnit v3 테스트 실행에도 8.0.4xx 이상이 필요하다([ADR-0021](../03-architecture/adr/0021-test-tooling-xunit-v3-and-awesomeassertions.md)) | 저장소 루트에서 `dotnet --version`(2026-09-28 확인 `8.0.425`) |
| Docker | Docker Desktop 실행 중. Engine API **1.44 이상** 권장. 1.43 이하면 통합 테스트(Testcontainers 4.15.0) 실행 셸에 `DOCKER_API_VERSION=1.43`이 필요하다(BL-102). AppHost 실행은 API 1.43(Engine 24.0.7)에서 이 변수 없이 동작했다(2026-09-28 확인) | `docker version --format '{{.Server.APIVersion}}'` |
| 로컬 도구 | 도구 매니페스트(`.config/dotnet-tools.json`)의 `dotnet-ef` 8.0.31 · `reportgenerator` 5.5.11. 전역 설치하지 않는다 | clone 뒤 `dotnet tool restore` |
| 개발 인증서 | AppHost 기본(`https`) 프로필을 쓸 때만 필요하다. 신뢰하지 않으면 대시보드에 로그 · 추적이 0건이다(BL-099). 아래 [개발 인증서 (https 프로필)](#개발-인증서-https-프로필) | `dotnet dev-certs https --check --trust` |
| 브라우저 | Aspire 대시보드 확인용 | - |

엔진 API 버전 확인(두 셸 공통):

```bash
docker version --format '{{.Server.APIVersion}}'
```

### 개발 인증서 (https 프로필)

AppHost를 기본 프로필(`https`)로 실행하면 서비스가 대시보드 OTLP 수신 주소 `https://localhost:21180`으로 로그 · 추적을 보냅니다([로깅 & 관측성 · Aspire 연동](../04-development/logging-observability.md#aspire-연동-serilog--otlp-adr-0020)). ASP.NET Core 개발 인증서를 신뢰하지 않으면 이 전송이 TLS에서 실패해 대시보드 구조화 로그 · 추적이 0건입니다(앱 파일 로그와 HTTP 동작은 정상, S03-T05 실측).

① 신뢰 여부를 확인합니다(두 셸 공통).

```bash
dotnet dev-certs https --check --trust
```

- 신뢰되지 않은 경우의 출력 예(2026-09-28 이 PC, 종료 코드 `7`): `The following certificates were found, but none of them is trusted: 1 certificate` 뒤에 인증서 지문 줄.

② 신뢰되지 않았으면 신뢰합니다(두 셸 공통). Windows에서 인증서 설치 확인 창이 나오면 승인합니다. 현재 사용자 인증서 저장소를 바꾸는 명령이므로 직접 판단해 실행합니다.

```bash
dotnet dev-certs https --trust
```

③ 인증서를 신뢰할 수 없거나 원하지 않으면 **`http` 프로필**을 씁니다. `http` 프로필은 대시보드 `http://localhost:15180`, OTLP 수신 `http://localhost:19180`이고 `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`라 인증서가 필요 없습니다(AppHost `launchSettings.json`). 실행 명령은 [서비스 빌드 및 실행](#서비스-빌드-및-실행)에 있습니다.

## 저장소 클론

- **짧은 경로에 clone합니다**(예: `C:\eh`). Windows 깊은 경로에 clone하면 빌드 출력 경로가 MAX_PATH(260자)를 넘어 `MSB3101` · `MSB3030`으로 빌드가 실패할 수 있습니다(S01-T05 발견, BL-049). Windows 긴 경로 사용(`LongPathsEnabled`)도 방법이지만 이 저장소에서는 확인하지 않았습니다.
- 저장소는 private입니다. 접근 권한이 있는 계정으로 로그인해 둡니다(`gh auth status`).
- GitHub 기본 브랜치는 `develop`입니다. PRD-001 토픽이 병합되기 전에는 코드가 토픽 브랜치 `feature/prd-001-foundation`에 있으므로 `--branch`로 지정합니다(병합 뒤에는 `--branch` 없이 `develop`).

PowerShell:

```powershell
git clone --branch feature/prd-001-foundation https://github.com/thkim-ezabele/task_20260926.git C:\eh
Set-Location C:\eh
dotnet --version
dotnet tool restore
```

Git Bash:

```bash
git clone --branch feature/prd-001-foundation https://github.com/thkim-ezabele/task_20260926.git /c/eh
cd /c/eh
dotnet --version
dotnet tool restore
```

- `dotnet --version`은 저장소의 `global.json`에 맞는 SDK(8.0.400 이상 8.0.x)를 출력해야 합니다. `A compatible .NET SDK was not found.`가 나오면 [트러블슈팅 · 빌드 오류](troubleshooting.md#빌드-오류)를 봅니다.
- `git flow` 설정은 clone하면 없어집니다. 개발 흐름(브랜치 작업)을 할 때만 [Git 워크플로우](../04-development/git-workflow.md#로컬-설정-git-flow-config)대로 다시 설정합니다(실행에는 필요 없음).

## 로컬 구성 (Aspire AppHost)

로컬 인프라는 docker compose가 아니라 **Aspire AppHost**(`src/Aspire/EmergencyHub.AppHost`)가 띄웁니다([ADR-0011](../03-architecture/adr/0011-use-aspire-local-orchestration.md)). PostgreSQL을 따로 설치하거나 컨테이너를 직접 띄우지 않습니다. 메시지 브로커는 도입을 보류해 없습니다([ADR-0023](../03-architecture/adr/0023-deferred-adoptions.md)).

| 리소스 (대시보드 이름) | 종류 | 내용 |
|---|---|---|
| `postgres` | 컨테이너 | `postgres:17`, 이름 있는 볼륨 `emergency-hub-postgres-data`, 첫 초기화 때 롤 `employee_app` 생성 스크립트 실행 |
| `employee-db` | Database | `emergency_hub_employee`(소유자 `employee_app`) 생성 스크립트 |
| `employee-migrations` | 프로젝트 | MigrationService. `employee-db`를 기다렸다가 마이그레이션을 적용하고 종료(정상이면 종료 코드 `0`) |
| `employee-api` | 프로젝트 | Employee Api `http://localhost:5180`. 마이그레이션 완료를 기다렸다가 시작하고 `/health/ready`로 상태 판정 |

- 시작 순서: `postgres` → `employee-db` → `employee-migrations`(종료 코드 0) → `employee-api`(Healthy). DB 관점 상세는 [데이터베이스 로컬 DB 구성](../04-development/database.md#로컬-db-구성-apphost)에 있습니다.
- 연결 문자열 · 환경 변수는 AppHost가 주입합니다. 키 목록은 [설정 & 시크릿 관리](../06-deployment/configuration.md#서비스별-설정-항목), 환경 이름은 [환경 구성](../06-deployment/environments.md)에 있습니다.

## 로컬 설정 (User Secrets)

**실행 전에 설정할 값은 없습니다.** 두 DB 비밀번호(`postgres-password`, `employee-app-password`)는 AppHost가 첫 실행 때 생성해 AppHost user-secrets에 저장하고(persist), 다음 실행부터 같은 값을 씁니다. `dotnet user-secrets set`으로 미리 넣지 않습니다([데이터베이스 로컬 DB 구성](../04-development/database.md#로컬-db-구성-apphost)).

- 저장되는 키 4종과 지우는 방법은 아래 [초기화 (볼륨 · user-secrets)](#초기화-볼륨--user-secrets)에 있습니다.
- 저장된 키 **이름만** 확인하는 명령입니다(값은 출력하지 않습니다). 첫 실행 전에는 `No secrets configured`로 시작하는 메시지가 나옵니다.

PowerShell:

```powershell
dotnet user-secrets list --project src/Aspire/EmergencyHub.AppHost | ForEach-Object { ($_ -split ' = ')[0] }
```

Git Bash:

```bash
dotnet user-secrets list --project src/Aspire/EmergencyHub.AppHost | sed -E 's/ = .*//'
```

- 첫 실행 뒤 출력 예(2026-09-28): `Parameters:postgres-password`, `Parameters:employee-app-password`, `Aspire:VersionCheck:LastCheckDate`, `Aspire:VersionCheck:KnownLatestVersion`, `AppHost:OtlpApiKey`.

## 서비스 빌드 및 실행

AppHost 실행 명령 **하나**로 빌드와 모든 리소스 기동이 끝납니다(`dotnet run`이 참조 프로젝트까지 빌드). 사전 명령(비밀번호 설정 · DB 생성 · 마이그레이션)은 없습니다.

기본(`https`) 프로필, 개발 인증서를 신뢰한 경우(두 셸 공통):

```bash
dotnet run --project src/Aspire/EmergencyHub.AppHost
```

`http` 프로필, 개발 인증서 없이(두 셸 공통):

```bash
dotnet run --project src/Aspire/EmergencyHub.AppHost --launch-profile http
```

- 첫 실행은 `postgres:17` 이미지를 내려받으므로 오래 걸릴 수 있습니다.
- 콘솔에 대시보드 주소가 나옵니다(`http` 프로필 2026-09-28 실행 예). 로그인 토큰이 붙은 URL(`login?t=<토큰>`)로 대시보드를 엽니다. 토큰은 공유하지 않습니다.

```text
Now listening on: http://localhost:15180
Login to the dashboard at http://localhost:15180/login?t=<토큰>
Distributed application started. Press Ctrl+C to shut down.
```

- `https` 프로필이면 대시보드가 `https://localhost:17180`입니다(개발 인증서를 신뢰하지 않았으면 브라우저 경고).
- 빌드만 따로 확인하려면(선택, 두 셸 공통): `dotnet build EmergencyHub.sln`. 경고는 오류로 처리됩니다(`TreatWarningsAsErrors`).

### 대시보드 확인

대시보드 **Resources** 화면에서 아래 상태가 되면 기동이 끝난 것입니다(S03-T05 · S04-T01 실측).

| 리소스 | 기대 상태 |
|---|---|
| `postgres` | Running |
| `employee-migrations` | Finished, 종료 코드 `0` |
| `employee-api` | Running, Healthy |

- `employee-migrations`가 `Waiting`에서 넘어가지 않거나 종료 코드가 `1`이면 [트러블슈팅 · DB 연결 문제](troubleshooting.md#db-연결-문제)를 봅니다.
- **Structured logs** · **Traces** 화면에 `employee-api` 로그 · 추적이 보여야 합니다. `https` 프로필에서 0건이면 [개발 인증서 (https 프로필)](#개발-인증서-https-프로필)를 확인합니다.

### 중지

- 실행한 콘솔에서 **Ctrl+C**를 누릅니다. 컨테이너는 정리되고 볼륨 `emergency-hub-postgres-data`와 AppHost user-secrets는 남습니다(다음 실행에서 데이터 유지).
- IDE · 백그라운드 셸에서 실행해 Ctrl+C가 닿지 않으면 AppHost 프로세스를 끝냅니다. 2026-09-28 두 명령 모두 컨테이너 · 프로젝트 프로세스가 정리되고 볼륨은 남았습니다.

PowerShell:

```powershell
Stop-Process -Name EmergencyHub.AppHost
```

Git Bash:

```bash
taskkill //F //IM EmergencyHub.AppHost.exe
```

- 끝낸 뒤 이 프로젝트 컨테이너가 남지 않았는지 확인합니다(두 셸 공통, 출력이 없어야 함). 남아 있으면 [트러블슈팅 · 실행 오류](troubleshooting.md#실행-오류)를 봅니다.

```bash
docker ps -a --filter volume=emergency-hub-postgres-data --format '{{.Names}}'
```

## 동작 확인 (Swagger / Health Check)

`employee-api`가 Healthy가 된 뒤 실행합니다. Api 주소는 `http://localhost:5180`이고, 엔드포인트 계약은 [직원 API](../05-api/employee-api.md)에 있습니다. 아래 결과는 2026-09-28 `http` 프로필 실행에서 확인했습니다.

- PowerShell 5.1에서 `curl`은 `Invoke-WebRequest`의 별칭이라 아래 PowerShell 예는 `Invoke-RestMethod`를 씁니다.
- 본문은 ASCII 값으로 둡니다. Git Bash에서 `-d`로 한글을 넘기면 명령줄 인코딩에서 깨져 `400` · `1001`이 납니다(S03-T05 실측). 한글은 UTF-8 파일을 `--data-binary @<파일>.json`으로 보냅니다.
- 같은 이메일로 다시 등록하면 `409` · `23001`(이메일 중복)입니다. 다시 실행할 때는 `email` 값을 바꿉니다.

### 헬스 체크

PowerShell:

```powershell
Invoke-RestMethod http://localhost:5180/health/ready
```

Git Bash:

```bash
curl -s -i http://localhost:5180/health/ready
```

- 기대: `200`, 본문 `Healthy`(Git Bash는 `HTTP/1.1 200 OK` 헤더와 함께). 검사 없는 생존 확인은 `/health/live`입니다.

### 등록 → 조회

PowerShell:

```powershell
$body = '{"displayName":"Hong Gildong","email":"hong.gildong@example.com","employeeStatus":1}'
$created = Invoke-RestMethod -Method Post -Uri http://localhost:5180/api/v1/employees -ContentType 'application/json' -Body $body
$created | ConvertTo-Json
Invoke-RestMethod "http://localhost:5180/api/v1/employees/$($created.id)" | ConvertTo-Json
```

Git Bash(두 번째 명령의 `<id>`에 첫 응답의 `id`를 넣습니다):

```bash
curl -s -i -X POST http://localhost:5180/api/v1/employees -H 'Content-Type: application/json' -d '{"displayName":"Hong Gildong","email":"hong.gildong@example.com","employeeStatus":1}'
curl -s -i http://localhost:5180/api/v1/employees/<id>
```

기대 결과(Git Bash 출력 발췌, `id` · 시각은 실행마다 다름):

```http
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5180/api/v1/employees/01a0e476-6e69-7265-b4ce-88b1310f916f

{"id":"01a0e476-6e69-7265-b4ce-88b1310f916f"}
```

```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{"id":"01a0e476-6e69-7265-b4ce-88b1310f916f","displayName":"Hong Gildong","email":"hong.gildong.t03@example.com","employeeStatus":1,"createdAt":"2026-09-27T20:02:42.293017+00:00","updatedAt":"2026-09-27T20:02:42.293017+00:00"}
```

- `email`은 소문자로 정규화되어 저장됩니다(위 예는 `Hong.Gildong.T03@Example.com`으로 등록). `employeeStatus`는 정수(`1` = Active), 시각은 UTC입니다. PowerShell은 같은 값을 `ConvertTo-Json` 형식으로 출력합니다.

### Swagger

- Swagger UI: `http://localhost:5180/swagger`(→ `/swagger/index.html`), OpenAPI 문서: `http://localhost:5180/swagger/v1/swagger.json`. Api 환경이 Development일 때만 노출됩니다([ADR-0019](../03-architecture/adr/0019-use-swashbuckle-openapi.md)). 로컬 Api는 Development입니다([환경 구성](../06-deployment/environments.md)).

## DB 마이그레이션

로컬에서는 **마이그레이션을 직접 적용하지 않습니다.** AppHost를 실행하면 MigrationService(`employee-migrations`)가 쓰기 연결로 `MigrateAsync`를 한 번 실행하고 종료하며, Api는 그 완료를 기다립니다([ADR-0012](../03-architecture/adr/0012-migration-apply-and-pre-production-reset.md), [데이터베이스 마이그레이션 규칙](../04-development/database.md#마이그레이션-규칙)). 아래 명령은 마이그레이션을 **만들고 확인할 때**만 씁니다.

- 명령은 저장소 루트에서 실행합니다. 두 셸(PowerShell · Git Bash)에서 명령 문자열이 같습니다.
- `dotnet-ef`는 저장소의 도구 매니페스트(`.config/dotnet-tools.json`, `dotnet-ef` 8.0.31, `rollForward: false`)로 씁니다. 전역 설치(`dotnet tool install --global dotnet-ef`)는 버전이 달라질 수 있으므로 쓰지 않습니다.
- Employee Infrastructure 어셈블리에 DbContext가 2개(쓰기 `EmployeeDbContext`, 읽기 `EmployeeReadDbContext`)라 `--context EmployeeDbContext`가 **필수**입니다. 마이그레이션은 쓰기 DbContext에서만 만듭니다.
- 설계 시점 팩터리(`EmployeeDbContextFactory`)는 환경 변수 `ConnectionStrings__Write`가 없으면 비밀 없는 더미 연결 문자열을 씁니다. 아래 허용 명령은 DB에 연결하지 않으므로 **연결 문자열을 설정하지 않고** 실행합니다.
- `<마이그레이션이름>`은 자리표시자입니다. PascalCase로 변경 의도를 적어 바꿔 넣습니다(예: `AddEmployeeNotificationChannels`). PowerShell에서 `<`는 예약 문자라 자리표시자를 그대로 두면 실행되지 않습니다.

### 허용 명령

| # | 명령 | 용도 | DB 연결 |
|---|---|---|---|
| 1 | `dotnet tool restore` | 매니페스트의 `dotnet-ef` 8.0.31 · `reportgenerator` 복원. clone 뒤 한 번, 매니페스트가 바뀌면 다시 | 없음 |
| 2 | `dotnet ef migrations add <마이그레이션이름> --project src/Services/Employee/EmergencyHub.Employee.Infrastructure --context EmployeeDbContext` | 새 마이그레이션 생성(`Persistence/Migrations/`). 만든 뒤 같은 폴더에 `<마이그레이션 ID>.Sealed.cs`(`public sealed partial class <마이그레이션이름>;`)를 **직접 추가**한다([sealed partial 선언](../04-development/database.md#마이그레이션-규칙)) | 없음 |
| 3 | `dotnet ef migrations script --idempotent --project src/Services/Employee/EmergencyHub.Employee.Infrastructure --context EmployeeDbContext` | 전체 마이그레이션의 멱등 SQL 출력. 명명(snake_case) · 타입 · 체크 제약 검토에 쓴다. 파일로 받으려면 `--output <경로>.sql`(저장소 밖 경로 권장, 커밋하지 않음) | 없음 |
| 4 | `dotnet ef migrations has-pending-model-changes --project src/Services/Employee/EmergencyHub.Employee.Infrastructure --context EmployeeDbContext` | 모델과 마지막 마이그레이션의 차이 확인. 차이가 없으면 `No changes have been made to the model since the last migration.`와 종료 코드 0 | 없음 |
| 5 | `dotnet ef migrations remove --project src/Services/Employee/EmergencyHub.Employee.Infrastructure --context EmployeeDbContext` | **push 전**, 내 로컬에만 있는 마지막 마이그레이션을 되돌릴 때만. 생성 파일만 지워지므로 직접 만든 `<마이그레이션 ID>.Sealed.cs`는 손으로 지운다. push 뒤에는 금지(아래 표) | 적용 여부 확인을 위해 연결을 시도할 수 있음 |

실행 예(두 셸 공통, 2026-09-28 dba 확인: 1 · 3 · 4번, 3번은 `Build succeeded.` 뒤 `CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory"`로 시작하는 SQL).

PowerShell:

```powershell
dotnet tool restore
dotnet ef migrations script --idempotent --project src/Services/Employee/EmergencyHub.Employee.Infrastructure --context EmployeeDbContext
dotnet ef migrations has-pending-model-changes --project src/Services/Employee/EmergencyHub.Employee.Infrastructure --context EmployeeDbContext
```

Git Bash:

```bash
dotnet tool restore
dotnet ef migrations script --idempotent --project src/Services/Employee/EmergencyHub.Employee.Infrastructure --context EmployeeDbContext
dotnet ef migrations has-pending-model-changes --project src/Services/Employee/EmergencyHub.Employee.Infrastructure --context EmployeeDbContext
```

### 금지 명령

| 명령 / 행동 | 금지 이유 | 대신 할 것 |
|---|---|---|
| `dotnet ef database update` | 적용 주체는 MigrationService 1개다. EF Core 8 `MigrateAsync`에는 잠금이 없어 적용 주체가 둘이면 충돌할 수 있다([ADR-0012](../03-architecture/adr/0012-migration-apply-and-pre-production-reset.md), TD-011) | AppHost 실행(MigrationService가 적용) |
| `dotnet ef database drop` | DB · 롤 · 비밀번호는 AppHost와 볼륨이 관리한다. DB만 지우면 볼륨 · user-secrets와 어긋난다 | [초기화 (볼륨 · user-secrets)](#초기화-볼륨--user-secrets) |
| 셸에서 비밀번호가 든 `ConnectionStrings__Write` 설정(`$env:ConnectionStrings__Write = ...`, `export ConnectionStrings__Write=...`) | 비밀 값이 셸 기록 · 프로세스 환경에 남는다(NFR-06). 허용 명령은 연결이 필요 없다 | 설정하지 않고 실행(더미 연결 사용) |
| push된 마이그레이션에 `dotnet ef migrations remove` | 이미 공유(push)된 마이그레이션은 고치지 않는다. 수정은 새 마이그레이션으로 한다 | 새 마이그레이션 추가(허용 2번) |
| 생성된 마이그레이션 파일(`<ID>_<이름>.cs` · `.Designer.cs` · `ModelSnapshot.cs`) 직접 수정 | 생성 코드는 고치지 않는다. sealed 처리는 partial 선언 파일로 한다 | `*.Sealed.cs` partial 선언 |
| 개별 마이그레이션 부분 수정 · 임의 재생성 | 운영 전 리셋(`InitialCreate` 재생성)만 예외이고 절차가 정해져 있다 | [ADR-0012](../03-architecture/adr/0012-migration-apply-and-pre-production-reset.md) 리셋 절차 ①~⑤ |
| 트랜잭션을 끄는 마이그레이션(`suppressTransaction: true`, `CREATE INDEX CONCURRENTLY` 등) | MigrationService 재시도가 `MigrateAsync` 전체를 다시 실행하므로 마이그레이션마다 트랜잭션이어야 안전하다 | 트랜잭션 안에서 실행되는 DDL로 설계 |


## 초기화 (볼륨 · user-secrets)

로컬 DB를 **처음 상태로 되돌리는** 절차입니다(비밀번호 교체, 볼륨 상태를 알 수 없을 때). 요약과 근거는 [데이터베이스 로컬 DB 구성](../04-development/database.md#로컬-db-구성-apphost)의 "로컬 실행 요약"에 있습니다.

- **볼륨 삭제와 user-secrets clear를 반드시 함께** 합니다. 두 비밀번호(`postgres-password`, `employee-app-password`)는 빈 볼륨을 처음 초기화할 때 서버에 저장되므로, user-secrets만 지우면 새 비밀번호가 생성되어 인증이 실패합니다(`28P01`, [트러블슈팅](troubleshooting.md#db-연결-문제)).
- 지우는 것은 **이름 있는 볼륨 `emergency-hub-postgres-data` 하나**와 **AppHost user-secrets**뿐입니다. 익명 볼륨 · 다른 프로젝트 볼륨은 지우지 않습니다. `docker volume prune`, `docker system prune --volumes`, 볼륨 이름 없이 지우는 명령은 쓰지 않습니다.
- AppHost user-secrets에 남는 키와 clear로 지워지는 키:

| 키 | 기록 주체 | 비고 |
|---|---|---|
| `Parameters:postgres-password` | AppHost 매개변수(persist) | DB와 묶인 키. 볼륨과 짝 |
| `Parameters:employee-app-password` | AppHost 매개변수(persist) | DB와 묶인 키. 볼륨과 짝 |
| `AppHost:OtlpApiKey` | Aspire(첫 실행 때 대시보드 OTLP 키 저장) | BL-100 |
| `Aspire:VersionCheck:*` | Aspire 9.5.2 버전 확인(실행마다 `LastCheckDate` · `KnownLatestVersion`, 대시보드에서 무시를 고르면 `IgnoreVersion`) | BL-097 |

- 키를 골라 지우지 않고 **전부 지웁니다**(`clear`). 같은 머신의 다른 clone도 같은 `UserSecretsId`와 볼륨 이름을 쓰므로 함께 초기화됩니다.
- `dotnet user-secrets list`는 **값을 평문으로 출력**합니다. 아래 확인 명령처럼 키 이름만 남기고, 출력을 로그 · 문서 · 이슈에 붙이지 않습니다.

### 절차

① AppHost를 멈춥니다. 콘솔에서 실행했으면 Ctrl+C입니다. IDE · 백그라운드에서 실행해 Ctrl+C가 닿지 않으면 AppHost 프로세스를 끝냅니다(컨테이너는 정리되고 볼륨은 남습니다). 볼륨을 쓰는 컨테이너가 남아 있지 않은지 확인합니다(출력이 없어야 합니다).

PowerShell:

```powershell
docker ps -a --filter volume=emergency-hub-postgres-data --format '{{.Names}}'
```

Git Bash:

```bash
docker ps -a --filter volume=emergency-hub-postgres-data --format '{{.Names}}'
```

② 볼륨을 확인하고 이름을 지정해 지웁니다(`--filter name=`은 부분 일치이므로 출력된 이름이 정확히 `emergency-hub-postgres-data`인지 봅니다).

PowerShell:

```powershell
docker volume ls --filter name=emergency-hub-postgres-data --format '{{.Name}}'
docker volume rm emergency-hub-postgres-data
```

Git Bash:

```bash
docker volume ls --filter name=emergency-hub-postgres-data --format '{{.Name}}'
docker volume rm emergency-hub-postgres-data
```

③ AppHost user-secrets를 모두 지우고, 키가 남지 않았는지 키 이름만 출력해 확인합니다(출력되는 키가 없어야 합니다).

PowerShell:

```powershell
dotnet user-secrets clear --project src/Aspire/EmergencyHub.AppHost
dotnet user-secrets list --project src/Aspire/EmergencyHub.AppHost | ForEach-Object { ($_ -split ' = ')[0] }
```

Git Bash:

```bash
dotnet user-secrets clear --project src/Aspire/EmergencyHub.AppHost
dotnet user-secrets list --project src/Aspire/EmergencyHub.AppHost | sed -E 's/ = .*//'
```

④ AppHost를 다시 실행합니다([서비스 빌드 및 실행](#서비스-빌드-및-실행)). 새 비밀번호 쌍이 생성되어 user-secrets에 저장되고, 빈 볼륨에 초기화 스크립트가 한 번 실행됩니다. 첫 실행 서버 로그에 남을 수 있는 `3D000`은 정상 잡음입니다([트러블슈팅](troubleshooting.md#db-연결-문제)).

- **볼륨만 지우는 경우**: 비밀번호 불일치(`28P01`) 복구나 생성 스크립트 실패 복구처럼 user-secrets는 그대로 두고 볼륨만 새로 만들면 되는 경우에는 ①, ②만 하고 다시 실행합니다. 남아 있는 비밀번호로 빈 볼륨이 초기화됩니다.
- **버전 확인 끄기(선택, BL-097)**: `Aspire:VersionCheck:*` 기록을 원하지 않으면 실행하는 셸에 `ASPIRE_VERSION_CHECK_DISABLED=true`를 줍니다(키 이름은 Aspire.Hosting 9.5.2 어셈블리 문자열로 확인). 저장소 기본값은 켜짐이며 설정 파일에는 두지 않습니다. PowerShell: `$env:ASPIRE_VERSION_CHECK_DISABLED = 'true'`, Git Bash: `export ASPIRE_VERSION_CHECK_DISABLED=true`.


---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-28 | dba | DB 마이그레이션 절(도구 매니페스트, `--context` 필수, 허용 명령 5개 · 금지 명령 표, 셸별 실행 예)과 초기화 (볼륨 · user-secrets) 절(이름 있는 볼륨만 삭제 + user-secrets clear 함께, 키 4종, 셸별 명령, 볼륨만 지우는 경우, 버전 확인 끄기) 작성. 나머지 절은 developer 단계에서 작성 (S04-T03) |
| 2026-09-28 | developer | `draft`로 작성: 셸 표기 규칙, 사전 준비(SDK 8.0.400 이상 · `global.json`, Docker Engine API 1.44 / `DOCKER_API_VERSION=1.43`(BL-102), 도구 매니페스트, 개발 인증서 확인 · 신뢰 명령과 `http` 프로필 대안(BL-099)), 저장소 클론(짧은 경로 · MAX_PATH, BL-049), docker compose 제목을 "로컬 구성 (Aspire AppHost)"으로 바꿈, 로컬 설정(사전 설정 없음, 키 이름만 확인), 서비스 빌드 및 실행(명령 1개, 프로필 2개, 대시보드 확인, 중지 · 프로세스 종료 명령), 동작 확인(헬스, 등록 → 조회 두 셸 실측, Swagger). DB 마이그레이션 · 초기화 절은 dba 작성분 유지하고 절 순서만 뒤로 (S04-T03) |
