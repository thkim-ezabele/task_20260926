---
title: "S04: 테스트 보강 · 문서 · 인수 증빙"
type: sprint
sprint: "S04"
status: done
prd: [PRD-001]
started: 2026-09-28
finished: 2026-09-28
adrs: []
worklogs: []
aliases: [S04]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-28
---

# S04: 테스트 보강 · 문서 · 인수 증빙

- PRD: [PRD-001](../prd/PRD-001-foundation.md)
- 토픽 브랜치: `feature/prd-001-foundation` · 스프린트 종료 태그: `sprint/S04`

## 목표

> 모든 FR / NFR에 증빙(테스트, CI 실행, 문서, 재현 기록)이 붙고, 새 환경에서 문서만 보고 clone → 명령 1개로 실행할 수 있다.

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S04-T01 | 커버리지 대상 확정 · 보강과 코드 정리 묶음 (기반 · 셋팅, 코드 포함) | NFR-03, NFR-07, FR-03, FR-09 | `coverlet.runsettings` Include를 BuildingBlocks 4개(Domain · Application · Infrastructure · Api)와 Employee Domain / Application으로 조정하고 주석을 결정에 맞춘다(BL-068). 로컬 커버리지 실행에서 reportgenerator가 합산한 cobertura 수 = 수집 대상 테스트 프로젝트 수이고, ArchitectureTests 수집 제외를 확정하며, 대상 어셈블리별 라인 · 분기 수치를 진행 기록에 남긴다(BL-063, TD-024). 80% 미만 대상은 테스트로 보강하고, 생성 · 구성 코드 제외만 사유와 함께 허용한다. AppHost가 MigrationService에 `DOTNET_ENVIRONMENT=Development`를 주입하고 AppHost 단위 테스트(주입 1, 기존 Api 환경 유지 1)를 먼저 작성해 통과한다(BL-110). `Error.cs` remarks의 파생 차단 문구를 CS8878 사실(protected 복사 생성자)에 맞게 고친다(TD-016 코드분). Aspire 스모크 테스트는 도입하지 않고 근거와 재도입 조건을 새 BL로 기록한다. build 경고 0, `dotnet format --verify-no-changes`, 전체 테스트가 통과한다 | 없음 | done | d699f27, 8957c06 |
| S04-T02 | 기준 문서 갱신 1: 구조 · 데이터 (clean-architecture, database, testing-strategy, tech-stack, package-versions, 아키텍처 문서 3개) | FR-11 | clean-architecture 저장소 구조 트리의 경로가 모두 실제로 존재하고(git ls-files · `test -e` 대조) Gateway · `deploy/` 제거, Outbox 보류가 반영된다(BL-048). 의존성 규칙 표(Api · MigrationService 행 포함)와 testing-strategy 아키텍처 테스트 절이 아키텍처 테스트 규칙 목록과 1:1이고 ClassesAreSealed의 현재 범위가 적힌다(BL-087, BL-090 기록). database.md ERD · Employee 코드 표가 InitialCreate 스냅샷과 일치하고, xmin 설명이 구현(IsConcurrencyToken + OnAddOrUpdate)과 맞으며, 임시 문구가 확정되고 초기화 절차에 `AppHost:OtlpApiKey`가 들어간다(BL-087, BL-100). testing-strategy 커버리지 절에 T01 대상과 스모크 미도입, 공통 기반 `RequestValidator`가 반영된다(BL-066, BL-068). tech-stack · package-versions의 남은 🟡는 처리 방식 표에만 있고, package-versions에 전이 참조 한 줄(TD-026), 아키텍처 문서 3개의 보류 🟡가 ADR-0023 링크로 바뀐다(BL-038). 바꾼 문서는 `updated` · 변경 이력을 갱신하고 check-docs 결함이 기준선보다 늘지 않는다 | T01 | done | bd368f3, bab2cd9, 1070ad4, 9ad8092, 2736241 |
| S04-T05 | 기준 문서 갱신 2: 규칙 · 관측 (coding-conventions, logging-observability, error-codes) | FR-07, FR-11 | error-codes 표와 코드 상수를 스크래치 대조 스크립트로 양방향 비교해 (코드, ErrorType, 이름) 누락 0 · 초과 0이고, 로그 이벤트 ID도 일치한다. coding-conventions DDD 규칙에 "Aggregate 불변식 위반은 예외, 입력 검증 실패는 Result" 경계 문장이 있고 Email · CQRS · Handler 예시(`IIdGenerator` 이름, SaveChanges 책임)가 코드와 일치한다(BL-089, BL-039). coding-conventions에 경고 억제 규칙 · 승인 목록(BL-055), 한 파일 한 형식을 테스트 코드에도 예외 없이 적용(BL-069), 외부 규약상 0이 의미 있는 internal enum 예외(BL-091), `AddHostedService` 명시 등록 허용(BL-092), Error 파생 문구 정정(TD-016 문서분)이 반영된다. logging-observability에 Aspire 연동(Serilog → OTLP), 20001 템플릿 정정(BL-089), 9003 = Warning 예시(BL-105), DB 정지 때 약 15초 뒤 503 실측(BL-108 기록), `EnableSensitiveDataLogging` 미구현 · 꺼짐 상태(BL-094 기록)가 반영된다. 바꾼 문서는 `updated` · 변경 이력을 갱신하고 check-docs 결함이 기준선보다 늘지 않는다 | T01 | done | 55eecfd, bd26185, 7e2f789 |
| S04-T03 | `todo` 문서 작성: local-setup, configuration, environments, ci-cd, troubleshooting (문서 전용) | FR-11, NFR-04, NFR-06 | 5개 문서가 템플릿으로 `draft` 이상 작성되고, 명령마다 셸(PowerShell / Git Bash)을 표시하며 비밀 값은 자리표시자만 쓴다. local-setup 사전 준비에 SDK 버전, Docker 최소 Engine API 1.44 또는 `DOCKER_API_VERSION=1.43`(BL-102), 짧은 경로 · MAX_PATH(BL-049), `dotnet tool restore`, dev-certs 신뢰 · 확인 명령과 http 프로필 대안(BL-099)이 있다. 초기화 절차는 이름 있는 볼륨만 지우고 user-secrets(Parameters 2키, `AppHost:OtlpApiKey`, `Aspire:VersionCheck:*`)를 함께 지우며, `dotnet ef`는 dba가 허용한 명령만 싣는다(BL-100, BL-097). configuration 키 목록이 appsettings · AppHost 주입 코드와 grep으로 일치하고, environments에 MigrationService Development 주입과 opt-in 키 기본 false가 적힌다(BL-110, BL-094). ci-cd에 워크플로 단계, 테스트 실패 시 Coverage report 건너뜀(BL-062), 커버리지 보고 위치 · 대상, 수집기 없음 메시지(TD-024), 메서드 커버리지 표시(BL-060)가 있고, troubleshooting에 MigrationService Waiting(BL-017), 42P04 · 3D000 잡음(BL-096), 대시보드 OTLP 0건(BL-099), Testcontainers 연결 실패(BL-102)가 있다. tester가 local-setup 명령을 두 셸에서 실행한 결과를 기록하고 check-docs 결함이 기준선보다 늘지 않는다 | T01, S01-T07 | done | 1935a93, 7a3e6a8, 03b20b2, a3332cc |
| S04-T04 | 인수 증빙 1: 새 환경 재현 · 대시보드 증빙 | FR-03, FR-08, FR-11, NFR-04 | clone 전에 T01 · T02 · T05 · T03 커밋이 끝나 있고 clone HEAD = 토픽 HEAD를 기록한다. `docker ps`로 기존 AppHost · 컨테이너 중지를 확인하고 삭제 대상 목록(이름 있는 볼륨 `emergency-hub-postgres-data`, 이 AppHost의 user-secrets)을 진행 기록에 남긴 뒤 삭제하며, 전후 `docker volume ls`로 익명 볼륨 2개가 남아 있음을 대조한다. `C:\eh-s04`에서 local-setup만 보고 AppHost 명령 1개로 PostgreSQL → 마이그레이션(종료 코드 0) → Api를 기동하고 `/health/ready` 200과 등록 → 조회 HTTP 응답을 이 문서 증빙 절에 남긴다. `dotnet dev-certs https --check --trust` 출력과 대시보드 structuredlogs · traces DOM 덤프로 로그 · 트레이스 수신을 증빙하고, 스크린샷은 사용자 추가 항목으로 표시한다. database.md psql 확인 1~11 결과와 재시작 때 42P04 한 쌍 외 리소스 로그 오류 0을 기록한다. 재현 차이점 표(warm 항목, UserSecretsId · 볼륨 이름 공유, 원래 트리도 새 비밀 쌍 사용)와 `C:\eh-s04` 삭제 확인을 기록한다 | T01, T02, T05, T03 | done | 8fb229d, dbeadd5, bcf55f3, 8672595 |
| S04-T06 | 인수 증빙 2: CI 실패 표시 확인 · FR / NFR 증빙 표 | FR-10, NFR-07, FR-01~11 · NFR-01~07 | 토픽 HEAD에서 `ci-check/s04-fail`을 만들고 기존 단위 테스트 프로젝트에 Assert 실패 1건(컴파일 · format 통과)만 커밋해 push한 뒤, `develop` 대상 Draft PR에서 Test 단계 실패 · PR 실패 표시 · 실행 링크를 기록한다. 실패 실행에서 Coverage report 단계 skipped와 trx · container-logs 아티팩트를 확인한다. PR을 병합하지 않고 닫고 원격 브랜치 `ci-check/s04-fail`을 삭제한 뒤, 원격 · 로컬 `ci-check/*` 0개, 토픽 브랜치에 실패 커밋 없음, PR CLOSED · merged false를 명령 출력으로 확인한다. 이 문서 증빙 절에 FR 11 · NFR 7 전 항목 ↔ 증빙(테스트 이름 또는 Trait 집계, CI 실행 링크, 문서 경로, 재현 기록) 표가 있고 빈 칸이 없다. NFR-07 · 토픽 PR 통과 행은 "종료 push 뒤 판정"으로 두고 DoD에서 채운다 | T04 | done | 5556aa7, 77ca927, f442c70 |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

실행 순서: T01 → T02 → T05 → T03 → T04 → T06

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S04-T01 | 해당 없음 | Include 조정, BL-110 TDD(AppHost 단위 테스트 먼저), `Error.cs` remarks, 필요 시 보강 테스트, 스모크 BL 기록 | 표준 진입 점검(경고 0, format, 테스트 먼저, AppHost 테스트 성공 1 · 기존 유지 1) | 커버리지 실행, cobertura 파일 수, 어셈블리별 라인 · 분기, AppHost 실기동으로 MigrationService Environment=Development · 종료 코드 0(`DOCKER_API_VERSION=1.43`) |
| S04-T02 | database.md · ERD ↔ InitialCreate 대조, 임시 문구 정리 | 나머지 문서 수정 | 문서 ↔ 코드 대조(경로, 규칙 이름), 링크 · frontmatter | 트리 경로 `test -e`, 의존성 규칙 표 ↔ 아키텍처 테스트 목록, check-docs 결함 수 비교 |
| S04-T05 | 해당 없음 | 작성, error-codes 대조용 상수 목록 제공 | 규칙 문장 판정 가능성, 예시 ↔ 코드 | 스크래치 Node 대조 스크립트 2개(에러 코드 양방향, 로그 이벤트 ID), check-docs |
| S04-T03 | DB 초기화 · `dotnet ef` 허용 / 금지 명령 검토 | 작성 | 템플릿 · 링크 · 명령 정확성 · 셸 표시 · 비밀 자리표시자 | local-setup 명령 두 셸 실행(볼륨 삭제는 T04에서만), configuration 키 grep 대조 |
| S04-T04 | 재현 절차 · 판정 기준(psql 1~11, 42P04 한 쌍) 확인 | 증빙 절 틀 준비 | 증빙 항목 ↔ PRD 인수 조건(FR-03 · 08 · 11, NFR-04) | 재현 실행 · 기록, DOM 덤프, `C:\eh-s04` 삭제 |
| S04-T06 | 해당 없음 | 실패 테스트 파일(임시 브랜치 전용 커밋), 증빙 표 초안 | 증빙 표 ↔ PRD 인수 조건 전 항목, 빈 칸 0 | 임시 브랜치 push · Draft PR · 실패 확인 5항목 · 정리 확인(원격 브랜치 삭제 포함) |

## 완료 기준 (DoD)

