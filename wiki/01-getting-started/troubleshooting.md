---
title: "트러블슈팅"
type: doc
status: todo
tags: [getting-started]
created: 2026-09-27
updated: 2026-09-28
---

# 트러블슈팅

> 자주 발생하는 문제와 해결 방법입니다.
>
> [위키 홈](../README.md)

## 빌드 오류

> TODO:

## 실행 오류

> TODO:

## DB 연결 문제

로컬 DB는 AppHost가 띄우는 `postgres` 리소스(볼륨 `emergency-hub-postgres-data`)입니다. 서버 로그는 대시보드의 `postgres` 리소스 콘솔 로그에서 봅니다. 볼륨 · user-secrets 명령은 [로컬 개발 환경 구성의 초기화](local-setup.md#초기화-볼륨--user-secrets)가 원본입니다.

| 증상 | 원인 | 해결 |
|---|---|---|
| 서버 로그 `FATAL:  password authentication failed for user "employee_app"`, `employee-migrations`가 `Finished` · 종료 코드 `1`(로그 SqlState `28P01`), `employee-api` `Failed to start` | 볼륨이 남아 있는데 user-secrets의 비밀번호(`Parameters:*`)가 지워지거나 바뀌어 새 값이 생성됨. 비밀번호는 빈 볼륨을 처음 초기화할 때만 서버에 저장된다(S03-T05 실측: `Parameters:employee-app-password`만 제거) | AppHost를 멈추고 이름 있는 볼륨만 지운 뒤 다시 실행(초기화 절차 ①, ②). 비밀번호 쌍까지 새로 만들려면 초기화 절차 전체(볼륨 + user-secrets clear) |
| `employee-migrations`가 `Waiting`에서 넘어가지 않고 `employee-api`도 시작하지 않음(BL-017) | MigrationService는 `employee-db`를 기다린다(`WaitFor`). `employee-db` 생성 스크립트(`CREATE DATABASE emergency_hub_employee OWNER employee_app`)가 실패하면 `employee-db`가 준비되지 않아 계속 기다린다. 서버 로그에서 아래 42P04 한 쌍 · 3D000을 뺀 `ERROR` · `FATAL`을 찾는다(예: 볼륨에 `employee_app` 롤이 없으면 `role "employee_app" does not exist`. 이 예는 미실측) | AppHost를 멈추고 이름 있는 볼륨 `emergency-hub-postgres-data`만 지운 뒤 다시 실행(초기화 절차 ①, ②). 빈 볼륨에서 초기화 스크립트가 롤을 다시 만든다. 그래도 반복되면 초기화 절차 전체 |
| 두 번째 실행부터 서버 로그에 `ERROR:  database "emergency_hub_employee" already exists`와 `STATEMENT:  CREATE DATABASE emergency_hub_employee OWNER employee_app`가 실행마다 한 쌍(`42P04`) | **정상 잡음**. Aspire가 실행마다 생성 스크립트를 실행하고 이 오류를 무시한다. PostgreSQL에 `CREATE DATABASE IF NOT EXISTS`가 없어 스크립트로 없앨 수 없다(BL-096 기록) | 조치 없음. 판정(S03-T05 확정): 이 한 쌍과 첫 실행의 `3D000`을 뺀 `ERROR` · `FATAL` · `already exists`가 0건이고, 한 쌍의 개수가 (실행 횟수 − 1)과 같으면 정상([데이터베이스](../04-development/database.md#로컬-db-구성-apphost)). `employee_app`의 `already exists`(`42710`)가 1건이라도 있으면 초기화 스크립트가 다시 실행된 것이므로 정상이 아니다 |
| 첫 실행(빈 볼륨) 서버 로그에 `FATAL:  database "emergency_hub_employee" does not exist`(`3D000`) | **정상 잡음**. `employee-db` 헬스 검사가 생성 스크립트보다 먼저 접속해 남을 수 있다. 42P04 한 쌍과 같은 Aspire 자체 검사 잡음이다(BL-096 기록) | 조치 없음. 개수를 기록하고 오류 0 판정에서 제외한다 |

## 메시지 브로커 연결 문제

> TODO:

## Docker 관련 문제

| 증상 | 원인 | 해결 |
|---|---|---|
| 통합 테스트(`EmergencyHub.Employee.IntegrationTests`)가 Testcontainers 연결 단계에서 전부 실패(`client version 1.44 is too new`, 로컬 실측 예: Engine 24.0.7 · API 1.43에서 110건이 npipe 연결 실패, BL-102) | Testcontainers 4.15.0은 Docker Engine API 1.44 이상을 요구한다. Docker Desktop 4.26(Engine 24) 등 API 1.43 이하 엔진에서는 연결하지 못한다. CI 러너는 해당 없음 | Docker Desktop을 올리거나, 테스트를 실행하는 셸에 `DOCKER_API_VERSION=1.43`을 준다(코드 · 설정 파일에 두지 않음). PowerShell: `$env:DOCKER_API_VERSION = '1.43'` 뒤 `dotnet test`, Git Bash: `DOCKER_API_VERSION=1.43 dotnet test`. 엔진 API 버전 확인: `docker version --format '{{.Server.APIVersion}}'` |

> TODO: (Docker 관련 나머지 항목)

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-28 | dba | DB 연결 문제 표(28P01 비밀번호 불일치, MigrationService Waiting(BL-017), 42P04 한 쌍 · 첫 실행 3D000 정상 잡음(BL-096, database.md 판정 문구)), Docker 관련 문제에 Testcontainers 연결 실패(`DOCKER_API_VERSION=1.43`, BL-102) 작성. 나머지 절은 developer 단계에서 작성 (S04-T03) |
