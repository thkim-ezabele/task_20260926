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
| TD-001 | 마이그레이션 전용 DB 롤과 애플리케이션 롤 분리 (로컬은 `employee_app` 단일 롤) | 설계 | 중 | S01 계획 리뷰(Q4) | Phase 4(배포 토픽)에서 마이그레이션 전용 롤 분리. security.md:71 정리(BL-038)와 연계 | open |
| TD-002 | .NET 8(2026-11-10) · Aspire 9.x 지원 종료에 따른 메이저 업그레이드(EF Core / Npgsql 포함) | 인프라 | 상 | S01 계획 리뷰 | BL-002 결정 뒤 .NET 10 · Aspire 13 · EF Core 메이저 전환 토픽. TD-005 · 007 · 008 · 012 연쇄 상환 | open |
| TD-003 | Npgsql.EFCore 8.0.11의 EF 의존에 상한이 없음: `CentralPackageTransitivePinningEnabled`로 EF 8.0.31 고정해야 EF9 유입 방지 | 인프라 | 하 | S01-T01 | CentralPackageTransitivePinning으로 완화. EF 메이저 전환(TD-002) 때 재검토 | open |
| TD-004 | Testcontainers `PostgreSqlBuilder` 매개변수 없는 생성자는 CS0618로 빌드 실패: 이미지 인자 필수 | 테스트 | 하 | S01-T01 | S03-T05 Testcontainers 이미지 인자 명시로 resolved | planned:S03 |
| TD-005 | Aspire 9.x 지원 종료(NuGet out of support) 상태로 9.5.2 사용: 보안 패치 없음. .NET 8 지원 종료와 함께 해소(재검토 BL-002) | 인프라 | 중 | S01-T01 | TD-002에 병합(Aspire 전환 시 상환). AppHost는 로컬 전용이라 운영 영향 제한적 | open |
| TD-006 | 클라이언트 통합을 쓰고 OpenTelemetry.Api만 1.15.3으로 고정하면 OpenTelemetry 1.9.0과 혼재: OTel 계열 전체 동일 버전 고정 필요(BL-004 결과에 따라 발생) | 인프라 | 하 | S01-T01 | 발생하지 않음: ADR-0011 클라이언트 통합 미사용 (608f828) | resolved |
| TD-007 | Aspire.Hosting 9.5.2 전이 MessagePack 2.5.192 취약(GHSA 11건, 높음 2건): 2.5.305 수동 고정 유지(AppHost 한정, Aspire 전환 시 제거) | 인프라 | 중 | S01-T01 | AppHost 한정 수동 고정 유지, NuGetAuditMode=all로 감시. Aspire 전환(TD-002) 때 제거 | open |
| TD-008 | net8.0 앱에 Microsoft.Extensions.* · System.Diagnostics.DiagnosticSource 10.0.0 전이 유입(Scrutor 7, Serilog.AspNetCore 10, OpenTelemetry 1.19). 취약점 없는 OTel은 모두 DiagnosticSource 10 요구 | 인프라 | 중 | S01-T01 | S02 · S03 실행 시 런타임 호환 관찰. .NET 10 전환(TD-002)으로 해소 | open |
| TD-009 | xUnit v2 선택 시 xunit 2.9.3이 NuGet Legacy 폐기(보안 수정만) | 테스트 | 하 | S01-T01 | 발생하지 않음: ADR-0021 xUnit v3 채택 (5b94975) | resolved |
| TD-010 | 커밋 응답 중 연결이 끊기면 실행 전략이 SaveChanges를 재실행해 pk_ 23505 · xmin 충돌을 잘못 보고할 수 있음. 운영 전 verifySucceeded 또는 멱등 키로 해소 | 설계 | 중 | S01-T02 | Phase 4 전 verifySucceeded 또는 Idempotency-Key로 해소. S03-T05 동시성 테스트에서 재현 범위 기록 권장 | open |
| TD-011 | EF Core 8 MigrateAsync에 마이그레이션 잠금 없음(EF 9 추가): 적용 주체를 MigrationService 1개로 제한, Phase 4는 번들 / 스크립트 | 인프라 | 중 | S01-T02 | S03-T04 적용 주체 MigrationService 1개 유지. Phase 4 번들 / 스크립트 | open |
| TD-012 | ServiceDefaults가 Aspire 템플릿과 달라짐(OTel 로그 공급자 제거, 신호별 OTLP 내보내기, ADR-0020). Aspire 버전을 올릴 때 차이를 다시 적용해야 함 | 인프라 | 하 | S01-T03 | S03-T04 구현 때 템플릿 차이를 주석 · ADR-0020 링크로 남김. Aspire 전환 때 재적용 | open |
| TD-013 | NetArchTest.Rules 1.3.2(2021-05 이후 릴리스 없음) · NSubstitute.Analyzers.CSharp 1.0.17(2024-02 이후 없음) 유지 중단. 막히면 ArchUnitNET 전환 검토(ADR-0021) | 테스트 | 중 | S01-T04 | S02-T05에서 막히면 ArchUnitNET 전환(ADR-0021). S02 계획 리뷰 위험으로 등록 | open |
| TD-014 | 테스트 프로젝트 판별이 이름 규칙(MSBuildProjectName이 Tests로 끝남)에 의존: 규칙 밖 이름은 OutputType=Exe · 문서 생성 제외가 적용되지 않음. 아키텍처 테스트 / CI 점검으로 보완 검토 | 코드 | 하 | S01-T05 | S02-T05 아키텍처 테스트 또는 CI 점검에 프로젝트 이름 규칙 추가 검토(S02 계획 리뷰) | open |
| TD-015 | 강타입 ID용 ValueConverter 공통 등록(ConfigureConventions)과 AggregateRoot.DomainEvents Ignore 공통 처리를 BuildingBlocks 영속성 계층에서 제공(첫 DbContext 작업에서 반영) | 설계 | 중 | S01-T06 | S02-T04 강타입 ID 변환 + ValueGeneratedNever, DomainEvents Ignore 공통 처리 | planned:S02 |
| TD-016 | Error는 non-sealed record라 컴파일러가 만드는 protected 복사 생성자로 어셈블리 밖에서도 파생 가능(불변식은 유지). coding-conventions:61 · Error.cs remarks의 "같은 어셈블리로 막음" 표현이 부정확(CS8878). 문서 수정 또는 아키텍처 테스트(BL-054)로 보완 | 코드 | 하 | S01-T06 | BL-056 병합. S04-T02에서 coding-conventions:61 · Error.cs remarks 문구 정정, 강제는 BL-029 | planned:S04 |
| TD-017 | Result<T>는 성공 값 null을 런타임에만 거부하고 `where T : notnull` 제약 없음(Mediator 인터페이스 전파 부담) | 코드 | 하 | S01-T06 | S02-T01 Mediator 인터페이스 정의 때 notnull 제약 전파 재판단 | open |
| TD-018 | GitHub Free private라 브랜치 보호 불가 → CI를 필수 체크로 걸 수 없음. "CI 미통과 PR 병합 금지"를 규칙으로만 지킴 | 인프라 | 중 | S01-T07 | 상환 수단 없음(요금제). "CI 통과 전 병합 금지"를 PR 체크리스트 · retro 점검으로 유지 | open |
| TD-019 | 테스트 프로젝트에 Microsoft.Extensions.Logging · DependencyInjection 8.0.1(Diagnostics.Testing 전이)과 *.Abstractions 10.0.0이 섞여 해석됨(메이저 혼합) | 의존성 | 하 | S02-T02 | .NET 10 전환(BL-002 · TD-002) 때 함께 정리(TD-008 관찰 연장) | new |
| TD-020 | Scrutor 7 Decorate가 keyed 서비스(IKeyedServiceProvider)에 의존: 앱 호스트 공유 프레임워크 DI 8.0과 Abstractions 10.0.0 혼합 런타임 호환 미확인 | 의존성 | 중 | S02-T03 | S03 호스트 실행 때 확인(TD-008 관찰 확장) | new |
| TD-021 | BadHttpRequestException의 원래 상태 코드(413 · 408 등)를 모두 400 · 1001로 응답(원래 코드는 로그 302에만) | 설계 | 하 | S02-T06 | api-guidelines 정비 때 상태 코드 보존 여부 결정 | new |
| TD-022 | ExceptionHandlerMiddleware 범주 전체를 로그 필터로 끄므로 같은 범주의 다른 로그(응답 시작 후 Warning, 요청 중단 Debug)도 사라짐. 이벤트 ID 단위 필터 불가 | 설계 | 중 | S02-T06 | .NET 10 전환(BL-002) 때 억제 옵션 검토 | new |
| TD-023 | 바인딩 오류 errors 키 순서가 ModelStateDictionary 열거 순서를 따르고 요청 속성 순서를 보장하지 않음 | 설계 | 하 | S02-T06 | 클라이언트 요구가 생기면 정렬 규칙 결정 | new |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | `new` 상태 추가: 발견 즉시 기록, 스프린트 종료 때 정리 |
| 2026-09-27 | - | S01 종료 정리: TD-001~018 (resolved 2, planned 3, open 13, TD-005는 TD-002에 병합) |
