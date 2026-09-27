---
title: "로컬 개발 환경 구성"
type: doc
status: todo
tags: [getting-started]
created: 2026-09-27
updated: 2026-09-28
---

# 로컬 개발 환경 구성

> 저장소 클론부터 로컬 실행까지의 절차입니다.
>
> [위키 홈](../README.md)

## 저장소 클론

> TODO:

## 로컬 인프라 실행 (docker compose: PostgreSQL / 메시지 브로커)

> TODO:

## 로컬 설정 (User Secrets)

> TODO:

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

## 서비스 빌드 및 실행

> TODO:

## 동작 확인 (Swagger / Health Check)

> TODO:

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
