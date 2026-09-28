---
title: "트러블슈팅"
type: doc
status: draft
tags: [getting-started]
created: 2026-09-27
updated: 2026-09-28
---

# 트러블슈팅

> 자주 발생하는 문제와 해결 방법입니다.
>
> [위키 홈](../README.md)

명령의 셸 표시는 [로컬 개발 환경 구성](local-setup.md)과 같습니다(PowerShell = Windows PowerShell 5.1, Git Bash). 비밀 값(비밀번호, 로그인 토큰, OTLP 키)은 증상을 공유할 때도 붙이지 않습니다.

## 빌드 오류

| 증상 | 원인 | 해결 |
|---|---|---|
| 저장소 안에서 모든 `dotnet` 명령이 `A compatible .NET SDK was not found.`와 `Requested SDK version: 8.0.400`으로 실패 | `global.json`(`8.0.400` + `rollForward: latestFeature`)을 만족하는 SDK가 없다. 8.0.2xx 등 8.0.400 미만만 설치된 경우 | .NET 8 SDK 8.0.400 이상(8.0.x)을 설치한다([사전 준비 사항](prerequisites.md#net-8-sdk)). 확인(두 셸 공통): `dotnet --list-sdks`, 저장소 루트에서 `dotnet --version` |
| 깊은 경로에 clone한 트리에서 빌드가 `MSB3101` · `MSB3030`으로 실패(BL-049) | 빌드 출력 · 중간 파일 경로가 Windows MAX_PATH(260자)를 넘는다(S01-T05 발견) | 짧은 경로(예: `C:\eh`)에 다시 clone한다([저장소 클론](local-setup.md#저장소-클론)). Windows 긴 경로 사용(`LongPathsEnabled`)은 이 저장소에서 확인하지 않았다 |
| 경고 하나로 빌드가 실패 | `Directory.Build.props`의 `TreatWarningsAsErrors=true`로 경고가 오류다(CI도 같음) | 경고 원인을 고친다. 억제는 [코딩 컨벤션 · 경고 억제 규칙](../04-development/coding-conventions.md#경고-억제-규칙)의 승인 절차를 따른다 |
| `dotnet ef ...`가 `dotnet-ef` 명령을 찾지 못하거나 버전이 다름 | 도구 매니페스트를 복원하지 않았거나 전역 설치 도구를 쓰고 있다 | 저장소 루트에서 `dotnet tool restore`(두 셸 공통). 명령 목록은 [DB 마이그레이션](local-setup.md#db-마이그레이션) |

## 실행 오류

| 증상 | 원인 | 해결 |
|---|---|---|
| `https` 프로필로 실행했는데 대시보드 **Structured logs** · **Traces**가 0건. Api 응답 · 앱 파일 로그(`logs/`)는 정상(BL-099) | ASP.NET Core 개발 인증서를 신뢰하지 않아 서비스 → 대시보드 OTLP 전송(`https://localhost:21180`)이 TLS에서 실패한다(S03-T05 실측) | `dotnet dev-certs https --check --trust`로 확인하고 `dotnet dev-certs https --trust`로 신뢰하거나, `--launch-profile http`로 실행한다([개발 인증서 (https 프로필)](local-setup.md#개발-인증서-https-프로필)) |
| AppHost 콘솔이 아닌 곳(IDE, 백그라운드 셸, 에이전트)에서 실행해 Ctrl+C가 AppHost에 닿지 않음 | Ctrl+C는 AppHost를 실행한 콘솔에만 전달된다 | AppHost 프로세스를 끝낸다: PowerShell `Stop-Process -Name EmergencyHub.AppHost`, Git Bash `taskkill //F //IM EmergencyHub.AppHost.exe`. 2026-09-28 두 명령 모두 컨테이너 · 프로젝트 프로세스가 정리되고 볼륨은 남았다([중지](local-setup.md#중지)) |
| AppHost를 끝낸 뒤 `docker ps -a --filter volume=emergency-hub-postgres-data --format '{{.Names}}'`에 컨테이너 이름이 남음 | 프로세스 강제 종료 때 정리가 끝나지 않은 경우(S03-T05 1회차 강제 종료에서 컨테이너가 남은 기록이 있음) | 출력된 이름의 컨테이너만 지운다(두 셸 공통): `docker rm -f <컨테이너 이름>`. 볼륨 · 다른 프로젝트 컨테이너는 건드리지 않는다 |
| Api를 AppHost 없이(`dotnet run --project src/Services/Employee/EmergencyHub.Employee.Api`) 실행하면 시작 시 `InvalidOperationException`(`ConnectionStrings:Write` 또는 `ConnectionStrings:Read`) | 연결 문자열은 AppHost 환경 변수로만 받고 설정 파일에 두지 않는다(메시지에 연결 문자열 값은 없음) | AppHost로 실행한다([서비스 빌드 및 실행](local-setup.md#서비스-빌드-및-실행)). 셸에 비밀번호가 든 연결 문자열을 설정하지 않는다([설정 & 시크릿 관리](../06-deployment/configuration.md#시크릿-관리-user-secrets--github-secrets)) |
| 등록 요청이 `409` · `code` `23001` | 같은 이메일(소문자 정규화 기준)로 이미 등록했다 | 다른 `email`로 보낸다([동작 확인](local-setup.md#동작-확인-swagger--health-check)) |
| Git Bash에서 한글 본문 등록이 `400` · `1001`(`errors` 키 `displayName`) | `curl -d`의 한글이 명령줄 인코딩에서 깨져 JSON 파싱 오류가 된다(S03-T05 실측) | UTF-8 파일을 `--data-binary @<파일>.json`으로 보내거나 ASCII 값으로 확인한다 |

## DB 연결 문제

로컬 DB는 AppHost가 띄우는 `postgres` 리소스(볼륨 `emergency-hub-postgres-data`)입니다. 서버 로그는 대시보드의 `postgres` 리소스 콘솔 로그에서 봅니다. 볼륨 · user-secrets 명령은 [로컬 개발 환경 구성의 초기화](local-setup.md#초기화-볼륨--user-secrets)가 원본입니다.

- 첫 실행 · 재시작 때 나와도 되는 로그(42P04 · 3D000 · 25006 탐침 등)의 목록과 판정은 [데이터베이스 · 알려진 잡음 로그](../04-development/database.md#알려진-잡음-로그-첫-실행--재시작)가 원본입니다. 그 표에 없는 `ERROR` · `FATAL`은 아래 표에서 원인을 찾습니다.
- DB 상태를 직접 보려면 [데이터베이스 · 로컬 DB 구성](../04-development/database.md#로컬-db-구성-apphost)의 psql 명령 틀을 씁니다. 비밀번호는 컨테이너 환경 변수로만 넘기고, 쿼리는 SQL 파일을 표준 입력으로 넘깁니다(PowerShell 5.1에서 `-c "..."`는 큰따옴표가 빠져 `syntax error at end of input`).
- 서버 로그 확인(두 셸 공통): `docker ps --filter volume=emergency-hub-postgres-data --format '{{.Names}}'`로 컨테이너 이름을 찾고 `docker logs <컨테이너 이름>`. 컨테이너는 AppHost 실행마다 새로 만들어지므로 `docker logs`에는 지금 실행의 서버 로그만 있습니다.

| 증상 | 원인 | 해결 |
|---|---|---|
| 서버 로그 `FATAL:  password authentication failed for user "employee_app"`, `employee-migrations`가 `Finished` · 종료 코드 `1`(로그 SqlState `28P01`), `employee-api` `Failed to start` | 볼륨이 남아 있는데 user-secrets의 비밀번호(`Parameters:*`)가 지워지거나 바뀌어 새 값이 생성됨. 비밀번호는 빈 볼륨을 처음 초기화할 때만 서버에 저장된다(S03-T05 실측: `Parameters:employee-app-password`만 제거) | AppHost를 멈추고 이름 있는 볼륨만 지운 뒤 다시 실행(초기화 절차 ①, ②). 비밀번호 쌍까지 새로 만들려면 초기화 절차 전체(볼륨 + user-secrets clear) |
| `employee-migrations`가 `Waiting`에서 넘어가지 않고 `employee-api`도 시작하지 않음(BL-017) | MigrationService는 `employee-db`를 기다린다(`WaitFor`). `employee-db` 생성 스크립트(`CREATE DATABASE emergency_hub_employee OWNER employee_app`)가 실패하면 `employee-db`가 준비되지 않아 계속 기다린다. 서버 로그에서 아래 42P04 한 쌍 · 3D000을 뺀 `ERROR` · `FATAL`을 찾는다(예: 볼륨에 `employee_app` 롤이 없으면 `role "employee_app" does not exist`. 이 예는 미실측) | AppHost를 멈추고 이름 있는 볼륨 `emergency-hub-postgres-data`만 지운 뒤 다시 실행(초기화 절차 ①, ②). 빈 볼륨에서 초기화 스크립트가 롤을 다시 만든다. 그래도 반복되면 초기화 절차 전체 |
| 두 번째 실행부터 서버 로그에 `ERROR:  database "emergency_hub_employee" already exists`와 `STATEMENT:  CREATE DATABASE emergency_hub_employee OWNER employee_app`가 실행마다 한 쌍(`42P04`) | **정상 잡음**. Aspire가 실행마다 생성 스크립트를 실행하고 이 오류를 무시한다. PostgreSQL에 `CREATE DATABASE IF NOT EXISTS`가 없어 스크립트로 없앨 수 없다(BL-096 기록) | 조치 없음. 판정(S03-T05 확정): 이 한 쌍과 첫 실행의 `3D000`을 뺀 `ERROR` · `FATAL` · `already exists`가 0건이고, 한 쌍의 개수가 (실행 횟수 − 1)과 같으면 정상([데이터베이스](../04-development/database.md#로컬-db-구성-apphost)). 실행 하나의 `docker logs`로 보면 빈 볼륨 첫 실행 0개, 그 뒤 실행마다 1개이고, 여러 실행 로그를 모아 세면 합계가 (실행 횟수 − 1)이다(BL-115). `employee_app`의 `already exists`(`42710`)가 1건이라도 있으면 초기화 스크립트가 다시 실행된 것이므로 정상이 아니다 |
| 첫 실행(빈 볼륨) 서버 로그에 `FATAL:  database "emergency_hub_employee" does not exist`(`3D000`) | **정상 잡음**. `employee-db` 헬스 검사가 생성 스크립트보다 먼저 접속해 남을 수 있다. 42P04 한 쌍과 같은 Aspire 자체 검사 잡음이다(BL-096 기록) | 조치 없음. 개수를 기록하고 오류 0 판정에서 제외한다 |
| 빈 볼륨 첫 실행에서 `employee-api` 로그에 `Health check "EmployeeReadDbContext"` · `"EmployeeDbContext"` `with status Unhealthy`(`Error` 2건, EventId 103), 다음 `/health/ready`부터 `Healthy`(BL-117) | 원인 미상. 같은 시각 서버 로그에 연결 오류가 없고, 재현 조건은 볼륨 삭제 + user-secrets clear 뒤 1회차다(S04-T04 실측, 2 · 3회차는 0건) | **잡음으로 제외하지 않는다.** 증빙에는 기록하고 판정받는다. 이후 `/health/ready`가 `200`이면 실행은 계속할 수 있다 |

## 메시지 브로커 연결 문제

해당 없음. 메시지 브로커는 도입을 보류해 로컬 구성 · CI에 없습니다([ADR-0023](../03-architecture/adr/0023-deferred-adoptions.md)). 도입할 때 이 절을 작성합니다.

## Docker 관련 문제

| 증상 | 원인 | 해결 |
|---|---|---|
| 통합 테스트(`EmergencyHub.Employee.IntegrationTests`)가 Testcontainers 연결 단계에서 전부 실패(`client version 1.44 is too new`, 로컬 실측 예: Engine 24.0.7 · API 1.43에서 110건이 npipe 연결 실패, BL-102) | Testcontainers 4.15.0은 Docker Engine API 1.44 이상을 요구한다. Docker Desktop 4.26(Engine 24) 등 API 1.43 이하 엔진에서는 연결하지 못한다. CI 러너는 해당 없음 | Docker Desktop을 올리거나, 테스트를 실행하는 셸에 `DOCKER_API_VERSION=1.43`을 준다(코드 · 설정 파일에 두지 않음). PowerShell: `$env:DOCKER_API_VERSION = '1.43'` 뒤 `dotnet test`, Git Bash: `DOCKER_API_VERSION=1.43 dotnet test`. 엔진 API 버전 확인: `docker version --format '{{.Server.APIVersion}}'` |

- AppHost 실행은 Engine API 1.43(Engine 24.0.7)에서도 `DOCKER_API_VERSION` 없이 동작했습니다(2026-09-28 확인). 이 변수는 통합 테스트 셸에만 줍니다.
- 다른 프로젝트 컨테이너 · 볼륨이 같은 Docker에 있을 수 있습니다. 정리할 때는 이름으로 이 프로젝트 것(`emergency-hub-postgres-data` 볼륨을 쓰는 컨테이너, 볼륨 `emergency-hub-postgres-data`)만 지정합니다. `docker volume prune` · `docker system prune --volumes`는 쓰지 않습니다([초기화 (볼륨 · user-secrets)](local-setup.md#초기화-볼륨--user-secrets)).

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-28 | dba | DB 연결 문제 표(28P01 비밀번호 불일치, MigrationService Waiting(BL-017), 42P04 한 쌍 · 첫 실행 3D000 정상 잡음(BL-096, database.md 판정 문구)), Docker 관련 문제에 Testcontainers 연결 실패(`DOCKER_API_VERSION=1.43`, BL-102) 작성. 나머지 절은 developer 단계에서 작성 (S04-T03) |
| 2026-09-28 | developer | `draft`로 작성: 빌드 오류(SDK 불일치 메시지, MAX_PATH `MSB3101` · `MSB3030`(BL-049), 경고 = 오류, `dotnet-ef` 복원), 실행 오류(https 프로필 대시보드 OTLP 0건(BL-099), Ctrl+C가 닿지 않을 때 프로세스 종료 두 셸 실측, 남은 컨테이너, AppHost 없이 Api 실행, 409 · 23001, Git Bash 한글 본문), 메시지 브로커 해당 없음(ADR-0023), Docker 관련 보충(AppHost는 API 1.43에서 동작, 이름 지정 정리). DB 연결 · Testcontainers 행은 dba 작성분 유지 (S04-T03) |
| 2026-09-28 | - | RETRO-PRD-001 개선안 #6 반영: DB 연결 문제 절에 알려진 잡음 로그 원본(database.md) · psql 명령 틀 링크와 서버 로그 확인 명령, 42P04 "(실행 횟수 − 1)" 풀이(BL-115), 첫 실행 헬스 검사 Unhealthy 행(잡음 아님, BL-117) |
