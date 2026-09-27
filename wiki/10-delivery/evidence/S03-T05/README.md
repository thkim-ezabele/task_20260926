---
title: "S03-T05 실행 증빙: Aspire AppHost 로컬 실행"
type: doc
status: stable
tags: [delivery, evidence]
created: 2026-09-28
updated: 2026-09-28
---

# S03-T05 실행 증빙: Aspire AppHost 로컬 실행

> [S03-T05](../../sprints/S03-aspire-employee.md) tester 단계에서 `dotnet run --project src/Aspire/EmergencyHub.AppHost`를 새 볼륨 · 빈 user-secrets에서 실행하고, 재시작 2회 · 실패 경로 · 복구까지 기록한 실행 증빙입니다(PRD-001 FR-03 · FR-08 · NFR-04 · NFR-06). 긴 원문은 같은 폴더의 `.txt`(마스킹 완료본)와 PNG에 있습니다.
>
> [위키 홈](../../../README.md) · [PRD-001](../../prd/PRD-001-foundation.md) · [database.md 로컬 DB 구성](../../../04-development/database.md#로컬-db-구성-apphost)

## 실행 환경과 방법

- 2026-09-28, Windows 11, .NET SDK 8.0.425(런타임 8.0.31), Docker, Aspire 9.5.2, 이미지 `postgres:17`(서버 17.11). 커밋 `7890dff` 기준 빌드(경고 0).
- 시작 전 상태: 볼륨 `emergency-hub-postgres-data` 없음, AppHost user-secrets 비어 있음(`No secrets configured`). 이전 스모크의 앱 파일 로그는 스크래치로 옮겨 이번 실행 로그만 남겼습니다.
- 중지: 1회차는 프로세스 강제 종료(컨테이너가 남아 `docker stop` · `docker rm`으로 이 AppHost의 컨테이너 · 세션 네트워크만 정리), 2회차부터는 숨은 콘솔에서 띄운 AppHost에 콘솔 Ctrl+Break를 보내 정상 종료(DCP가 컨테이너 · 네트워크 정리, 약 4초).
- 캡처: Edge 헤드리스. `--screenshot` + `--virtual-time-budget`은 Blazor 대시보드가 그려지지 않아 빈 화면이었고, 같은 Edge를 `--remote-debugging-port`로 띄워 CDP로 `login?t=…` → 각 페이지 이동 → 8초 대기 → `Page.captureScreenshot`으로 찍었습니다. 로그인 토큰은 파일에서 읽기만 하고 출력하지 않았습니다.

| 회차 | 조건 | 프로필 | 결과 |
|---|---|---|---|
| 1 | 새 볼륨 · 빈 user-secrets | `https`(기본) | postgres → migrations(Finished) → api Healthy, POST 201 · GET 200. **대시보드 구조화 로그 · 추적 0건**(아래 발견 2) |
| 2 | 재시작 1(같은 볼륨 · user-secrets) | `http` | 기동 → ready 200 약 4초, 1회차 행 조회 200, 대시보드 로그 19건 · 추적 9건 |
| 3 | 재시작 2 | `http` | 기동 → ready 200 약 4초, 이력 1행 · 직원 2행 유지 |
| 4 | 실패 경로: `Parameters:employee-app-password`만 제거(새 값 생성), 볼륨 유지 | `http` | 28P01 → migrations 종료 코드 1 → api `Failed to start` |
| 5 | 복구: AppHost 중지 → `docker volume rm emergency-hub-postgres-data`(이 볼륨만) → 재실행 | `http` | 새 볼륨 초기화, ready 200, 직원 0행 → POST 201 |

## 텍스트 증빙 최소 구성

| 항목 | 결과 | 근거 |
|---|---|---|
| (a) 시작 순서 | 1회차 시작 시각 postgres · employee-db 12:59:35 → employee-migrations 12:59:38(20901 `Migrations applied` 15:59:41Z, 이어서 종료) → employee-api 12:59:41(`WaitForCompletion` 뒤 시작). 2회차 1:07:34 → 1:07:34(20901 16:07:36Z) → 1:07:37. migrations 상태 `Finished`, 종료 코드 배지 없음(4회차 실패 때만 `1` 배지) | [apphost-console.txt](apphost-console.txt), [app-file-logs.txt](app-file-logs.txt), [리소스 목록](dashboard-01-resources-healthy.png), [migrations 콘솔](dashboard-05-console-migrations.png) |
| (b) 헬스 | `/health/live` 200 `Healthy`, `/health/ready` 200 `Healthy`(두 DbContext 검사, 읽기 연결 `CanConnect`가 25006 없이 통과). 헬스 요청은 요청 로그 · 추적에 없음(정상) | [http-curl.txt](http-curl.txt) |
| (c) 등록 → 조회 | POST 201 + `Location: http://localhost:5180/api/v1/employees/01a0e398-96ac-731e-a0d7-eabec1a1c432` + `{ id }` → GET 200(이메일 `hong.gildong@example.com`로 정규화, `employeeStatus` 정수 1, UTC 시각). 없는 ID 404 · 22001. 2회차 재시작 뒤 1회차 행 GET 200 | [http-curl.txt](http-curl.txt) |
| (d) psql | 소유자 `employee_app`, `public` 소유자 `pg_database_owner`, 롤 속성 모두 `f`(로그인만 `t`), 이력 테이블 1개 · 1행, Read 연결 옵션 세션 `on` → 25006. 아래 표 | [psql-checks.txt](psql-checks.txt) |
| (e) 재시작 뒤 오류 | 판정 제외 대상 말고 ERROR · FATAL 0. 아래 BL-014 표 | [postgres-server-logs.txt](postgres-server-logs.txt) |
| (f) TraceId · 이벤트 101 | POST(traceparent `4bf92f35…4736`) 로그 5건 모두 같은 `@tr`: 20101 SELECT EXISTS → 20001 `EmployeeRegistered`(EmployeeId만) → 20101 INSERT → **101 `CommandSucceeded`**(`LoggingCommandHandlerDecorator`) 1건. GET은 **103 `QuerySucceeded`** 1건 | [app-file-logs.txt](app-file-logs.txt), [구조화 로그](dashboard-02-structured-logs.png) |

대시보드 캡처(2회차, http 프로필): [리소스 목록](dashboard-01-resources-healthy.png)(employee-api Running · 녹색 확인, migrations Finished), [구조화 로그](dashboard-02-structured-logs.png)(19건, 추적 ID 열), [추적 목록](dashboard-03-traces.png)(9건: migrations 5 · api POST 1 · GET 3), [POST 추적 상세](dashboard-04-trace-detail-post.png)(api span + Npgsql span 4개, employee-db), [migrations 콘솔](dashboard-05-console-migrations.png), [api 콘솔(1회차)](dashboard-06-console-api-run1.png). 환경 변수 · 연결 문자열이 보이는 리소스 상세 패널은 찍지 않았습니다.

## psql 확인 항목

접속은 `docker exec <컨테이너> sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -X -h 127.0.0.1 …'`(앱 롤은 `$EMPLOYEE_APP_PASSWORD`), 컨테이너는 `docker ps --filter volume=emergency-hub-postgres-data`로 찾았습니다. SQL은 dba 원본(`checks-superuser.sql` · `checks-app.sql` · `checks-read.sql`)을 그대로 썼습니다.

| # | 결과 | 판정 |
|---|---|---|
| 1 | `emergency_hub_employee` · `employee_app` | 통과 |
| 2 | `rolsuper f`, `rolcreatedb f`, `rolcreaterole f`, `rolreplication f`, `rolbypassrls f`, `rolcanlogin t` | 통과 |
| 3 | `scram` = `t`(해시 미출력) | 통과 |
| 4 | `17.11 (Debian 17.11-1.pgdg13+2)` | 통과 |
| 5 | `public` · `pg_database_owner` | 통과 |
| 6 | 사용자 스키마 `public`만, `employee_app` 스키마 0, `\dn`도 `public`만 | 통과 |
| 7 | `to_regclass` `t`, `__EFMigrationsHistory` 테이블 1개 | 통과 |
| 8 | `20260927134235_InitialCreate` · `8.0.31` 1행(3회차 · 5회차도 1행) | 통과 |
| 9 | `__EFMigrationsHistory` · `employees` 모두 `employee_app` | 통과 |
| 10 | `application_name LIKE 'employee-%'` 행: `employee_app` · `employee-api-read` 1, `employee_app` · `employee-api-write` 1. 10초 간격 3회 반복에서 변화 없음(헬스 주기마다 늘지 않음). 전체 행에는 Aspire 헬스 검사의 `postgres` 연결(`application_name` 빈 값, `emergency_hub_employee` 1 · `postgres` 2)이 있고 판정에서 제외 | 통과 |
| 11 | `SHOW default_transaction_read_only` = `on` → `CREATE TABLE read_only_probe` = `ERROR: 25006` | 통과 |

## 재시작 2회 판정 (BL-014)

판정 규칙(메인 세션 확정): 42P04 한 쌍(`ERROR: database "emergency_hub_employee" already exists` + `STATEMENT: CREATE DATABASE …`)과 첫 실행 3D000(`FATAL: database "emergency_hub_employee" does not exist`, Aspire employee-db 헬스 검사가 생성 스크립트보다 먼저 접속)은 제외하고 개수만 기록, 42710 · 그 밖의 ERROR · FATAL은 0이어야 합니다.

| 회차 | 42P04 쌍 | 3D000 | 42710 | 그 밖 ERROR · FATAL | 비고 |
|---|---|---|---|---|---|
| 1(새 볼륨) | 0 | 1(제외) | 0 | 0 | ERROR 1건은 tester의 psql 11번 25006 탐침(의도한 오류, 제외) |
| 2(재시작 1) | 1 | 0 | 0 | 0 | |
| 3(재시작 2) | 1 | 0 | 0 | 0 | |
| 합계(1~3) | **2 = 실행 3 − 1** | 1 | **0** | **0** | **통과** |

- 앱 리소스 로그: MigrationService · Api 파일 로그의 Error · Fatal · Warning 0건(4회차 실패 경로의 20902 1건 제외). AppHost 콘솔의 `fail:` 1건(1회차 `HTTP/2 over TLS was not negotiated`)은 tester가 OTLP 21180에 보낸 HTTP/1.1 확인 요청 때문이라 제외했습니다.
- `FATAL: the database system is starting up`은 나오지 않았습니다.
- 참고: 4회차(실패 경로)에도 42P04 한 쌍 1개, 5회차(새 볼륨)에 3D000 1개가 있습니다.

## 실패 경로 (잘못된 비밀번호, 28P01)

- 방법: AppHost 중지 뒤 `dotnet user-secrets remove "Parameters:employee-app-password"`(값을 입력하지 않음) → 재실행하면 새 비밀번호가 생성 · 저장되지만 볼륨의 롤 비밀번호는 예전 값입니다([database.md 비밀번호와 볼륨](../../../04-development/database.md#로컬-db-구성-apphost)).
- 결과: 서버 로그 `FATAL: password authentication failed for user "employee_app"`, MigrationService 20902 `Migrations failed for "EmployeeDbContext" with exception "Npgsql.PostgresException" and SqlState "28P01" after 1040 ms, exit code 1`(Error, 약 1초 · 재시도 없음), 대시보드 migrations `Finished` + 종료 코드 `1` 배지, employee-api `Failed to start`, `http://localhost:5180/health/ready` 연결 없음. 로그에 비밀번호 · 연결 문자열 없음(값 대조 종료 코드 1).
- 캡처: [리소스 목록](dashboard-09-fail-28P01-resources.png), [migrations 콘솔](dashboard-10-fail-28P01-console-migrations.png)
- 복구: 문서 절차대로 이 볼륨만 지우고 재실행(5회차) → 소유자 `employee_app`, 이력 1행, 직원 0행, POST 201.

## Kestrel curl (BL-083 H1 · H2, TD-021)

실제 Kestrel(`http://localhost:5180`)에 보낸 요청입니다. 원문은 [http-curl.txt](http-curl.txt)입니다.

| 요청 | 응답 | 로그 |
|---|---|---|
| H1 `GET /api/v1/employees/not-a-guid` · `/12345` · `/%E2%82%AC`(€) · 35자 Guid · `--path-as-is /%ZZ` | 모두 400 ProblemDetails `code` 1001, `errors` 키 `id`(`code` 1001), `traceId` = traceparent의 trace-id | 304 `ModelBindingFailed`(Debug, fields "id") |
| H1 요청 줄 자체 오류(`GET /api/v1/employees/a b HTTP/1.1`, 원시 소켓) | 400, `Content-Length: 0`, 본문 없음(Kestrel이 파이프라인 전에 거부, ProblemDetails 아님) | 앱 로그 없음(`Microsoft.AspNetCore` = Warning) |
| H2 POST 본문 32,505,919바이트(`Content-Length`) | 400 ProblemDetails `code` 1001(`errors` 없음), `Connection: close` | **302 `BadHttpRequestRejected` "with status 413, returned error 1001"**(Information) |
| H2 같은 본문 `Transfer-Encoding: chunked` | `100 Continue` 뒤 400 · 1001 | 302 status 413 |

- TD-021 현행 동작 확인: 원래 상태 코드 413은 로그 302에만 남고 응답은 400 · 1001입니다.
- 참고: Git Bash에서 curl `-d`로 한글 본문을 넘기면 명령줄 인코딩에서 깨져(UTF-8 아님) 400 · 1001(`errors` 키 `displayName`, 304)이 났습니다. JSON 파싱 오류가 필드 키 1001로 응답되는 현행 동작의 실측 예이기도 합니다. 정상 등록은 UTF-8 파일(`--data-binary @body.json`)로 보냈습니다.

## Scrutor Decorate 런타임 (TD-020)

- 실행 중인 Api 프로세스의 로드 모듈: `System.Private.CoreLib` · `Microsoft.AspNetCore`는 공유 프레임워크 8.0.31, `Microsoft.Extensions.DependencyInjection` · `.Abstractions`는 **둘 다 Api 출력 폴더의 10.0.0**(파일 버전 10.0.25.52411), `Scrutor` 7.0.0. 앱 로컬 어셈블리가 공유 프레임워크보다 높아 10.0.0이 로드됩니다. TD-020 전제("공유 프레임워크 DI 8.0 + Abstractions 10.0.0 혼합")는 실제로는 생기지 않고 DI 구현 · 추상화가 같은 10.0.0입니다.
- 동작: Command 1건당 101 `CommandSucceeded` 1건, Query 1건당 103 `QuerySucceeded` 1건(데코레이터 중복 · 누락 없음), 검증 실패 · 없는 ID 경로도 정상 응답. keyed 서비스 관련 예외 없음.

## 비밀 점검 (NFR-06)

| # | 명령(요지) | 결과 |
|---|---|---|
| 1 | `git grep --untracked -nIiE "(password\|pwd)[[:space:]]*=[[:space:]]*[^;{[:space:]\"']" -- . ":(exclude)wiki/08-worklog/raw"` | 원본 7행 = 알려진 가짜 값 제외 대상(테스트 `do-not-leak` 5행, ADR-0011 55행 자리 표시 1행, 스프린트 문서 dba 진행 기록 인용 1행) → **실제 비밀 0건**. 이 증빙 폴더 추가분 0행 |
| 2 | `git ls-files --others --cached --exclude-standard \| grep -iE "secrets\.json\|\.env$\|\.pfx$\|\.key$"` | 0건 |
| 3 | AppHost csproj · appsettings*.json · launchSettings.json | csproj에 `UserSecretsId`만, 설정 파일에 password · ConnectionStrings 0건 |
| 4 | 증빙 텍스트 로그인 토큰 · 비밀번호 | `login?t=<masked>` 2곳, 비밀번호 · 연결 문자열 · OTLP 키 0건 |
| 5 | user-secrets 값마다 `git grep -qF --untracked`(값 출력 금지, CR 제거) | `Parameters:postgres-password` · `Parameters:employee-app-password` · `AppHost:OtlpApiKey` 모두 **종료 코드 1**(1회차 생성 직후, 4회차 재생성 뒤, 증빙 작성 뒤 각각). 앱 · 서버 로그 대조도 종료 코드 1. `Aspire:VersionCheck:*`는 대조 제외 |
| 6 | 스크린샷 | 리소스 목록 · 구조화 로그 · 추적 · 콘솔 로그만, 리소스 상세(환경 변수) 패널 없음. 매개변수 행은 이름 · 원본 키만 보임 |

- 관찰: 첫 실행 때 user-secrets에 `AppHost:OtlpApiKey`도 저장됩니다(Aspire가 대시보드 OTLP API 키를 persist). 매개변수가 아니지만 비밀 값이라 (5) 대조에 포함했습니다.

## 발견 사항

1. **요청 완료 로그 누락(ADR-0020 "요청 로그" 위반)**: 실제 실행에서 POST · GET · 400 · 404 요청 어디에도 `UseSerilogRequestLogging`의 요청 한 줄("HTTP POST … responded 201 …")이 파일 · 콘솔 · 대시보드 로그에 없습니다. `RequestLoggingOptions.Logger`의 기본값은 정적 `Serilog.Log`(Serilog.AspNetCore 10.0.0 XML 문서)인데 ServiceDefaults가 `AddSerilog(..., preserveStaticLogger: true)`라 정적 로거가 무음 로거 그대로입니다. 단위 테스트에 요청 로그 단언이 없어 드러나지 않았습니다. S03-T07(HTTP · 로그 통합 인수)에 인계.
2. **https 기본 프로필에서 대시보드 텔레메트리 0건**: 이 PC의 ASP.NET Core 개발 인증서가 신뢰되지 않아(`dotnet dev-certs https --check --trust` → 신뢰된 인증서 없음) https OTLP 엔드포인트(21180) 전송이 실패하고, 대시보드 구조화 로그 · 추적이 비었습니다([구조화 로그 0건](dashboard-07-https-structured-logs-empty.png), [추적 0건](dashboard-08-https-traces-empty.png)). 앱 파일 로그와 HTTP 동작은 정상입니다. `--launch-profile http`에서는 로그 · 추적이 모두 보였습니다. 시스템 신뢰 저장소는 바꾸지 않았습니다. S04 로컬 개발 환경 문서에 "https 프로필은 `dotnet dev-certs https --trust` 필요, 아니면 http 프로필" 기록이 필요합니다(NFR-04 "새 환경에서 명령 1개").
3. 요청 줄 형식 오류는 Kestrel이 본문 없는 400으로 거부해 ProblemDetails · `code`가 없습니다(프레임워크 동작, 기록만).
4. MigrationService는 Aspire에서 `Hosting environment: Production`입니다(developer 인계, 결과 리뷰 판단 대상).

## 수동 캡처 요청

자동 캡처로 FR-03 증빙(리소스 순서 · Healthy · 로그 · 추적)은 모두 확보했습니다. 아래는 보강용이며 진행을 막지 않습니다.

- [ ] employee-api 리소스 상세의 헬스 검사 상태(`Healthy`) 화면: 상세 패널에 환경 변수(연결 문자열)가 함께 보이므로 환경 변수 영역을 가린 상태로만 찍습니다.
- [ ] employee-migrations 리소스 상세의 종료 코드 `0` 표시: 위와 같이 환경 변수 영역을 가립니다.

## 최종 상태

- 프로세스: AppHost · DCP · 대시보드 · Api · MigrationService · 캡처용 Edge 모두 종료.
- 컨테이너 · 네트워크: 이 AppHost가 만든 postgres 컨테이너와 `aspire-session-network-*` 없음. 다른 프로젝트 컨테이너 · 볼륨(`vital-*`, `elastic8`, `mysql`, `backend_postgres_data` 등)은 건드리지 않음.
- 볼륨 `emergency-hub-postgres-data`: **남아 있음**(5회차 복구 실행으로 초기화, 이력 1행 · 직원 1행).
- user-secrets: **남아 있음**(`Parameters:postgres-password` · `Parameters:employee-app-password` · `AppHost:OtlpApiKey` · `Aspire:VersionCheck:*`). 비밀번호 두 개는 위 볼륨과 일치해 다음 `dotnet run`이 그대로 동작합니다. 초기 상태로 돌리려면 볼륨 삭제와 `dotnet user-secrets clear --project src/Aspire/EmergencyHub.AppHost`를 함께 합니다.
- 앱 파일 로그(`src/Services/Employee/*/logs/`, gitignore): 이번 실행 로그가 남아 있습니다. 요약은 [app-file-logs.txt](app-file-logs.txt).

## 파일

| 파일 | 내용 |
|---|---|
| [apphost-console.txt](apphost-console.txt) | AppHost 콘솔 출력(1회차 https, 4회차 실패 경로) |
| [postgres-server-logs.txt](postgres-server-logs.txt) | postgres 서버 로그 1~5회차 |
| [http-curl.txt](http-curl.txt) | (b) (c) H1 H2, 2회차 · 5회차 요청 원문 |
| [psql-checks.txt](psql-checks.txt) | psql 1~11, 10번 반복, 3회차 재확인 |
| [app-file-logs.txt](app-file-logs.txt) | MigrationService · Api 파일 로그 요약(이벤트 ID · TraceId) |
| `dashboard-*.png` | 대시보드 캡처 10장(위 본문 링크) |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-28 | tester | 문서 생성(S03-T05 tester 실행 증빙) |