- [x] 모든 작업이 `done`이거나 백로그로 이관되었다
- [x] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다
- [x] 관련 위키 문서(API, DB)를 갱신했다. 이벤트 문서는 해당 없음(도메인 이벤트는 수집만, 디스패치는 이후 토픽)
- [x] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 PR CI 통과와 소요 시간 10분 이내를 기록했다(NFR-07). 같은 측정으로 BL-059를 판단했다
- [ ] T06 증빙 표의 "종료 push 뒤 판정" 행을 채웠다
- [ ] 토픽 브랜치를 push하고 `sprint/S04` 태그를 붙였다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| 2026-09-28 | S04-T01 | dba | 해당 없음 | 파이프라인 표 dba 열 해당 없음, 호출 생략 |
| 2026-09-28 | S04-T01 | developer | PASS | Include 6개(BB 4 + Employee Domain / Application, BL-068), cobertura 12 = 수집 프로젝트 12(BL-063), ArchitectureTests 수집 제외 확정(TD-024). 커버리지 BB.Api 라인 98% · 분기 92.3%, BB.Application 100 · 100, BB.Domain 100 · 100, BB.Infrastructure 99.7 · 91.7, Employee.Application 100 · 100, Employee.Domain 100 · 100, 합계 라인 99.4% (875/880) · 분기 94.6% (333/352), 보강 불필요. BL-110 ServiceEnvironmentTests 4건 먼저(Red 2 → Green), WithEnvironment 주입. Error.cs remarks CS8878 정정(TD-016 코드분). 스모크 미도입 BL-111. 경고 0, format 통과, 테스트 1537 통과 · 1 건너뜀. handoff: T02 testing-strategy 커버리지 · CI 절, T03 ci-cd 수집기 없음 메시지 원문 · environments BL-110, T05 coding-conventions:61 |
| 2026-09-28 | S04-T01 | reviewer | PASS | 표준 진입 점검 15항목 통과(경고 0, 1537 통과 · 1 건너뜀, format, ServiceEnvironmentTests 성공 · 기존 유지 · 실패 · 엣지). handoff: T03 environments 문구, T05 Error 파생 문구 |
| 2026-09-28 | S04-T01 | tester | PASS | CI와 같은 커버리지 명령: 1537 통과 · 1 건너뜀, cobertura 12 = 수집 프로젝트 12(MultiReport 12x, Assemblies 6), 어셈블리별 수치 developer 측정과 동일(합계 라인 99.4% · 분기 94.6% · 메서드 99.5%). AppHost 실기동(DOCKER_API_VERSION=1.43, http): employee-migrations Finished · exitCode=0, spec DOTNET_ENVIRONMENT=Development, 로그 Hosting environment Development · 20901, Api /health/ready 200 · DOTNET_ENVIRONMENT 미주입(기존 유지). 종료 후 프로젝트 컨테이너 0, 볼륨 목록 시작 전과 동일(익명 2개 유지) |
| 2026-09-28 | S04-T02 | dba | PASS | database.md: ERD 아래 InitialCreate 대조 표 추가(varchar 100/254, smallint + ck_ IN (1, 2), timestamptz, ux_employees_email, pk_employees), xmin 설명을 구현(IsConcurrencyToken + OnAddOrUpdate, IsRowVersion과 같은 구성)에 맞추고 '마이그레이션 C#에는 있고 생성 SQL에는 없음'으로 정정(BL-087), 이미지 태그 · 판정 기준 · 이력 테이블 ADR 문구 확정, 초기화 절차에 user-secrets 4종 · 볼륨 함께(BL-100 · 097), MigrationService Development(BL-110). check-docs 4건(기준선). handoff: T03 앵커 #초기화-볼륨--user-secrets 등, T04 판정 기준 |
| 2026-09-28 | S04-T02 | developer | PASS | clean-architecture 저장소 트리 실경로화(경로 94개 존재 · 누락 0, BL-048), 의존성 규칙 표 신설(Api · MigrationService 행) · 아키텍처 테스트 규칙 25개(의존성 10 · 컨벤션 12 · 주입 3)와 testing-strategy 표 1:1 누락 0 · 초과 0, DependencyRules.cs Source 문자열 갱신(BL-087, 빌드 경고 0 · format · ArchitectureTests 99 통과 · 1 건너뜀). BL-090 실측: ClassesAreSealed는 internal 생성 형식도 잡음 → 범위 기록 · BL-090 비고 정정. BL-066 Validator 기반 문구. testing-strategy 커버리지 대상 6개 · 스모크 미도입 BL-111(BL-068). tech-stack 🟡 0(S01-T04 완료 근거) · 커버리지 행 갱신, package-versions TD-026 행, 아키텍처 문서 3개 보류 🟡 → ADR-0023(BL-038). check-docs 4건. 새 BL-112(보류 아닌 🟡 표기 통일). handoff: T05 sealed 범위 · Validator 예시, T03 앵커 |
| 2026-09-28 | S04-T02 | reviewer | REJECT → developer | 반려 1회. testing-strategy 필수 테스트 케이스 절(약 40~41행)에 완료 조건에 없는 판정 기준(설정 · 구성 코드의 실패 = 부정 범위)을 추가: 원본 agents.md 테스트 범위 절과 어긋나고 reviewer 점검 3번 기준을 완화, 예시도 사실과 다름. 나머지 대조(트리 경로 누락 0, 규칙 25개 1:1, database.md ↔ 코드, 커버리지 6개, frontmatter, check-docs 4건, ADR 불변)는 통과. 원인: 계획 인계 메모의 '선택' 항목. 새 BL-113 |
| 2026-09-28 | S04-T02 | developer | PASS | 재작업: testing-strategy 부정 범위 해석 문단과 변경 이력 문구 삭제(grep 0건), tech-stack 변경 이력 날짜순 정렬. check-docs 4건 |
| 2026-09-28 | S04-T02 | reviewer | PASS | 재판정: 반려 사유 해소('부정 범위' grep 0), 재작업 범위가 반려 부분 · tech-stack 변경 이력 정렬뿐, check-docs 4건, ADR 불변. handoff: T05 해석 기준 임의 추가 금지(BL-113) |
| 2026-09-28 | S04-T02 | tester | REJECT → dba | 반려 2회. database.md 약 443행 '따옴표 식별자는 "__EFMigrationsHistory"뿐'이 idempotent SQL 실측("__EFMigrationsHistory" 5, "migration_id" 3)과 불일치(bd368f3). 나머지 점검 통과: 트리 94 누락 0 · csproj 24 역방향 누락 0, 규칙 25 1:1, InitialCreate · 스냅샷 · enum 대조, Include 6, 🟡 0, check-docs 4, 링크, ADR 불변, 테스트 1537 통과 · 1 건너뜀(DOCKER_API_VERSION=1.43) |
| 2026-09-28 | S04-T02 | dba | PASS | 재작업: database.md 443행 범위를 DDL · 이력 INSERT로 좁히고 조회 조건 3곳의 "migration_id" 따옴표 문장 추가(실측 "__EFMigrationsHistory" 5 · "migration_id" 3). 다른 SQL 인용 문장 대조 일치. check-docs 4건 |
| 2026-09-28 | S04-T02 | developer | PASS | dba 재작업 반영 확인: developer 담당 문서의 이력 테이블 · 따옴표 설명(testing-strategy Q10~Q12, 98행)이 database.md 443행과 일치, 변경 없음 |
| 2026-09-28 | S04-T02 | reviewer | PASS | 재판정: 443행이 idempotent.sql 실측과 일치, 재작업 범위는 443행 · 변경 이력 1행뿐, ADR 불변, check-docs 4건 |
| 2026-09-28 | S04-T02 | tester | PASS | 재점검: idempotent SQL 재실측("__EFMigrationsHistory" 5, "migration_id" 3 조회 조건만)이 443행과 일치, 재작업 범위 database.md 443행 · 변경 이력뿐, 이전 6개 항목 결과 재사용, check-docs 4건 |
| 2026-09-28 | S04-T05 | dba | 해당 없음 | 파이프라인 표 dba 열 해당 없음, 호출 생략 |
| 2026-09-28 | S04-T05 | developer | PASS | coding-conventions: 실패 처리 경계 문장(요청 값만으로 판정 가능 = Validator · Result, Aggregate 재검사 위반 = 예외, 저장 데이터 · 상태 의존 = Result), Email.Create 값 객체 예시 제거(BL-089), CQRS · Repository 예시를 실제 코드와 diff 일치(BL-039), 테스트 코드 한 파일 한 형식(BL-069), 0 예약 예외(BL-091), AddHostedService 허용(BL-092), Error 파생 문구(TD-016), sealed · Validator 설명은 testing-strategy 링크, 경고 억제 규칙 절 · 승인 목록 20건(BL-055). logging-observability: Aspire 연동 절, 20001 템플릿 정정, 9003 = 301 Warning 예시(BL-105), BL-094 · 108 기록. error-codes: 에러 코드 20 · 로그 이벤트 15 양방향 대조 누락 0 · 초과 0 · 불일치 0(문서 변경 없음). check-docs 4건. 확인 요청: 승인 목록 Employee CA1812 3건은 S03 개별 승인 기록 없음 |
| 2026-09-28 | S04-T05 | reviewer | REJECT → developer | 반려 1회(형식). coding-conventions 162 · 221행 예시 도입 문장이 실제 편집(설명 주석 2줄 추가, using · namespace 제외, 두 파일 합침)과 다름, 395행 승인 절차 문장의 주체 · 순서 판정 불가. 승인 결정: Employee CA1812 3건(RegisterEmployeeCommandHandler, RegisterEmployeeCommandValidator, GetEmployeeByIdQueryHandler)은 S04-T05 reviewer 사후 승인(S03-T01 도입, 기존 CA1812 DI 생성과 같은 사유) → 406행 승인 기록 칸 수정. 나머지(에러 코드 · 로그 이벤트 대조, logging-observability, BL-069 · 091 · 092 · TD-016) 통과 |
| 2026-09-28 | S04-T05 | developer | PASS | 재작업: CQRS 예시 설명 주석 2줄을 코드 블록 밖 목록으로, 도입 문장을 실제 발췌 범위(두 파일, using · namespace · XML 주석 · SuppressMessage 제외)로, Repository 도입 문장도 같게(코드 블록 ↔ 소스 diff 차이 없음), 승인 절차 문장 주체 · 순서 명시, Employee CA1812 3건 승인 기록 칸 'S04-T05 reviewer(사후 승인, S03-T01 도입)'. check-docs 4건 |
| 2026-09-28 | S04-T05 | reviewer | PASS | 재판정: 반려 사유 3건 해소, PASS 부분 불변. 승인 기록: Employee.Application CA1812 억제 3건(RegisterEmployeeCommandHandler.cs, RegisterEmployeeCommandValidator.cs, GetEmployeeByIdQueryHandler.cs, S03-T01 도입)을 S04-T05 reviewer가 사후 승인(AddConventionalServices 어셈블리 검색 DI 생성, 기존 CA1812 승인과 같은 사유) |
| 2026-09-28 | S04-T05 | tester | PASS | 독립 대조 스크립트(여러 줄 선언 허용 · 완전성 검사): 에러 코드 20 · 로그 이벤트 15 fail=0, 음성 대조(결함 3개 삽입) 3건 검출. developer 스크립트 재실행 일치. 경고 억제 19개 파일 20건 = 승인 목록, NoWarn · #pragma는 생성 마이그레이션 612, 618만. 코드 블록 ↔ 소스 diff 차이 0. logging-observability 사실 문장(21180 / 19180, 20001 템플릿, 301 Warning, 9003, EnableSensitiveDataLogging 호출 0) 코드 확인. check-docs 4건 |
| 2026-09-28 | S04-T03 | dba | PASS | local-setup '## DB 마이그레이션'(도구 매니페스트 dotnet-ef 8.0.31, --context 필수, 허용 5 · 금지 7, 두 셸 실측 예), '## 초기화 (볼륨 · user-secrets)'(이름 있는 볼륨만 · prune 금지, 키 4종, 셸별 절차, ASPIRE_VERSION_CHECK_DISABLED 안내 BL-097). troubleshooting DB 연결 4행(28P01, Waiting BL-017, 42P04 · 3D000 BL-096) · Testcontainers 1행(BL-102). database.md 앵커 링크. 삭제 · 초기화 명령 미실행. has-pending-model-changes를 허용 명령에 추가(dba 판단). check-docs 4건 |
| 2026-09-28 | S04-T03 | developer | PASS | 5개 문서 draft(셸 표시, 비밀 자리표시자). local-setup: 사전 준비(SDK · Docker API · 도구 매니페스트 · dev-certs 확인 / 신뢰 · http 대안), 클론(MAX_PATH), Aspire 구성, 실행 명령 1개, 대시보드, 중지, 헬스 → 등록 → 조회 실측(POST 201 · GET 200 · 재등록 409), dba 절 유지. user-secrets 비밀번호는 사전 설정 없음(첫 실행 생성 · persist, database.md 결정)으로 작성. configuration 키 31개 대조 누락 0, environments(BL-110 · 094), ci-cd(15단계, BL-062 · 060 · TD-024 원문), troubleshooting 확장, prerequisites SDK · dotnet-ef 정정, database.md · logging-observability 앵커. check-docs 4건. 참고: 이 PC dev-certs 미신뢰(--check --trust 종료 코드 7) |
| 2026-09-28 | S04-T03 | reviewer | REJECT → developer | 반려 1회(형식). local-setup '등록 → 조회' 명령의 email(hong.gildong@example.com)과 기대 결과(hong.gildong.t03@example.com) · 243행 설명 불일치. 나머지(템플릿 · 셸 · 자리표시자, BL / TD 12건, configuration · ci-cd 대조, 볼륨 이름 지정, 새 규칙 없음, user-secrets 절 · has-pending-model-changes 적절, check-docs 4) 통과 |
| 2026-09-28 | S04-T03 | developer | PASS | 재작업: 등록 명령 email을 Hong.Gildong@Example.com(두 셸), 기대 결과를 hong.gildong@example.com으로 맞추고 243행 설명 정정, 연속 빈 줄 정리. check-docs 4건 |
| 2026-09-28 | S04-T03 | reviewer | PASS | 재판정: email 입력 · 기대 결과 일치, 다른 부분 불변(공백 · 변경 이력만), check-docs 4건 |
| 2026-09-28 | S04-T03 | tester | PASS | 두 셸(PowerShell 5.1 · Git Bash) 문서 그대로 실행: 사전 준비(SDK 8.0.425, Docker API 1.43, tool restore, dev-certs --check --trust exit 7), user-secrets 키 5개(값 미출력), AppHost http · https 기동 · /health/ready 200, 등록 409 · 23001(기존 이메일) · 새 email 201 → GET 200 소문자 정규화, 중지 후 컨테이너 · 프로세스 0 · 볼륨 유지, migrations script --idempotent · has-pending-model-changes exit 0, 통합 테스트 DOCKER_API_VERSION=1.43 두 셸 문법 exit 0. configuration 키 grep 일치. 미실측 3건(No secrets configured, https OTLP 0건, 초기화 ②③)은 T04로. migrations remove는 --help만(과장 없는 문구). 정리 후 볼륨 목록 diff 없음. 새 BL-114 |
| 2026-09-28 | S04-T04 | 계획 변경 | 승인 | 오케스트레이션 세션 결정: 이 PC 개발 인증서 미신뢰(--check --trust exit 7)라 T04 재현 · FR-03 대시보드 증빙은 http 프로필, dev-certs --trust 실행 안 함, https는 한 번 띄워 OTLP 0건 확인(실측 못 하면 미실측), 사용자 https 재확인 절차는 결과 리뷰 사용자 확인 사항에. PRD 변경 이력 · 인수 조건 대비 변경 표 FR-03 보충 |
| 2026-09-28 | S04-T04 | dba | PASS | 재현 절차 · 판정 기준 확정(명령 미실행): 로컬 저장소 clone(원격이 24커밋 뒤처짐) → C:\eh-s04에서 local-setup 초기화 ①~③ 한 번 → http 1회차(새 볼륨 psql 1~11, 42P04 0 기대) → 2회차 재시작(42P04 한 쌍 1, 등록 id 조회 200) → https 회차 선택. 삭제 대상: 볼륨 emergency-hub-postgres-data, UserSecretsId 4264c4b6-… 키 전부(이름만 기록). 보호 대상 3시점 diff. psql은 Git Bash heredoc · 컨테이너 환경 변수 비밀번호. 새 BL-115 · 116(판정 문구 · psql 명령 틀 보완) |
| 2026-09-28 | S04-T04 | developer | PASS | 스프린트 문서 '## 증빙' 절 틀: T04 하위 절 10개(환경 · 시작 조건, 삭제 전 대상 목록, 실행 절차 1~14단계, psql 1~11, 회차별 서버 로그, HTTP, 대시보드, https 확인, 재현 차이점, 정리 확인), 값 칸 '(tester 기록)', T06 자리. check-docs 4건 |
| 2026-09-28 | S04-T04 | reviewer | REJECT → developer | 반려 1회(형식). 증빙 틀에 FR-03 시작 순서(PostgreSQL → 마이그레이션 종료 코드 0 → Api) 판정 칸 없음, 회차별 employee-migrations 종료 코드 · 앱 리소스 Error · Critical 수 칸 없음. 나머지(기대값 ↔ 원본, 비밀 칸 없음, http 결정 · 인수 조건 대비 변경 반영, check-docs 4) 통과 |
| 2026-09-28 | S04-T04 | developer | PASS | 재작업: 대시보드 증빙에 시작 순서 행(local-setup 원문 순서), 회차별 서버 로그 표에 employee-migrations 종료 코드 · 앱 리소스 Error · Critical 열(판정은 두 기준 모두), 선행 작업 커밋 포함 행, '기타 ERROR · FATAL · PANIC', 7행 UserSecretsId 공유 안내. check-docs 4건 |
| 2026-09-28 | S04-T04 | reviewer | PASS | 재판정: 시작 순서 행 · 회차별 앱 리소스 열로 반려 사유 해소, 다른 부분 불변, 표 구조 결함 0, check-docs 4건 |
| 2026-09-28 | S04-T04 | tester | PASS | 재현 실행: 삭제 전 목록 기록 → C:\eh-s04 로컬 clone(HEAD bcf55f3 = 토픽 HEAD) → 문서 초기화 ①~③(볼륨 emergency-hub-postgres-data 이름 지정 삭제, user-secrets clear, 'No secrets configured' 확인) → http 1회차: 시작 순서 postgres → migrations(exitCode 0) → api, /health/ready 200, 등록 201 · 조회 200, structuredlogs 27 · traces 10(CDP DOM 덤프), psql 1~11 기대값 일치 → 2회차 재시작 · 3회차 https: 42P04 한 쌍 1 · 나머지 오류 0, 같은 id 조회 200, https 미신뢰(exit 7)에서 OTLP 로그 0 · 추적 0. 보호 대상(익명 볼륨 2, backend_postgres_data, vital-* · elastic8 · mysql) 불변, C:\eh-s04 삭제. 편차: 1회차 employee-api 첫 헬스 검사 Unhealthy Error 2건(서버 로그 오류 없음, 이후 Healthy) — 완료 조건(재시작 때 오류 0)과 FR-03은 충족, 틀의 1회차 앱 리소스 기준 불일치로 기록 → 새 BL-117, BL-118(CDP 덤프 방법) |
| 2026-09-28 | S04-T06 | dba | 해당 없음 | 파이프라인 표 dba 열 해당 없음, 호출 생략 |
| 2026-09-28 | S04-T06 | developer | PASS | 실패 테스트 파일은 스크래치에만(t06/CiCheck/CiFailureCheckTests.cs → tests/BuildingBlocks/EmergencyHub.BuildingBlocks.Domain.UnitTests/CiCheck/, Assert 실패 1건, 로컬 build 경고 0 · format 통과 · 실패 1 · 통과 215, 작업 트리 원복 확인). 증빙 절: CI 실패 확인 틀(실행 기록 4 · 실패 확인 5 · 정리 확인 5), FR / NFR 증빙 표 18행 빈 셀 0(Trait 재집계 FR-03 10 · 04 1 · 05 11 · 06 27 · 07 8 · 08 12 · 09 20 · 11 2, NFR-02 4 · NFR-06 2), FR-10 · NFR-07은 종료 push 뒤 판정 표시. check-docs 4건 |
| 2026-09-28 | S04-T06 | reviewer | REJECT → developer | 반려 1회(형식). 증빙 표 FR-03 행에 PRD 선택 항목 스모크 처리(미도입, BL-111) 누락. 나머지(18행 · 빈 칸 0, Trait 재집계, CI run 4건, 수치 · 경로 표본, S01~S03 모순 없음, 실패 확인 틀, 실패 테스트 컨벤션 · format) 통과 |
| 2026-09-28 | S04-T06 | developer | PASS | 재작업: FR-03 행 인수 조건 요지에 스모크 선택, 판정에 '스모크 미도입, 재도입 조건은 BL-111(S04-T01)', 표 머리말 Trait 수 단위 한 문장. check-docs 4건 |
| 2026-09-28 | S04-T06 | reviewer | PASS | 재판정: FR-03 행 스모크 처리 반영으로 반려 사유 해소, 다른 부분 불변, 표 8열 18행, check-docs 4건 |
| 2026-09-28 | S04-T06 | tester | PASS | CI 실패 표시 확인: 임시 브랜치 ci-check/s04-fail(1ce433a, 부모 77ca927) Draft PR #9 → run 36350200891 failure(약 3분 28초). Build · Format success, 실패 테스트 CiFailureCheckTests 1건뿐(Domain Failed 1 · Passed 215), Test (coverage) failure, Coverage report skipped, test-results · container-logs 아티팩트, PR 체크 fail. 정리: PR #9 닫음(CLOSED, merged false), 원격 · 로컬 ci-check/* 0, 토픽 브랜치에 1ce433a 없음. 증빙 절 기록 채움, 빈 칸 0. check-docs 4건 |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모:

