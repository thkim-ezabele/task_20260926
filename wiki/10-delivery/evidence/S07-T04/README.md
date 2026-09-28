---
title: "S07-T04 실행 증빙: Aspire curl 예시 실행과 BL-024 대시보드 추적 확인"
type: doc
status: draft
tags: [delivery, evidence]
created: 2026-09-29
updated: 2026-09-29
---

# S07-T04 실행 증빙: Aspire curl 예시 실행과 BL-024 대시보드 추적 확인

> [S07-T04](../../sprints/S07-query-api-docs.md) developer 단계에서 AppHost(`http` 프로필)로 Employee Api를 띄우고, [직원 API](../../../05-api/employee-api.md) · [로컬 개발 환경 구성](../../../01-getting-started/local-setup.md#등록--조회)의 curl 예시를 모두 실행한 기록과, Aspire 대시보드 추적 화면에서 BL-024(PRD-002 NFR-04)를 확인한 기록입니다. 긴 원문은 같은 폴더의 `.txt`와 PNG에 있습니다.
>
> [위키 홈](../../../README.md) · [PRD-002](../../prd/PRD-002-employee-contacts.md) · [S03-T05 증빙](../S03-T05/README.md)(형식 참조)

## 실행 환경과 방법

- 2026-09-29(로그 시각 UTC 2026-09-28 21:39 ~ 21:49), Windows 11, .NET SDK 8.0.425, Docker Engine API 1.43, Aspire 9.5.2, 브랜치 `feature/prd-002-employee-contacts` HEAD `aa17781`(작업 트리의 코드 변경 없음, 문서만 변경).
- 시작 전: 이 프로젝트 볼륨 `emergency-hub-postgres-data`만 `docker volume rm`으로 지움(user-secrets 유지, local-setup "볼륨만 지우는 경우"). 다른 볼륨 · 컨테이너는 건드리지 않음.
- 실행: `dotnet run --project src/Aspire/EmergencyHub.AppHost --launch-profile http`(개발 인증서 미신뢰, BL-099). 대시보드 캡처를 위해 **실행 셸에만** `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true`를 줌(코드 · 설정 파일 변경 없음). 그래서 콘솔에 로그인 URL · 토큰이 없고, 대시보드 상단에 "원격 분석 엔드포인트가 보안되지 않음" 경고가 보입니다.
- curl: Git Bash에서 실행. 1회차는 Git for Windows 내장 `curl` 8.6.0(mingw), 2회차(본문 결과)는 Windows 내장 `C:\Windows\System32\curl.exe` 8.21.0([발견 1](#발견)). 2회차 전 `employees`를 `TRUNCATE`(앱 롤 `employee_app`, psql)로 0행으로 비움.
- 캡처: Edge 헤드리스(`msedge.exe --headless=new --remote-debugging-port=9333`)를 CDP로 제어해 `/traces`, `/traces/detail/<traceId>`를 열고 span 행을 눌러 상세 패널을 `Page.captureScreenshot`(폭 3000px)으로 찍고 `document.body.innerText`를 저장(S03-T05와 같이 `--screenshot` 단독은 쓰지 않음).
- 종료: `taskkill //F //IM EmergencyHub.AppHost.exe` 뒤 `docker ps -a --filter volume=emergency-hub-postgres-data` 출력 없음(컨테이너 정리됨), Edge 헤드리스 프로세스 종료.
- 마스킹: 대상 없음. 비밀번호 · 토큰이 원문 · 캡처에 나오지 않습니다(`http-curl.txt` · `apphost-console.txt`에서 `grep -i 'password|login?t=|token|secret'` 0건(`apphost-console.txt` 1행 머리말의 grep 설명 문장 1줄 제외), span 텍스트 `Password` 0건).

## curl 예시 실행 결과

원문은 [http-curl.txt](http-curl.txt)입니다. 명령은 [직원 API · curl 예시](../../../05-api/employee-api.md#curl-예시)와 같은 문자열입니다.

| 묶음 | 건수 | 상태 코드 · `code` |
|---|---|---|
| 성공 P01 ~ P07(multipart `file` CSV · JSON, multipart `data` CSV · JSON, form-urlencoded `data`, raw `text/csv`, raw `application/json` 대괄호 없는 나열) | 7 | 모두 `201`, `count` 3 · 2 · 2 · 1 · 1 · 2 · 2(합 13) |
| 실패 F01 ~ F13 | 13 | F01 · F02 · F04 ~ F11 `400` · 1001(행 · 요청 전체 코드 21012 · 21004 · 21016 / 21018 / 21004 · 21011 · 21016 / 21021 / 21025 · 21024 / 21023 / 21028 / 21028 / 21029 / 21028), F03 `409` · 23001(`rows[1..3].email`), F12 `415` · 1005, F13 `413` · 1004 |
| 목록 G01 ~ G05 | 5 | `200` 13건 · `200` 6 ~ 10번째 · `200` 빈 `items`(`totalCount` 13) · `400` · 1001(`page` · `pageSize` 1003) · `400` · 1001(`page` 1001) |
| 이름 G06 ~ G08 | 3 | `200`(동명이인 중 1999-12-31) · `404` · 22001 · `400` · 1001(`name` 21007), `instance` `/api/employee/{name}` |
| local-setup 등록 → 조회(Git Bash `curl`, PowerShell 5.1 `curl.exe` 각 3개, 셸마다 빈 DB) | 6 | 두 셸 모두 `201` count 3 → `200`(`totalCount` 3) → `200`(`김이름`) |
| BL-024용 T1 ~ T3(`traceparent`로 trace ID 지정) | 3 | `201` · `200` · `409` · 23001 |

- 0행 입력(F08 JSON `[]`)은 `400` · 1001 · `errors[""]` 21028입니다(S07-T05 결과와 같음).
- Swagger: `GET /swagger/v1/swagger.json`의 `paths`는 `/api/employee`(`post` · `get`)와 `/api/employee/{name}`(`get`), `post` 요청 본문 형식은 `multipart/form-data` · `application/x-www-form-urlencoded` · `text/csv` · `application/json`.

## BL-024 대시보드 추적 확인

요청 T1(POST 201, 등록 값 `추적확인` / `bl024.probe@example.com` / `010-2424-2424` / `2024-02-24`), T2(`GET /api/employee/{name}` 200, 같은 이름), T3(POST 409, 대소문자만 다른 이메일)의 추적을 대시보드에서 열었습니다. span 속성 전문은 [dashboard-spans.txt](dashboard-spans.txt)입니다.

| 확인 대상 | 결과 | 판정 | 근거 |
|---|---|---|---|
| Npgsql span `db.connection_string`에 비밀번호 없음 | 쓰기 `Host=localhost;Port=51305;Database=emergency_hub_employee;Username=employee_app;Application Name=employee-api-write`, 읽기는 같은 값 + `Application Name=employee-api-read;Options="-c default_transaction_read_only=on"`. `Password` 없음(Npgsql span 6개 모두) | 통과 | [POST SELECT](dashboard-02-post-select-emails.png), [POST INSERT](dashboard-03-post-insert.png), [GET SELECT](dashboard-05-get-name-select.png) |
| span 태그에 등록 값 없음 | `db.statement`는 매개변수 자리표시자만: `... = ANY (@__normalizedEmails_0)`, `INSERT INTO employees (...) VALUES (@p0, ..., @p8) RETURNING xmin;`, `... WHERE e.name = @__name_0 ORDER BY e.joined_on, e.id LIMIT 1`, `SAVEPOINT` · `RELEASE SAVEPOINT`. 9개 span 텍스트에서 `bl024`(대소문자 무시) · `010-2424` · `추적확인` · `%EC` · `2024-02-24` 모두 0건 | 통과 | [dashboard-spans.txt](dashboard-spans.txt) 끝 판정 grep |
| 조회 요청 span `url.path`가 템플릿 | T2 서버 span `url.path` = `/api/employee/{name}`, `http.route` = `api/employee/{name}`(Kestrel, TestServer 기준값과 같음) | 통과 | [GET 서버 span](dashboard-04-get-name-server-span.png) |
| 409 경로 | T3 SELECT span도 `@__normalizedEmails_0`만, 응답 `traceId` = 지정한 `4b1d0024000000000000000000000003` | 통과 | [POST 409 SELECT](dashboard-06-post-409-select.png) |

- 추적 목록: [dashboard-01-traces.png](dashboard-01-traces.png)(migrations · api 추적).
- 500 경로의 `http.route` 유지는 이번 수동 확인 대상이 아닙니다(S07-T02 · T03 자동 테스트가 기준).

## 로그 판정

| 로그 | 결과 | 판정 |
|---|---|---|
| `postgres` 서버 로그 `ERROR` · `FATAL` | `FATAL: database "emergency_hub_employee" does not exist` 1건(빈 볼륨 첫 실행) | N2, 개수 기록 후 제외([database.md 알려진 잡음 로그](../../../04-development/database.md#알려진-잡음-로그-첫-실행--재시작)) |
| AppHost 콘솔 `fail:` · `crit:` | 0건 | 해당 없음([apphost-console.txt](apphost-console.txt)) |
| employee-api 파일 로그(`logs/employee-20260929.json`, 223줄, 이 실행분만) `Error` · `Fatal` · `Warning` | 0건. BL-117(첫 `/health/ready` Error)은 이번 실행 0건 | 해당 없음 |
| 같은 파일 로그의 등록 값(`bl024` · `010-2424` · `추적확인` · `password`) | 0건 | NFR-04 참고(판정 원본은 S06-T06 자동 테스트) |

## 발견

1. **Git Bash 내장 curl의 한글 인자**: 1회차에서 파일 입력 P01 · P02는 `201`이었지만 명령줄에 한글이 든 P03 ~ P07은 모두 `400` · 21022였습니다. `--data-binary '김'`의 전송 바이트가 mingw curl 8.6.0은 2바이트(CP949), Windows curl 8.21.0은 3바이트(UTF-8)였습니다(`--trace-ascii`). 서버 동작은 명세대로이고(잘못된 UTF-8 → 21022), 문서에 주의를 적었습니다([직원 API](../../../05-api/employee-api.md#curl-예시), [local-setup](../../../01-getting-started/local-setup.md#등록--조회)).
2. F13 1회차 명령은 `tr` 인자 이스케이프가 어긋나 문서 명령과 달랐으므로, 문서와 같은 명령(`head -c 1048577 /dev/zero | curl ... --data-binary @-`)으로 다시 실행해 `413` · 1004를 얻었습니다(원문의 "F13 (재실행)").

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-29 | developer | 문서 생성: curl 예시 실행 결과, BL-024 대시보드 확인(캡처 6장 · span 텍스트), 로그 판정, 발견 2건 (S07-T04) |
