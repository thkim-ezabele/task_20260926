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

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | `new` 상태 추가: 발견 즉시 기록, 스프린트 종료 때 정리 |