- **N2 CI 실패 확인 push**: → **해소**. 사전 합의 3에 따라 S04-T06 안에서 임시 브랜치 `ci-check/s04-fail` + `develop` 대상 Draft PR로 확인하고, 병합 없이 닫은 뒤 원격 브랜치를 삭제한다.

### 사전 사용자 결정 (스프린트 시작 전, 2026-09-27)

1. 시작 조건: `sprint/S03` 태그와 깨끗한 작업 트리. 같은 작업 트리에서 다른 세션과 동시 작업 금지
2. 승인 지점은 권고안으로 진행(오케스트레이션 세션이 사용자 권한을 위임받아 판단)
3. N2: T06 안에서 임시 브랜치 · Draft PR 예외 허용, 확인 후 PR 닫기 · 브랜치 삭제
4. 종료 push · 태그: 결과 리뷰 승인 뒤 진행(오케스트레이션 세션의 ③ 승인으로 대체). `/retro` · 병합 · 릴리스는 범위 밖
5. 재현: 로컬 저장소에서 `C:\eh-s04`로 clone, 이름 있는 볼륨 삭제 허용(ADR-0012 리셋 정책), user-secrets는 local-setup대로 재설정, 끝나면 `C:\eh-s04` 삭제
6. 대시보드 스크린샷은 실행 기록으로 대체, 스크린샷은 사용자 추가 항목
7. 재현 기록은 worklog가 아니라 이 문서 증빙 절, `/worklog`에서 링크
8. Aspire 스모크: CI 전체 7분 이하이고 안정적이면 도입, 아니면 BL
9. 커버리지 80% 미만은 보강 기본, 생성 · 구성 코드 제외는 사유 기록
10. BL-038 · 066 · 067 · 069 편입, BL-001은 BL-018 해결 시에만
11. 새 ADR 없음, ADR 후보는 결과 리뷰 목록으로만

S03 인계: 로컬 통합 테스트 `DOCKER_API_VERSION=1.43`(BL-102), https 대시보드 텔레메트리는 dev-certs 신뢰 필요(BL-099), 익명 Docker 볼륨 2개는 건드리지 않음, 회고 개선안은 스킬 · 에이전트 파일에 반영하지 않고 기록만(/retro에서 반영).

### 에이전트 리뷰 요약

- **공통 지적**: (1) 문서 작업(T02 · T03)에 코드 변경 혼재(BL-110 AppHost, TD-016 Error.cs) (2) 목표 "모든 FR / NFR 증빙"에 맞는 증빙 표 작업 없음 (3) T04 CI 10분 조건은 종료 push 뒤에만 판정 가능 (4) Aspire 스모크는 스프린트 안 CI 측정 경로가 없고 개발 볼륨 · persist 비밀을 공유
- **dba**: T04 재현은 볼륨 삭제와 user-secrets clear를 반드시 함께, 판정은 database.md psql 확인 1~11. database.md xmin 설명 불일치(BL-087), 임시 문구 정리. `dotnet ef` 허용 / 금지 명령 목록
- **developer**: BL-068 · 087이 사전 합의 목록에서 누락. 현재 대상 4개는 100%(411/411)라 T01 핵심은 대상 범위 결정. 스모크는 BL 권고
- **reviewer**: 증빙 매트릭스, T02 · T03 편입 BL ID와 판정 방법(grep 양방향, `git ls-files`, 스냅샷 대조) 명시, 규칙 변경 항목의 판정 가능한 문장
- **tester**: 스모크 CI 측정 경로(차단 질문), FR-03 로그 · 트레이스는 DOM 덤프로 증빙, CI 실패 확인 5항목 · 정리 확인 항목

### 결정 (오케스트레이션 세션 승인 2026-09-28)

