---
title: "기술부채"
type: index
status: stable
tags: [delivery, tech-debt]
aliases: [Tech Debt]
created: 2026-09-27
updated: 2026-09-27
---

# 기술부채

> 알면서 타협한 구현, 누락된 테스트, 미뤄 둔 리팩터링처럼 나중에 갚아야 할 기술적 빚을 기록합니다.
> 새 기능이나 미룬 요구사항은 [백로그](backlog.md)에 적습니다.
>
> [개발 관리](README.md)

## 작성 규칙

- 타협한 즉시 `new` 상태로 행을 추가하고 그 단계의 커밋에 포함한다. 영향도와 상환 계획은 스프린트 종료 때 orchestrator가 정리한다. 코드에는 `// TODO(TD-NNN): ...` 주석으로 위치를 남긴다. ID는 `TD-NNN`으로 순서대로 붙이고 재사용하지 않는다.
- 행은 지우지 않는다. 갚으면 상태를 `resolved`로 바꾸고 해결 커밋 / 작업 ID를 적는다.
- **유형**: `코드` · `설계` · `테스트` · `문서` · `인프라`
- **영향도**: `상`(장애·보안·데이터 위험) / `중`(변경 비용 증가) / `하`(가독성·정리)
- **상태**: `new`(미정리) · `open` · `planned:SNN` · `resolved`

## 목록

| ID | 제목 | 유형 | 영향도 | 발생 | 상환 계획 | 상태 |
|---|---|---|---|---|---|---|
| TD-001 | 마이그레이션 전용 DB 롤과 애플리케이션 롤 분리 (로컬은 `employee_app` 단일 롤) | 설계 | | S01 계획 리뷰(Q4) | | new |
| TD-002 | .NET 8(2026-11-10) · Aspire 9.x 지원 종료에 따른 메이저 업그레이드(EF Core / Npgsql 포함) | 인프라 | | S01 계획 리뷰 | | new |
| TD-003 | Npgsql.EFCore 8.0.11의 EF 의존에 상한이 없음: `CentralPackageTransitivePinningEnabled`로 EF 8.0.31 고정해야 EF9 유입 방지 | 인프라 | | S01-T01 | | new |
| TD-004 | Testcontainers `PostgreSqlBuilder` 매개변수 없는 생성자는 CS0618로 빌드 실패: 이미지 인자 필수 | 테스트 | | S01-T01 | | new |
| TD-005 | Aspire 9.x 지원 종료(NuGet out of support) 상태로 9.5.2 사용: 보안 패치 없음. .NET 8 지원 종료와 함께 해소(재검토 BL-002) | 인프라 | | S01-T01 | | new |
| TD-006 | 클라이언트 통합을 쓰고 OpenTelemetry.Api만 1.15.3으로 고정하면 OpenTelemetry 1.9.0과 혼재: OTel 계열 전체 동일 버전 고정 필요(BL-004 결과에 따라 발생) | 인프라 | | S01-T01 | | new |
| TD-007 | Aspire.Hosting 9.5.2 전이 MessagePack 2.5.192 취약(GHSA 11건, 높음 2건): 2.5.305 수동 고정 유지(AppHost 한정, Aspire 전환 시 제거) | 인프라 | | S01-T01 | | new |
| TD-008 | net8.0 앱에 Microsoft.Extensions.* · System.Diagnostics.DiagnosticSource 10.0.0 전이 유입(Scrutor 7, Serilog.AspNetCore 10, OpenTelemetry 1.19). 취약점 없는 OTel은 모두 DiagnosticSource 10 요구 | 인프라 | | S01-T01 | | new |
| TD-009 | xUnit v2 선택 시 xunit 2.9.3이 NuGet Legacy 폐기(보안 수정만) | 테스트 | | S01-T01 | | new |
| TD-010 | 커밋 응답 중 연결이 끊기면 실행 전략이 SaveChanges를 재실행해 pk_ 23505 · xmin 충돌을 잘못 보고할 수 있음. 운영 전 verifySucceeded 또는 멱등 키로 해소 | 설계 | | S01-T02 | | new |
| TD-011 | EF Core 8 MigrateAsync에 마이그레이션 잠금 없음(EF 9 추가): 적용 주체를 MigrationService 1개로 제한, Phase 4는 번들 / 스크립트 | 인프라 | | S01-T02 | | new |
| TD-012 | ServiceDefaults가 Aspire 템플릿과 달라짐(OTel 로그 공급자 제거, 신호별 OTLP 내보내기, ADR-0020). Aspire 버전을 올릴 때 차이를 다시 적용해야 함 | 인프라 | | S01-T03 | | new |
| TD-013 | NetArchTest.Rules 1.3.2(2021-05 이후 릴리스 없음) · NSubstitute.Analyzers.CSharp 1.0.17(2024-02 이후 없음) 유지 중단. 막히면 ArchUnitNET 전환 검토(ADR-0021) | 테스트 | | S01-T04 | | new |
| TD-014 | 테스트 프로젝트 판별이 이름 규칙(MSBuildProjectName이 Tests로 끝남)에 의존: 규칙 밖 이름은 OutputType=Exe · 문서 생성 제외가 적용되지 않음. 아키텍처 테스트 / CI 점검으로 보완 검토 | 코드 | | S01-T05 | | new |
| TD-015 | 강타입 ID용 ValueConverter 공통 등록(ConfigureConventions)과 AggregateRoot.DomainEvents Ignore 공통 처리를 BuildingBlocks 영속성 계층에서 제공(첫 DbContext 작업에서 반영) | 설계 | | S01-T06 | | new |
| TD-016 | Error는 non-sealed record라 컴파일러가 만드는 protected 복사 생성자로 어셈블리 밖에서도 파생 가능(불변식은 유지). coding-conventions:61 · Error.cs remarks의 "같은 어셈블리로 막음" 표현이 부정확(CS8878). 문서 수정 또는 아키텍처 테스트(BL-054)로 보완 | 코드 | | S01-T06 | | new |
| TD-017 | Result<T>는 성공 값 null을 런타임에만 거부하고 `where T : notnull` 제약 없음(Mediator 인터페이스 전파 부담) | 코드 | | S01-T06 | | new |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | `new` 상태 추가: 발견 즉시 기록, 스프린트 종료 때 정리 |
