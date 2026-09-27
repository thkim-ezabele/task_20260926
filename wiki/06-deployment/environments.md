---
title: "환경 구성"
type: doc
status: draft
tags: [deployment]
created: 2026-09-27
updated: 2026-09-28
---

# 환경 구성

> 지금 있는 실행 환경(로컬 Aspire, 테스트, CI)과 각 환경의 호스트 환경 이름 · 인프라 · 접속 정보, 환경별 차이를 정리합니다. 배포 환경(Dev / Staging / Prod)은 아직 없습니다.
>
> [위키 홈](../README.md)

## 환경 목록 (Local / Dev / Staging / Prod)

| 환경 | 상태 | 용도 | 원본 문서 |
|---|---|---|---|
| 로컬 (Aspire AppHost) | 있음 | 개발자 PC에서 전체 실행 · 수동 확인 | [로컬 개발 환경 구성](../01-getting-started/local-setup.md) |
| 로컬 테스트 | 있음 | `dotnet test`(단위 · 통합 · 아키텍처) | [테스트 전략](../04-development/testing-strategy.md) |
| CI (GitHub Actions) | 있음 | PR마다 빌드 · 형식 · 테스트 · 커버리지 | [CI/CD](ci-cd.md) |
| Dev / Staging / Prod | 없음 | 배포 대상. 컨테이너 이미지 · 배포(CD) · Kubernetes는 이후 토픽(Phase 4) | [기술 스택 · 남은 항목 처리 방식](../03-architecture/tech-stack.md#남은-항목-처리-방식) |

## 환경별 인프라 구성

| 환경 | PostgreSQL | 서비스 실행 | 관측 |
|---|---|---|---|
| 로컬 (Aspire) | AppHost `postgres` 컨테이너 `postgres:17`, 이름 있는 볼륨 `emergency-hub-postgres-data`(데이터 유지) | AppHost가 MigrationService · Api를 프로젝트로 실행 | Aspire 대시보드(로그 · 추적 · 메트릭, OTLP) |
| 로컬 테스트 | 통합 테스트 fixture가 Testcontainers로 `postgres:17`을 띄움(일회용) | 통합 테스트는 `WebApplicationFactory`로 Api를 테스트 호스트에서 실행 | OTLP 없음(엔드포인트 빈 값) |
| CI | 로컬 테스트와 같음(ubuntu 러너 기본 Docker, 이미지는 Test 전에 pull) | 같음 | 잡 요약 · 아티팩트([CI/CD](ci-cd.md)) |

- 메시지 브로커 · API Gateway · 로그 수집기는 모든 환경에 없습니다(보류, [ADR-0023](../03-architecture/adr/0023-deferred-adoptions.md)).
- 이미지 태그 `17`의 원본은 `Directory.Build.props`의 `EmergencyHubPostgresImageTag` 한 곳입니다([설정 & 시크릿 관리 · 빌드 설정](configuration.md#빌드-설정-런타임-설정-아님)).

## 접속 정보

로컬 (Aspire) 기준입니다. 비밀번호 · 로그인 토큰은 적지 않습니다.

| 대상 | 주소 | 비고 |
|---|---|---|
| Aspire 대시보드 | `https` 프로필 `https://localhost:17180`, `http` 프로필 `http://localhost:15180` | 콘솔의 `login?t=<토큰>` URL로 로그인 |
| 대시보드 OTLP 수신 | `https` 프로필 `https://localhost:21180`, `http` 프로필 `http://localhost:19180` | 서비스가 보내는 곳(Aspire가 주입) |
| Employee Api | `http://localhost:5180` | 헬스 `/health/live` · `/health/ready`, Swagger `/swagger`(Development만) |
| PostgreSQL | 호스트 포트는 실행마다 Aspire가 정함 | 서비스 연결은 AppHost 주입 연결 식. 직접 확인은 컨테이너 안 psql([데이터베이스 · psql 확인 항목](../04-development/database.md#로컬-db-구성-apphost)) |

- DB 계정: 서비스는 `employee_app`(슈퍼유저 아님), 슈퍼유저 `postgres`는 서버 초기화 · 생성 스크립트 전용입니다. 비밀번호는 AppHost user-secrets에 있습니다([설정 & 시크릿 관리 · 시크릿 관리](configuration.md#시크릿-관리-user-secrets--github-secrets)).

## 환경별 차이점

호스트 환경 이름(`IHostEnvironment.EnvironmentName`)은 환경 변수가 정하고, 값이 없으면 Production입니다.

| 호스트 | 로컬 (Aspire) | 로컬 테스트 · CI |
|---|---|---|
| AppHost | `Development`(AppHost `launchSettings.json` 두 프로필 모두 `ASPNETCORE_ENVIRONMENT` · `DOTNET_ENVIRONMENT`) | 실행하지 않음(AppHost 단위 테스트만) |
| Employee Api | `Development`(Api `launchSettings.json` `http` 프로필 `ASPNETCORE_ENVIRONMENT`). AppHost는 Api 환경을 주입하지 않는다 | 통합 테스트 호스트 기본 `Development`(`EmployeeApiFactoryOptions.Environment`), 단위 테스트는 테스트마다 지정 |
| Employee MigrationService | **`Development`: AppHost가 `DOTNET_ENVIRONMENT=Development`를 고정 값으로 주입**한다(BL-110, S04-T01). MigrationService에는 launchSettings가 없어 주입하지 않으면 Production으로 뜬다. 값은 AppHost 자신의 환경과 무관한 상수(`EmergencyHubApplication.MigrationEnvironmentName`)다 | 실행하지 않음(단위 테스트만) |

- 주입 결과는 AppHost 단위 테스트(`ServiceEnvironmentTests`)와 실기동(S04-T01 tester: 리소스 spec `DOTNET_ENVIRONMENT=Development`, 로그 `Hosting environment: Development`)으로 확인했습니다.
- Development에서 달라지는 것:
  - 로그 수준: `appsettings.Development.json`(기본 `Debug`, `Microsoft.EntityFrameworkCore.Database.Command` = `Information`이라 실행한 SQL 문이 로그에 남음). MigrationService도 같다. SQL 파라미터 값은 남지 않는다.
  - Swagger UI · OpenAPI 문서는 Development에서만 노출된다([ADR-0019](../03-architecture/adr/0019-use-swashbuckle-openapi.md)).
  - 마이그레이션 동작(쓰기 연결 · 기본 재시도 · `MigrateAsync`만)은 환경에 따라 바뀌지 않는다([데이터베이스 · 로컬 실행 요약](../04-development/database.md#로컬-db-구성-apphost)).
- **`EnableSensitiveDataLogging` opt-in 키**: [ADR-0020](../03-architecture/adr/0020-logging-with-serilog-and-otlp.md)은 `IsDevelopment() && Database:EnableSensitiveDataLogging`(기본 `false`)으로 정했지만 **구현되지 않았고 모든 환경에서 꺼져 있습니다**(BL-094). 로컬 AppHost는 이 키를 주입하지 않고 설정 파일에도 없습니다. 구현 전에는 키를 넣어도 효과가 없습니다([로깅 & 관측성 · 개인정보 · 보안](../04-development/logging-observability.md#개인정보--보안)).
- OTLP: 로컬 (Aspire)에서만 켜집니다(Aspire가 `OTEL_EXPORTER_OTLP_ENDPOINT` 주입). 테스트 · CI에는 값이 없어 exporter를 붙이지 않습니다.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-28 | developer | `draft`로 작성: 환경 목록(로컬 Aspire · 로컬 테스트 · CI, Dev / Staging / Prod 없음), 환경별 인프라, 로컬 접속 정보, 호스트 환경 이름(MigrationService `DOTNET_ENVIRONMENT=Development` 고정 주입 BL-110, Api는 launchSettings), Development 차이, `Database:EnableSensitiveDataLogging` 기본 `false` · 미구현 · 로컬 AppHost 미주입(BL-094) (S04-T03) |