- 작업 4 → 6: T01을 코드 묶음으로 재정의(BL-110 · TD-016 코드분 이동), T02를 구조 · 데이터 문서로 좁히고 T05(규칙 · 관측 문서) 추가, T03은 문서 전용, T04는 재현 · 대시보드, T06(CI 실패 확인 · FR / NFR 증빙 표) 추가. NFR-07 · 토픽 PR 통과는 DoD로
- 열린 질문(추천안 채택): 스모크 미도입 · BL / BL-068 BuildingBlocks 4개 전체 + Employee Domain / Application(어셈블리별 수치, 대안은 현행 유지) / BL-110 AppHost `WithEnvironment` + 단위 테스트(T01), T03은 기록 / BL-097 기록만(기본값 유지) / user-secrets clear는 합의 5 범위 / T04 https 프로필 + dev-certs 출력 / BL-069 테스트에도 예외 없이 / BL-001 · 018 · 109, TD-014 open 유지
- 추가 조건: T04 삭제 범위는 이름 있는 볼륨 `emergency-hub-postgres-data`와 이 AppHost의 user-secrets로 한정, 삭제 전 대상 목록을 진행 기록에. T06은 PR 닫은 뒤 원격 브랜치 삭제까지 정리 확인에 포함, T01~T05가 임시 브랜치로 먼저 원격에 올라가는 것 허용. T01 보강이 테스트 파일 약 10개 이상이면 진행 전 오케스트레이션 세션에 범위 판단 요청
- ADR 후보(결과 리뷰 목록으로만): 불변식 예외 / Result 경계(BL-089), ADR-0018 보충 RequestValidator(BL-066), 경고 억제 규칙(BL-055), ADR-0011 보충 42P04(BL-096)

### PRD 인수 조건 대비 변경 (PRD 본문 유지, PRD 변경 이력에 해석 기록)

| 대상 | 변경 |
|---|---|
| FR-03 | 스크린샷 대신 실행 기록(리소스 상태, 헬스, 등록 → 조회 HTTP) + 대시보드 DOM 덤프 + dev-certs 확인 출력. 스크린샷은 사용자 추가 항목. 선택 사항인 스모크는 미도입 · BL. 재현 · 대시보드 증빙은 http 프로필(dev-certs 미신뢰, BL-099, 2026-09-28 오케스트레이션 세션 결정), 개발 인증서 신뢰는 하지 않음 |
| FR-10 | 실패 표시는 `ci-check/s04-fail` → `develop` Draft PR로 확인, 병합 없이 닫음 |
| FR-11 | 재현 기록 위치를 worklog에서 이 문서 증빙 절로(`/worklog`에서 링크) |
| NFR-03 | "BuildingBlocks"를 Domain · Application · Infrastructure · Api 4개로 해석, Employee는 Domain / Application |
| NFR-04 | 같은 머신 재현의 warm 항목 한계를 기록, 완전한 클린 증빙은 CI 클린 러너 |
| NFR-07 | 작업 완료 조건이 아니라 DoD(종료 push 뒤 토픽 PR CI)에서 판정 |

### 백로그 / 기술부채 편입

| 작업 | 편입 |
|---|---|
| S04-T01 | BL-063, BL-068, BL-110(코드), TD-016(코드분), TD-024(수집 제외 확정) |
| S04-T02 | BL-038, BL-048, BL-066(문서분), BL-087(BL-090 범위 기록 포함), BL-100(database.md), TD-026 |
| S04-T05 | BL-039, BL-055, BL-069, BL-089, BL-091, BL-092, BL-105, TD-016(문서분), BL-094 · BL-108(기록만) |
| S04-T03 | BL-017, BL-049, BL-060(한 줄, dropped 유지), BL-062, BL-096(기록만), BL-097, BL-099, BL-100, BL-102, BL-110(environments 기록), TD-024(ci-cd) |
| DoD | BL-059(종료 push CI 시간으로 판단) |
| open 유지 | BL-001, BL-018, BL-109, TD-014, BL-090 · 094 · 096 · 108(기록만, 구현은 트리거 대기) |

### 인계 메모 (handoff)

- T01: 로컬 통합 테스트는 `DOCKER_API_VERSION=1.43`. Include 확대 후 cobertura 수와 대상별 수치를 따로 기록. testing-strategy의 스모크 문구는 T02에서 고친다
- T02: dba 정리 대상 database.md 이미지 태그 · 링크 · 초기화 · 판정 · ADR 후보 문구(약 67 · 110 · 116 · 117 · 260 · 314행). BL별 반영 위치 표를 진행 기록에. FR-11의 tech-stack 완료 근거를 명시
- T05: BL-055 승인 목록 원본은 S02 · S03 reviewer 억제 목록. 대조 스크립트는 스크래치에만(커밋 안 함)
- T03: 허용 명령 `dotnet tool restore`(dotnet-ef 8.0.31), `dotnet ef migrations add <Name> --project src/Services/Employee/EmergencyHub.Employee.Infrastructure --context EmployeeDbContext`, `dotnet ef migrations script --idempotent --context EmployeeDbContext`, `<ID>.Sealed.cs` 추가. 금지 명령 `dotnet ef database update`(ADR-0012 · TD-011), 셸에 비밀번호가 든 연결 문자열 설정, push 뒤 `migrations remove`(더미 연결 동작은 tester가 확인). 볼륨 삭제는 T04에서만
- T04: `dotnet user-secrets clear --project src/Aspire/EmergencyHub.AppHost`, 볼륨 `emergency-hub-postgres-data`. `C:\eh-s04` 삭제 후 원래 트리에서 다시 초기화하지 않는다
- T06: 실패 확인 5항목(단언 실패 1건 · 컴파일 성공, Test 단계 failure, Coverage report skipped, trx · container-logs 아티팩트, PR 실패 표시), 정리 확인(원격 브랜치 삭제, 원격 · 로컬 `ci-check/*` 0, 토픽에 실패 커밋 없음, PR CLOSED · merged false). FR Trait 집계(계획 리뷰 시점): FR-03 9 · 04 1 · 05 11 · 06 27 · 07 8 · 08 12 · 09 20 · 11 2
- check-docs 기준선(계획 확정 시점): 결함 4건(raw 로그 frontmatter, BL-018)

## 증빙

> 인수 증빙 기록입니다. 값 칸은 실행한 단계(tester)가 채웁니다. 비밀 값(비밀번호, 연결 문자열, user-secrets 값, 대시보드 토큰)은 적지 않습니다. user-secrets는 키 이름만, 대시보드 로그인 URL은 `login?t=<토큰>`으로 적습니다.

### S04-T04 새 환경 재현

