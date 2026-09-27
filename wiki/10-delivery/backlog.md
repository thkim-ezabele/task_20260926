---
title: "백로그"
type: index
status: stable
tags: [delivery, backlog]
aliases: [Backlog]
created: 2026-09-27
updated: 2026-09-27
---

# 백로그

> 개발하면서 생긴 추가 작업, 범위 밖으로 미룬 요구사항, 스프린트에서 끝내지 못한 작업을 기록합니다.
> 기술적으로 빚진 것(임시 구현, 누락된 테스트 등)은 [기술부채](tech-debt.md)에 적습니다.
>
> [개발 관리](README.md)

## 작성 규칙

- 발견한 즉시 `new` 상태로 행을 추가하고 그 단계의 커밋에 포함한다. 우선순위는 비워 두고, 스프린트 종료 때 orchestrator가 정리한다. ID는 `BL-NNN`으로 순서대로 붙이고 재사용하지 않는다.
- 행은 지우지 않는다. 끝나거나 버린 항목은 상태만 바꾼다.
- **출처**: 어디서 생겼는지 (`PRD-001/FR-05`, `S01-T02`, PR `#12`)
- **우선순위**: `상` / `중` / `하`
- **상태**: `new`(미정리) · `open` · `planned:SNN`(스프린트에 편입) · `done` · `dropped`(비고에 이유)

## 목록

| ID | 제목 | 출처 | 우선순위 | 상태 | 비고 |
|---|---|---|---|---|---|
| BL-001 | 문서 점검 Node 스크립트(S01-T02)를 CI에 편입할지 검토 | S01 계획 리뷰 | | new | S04-T02 또는 이후 |
| BL-002 | Aspire 9.x 지원 종료(NuGet 폐기) 대응: .NET 8 지원 종료(2026-11-10)와 함께 .NET 10 · Aspire 13 전환 여부 재검토 | S01-T01 | | new | |
| BL-003 | 롤 생성은 초기화 스크립트로 분리하고 `WithCreationScript`는 `CREATE DATABASE ... OWNER` 한 문장만. Docker로 두 번 시작해 오류 로그 없음 검증 | S01-T01 | | new | S01-T02 / S03 |
| BL-004 | 클라이언트 통합 vs `AddDbContext`+`UseNpgsql` 판단 근거에 NU1902(OpenTelemetry.Api 1.9.0 전이) 추가 | S01-T01 | | new | S01-T02 |
| BL-005 | 로컬 SDK 8.0.425 설치(현재 8.0.202 · 런타임 8.0.3, 패치 끊긴 밴드). S01-T05 빌드와 xunit.v3의 전제 조건(사용자 작업) | S01-T01 | | new | T05 전 |
| BL-006 | Directory.Build.props에 `NuGetAuditMode=all`, Directory.Packages.props에 MessagePack 2.5.305 전이 고정, global.json 8.0.400 + latestFeature | S01-T01 | | new | S01-T05 |
| BL-007 | 로깅 ADR: ServiceDefaults OpenTelemetry는 템플릿 값(1.9.0) 대신 1.19.x 고정 | S01-T01 | | new | S01-T03 |
| BL-008 | BL-004 판단 자료 추가: OpenTelemetry 1.19.x와 함께 쓰면 클라이언트 통합 NU1902 해소, TD-006도 해소 가능 | S01-T01 | | new | S01-T02 |
| BL-009 | ServiceDiscovery · Http.Resilience 사용 여부 결정(서비스 1개) | S01-T01 | | new | S01-T02 / T03 |
| BL-010 | package-versions.md 클라이언트 통합 행에 net8.0 대상 EF Core 의존 버전과 하한 명시(사용 여부 결정 시) | S01-T01 | | new | S01-T02 |
| BL-011 | NFR-05 증빙: PostgreSQL 이미지(PostgreSQL License) · .NET SDK(MIT) 라이선스 행 추가 검토 | S01-T01 | | new | |
| BL-012 | Aspire 리소스 이름에 밑줄 불가(ASPIRE006): `AddDatabase("<하이픈 이름>", databaseName: "emergency_hub_employee")`로 분리하고 ADR 0009 Write / Read 연결 키와 맞춤 | S01-T01 | | new | S01-T02 / T05 |
| BL-013 | package-versions.md: Aspire.Npgsql.EntityFrameworkCore.PostgreSQL 9.5.2 및 암묵 추가 Aspire.Dashboard.Sdk · Orchestration 패키지의 폐기(Legacy) 표시 추가 | S01-T01 | | new | S01-T02 |
| BL-014 | BL-003 실측을 S03 Aspire 구현 작업 완료 조건에 넣기: 새 볼륨 → 시작 → 중지 → 재시작, 리소스 로그 오류 0, pg_database 소유자 employee_app, public 소유자 pg_database_owner | S01-T02 | | new | S03 |
| BL-015 | 서비스 DB가 2개 이상이 되면 `REVOKE CONNECT, TEMPORARY ON DATABASE emergency_hub_<service> FROM PUBLIC` 적용 | S01-T02 | | new | |
| BL-016 | package-versions.md: WithInitBindMount Obsolete(9.5.2) → WithInitFiles 표기, 클라이언트 통합 미사용 시 Npgsql.OpenTelemetry 8.0.9 · HealthChecks.EntityFrameworkCore 8.0.31 행 추가 | S01-T02 | | new | S01-T02 |
| BL-017 | local-setup / troubleshooting: "MigrationService가 Waiting 상태로 멈춤" = 생성 스크립트 실패, 해결은 이름 있는 볼륨 삭제 | S01-T02 | | new | S04 |
| BL-018 | raw-log(08-worklog/raw/*.md) frontmatter에 created · updated 없음: hook 템플릿에 추가할지, 필수 키 표에서 raw-log를 예외로 둘지 결정(BL-001 CI 편입 전제) | S01-T02 | | new | |
| BL-019 | error-codes.md: 매핑 없는 23505(유니크 위반)용 공통 Conflict 코드 할당 | S01-T02 | | new | S02-T06 |
| BL-020 | check-docs.js에 유형별 추가 필드(adr의 supersedes 등) 점검 추가 검토(기존 ADR 0005 등이 결함으로 잡힘) | S01-T02 | | new | |
| BL-021 | check-docs.js: 링크 대상에 공백이 있거나 `<...>` 안에 공백이 있는 형식은 점검에서 빠짐(현재 해당 링크 없음). CI 편입 전 보완 검토 | S01-T02 | | new | |
| BL-022 | database.md 트랜잭션 절 "기본값(Read Committed)을 쓴다"를 ADR-0014 "Read Committed 명시"에 맞춰 정리 | S01-T02 | | new | S01-T04 |
| BL-023 | 23505가 UoW에서 Result로 바뀌어도 EF가 CommandError · SaveChangesFailed를 Error로 먼저 기록(정상 경합 경로에 Error 로그 2건). ConfigureWarnings로 수준을 낮출지 결정 | S01-T03 | | new | S02-T04 |
| BL-024 | Aspire 대시보드 추적에서 db.connection_string 태그에 비밀번호가 없는지, 로그 · 추적에 파라미터 값이 없는지 실측 | S01-T03 | | new | S03-T05 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | `new` 상태 추가: 발견 즉시 기록, 스프린트 종료 때 정리 |