절차 원본: [로컬 개발 환경 구성](../../01-getting-started/local-setup.md), psql 확인 원본: [데이터베이스 로컬 DB 구성](../../04-development/database.md#로컬-db-구성-apphost).

#### 환경 · 시작 조건

| 항목 | 값 |
|---|---|
| 날짜 | 2026-09-28(KST 05:36~05:47, 대시보드 · DCP 시각은 UTC 2026-09-27 20:3x~20:4x) |
| 토픽 HEAD(원래 트리 `git rev-parse HEAD`) | `bcf55f35188ce8345c54a5403d3fa9f3706f7a1e`(커밋되지 않은 변경은 이 스프린트 문서 진행 기록 · raw 로그뿐) |
| clone 원본 · 경로 | 로컬 저장소 `C:\00. src\02. src\emergency-hub` → `C:\eh-s04`(`--branch feature/prd-001-foundation`, PowerShell) |
| clone HEAD(`git -C C:\eh-s04 rev-parse HEAD`) | `bcf55f35188ce8345c54a5403d3fa9f3706f7a1e`(토픽 HEAD와 같음) |
| init 스크립트 줄바꿈(`git ls-files --eol`, `01-create-employee-app-role.sh`) | `i/lf w/lf attr/text=auto eol=lf` |
| SDK(`dotnet --version`, clone 루트) | `8.0.425` |
| Docker Engine / API | Engine `24.0.7` / API `1.43`(AppHost 셸에 `DOCKER_API_VERSION` 없음: 셸 · 사용자 · 머신 환경 변수 모두 비어 있음 확인) |
| dev-certs 상태(`dotnet dev-certs https --check --trust` 종료 코드) | `7`(미신뢰, 출력 `The following certificates were found, but none of them is trusted: 1 certificate`) |
| 프로필 | 1 · 2회차 `http`(`--launch-profile http`), 3회차 기본 `https`(`--launch-profile` 없음) |
| 원래 트리에서 실행 중인 AppHost 없음 | 확인. `tasklist`에 `EmergencyHub.AppHost` · `dcp` 없음, `docker ps -a --filter volume=emergency-hub-postgres-data` 출력 없음 |
| 선행 작업 T01 · T02 · T05 · T03 커밋 포함(`git merge-base --is-ancestor <커밋> HEAD` 또는 `git log`로 확인, clone HEAD 기준) | 모두 포함(clone에서 `git merge-base --is-ancestor` 종료 코드 0): T01 `d699f27` · `8991b6c`, T02 `9141e31`, T05 `ccf2c85`, T03 `fcc6a0f` |

#### 삭제 전 대상 목록

| 구분 | 대상 | 확인 결과 |
|---|---|---|
| 볼륨 이름(삭제 대상) | `emergency-hub-postgres-data` | `docker volume ls --filter name=emergency-hub-postgres-data` 출력이 정확히 이 이름 1개. 쓰는 컨테이너 없음 |
| user-secrets 키 이름(삭제 대상, 값 미기록) | `UserSecretsId` 4264c4b6-2b53-4765-93fd-f38191980ff4의 키 전부 | 5개: `AppHost:OtlpApiKey`, `Aspire:VersionCheck:KnownLatestVersion`, `Aspire:VersionCheck:LastCheckDate`, `Parameters:employee-app-password`, `Parameters:postgres-password`(`IgnoreVersion` 없음) |
| 보호 대상 볼륨 | 익명 볼륨, `backend_postgres_data` | 익명 `5d710796…95d2f`, 익명 `fff0aace…591f8`, `backend_postgres_data` |
| 보호 대상 컨테이너 | `vital-*` | `vital-api`, `vital-db`. 그 밖에 이 프로젝트가 아닌 `elastic8`, `mysql`도 있어 보호 대상에 포함 |
| 목록 스냅숏 파일(스크래치) | volumes-0 · containers-0 | `t04/volumes-0.txt`(4개) · `t04/containers-0.txt`(4개) · `t04/secrets-keys-0.txt`, 삭제 직후 volumes-1 · containers-1, 마지막 volumes-2 · containers-2 |

#### 실행 절차와 결과

local-setup 절 순서대로 적습니다. 명령은 문서의 명령 그대로 쓰고, 다르게 실행했으면 결과 칸에 차이를 적습니다.

| # | 단계(local-setup 절) | 명령 | 셸 | 결과 |
|---|---|---|---|---|
| 1 | 사전 준비 | `dotnet --version`, `docker version --format '{{.Server.APIVersion}}'`, `dotnet dev-certs https --check --trust` | PowerShell 5.1.26100 · Git Bash | `8.0.425`, `1.43`, 종료 코드 `7`(미신뢰, local-setup 출력 예와 같음) |
| 2 | 저장소 클론 | `git clone --branch feature/prd-001-foundation <원본> C:\eh-s04` | PowerShell | 성공(파일 730개). 원본은 GitHub URL 대신 로컬 저장소 경로(재현 차이점 표) |
| 3 | 저장소 클론(도구) | `dotnet tool restore` | PowerShell | 종료 코드 0, `dotnet-ef` 8.0.31 · `reportgenerator` 5.5.11 복원 |
| 4 | 초기화 ① 컨테이너 없음 확인 | `docker ps -a --filter volume=emergency-hub-postgres-data --format '{{.Names}}'` | PowerShell | 출력 없음 |
| 5 | 초기화 ② 볼륨 확인 · 삭제 | `docker volume ls --filter name=emergency-hub-postgres-data --format '{{.Name}}'`, `docker volume rm emergency-hub-postgres-data` | PowerShell | `emergency-hub-postgres-data` 1개 확인 → 삭제 종료 코드 0. volumes-0 → 1 diff는 이 볼륨 삭제 1건뿐 |
| 6 | 초기화 ③ user-secrets clear · 키 이름 확인 | `dotnet user-secrets clear --project src/Aspire/EmergencyHub.AppHost`, local-setup ③의 키 이름 확인 명령 | PowerShell | clear 종료 코드 0, 확인 명령 출력 `No secrets configured for this application.`(키 0개). C:\eh-s04에서 성공해 원래 트리 대안은 쓰지 않음 |
| 7 | 로컬 설정(User Secrets) 첫 실행 전 키 이름 | local-setup "로컬 설정" 절의 키 이름 확인 명령 | PowerShell | `No secrets configured for this application.`(미실측 (1) 해소: 문서의 "`No secrets configured`로 시작" 문구와 일치). 원래 트리와 같은 `UserSecretsId`라 이 clear로 원래 트리 키도 함께 지워짐(재현 차이점 표 `UserSecretsId` 행) |
| 8 | 서비스 빌드 및 실행(1회차) | `dotnet run --project src/Aspire/EmergencyHub.AppHost --launch-profile http` | Git Bash(백그라운드) | 05:37:23 시작 → 05:38:48 `/health/ready` 200(빌드 포함 약 85초). 콘솔에 `Now listening on: http://localhost:15180`, `Login to the dashboard at http://localhost:15180/login?t=<토큰>` |
| 9 | 대시보드 확인 | Resources 화면 상태 | Edge 헤드리스(CDP) | `postgres` Running, `employee-db` Running, `employee-migrations` Finished, `employee-api` Running(`http://localhost:5180`), 매개변수 2개 Running. 6개 리소스 |
| 10 | 동작 확인 · 헬스 체크 | `curl -s -i http://localhost:5180/health/ready` 또는 `Invoke-RestMethod` | Git Bash | `HTTP/1.1 200 OK`, 본문 `Healthy` |
| 11 | 동작 확인 · 등록 → 조회 | local-setup "등록 → 조회" 명령 | Git Bash | `201 Created` + `Location` → `GET` `200 OK`, 문서 명령 그대로(아래 HTTP 증빙) |
| 12 | 첫 실행 뒤 키 이름 | local-setup "로컬 설정" 절의 키 이름 확인 명령 | Git Bash | 5개: `AppHost:OtlpApiKey`, `Aspire:VersionCheck:KnownLatestVersion`, `Aspire:VersionCheck:LastCheckDate`, `Parameters:employee-app-password`, `Parameters:postgres-password`(local-setup 출력 예와 같은 집합) |
| 13 | 중지(1회차) | Ctrl+C 또는 local-setup "중지" 절 명령, 뒤이어 컨테이너 없음 확인 | Git Bash | `taskkill //F //IM EmergencyHub.AppHost.exe`(백그라운드라 Ctrl+C 불가) → 15초 뒤 컨테이너 확인 출력 없음, `EmergencyHub.AppHost` · `dcp` 프로세스 없음, 볼륨 남음 |
| 14 | 재시작(2회차 이후) | 8과 같은 명령(회차별 프로필은 서버 로그 표) | Git Bash(백그라운드) | 2회차 `http` 05:42:57 → 05:43:23 Healthy, 3회차 기본 `https` 05:44:52 → 05:45:14 Healthy. 각 회차 뒤 같은 방법으로 중지, 컨테이너 0 |

#### psql 확인 1~11 결과

1회차(새 볼륨) 기준입니다. 2회차에서 다시 본 항목은 실제 칸에 회차를 붙여 적습니다.

| # | 확인 내용 | 기대 | 실제 |
|---|---|---|---|
| 1 | `emergency_hub_employee` 소유자 | `employee_app` | `employee_app` |
| 2 | `employee_app` 롤 속성 | `f f f f f t` | `f f f f f t` |
| 3 | `employee_app` 비밀번호 SCRAM 여부 | `t`(해시 미출력) | `t` |
| 4 | 서버 버전 | `17.x` | `17.11 (Debian 17.11-1.pgdg13+2)` |
| 5 | `public` 스키마 소유자 | `pg_database_owner` | `pg_database_owner` |
| 6 | 스키마 목록 · `employee_app` 스키마 수 | `public`만, `0` | `\dn` `public`(소유자 `pg_database_owner`) 1행, `0` |
| 7 | `__EFMigrationsHistory` 존재 · 수 | `t`, `1` | `t`, `1` |
| 8 | 마이그레이션 이력 | 1행, `20260927134235_InitialCreate` · `8.0.31` | 1행, `20260927134235_InitialCreate` · `8.0.31`. 2회차도 같은 1행 |
| 9 | 테이블 · 소유자 | `__EFMigrationsHistory` · `employees`, 둘 다 `employee_app` | `__EFMigrationsHistory` · `employees`, 둘 다 `employee_app` |
| 10 | 연결(등록 → 조회 직후와 약 30초 뒤) | `employee-%` 행이 `employee-api-read` 1 · `employee-api-write` 1, `usename` `employee_app`만, 두 번 수 같음, `employee-migration` 행 없음(빈 `application_name`의 `postgres` 연결 제외) | 05:39:14 · 05:40:19(65초 간격) 두 번 모두 `employee_app` · `employee-api-read` 1, `employee_app` · `employee-api-write` 1, `employee-migration` 행 없음(빈 `application_name` 행도 이번에는 없음). 2회차(조회만 한 직후)는 `employee-api-read` 1만(쓰기 연결을 아직 쓰지 않음) |
| 11 | 읽기 전용 옵션 연결 | `on`, 그다음 `25006`(read-only transaction) | `on`, 그다음 `ERROR:  cannot execute CREATE TABLE in a read-only transaction` |

#### 실행 회차별 서버 로그

각 회차를 멈추기 전에 `docker logs`를 스크래치에 저장해 셉니다. 판정 기준: 1회차는 42P04 0 · 42710 0 · 기타 0(3D000 · 25006 탐침은 수만 기록하고 제외), 2회차 이후는 42P04 한 쌍(ERROR 1 + STATEMENT 1) 정확히 1 · 3D000 0 · 42710 0 · 기타 0. 앱 리소스는 회차마다 대시보드에서 확인하며, `employee-migrations` 종료 코드 `0`(2회차 이후는 적용할 마이그레이션 없음), `employee-migrations` · `employee-api` Error · Critical 0이어야 합니다. 판정 열은 서버 로그 기준과 앱 리소스 기준을 모두 만족할 때만 통과입니다.

| 회차 | 프로필 | 42P04 쌍 | 3D000 | 25006 탐침 | 42710 | 기타 ERROR · FATAL · PANIC | `employee-migrations` 종료 코드 | `employee-migrations` · `employee-api` Error · Critical 수 | 판정 |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `http` | 0 | 1(20:38:24 UTC, 기록 · 제외) | 1(기록 · 제외) | 0 | 0 | `0`(DCP API `Finished` · `exitCode 0`, InitialCreate 적용 로그) | `employee-migrations` 0 · `employee-api` Error 2(Critical 0): 첫 `/health/ready` 요청(20:38:47 UTC)에서 `Health check "EmployeeReadDbContext"`(95ms) · `"EmployeeDbContext"`(948ms) `with status Unhealthy ... with message 'null'`(EventId 103, `DefaultHealthCheckService`). 같은 시각 서버 로그에 연결 오류 없음. 다음 요청부터 Healthy | 서버 로그 통과, 앱 리소스 불일치(Error 2). 틀 기준 불일치는 기록 유지, 인수 판정은 완료 조건 기준(재시작 회차 오류 0으로 충족), 원인 조사 BL-117 |
| 2 | `http` | 1(ERROR 1 + STATEMENT 1) | 0 | 0 | 0 | 0 | `0`(적용할 마이그레이션 없음: CREATE 문 없이 이력 조회만) | 0(대시보드 Error 행 0, 파일 로그 Error 수 1회차 뒤와 같음) | 통과 |
| 3 | `https` | 1(ERROR 1 + STATEMENT 1) | 0 | 0 | 0 | 0 | `0`(DCP API) | 0(대시보드는 OTLP 0건이라 앱 파일 로그 `logs/*.json`으로 확인, Error · Fatal 증가 0) | 통과(앱 리소스는 파일 로그 기준) |

#### HTTP 증빙

| 확인 | 회차 | 기대 | 실제 |
|---|---|---|---|
| 헬스(`/health/ready`) | 1(2 · 3회차도 같음) | `200`, `Healthy` | `HTTP/1.1 200 OK`, `Content-Type: text/plain`, 본문 `Healthy` |
| 등록(`POST /api/v1/employees`) | 1 | `201`, `Location` · `id` | `HTTP/1.1 201 Created`, `Location: http://localhost:5180/api/v1/employees/01a0e497-a2d4-75d6-b954-8ee4c6400722`, 본문 `{"id":"01a0e497-a2d4-75d6-b954-8ee4c6400722"}` |
| 조회(`GET /api/v1/employees/{id}`) | 1 | `200`, `email` 소문자 정규화, `employeeStatus` 정수 | `HTTP/1.1 200 OK`, `"displayName":"Hong Gildong"`, `"email":"hong.gildong@example.com"`(입력 `Hong.Gildong@Example.com`), `"employeeStatus":1`, `createdAt` · `updatedAt` `2026-09-27T20:38:58.312023+00:00` |
| 재시작 후 조회(1회차 등록 `id`) | 2 · 3 | `200`, 1회차 등록 값과 같음 | 2 · 3회차 모두 `HTTP/1.1 200 OK`, 본문이 1회차 조회 응답과 글자 단위로 같음. 3회차 새 등록(`Kim.Https@Example.com`)도 `201` |

#### 대시보드 증빙

| 확인 | 회차 | 기대 | 실제 |
|---|---|---|---|
| 대시보드 로그인 URL | 1 · 2(`http`), 3(`https`) | `login?t=<토큰>` 형식(토큰 미기록) | `http://localhost:15180/login?t=<토큰>`(1 · 2회차), `https://localhost:17180/login?t=<토큰>`(3회차). 토큰은 32자, 스크래치 로그에서도 가림 |
| 시작 순서 | 1(2 · 3회차도 같은 순서) | local-setup "로컬 구성" 절 시작 순서: `postgres` → `employee-db` → `employee-migrations`(종료 코드 0) → `employee-api`(Healthy) | DCP API(`/apis/usvc-dev.developer.microsoft.com/v1/executables` · `containers`) UTC: `postgres` 시작 20:38:20.40 → 서버 로그 3D000(`employee-db` 헬스 검사) 20:38:24 → `employee-migrations` 시작 20:38:27.19 · 종료 20:38:36.04(`exitCode 0`) → `employee-api` 시작 20:38:36.60(마이그레이션 종료 0.56초 뒤) → Healthy. 2회차 migrations 종료 20:43:19.06 → api 시작 20:43:19.64, 3회차 20:45:11.40 → 20:45:11.97 |
| Resources 상태 | 1 · 2 · 3 | `postgres` Running, `employee-migrations` Finished, `employee-api` Running · Healthy | 세 회차 모두 `postgres` Running, `employee-db` Running, `employee-migrations` Finished, `employee-api` Running(`/health/ready` 200) |
| `employee-migrations` 종료 코드 | 1 · 2 · 3 | `0`(2회차 이후는 적용할 마이그레이션 없음) | 세 회차 모두 DCP API `state Finished` · `exitCode 0`. 2회차 로그는 이력 조회 뒤 `Migrations applied for EmployeeDbContext in 1111.1461 ms`(CREATE 문 없음) |
| Structured logs DOM 덤프 요지 | 1 · 2 | `employee-api` 로그 있음, `employee-migrations` · `employee-api` Error · Critical 0 | 1회차 27건(Information · Debug, Error 2): `employee-migrations` InitialCreate DDL · `Migrations applied ...`, `employee-api` `Employee 01a0e497-… registered`, `HTTP POST /api/v1/employees responded 201`, `HTTP GET /api/v1/employees/01a0e497-… responded 200`. Error 2건은 위 서버 로그 표 1회차의 헬스 검사 Unhealthy(`log-row-error`). 2회차 Error · Critical 0 |
| Traces DOM 덤프 요지 | 1 · 2 | `employee-api` 등록 · 조회 추적 있음 | 1회차 10건: `employee-migrations: emergency_hub_employee` 8건, `employee-api: POST api/v1/employees`, `employee-api: GET api/v1/employees/{id}`. 2회차 6건(`employee-api: GET api/v1/employees/{id}` 포함) |
| 스크린샷 | - | 사용자 추가 항목 | (사용자 추가) |

- DOM 덤프 방법: `--dump-dom`은 Blazor 대화형 렌더링 전에 끝나 본문이 비어서, Edge 헤드리스를 원격 디버깅(CDP)으로 띄우고 로그인 URL 이동 → `structuredlogs` · `traces` · 리소스 화면에서 8초 기다린 뒤 `outerHTML` · `innerText`를 저장했다(스크래치 `t04/cdp-dump.js`, 덤프 원본 `t04/dom-run<N>-*.html`). 로그 수준은 행 CSS 클래스(`log-row-error` 등)로 셌다.

#### https 프로필 확인

| 확인 | 기대 | 실제 |
|---|---|---|
| `dotnet dev-certs https --check --trust` 출력 · 종료 코드 | 신뢰 여부 기록(미신뢰면 종료 코드 `7`) | `The following certificates were found, but none of them is trusted: 1 certificate`(지문 줄 1개), 종료 코드 `7`. `--trust`는 실행하지 않음 |
| https 회차 실행 여부 | 실행 또는 미실측(사유) | 실행(3회차, `dotnet run --project src/Aspire/EmergencyHub.AppHost`). 대시보드 `https://localhost:17180`, Api `/health/ready` 200, 1회차 id 조회 200, 새 등록 201 |
| https 회차 OTLP(구조화 로그 · 추적) 건수 | 신뢰 시 0건 아님, 미신뢰 시 0건 여부 또는 미실측 | 미신뢰 · 0건: 요청 뒤 10초 이상 지나 덤프한 구조화 로그 `0개의 구조적 로그`(구조화된 로그를 찾을 수 없음), 추적 `0개의 추적`. 앱 파일 로그에는 3회차 기록이 남음(미실측 (2) 해소, local-setup 설명과 일치) |

#### 재현 차이점

| 항목 | 새 환경이라면 | 이번 재현 | 영향 |
|---|---|---|---|
| warm 항목(SDK · NuGet 캐시 · `postgres:17` 이미지 · Docker) | 설치 · 다운로드부터 시작 | SDK 8.0.425 설치됨, NuGet 캐시(Aspire.Dashboard.Sdk 9.5.2 등) 있음, `postgres:17` 이미지 있음, Docker Desktop 실행 중 | 이미지 다운로드 · 패키지 복원 시간이 빠짐(1회차 빌드 포함 약 85초). 새 환경의 설치 절차(prerequisites)와 첫 다운로드는 검증하지 않음 |
| clone 원본 | GitHub 원격 | 로컬 저장소 `C:\00. src\02. src\emergency-hub`(원격 토픽 브랜치가 24커밋 뒤라 GitHub clone 금지, 사전 결정) | clone HEAD = 토픽 HEAD `bcf55f3`로 내용은 같음. `gh` 인증 · GitHub URL clone은 검증하지 않음 |
| 초기화 실행 디렉터리 | clone 루트 | clone 루트 `C:\eh-s04`(clear 성공, 원래 트리 대안 미사용) | 없음 |
| `UserSecretsId` · 볼륨 이름 공유 | 다른 clone 없음 | 원래 트리와 같은 `UserSecretsId` 4264c4b6-… · 볼륨 `emergency-hub-postgres-data`를 공유. clone에서의 clear · 볼륨 삭제가 원래 트리 상태도 초기화 | 원래 트리의 이전 비밀번호 쌍 · 확인용 직원 행(t03bash · t03ps 등)이 사라짐. 다른 clone이 없는 새 환경과 결과는 같음 |
| 원래 트리 상태 | 해당 없음 | 원래 트리도 T04가 만든 새 비밀번호 쌍 · 새 볼륨을 사용(복구 · 재초기화 안 함). 원래 트리 `user-secrets list` 키 이름 5개가 1회차 뒤 집합과 같음 | 원래 트리에서 다음 실행은 42P04 한 쌍이 남는 재시작 상태 |
| 프로필 | 기본 `https`(dev-certs 신뢰) | `http`(1 · 2회차). dev-certs 미신뢰(`--check --trust` 종료 코드 7), `--trust` 실행 안 함(오케스트레이션 결정) | 신뢰된 https에서 OTLP 수신은 검증하지 않음(사용자 https 재확인 절차는 결과 리뷰 사용자 확인 사항) |
| https 회차 | 실행 | 실행(3회차, 미신뢰). 기동 · HTTP 정상, 대시보드 OTLP 0건 | local-setup "신뢰하지 않으면 대시보드에 로그 · 추적이 0건" 설명을 실측으로 확인 |

#### 정리 확인

| 확인 | 기대 | 실제 |
|---|---|---|
| `C:\eh-s04` 삭제 | 경로 없음 | 삭제함. `Test-Path C:\eh-s04` = `False` |
| 보호 대상 diff(볼륨 0 → 1) | `emergency-hub-postgres-data` 삭제 1건만 | `< emergency-hub-postgres-data` 1건만. 익명 2개 · `backend_postgres_data` 그대로 |
| 보호 대상 diff(볼륨 1 → 2) | `emergency-hub-postgres-data` 재생성 1건만 | `> emergency-hub-postgres-data` 1건만(volumes-0과 volumes-2는 같은 집합) |
| 보호 대상 diff(컨테이너 0 → 1) | 차이 없음 | 차이 없음(0 → 1, 0 → 2 모두). `elastic8`, `mysql`, `vital-api`, `vital-db` |
| user-secrets 키 집합(1회차 뒤와 마지막 회차 뒤) | 같음(키 이름만 비교) | 같음(1회차 뒤 · 2회차 뒤 · 3회차 뒤 · 원래 트리 모두 같은 5개) |
| AppHost · 프로젝트 프로세스 | 0 | 0(`EmergencyHub.AppHost` · `dcp` · `dcpctrl` 0, 명령줄에 `EmergencyHub.Employee` · CDP 포트가 있는 프로세스 0) |
| 이 프로젝트 컨테이너 | 0 | 0(`docker ps -a --filter volume=emergency-hub-postgres-data` 출력 없음) |

### S04-T06 CI 실패 표시 확인

인수 조건 원본: [PRD-001](../prd/PRD-001-foundation.md) FR-10("실패 테스트를 넣은 임시 브랜치에서 실패로 표시됨을 한 번 확인"), 확인 방법은 이 문서 계획 리뷰 "PRD 인수 조건 대비 변경" FR-10 행. 임시 브랜치에 넣는 파일은 `tests/BuildingBlocks/EmergencyHub.BuildingBlocks.Domain.UnitTests/CiCheck/CiFailureCheckTests.cs` 1개(형식 1개, `Result.Success().IsSuccess.Should().BeFalse(...)` 단언 실패 1건)이며 토픽 브랜치에는 커밋하지 않습니다.

#### 실행 기록

| 항목 | 기대 | 실제 |
|---|---|---|
| 임시 브랜치 시작점 | `ci-check/s04-fail`의 부모 커밋 = 토픽 HEAD | 일치. `git log -1 --format=%P 1ce433a` = `77ca927`(토픽 HEAD `git rev-parse feature/prd-001-foundation` = `77ca927`) |
| 실패 커밋 · 변경 파일 | 커밋 1개, 변경 파일은 위 `CiFailureCheckTests.cs` 1개뿐(`git show --stat`) | 일치. 커밋 `1ce433a` 1개, `git show --stat` = `.../CiCheck/CiFailureCheckTests.cs` 1 file, 14 insertions |
| Draft PR | `ci-check/s04-fail` → `develop`, Draft | 일치. [PR #9](https://github.com/thkim-ezabele/task_20260926/pull/9) `isDraft` true, base `develop`, head `ci-check/s04-fail` |
| 실패 실행 링크 | GitHub Actions `ci` 워크플로 실행 URL | [run 36350200891](https://github.com/thkim-ezabele/task_20260926/actions/runs/36350200891)(head `1ce433a`, conclusion failure, 2026-09-27T21:02:08Z~21:05:36Z 약 3분 28초, 작업 3분 25초) |

#### 실패 확인 5항목

| # | 확인 | 기대 | 실제 |
|---|---|---|---|
| 1 | 단언 실패 1건 · 컴파일 성공 | Build · Format check 단계 success, Test 결과 실패 1(`CiFailureCheckTests.Success_Always_FailsIntentionallyForCiFailureCheck`), 그 밖 실패 0 | 일치. Build · Format check success. 로그 `Failed CiFailureCheckTests.Success_Always_FailsIntentionallyForCiFailureCheck` 1줄뿐, Domain.UnitTests 실패 1 · 통과 215, 나머지 12개 어셈블리 실패 0(ArchitectureTests 건너뜀 1은 기존 항목) |
| 2 | Test 단계 결과 | `Test (coverage)` 단계 failure | 일치. `Test (coverage)` conclusion failure |
| 3 | Coverage report 단계 결과 | `Coverage report` 단계 skipped | 일치. `Coverage report` conclusion skipped |
| 4 | 아티팩트 | `test-results`(trx) · `container-logs` 업로드됨 | 일치. 실행 아티팩트 `test-results` 344617 바이트, `container-logs` 4512 바이트(expired false) |
| 5 | PR 실패 표시 | PR 체크 `ci` 실패 표시(`gh pr checks` 또는 PR 화면) | 일치. `gh pr checks 9` = `build · format · test · coverage` fail 3m25s, 종료 코드 1 |

#### 정리 확인

| 확인 | 기대 | 실제 |
|---|---|---|
| PR 상태 | `CLOSED`, merged `false`(병합하지 않고 닫음) | 일치. `gh pr close 9` 코멘트와 함께 닫음, `gh pr view 9 --json state` = CLOSED, `.merged` = false |
| 원격 브랜치 `ci-check/s04-fail` 삭제 | 삭제됨 | 일치. `git push origin --delete ci-check/s04-fail` = `[deleted]` |
| 원격 `ci-check/*` 수 | 0(`git ls-remote --heads origin 'ci-check/*'` 출력 없음) | 일치. 0줄 |
| 로컬 `ci-check/*` 수 | 0(`git branch --list 'ci-check/*'` 출력 없음) | 일치. `git branch -D ci-check/s04-fail` 뒤 0줄 |
| 토픽 브랜치에 실패 커밋 없음 | 실패 커밋이 토픽 브랜치 조상이 아님, 토픽 브랜치에 `CiCheck/` 경로 없음 | 일치. `git merge-base --is-ancestor 1ce433a feature/prd-001-foundation` 종료 코드 1, `git branch -a --contains 1ce433a` 빈 출력, 토픽 로그 grep 0, `git ls-tree -r` `CiCheck` 0 |

### FR / NFR 증빙 표

행은 [PRD-001](../prd/PRD-001-foundation.md)의 FR-01~FR-11, NFR-01~NFR-07 전 항목입니다. 인수 조건 해석은 이 문서 계획 리뷰 "PRD 인수 조건 대비 변경"과 PRD 변경 이력을 따릅니다.

- 테스트 열의 Trait 집계는 `grep -rho 'Trait("[^"]*", *"PRD-001[^"]*")' tests --include=*.cs`(2026-09-28, S04-T06 developer)로 센 값이고, 모든 ID에서 Trait 수 = 파일 수입니다. Trait 수 = Trait이 붙은 클래스(파일) 수이며 테스트 케이스 수가 아닙니다. 전체 테스트 수는 S04-T01 기준 1537 통과 · 1 건너뜀입니다.
- CI 실행 기호: **CI-A** = [run 36340112141](https://github.com/thkim-ezabele/task_20260926/actions/runs/36340112141)(S03 종료, head `f0180bf`, success, 4분 12초), **CI-B** = [run 36340575571](https://github.com/thkim-ezabele/task_20260926/actions/runs/36340575571)(원격 토픽 브랜치 최신 head `b619128`, success, 3분 52초, 모든 단계 success · `Upload container logs` skipped). 둘 다 S04 커밋을 포함하지 않으므로 S04 HEAD의 CI는 FR-10 · NFR-07 행에서 종료 push 뒤 판정합니다.
- 스프린트 결과 리뷰 FR 충족 표: [S01](S01-decisions-build-ci.md), [S02](S02-building-blocks.md), [S03](S03-aspire-employee.md).

| ID | 요구사항 | 인수 조건 요지 | 테스트(Trait 집계 · 테스트 이름) | CI 실행 | 문서 | 재현 기록 | 판정 |
|---|---|---|---|---|---|---|---|
| FR-01 | 기술 결정 ADR 작성 | ADR은 사용자 확인 후 `accepted`로 구현보다 먼저 커밋, 나열 항목 🟡 해소, 남은 🟡는 처리 방식 표 | 해당 없음(문서 요구사항, Trait 0) | 해당 없음 | [ADR 목록](../../03-architecture/adr/README.md)(0011~0023 accepted, 0024 추가), [패키지 버전 · 라이선스](../../03-architecture/package-versions.md), [기술 스택](../../03-architecture/tech-stack.md)(🟡 0, "남은 항목 처리 방식" 절), S01 결과 리뷰 FR-01(ADR 커밋이 구현보다 앞섬) | 해당 없음 | 충족 |
| FR-02 | 빌드 설정 | 새 clone에서 `dotnet build` 경고 0 · 오류 0(마이그레이션 생성 코드 포함) | 해당 없음(빌드 결과가 증빙, Trait 0). `Directory.Build.props` `TreatWarningsAsErrors` | CI-A · CI-B `Build` · `Format check` success(클린 체크아웃) | [Clean Architecture](../../03-architecture/clean-architecture.md), [코딩 컨벤션](../../04-development/coding-conventions.md)(`.editorconfig`), S01 · S03 결과 리뷰 FR-02 | S01-T05 스크래치 clone build 경고 0, 이 문서 증빙 절 S04-T04(`C:\eh-s04` clone에서 AppHost 빌드 · 실행 성공, 실행 절차 8) | 충족 |
| FR-03 | Aspire 구성 | 명령 1개로 PostgreSQL → 마이그레이션 → Api, Write / Read 주입, 대시보드에서 Api Healthy와 로그 · 트레이스(Aspire.Hosting.Testing 스모크 테스트는 CI 시간을 보고 선택) | FR-03 Trait 10(`PostgresResourceTests`, `PostgresImageTagTests`, `ServiceResourceTests`, `ServiceEnvironmentTests`, `ServiceDefaultsExtensionsTests`, `HealthEndpointsTests`, `HealthReadyOutageTests`, `SerilogDefaultsTests`, `RequestLoggingTests`, `RequestCompletionLogTests`) | CI-A · CI-B `Test (coverage)` success | [로컬 개발 환경 구성](../../01-getting-started/local-setup.md), [S03-T05 증빙](../evidence/S03-T05/README.md)(대시보드 캡처) | 이 문서 증빙 절 S04-T04(시작 순서, Resources, structuredlogs 27 · traces 10 DOM 덤프, dev-certs 출력, https 미신뢰 OTLP 0건), S03-T05 증빙 | 충족(스크린샷은 사용자 추가 항목, 신뢰된 https 확인은 결과 리뷰 사용자 확인 사항, 1회차 첫 헬스 검사 Error 2건은 BL-117, 스모크 미도입, 재도입 조건은 BL-111(S04-T01)) |
| FR-04 | BuildingBlocks.Domain | 단위 테스트(성공 / 실패 / 엣지) 통과, 에러 코드 유형 자리 ↔ `ErrorType` 일치, 프레임워크 패키지 미참조 | FR-04 Trait 1(`DomainAssemblyAcceptanceTests`). Domain.UnitTests 215 통과(2026-09-28 S04-T06 developer 실행, `ErrorTypeTests` · `ErrorTests` · `ResultTests` · `AggregateRootTests` 등) | CI-A · CI-B `Test (coverage)` success | [에러 코드](../../05-api/error-codes.md), S01 결과 리뷰 FR-04(Domain 패키지 참조 0건) | 해당 없음(단위 테스트) | 충족 |
| FR-05 | BuildingBlocks.Application | 로깅 → 검증 → 트랜잭션 → Handler 순서, 검증 실패 시 Handler 미호출 · 실패 로그, 실패 `Result` 미커밋 | FR-05 Trait 11(`PipelineOrderAcceptanceTests`, `DecoratorChainAcceptanceTests`, `DispatcherAcceptanceTests`, `ConcurrentPipelineAcceptanceTests`, `UnitOfWorkTransactionTests`, `CommandFaultInterceptorTests` 등) | CI-A · CI-B `Test (coverage)` success | [ADR-0014](../../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md), [ADR-0015](../../03-architecture/adr/0015-custom-mediator-pipeline.md), S02 · S03 결과 리뷰 FR-05, PRD 변경 이력(데코레이터 · UoW 책임 해석) | 해당 없음(단위 · 통합 테스트) | 충족 |
| FR-06 | BuildingBlocks.Infrastructure | `ValidateOnBuild` / `ValidateScopes`에서 마커 구현이 모두 Scoped 해석, UUID v7 · 문자열 정렬 · 같은 밀리초 단조 증가 | FR-06 Trait 27(`ServiceCompositionAcceptanceTests`, `ConventionalServiceCollectionExtensionsTests`, `UuidV7IdGeneratorTests`, `AuditSaveChangesInterceptorTests`, `CommonModelConventionsTests`, `UniqueIndexNameTests`, `EmployeePersistenceRoundTripTests` 등) | CI-A · CI-B `Test (coverage)` success | [데이터베이스](../../04-development/database.md), [ADR-0013](../../03-architecture/adr/0013-uuid-v7-with-uuidnext.md), [ADR-0017](../../03-architecture/adr/0017-scrutor-for-convention-based-di.md), S02 · S03 결과 리뷰 FR-06 | 해당 없음(단위 · 통합 테스트) | 충족 |
| FR-07 | 공통 API 처리 | 실패 `Result` · 바인딩 오류 · 정의되지 않은 enum · 처리되지 않은 예외가 ProblemDetails(`code` 포함)로 응답 | FR-07 Trait 8(`ResultResponseAcceptanceTests`, `ExceptionResponseAcceptanceTests`, `ApiPipelineAcceptanceTests`, `ProblemDetailsHttpTests`, `OpenApiContractHttpTests`, `EmployeesControllerTests` 등) | CI-A · CI-B `Test (coverage)` success | [API 설계](../../04-development/api-guidelines.md), [에러 코드](../../05-api/error-codes.md)(S04-T05 대조: 에러 코드 20 · 로그 이벤트 15 누락 0 · 초과 0), S02 · S03 결과 리뷰 FR-07 | S03-T05 증빙(Kestrel 경로 바인딩 · 본문 초과 curl, 400 · 1001) | 충족 |
| FR-08 | Employee 샘플 서비스 | Aspire에서 HTTP 등록 → 조회, 통합 테스트로 등록 → 조회 · 이메일 중복 충돌 · 체크 제약 거부 · 동시성 충돌 · 읽기 연결 쓰기 거부 · UUID v7 DB 정렬 | FR-08 Trait 12(`EmployeeRegistrationHttpTests`, `ReadOnlyWriteRejectionHttpTests`, `UnitOfWorkConflictTests`, `EmployeeSchemaTests`, `MigrationReapplyTests`, `EmployeeRepositoryTests`, `EmployeeReadRepositoryTests` 등) | CI-A · CI-B `Test (coverage)` success | [Employee API](../../05-api/employee-api.md), [데이터베이스](../../04-development/database.md), S03 결과 리뷰 FR-08(InitialCreate idempotent SQL 검토) | 이 문서 증빙 절 S04-T04(HTTP 증빙 201 · 200, psql 1~11 · 11번 read-only 거부), S03-T05 증빙 | 충족 |
| FR-09 | 테스트 프로젝트 | `dotnet test` 전부 통과, 아키텍처 테스트가 의존성 규칙마다 검증 | FR-09 Trait 20(`DependencyRuleTests`, `ConventionRuleTests`, `ArchitectureAssemblyCoverageTests`, `EmployeeDatabaseFixtureTests`, `RespawnHistoryTableTests`, `EmployeeMigrationsTests`, `MigrationWorkerTests` 등). 전체 1537 통과 · 1 건너뜀(S04-T01), 아키텍처 규칙 25개(의존성 10 · 컨벤션 12 · 주입 3, S04-T02) | CI-A 13개 테스트 어셈블리 통과 · 통합 129건, CI-B success | [테스트 전략](../../04-development/testing-strategy.md), [Clean Architecture](../../03-architecture/clean-architecture.md)(의존성 규칙 표 ↔ 규칙 목록 1:1, S04-T02) | 해당 없음(테스트 실행) | 충족(S04 HEAD CI는 FR-10 행 종료 push 뒤 판정) |
| FR-10 | CI | 토픽 PR에서 워크플로 통과, 실패 테스트를 넣은 임시 브랜치에서 실패 표시 1회 확인 · 기록 | 실패 확인용 `CiFailureCheckTests`(임시 브랜치 전용, 토픽 브랜치 미포함) | 통과: S01 [run 36303017840](https://github.com/thkim-ezabele/task_20260926/actions/runs/36303017840), S02 [run 36318766373](https://github.com/thkim-ezabele/task_20260926/actions/runs/36318766373), CI-A, CI-B. 실패 표시: [run 36350200891](https://github.com/thkim-ezabele/task_20260926/actions/runs/36350200891)(임시 브랜치 `ci-check/s04-fail`, PR #9 fail, 실패 1건만 · Coverage report skipped, 확인 뒤 PR 닫고 브랜치 삭제). S04 HEAD 토픽 PR: 종료 push 뒤 판정 | [CI / CD](../../06-deployment/ci-cd.md), `.github/workflows/ci.yml` | 이 문서 증빙 절 "S04-T06 CI 실패 표시 확인" | 종료 push 뒤 판정 |
| FR-11 | 문서 갱신 | 해당 문서 `draft` 이상, 새 환경 실행은 CI 클린 러너 통과 + local-setup 수동 재현 기록 | FR-11 Trait 2(`RequestLogLevelsTests`, `ProgramTests`) | CI-A · CI-B(클린 러너 통과) | 결정 반영(`draft`): [Clean Architecture](../../03-architecture/clean-architecture.md), [기술 스택](../../03-architecture/tech-stack.md), [데이터베이스](../../04-development/database.md), [로깅](../../04-development/logging-observability.md), [에러 코드](../../05-api/error-codes.md), [코딩 컨벤션](../../04-development/coding-conventions.md). `todo` 문서(`draft`): [local-setup](../../01-getting-started/local-setup.md), [configuration](../../06-deployment/configuration.md), [environments](../../06-deployment/environments.md), [ci-cd](../../06-deployment/ci-cd.md), [troubleshooting](../../01-getting-started/troubleshooting.md) | 이 문서 증빙 절 S04-T04(재현 기록 위치는 PRD 변경 이력대로 worklog 대신 이 문서) | 충족 |
| NFR-01 | 빌드 품질 | `TreatWarningsAsErrors`에서 경고 0, 생성 코드 분석 제외 | 해당 없음(빌드 결과가 증빙, Trait 0). S04-T01 build 경고 0 · format 통과 | CI-A · CI-B `Build` success | [코딩 컨벤션](../../04-development/coding-conventions.md)(경고 억제 규칙 · 승인 목록, S04-T05), `Directory.Build.props`, `.editorconfig`(`Migrations/**` generated_code) | S01 결과 리뷰 NFR-01(부정 점검 5종) | 충족 |
| NFR-02 | 레이어 규칙 준수 | 아키텍처 테스트 통과(CI 필수) | NFR-02 Trait 4(`DependencyRuleTests`, `ConventionRuleTests`, `DeclaredReferenceTests`, `ArchitectureAssemblyCoverageTests`). ArchitectureTests 99 통과 · 1 건너뜀(S04-T02) | CI-A · CI-B `Test (coverage)` success(ArchitectureTests 포함) | [Clean Architecture](../../03-architecture/clean-architecture.md)(의존성 규칙), [테스트 전략](../../04-development/testing-strategy.md)(아키텍처 테스트 절), S02 · S03 결과 리뷰 NFR-02(위반 재현) | 해당 없음(테스트 실행) | 충족 |
| NFR-03 | 테스트 커버리지 | BuildingBlocks · Employee Domain / Application 라인 80% 목표, CI 측정 · 보고만(필수 체크 없음) | 해당 없음(Trait 0). S04-T01 커버리지: 대상 6개 어셈블리 라인 98.0~100%, 합계 라인 99.4% · 분기 94.6%, cobertura 12 = 수집 프로젝트 12 | CI-A `Coverage report` success(S03 대상 기준 라인 100%). S04 대상 6개 CI 보고는 종료 push 뒤 CI에서 확인 | [테스트 전략](../../04-development/testing-strategy.md)(커버리지 절), [CI / CD](../../06-deployment/ci-cd.md), `coverlet.runsettings` | 이 문서 진행 기록 S04-T01 tester(CI와 같은 명령 로컬 실행) | 충족 |
| NFR-04 | 로컬 실행 용이성 | 사전 준비(.NET 8 SDK, Docker)만 된 새 환경에서 clone → 명령 1개로 전체 실행(마이그레이션 자동 적용) | 해당 없음(Trait 0, AppHost 구성은 FR-03 Trait) | CI-A · CI-B(클린 러너 빌드 · 테스트) | [로컬 개발 환경 구성](../../01-getting-started/local-setup.md), [사전 준비](../../01-getting-started/prerequisites.md) | 이 문서 증빙 절 S04-T04(`C:\eh-s04` clone, 명령 1개, `employee-migrations` 종료 코드 0, 재현 차이점 표의 warm 한계), S03-T05 증빙 | 충족(같은 머신 warm 한계 기록, PRD 변경 이력 해석) |
| NFR-05 | 라이선스 | 상용 라이선스 패키지 금지, 패키지마다 라이선스 기록 | 해당 없음(Trait 0) | 해당 없음 | [패키지 버전 · 라이선스](../../03-architecture/package-versions.md)(상용 0건), S01 · S02 결과 리뷰 NFR-05 | 해당 없음 | 충족 |
| NFR-06 | 비밀 정보 | DB 비밀번호 등 비밀 값 미커밋(Aspire 매개변수 / user-secrets) | NFR-06 Trait 2(`PostgresResourceTests`, `ServiceResourceTests`) | 해당 없음(워크플로 비밀 0, S01 결과 리뷰 NFR-06) | [configuration](../../06-deployment/configuration.md), [로컬 개발 환경 구성](../../01-getting-started/local-setup.md)(user-secrets · 자리표시자), S03 결과 리뷰 NFR-06 | S03-T05 증빙(비밀 점검 6개 실제 비밀 0), 이 문서 증빙 절 S04-T04(user-secrets 키 이름만 · 토큰 가림) | 충족 |
| NFR-07 | CI 소요 시간 | PR 워크플로 10분 이내(이미지 pull 포함) | 해당 없음 | 참고치: CI-A 4분 12초, CI-B 3분 52초. S04 HEAD: 종료 push 뒤 판정 | [CI / CD](../../06-deployment/ci-cd.md) | 해당 없음 | 종료 push 뒤 판정 |

## 결과 리뷰

> 스프린트 종료 시 orchestrator의 결과 리뷰(계획 대비 실제, 완료 조건 · FR 충족, 반려 분석)를 요약합니다. 오케스트레이션 세션 승인 2026-09-28.

### 계획 대비 실제

- 작업 6개(계획 확정 때 4 → 6) 모두 `done`. 백로그 이관 0, BLOCKED 0, 반려 6회(작업당 최대 2회, T02).
- 코드 변경은 T01(`d699f27`: 커버리지 Include 6개, MigrationService Development 주입 + AppHost 테스트 4건, `Error.cs` remarks)과 T02(`bab2cd9`: `DependencyRules.cs` Source 문자열)뿐이고 나머지는 문서 · 증빙.
- 계획에 없던 판단 1건: T04 http 프로필 사용(이 PC 개발 인증서 미신뢰, 오케스트레이션 세션 승인, PRD 변경 이력 기록).
- 원격 토픽 브랜치가 로컬보다 뒤처져 있어 재현은 로컬 저장소 clone으로 했다(재현 차이점 표).
- DoD 빌드 · 테스트(메인 세션 재실행): `dotnet build -c Release -warnaserror` 오류 0, `dotnet test` 13개 프로젝트 1537 통과 · 실패 0 · 건너뜀 1(원래 있던 ArchitectureTests 1건, `DOCKER_API_VERSION=1.43`).

### FR 충족

[증빙 표](#fr--nfr-증빙-표) 18행, 빈 칸 0.

| 구분 | 충족 | 부분 | 비고 |
|---|---|---|---|
| FR | 10 | 1 | FR-10: 실패 표시 확인 완료(run 36350200891, PR #9 CLOSED · merged false). S04 HEAD 토픽 PR 통과는 종료 push 뒤 DoD |
| NFR | 6 | 1 | NFR-07: 참고치 3분 28초~4분 12초. S04 HEAD 판정은 종료 push 뒤 DoD. NFR-03 대상 6개 모두 80% 이상(합계 라인 99.4% · 분기 94.6%) |

### 반려 분석

| 작업 | 단계 | 유형 | 원인 |
|---|---|---|---|
| S04-T02 | reviewer | 범위 밖 추가 | testing-strategy에 완료 조건에 없는 "부정 범위" 해석 문단. 계획 인계 메모의 "선택" 제안을 그대로 반영(→ BL-113) |
| S04-T02 | tester | 사실 불일치 | database.md 443행 따옴표 식별자 문장이 idempotent SQL 실측과 다름. 이전 기록 문장을 다시 실측하지 않고 옮김 |
| S04-T05 | reviewer | 문장 정확성 | 예시 도입 문장이 실제 발췌 범위와 다름, 경고 억제 승인 절차의 주체 · 순서 판정 불가 |
| S04-T03 | reviewer | 사실 불일치 | local-setup 등록 명령의 email과 기대 결과가 다름 |
| S04-T04 | reviewer | 누락 | 증빙 틀에 FR-03 시작 순서 칸, 회차별 앱 리소스 칸 없음 |
| S04-T06 | reviewer | 누락 | 증빙 표 FR-03 행에 PRD 선택 항목(스모크) 처리 누락 |

반려 6회 모두 문서 · 증빙 작업에서 나왔고 코드 작업(T01)은 0. 공통 원인은 완료 조건 문장을 산출물(틀 · 표 행)로 옮길 때 항목 대응을 점검하지 않은 것과, 사실 문장을 실측 없이 옮긴 것. 모두 1회 재작업으로 해소.

### 결과 리뷰에서 정한 판단

- **S04-T04 1회차 편차(BL-117)**: T04는 PASS 유지. 새 볼륨 1회차 `employee-api` 첫 헬스 검사 Unhealthy(Error 2건, 서버 로그 오류 없음, 이후 Healthy)는 증빙 틀의 1회차 앱 리소스 기준 불일치로 기록을 유지한다. 판정 방식: **틀 기준 불일치는 기록 유지, 인수 판정은 완료 조건 기준**(완료 조건 "재시작 때 42P04 한 쌍 외 리소스 로그 오류 0"과 FR-03은 충족). 원인이 확인되지 않아 "첫 실행 잡음"으로 제외하지 않고 BL-117(중) open으로 조사한다.

### ADR 후보 (파일은 만들지 않음, /retro에서 판단)

1. 불변식 위반은 예외, 입력 검증 실패는 Result라는 실패 처리 경계(BL-089, coding-conventions S04-T05 문장이 원본)
2. ADR-0018 보충: 공통 기반 `RequestValidator<TRequest>`(RuleLevelCascadeMode = Stop)(BL-066)
3. 경고 억제 규칙: `[SuppressMessage]` + Justification, 전역 NoWarn 금지, 승인 절차(BL-055, S01 후보와 병합)
4. ADR-0011 보충: 42P04 · 3D000 잡음 판정 기준, GenerateParameterDefault + persist(BL-096 · BL-115, BL-117 원인에 따라 첫 실행 헬스 편차 포함 여부)
5. 누적 후보: S03의 ADR-0012 이력 컬럼 조항 대체, ADR-0020 BL-023 조항 대체, (선택) Api DB 재시도 · 제한 시간. S01의 액션 SHA 고정, 공급망 정책, SDK 고정, 테스트 프로젝트 이름 규칙, ErrorType · Result 계약

### 사용자 확인 사항

1. **(선택) 신뢰된 https 프로필 OTLP 수신 재확인** — FR-03 판정에는 영향 없음. 절차:
   1. PowerShell: `dotnet dev-certs https --trust` (Windows 사용자 인증서 저장소를 바꾸므로 사용자가 판단, 확인 창에서 "예")
   2. `dotnet dev-certs https --check --trust` 종료 코드가 `0`인지 확인(현재 `7`)
   3. 저장소 루트에서 `dotnet run --project src/Aspire/EmergencyHub.AppHost` (기본 https 프로필)
   4. 콘솔의 `https://localhost:17180/login?t=<토큰>`으로 로그인 → `employee-api` Running · Healthy 확인
   5. [local-setup](../../01-getting-started/local-setup.md) "등록 → 조회" 명령 1회(새 email) → 10초 이상 기다린 뒤 Structured logs · Traces에 `employee-api` 항목이 0건이 아닌지 확인
   6. 중지 후 결과(종료 코드, 로그 · 추적 건수)를 이 문서 "https 프로필 확인" 표에 한 행 추가하거나 /retro 자료로 전달
2. **대시보드 스크린샷 추가** — http 또는 신뢰된 https 실행 중 Resources, Structured logs, Traces 3장을 캡처해 `wiki/10-delivery/evidence/S04-T04/`에 넣고, 대시보드 증빙 표의 "(사용자 추가)"를 링크로 바꾼다. 토큰이 든 주소창은 가린다.
3. ~~BL-117 판정 방식 승인~~ → 오케스트레이션 세션 승인으로 처리(위 "결과 리뷰에서 정한 판단").
4. **BL-099 후속: 기본 프로필(https 우선) 유지 확인** — 권고는 유지(.NET 표준 개발 흐름이 dev-certs 신뢰를 전제로 하고, http 대안이 local-setup에 있음).

## 생긴 백로그 / 기술부채

| ID | 제목 | 발생 작업 | 정리 결과 |
|---|---|---|---|
| BL-111 | Aspire 스모크 테스트 도입(재도입 조건: AppHost 테스트 모드, CI 3회 연속 통과, 추가 시간 최대 7분) | S04-T01 | open(중) |
| BL-112 | 아키텍처 문서 3개의 보류 아닌 🟡 표기 통일 | S04-T02 | open(하) |
| BL-113 | 설정 · 구성 코드의 "실패" 테스트 해석 기준 | S04-T02 | open(하), /retro 개선안에서 사용자 결정 |
| BL-114 | PowerShell 5.1 409 응답 본문 확인 방법 안내 | S04-T03 | open(하) |
| BL-115 | database.md 42P04 판정 문구 풀이 · psql 명령 틀 보완(BL-116 병합) | S04-T04 | open(하) |
| BL-116 | database.md psql 확인 절 명령 틀 | S04-T04 | dropped(BL-115에 병합) |
| BL-117 | 새 볼륨 첫 실행 첫 헬스 검사 Unhealthy(Error 2건) 원인 조사 | S04-T04 | open(중) |
| BL-118 | 증빙 도구 문서화(CDP DOM 덤프, DCP API 종료 코드 조회) | S04-T04 | open(하), 범위 확장 |

planned:S04 정리: BL 21건(017 · 038 · 039 · 048 · 049 · 055 · 062 · 063 · 066 · 068 · 069 · 087 · 089 · 091 · 092 · 097 · 099 · 100 · 102 · 105 · 110) `done`. TD-016 · TD-024 `resolved`, TD-026 open 유지(직접 참조 여부는 TD-002 때). BL-059는 DoD에서 판단.

## 회고

> 기록만 한다. 스킬 · 에이전트 파일 반영은 /retro에서 한다(오케스트레이션 세션 지시).

### 잘된 점

- 계획 리뷰에서 작업을 4 → 6개로 나눠 코드(T01)와 문서(T02 · T05 · T03)를 분리했다. 코드 작업 반려 0, 전체 BLOCKED 0.
- FR 11 · NFR 7 증빙 표 18행에 빈 칸이 없고, Trait 재집계 · CI 실행 링크 · 문서 경로 · 재현 기록이 한 표에 연결된다.
- 대조를 스크립트로 했다: error-codes 양방향(음성 대조 3건 검출), 저장소 트리 경로 94개, 아키텍처 규칙 25개 1:1, 경고 억제 20건 = 승인 목록.
- 새 clone 재현에서 T03의 미실측 3건을 모두 해소했고, 보호 대상 볼륨 · 컨테이너를 3시점 diff로 확인했다.
- 임시 브랜치 CI 실패 확인(FR-10)을 실패 확인 5항목 · 정리 확인 5항목으로 닫았고 토픽 브랜치에 흔적이 없다.
- 누적 인계 메모 파일로 작업 간 인계가 전달됐다.

### 문제

- 반려 6회가 모두 문서 · 증빙 작업에서 나왔다. 사실 문장을 실측 없이 옮김(T02 SQL, T03 email), 완료 조건을 증빙 틀 · 표로 옮기다 누락(T04, T06).
- 계획 인계 메모의 "선택" 제안이 완료 조건 밖 추가로 이어져 반려됐다(T02 → BL-113).
- 증빙 틀의 1회차 기준이 완료 조건보다 엄격하게 만들어져 판정 불일치가 생겼다(BL-117). S03 회고의 "판정 기준이 작업 중에 늘어남"과 같은 유형.
- 원격 토픽 브랜치가 뒤처져 GitHub clone 재현을 못 했다. 새 환경 증빙이 로컬 clone과 warm 캐시에 한정된다.
- clone과 원래 트리가 같은 UserSecretsId · 볼륨 이름을 공유해, 재현 초기화가 원래 트리 상태까지 지웠다.
- 이 PC의 개발 인증서가 신뢰되지 않아 기본 https 프로필의 대시보드 수신을 검증하지 못했다(사용자 확인 사항).

### 다음에 바꿀 것

- 증빙 틀 · 표를 만드는 developer 자가 점검에 "완료 조건 문장 ↔ 틀 칸 · 행 대응 표"를 넣는다. 틀의 판정 기준이 완료 조건보다 엄격하면 계획 리뷰나 메인 세션 판단으로 명시한다.
- 인계 메모의 "선택" 제안이 완료 조건 밖이면 문서에 넣지 말고 BL로 올린다(agents.md handoff 규칙 반영 후보).
- 문서의 사실 문장(SQL 인용, 명령 기대 출력)에는 실측 명령과 출력 발췌를 붙이고, 이전 기록을 옮길 때는 다시 실측한다.
- 재현 증빙이 필요한 스프린트는 시작 전에 원격 브랜치를 맞추는 방안을 사전 합의에 넣는다.
- 첫 실행 · 재시작 잡음 목록(42P04, 3D000, 25006 탐침, 필요하면 BL-117)을 한 문서에 모아 판정 기준 원본으로 쓴다(BL-115 · BL-096).
- 반복되는 증빙 도구(CDP DOM 덤프, DCP API 종료 코드 조회)를 스크립트로 만들거나 문서화한다(BL-118).

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-001 분할) |
| 2026-09-28 | orchestrator | 계획 확정: 작업 4 → 6(T05 · T06 추가), 완료 조건 · 파이프라인 · DoD 수정, BL / TD 편입 (오케스트레이션 세션 승인) |
| 2026-09-28 | orchestrator | 스프린트 종료: 결과 리뷰, 생긴 백로그 / 기술부채 정리, 회고, DoD (오케스트레이션 세션 승인) |
