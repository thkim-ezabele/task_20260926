---
title: "기술 스택"
type: doc
status: draft
tags: [architecture]
created: 2026-09-27
updated: 2026-09-27
---

# 기술 스택

> 백엔드에서 사용하는 기술과 결정 상태를 정리합니다. 결정 배경은 [ADR](adr/README.md)을 참고합니다.
> 고정 버전 · 라이선스 · 출처는 [패키지 버전 · 라이선스](package-versions.md)에 있습니다.
> 상태: 🟢 확정(ADR 또는 기준 문서에서 채택) · 보류(도입 보류 ADR, 재검토 시점 있음) · 이후 토픽(지금 범위에 필요 없음, 필요할 때 결정). 확정되지 않은 항목의 처리 방식은 [남은 항목 처리 방식](#남은-항목-처리-방식)에 모읍니다.
>
> [위키 홈](../README.md)

## 런타임 & 아키텍처

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 런타임 | .NET 8 (LTS) / C# 12 | 🟢 확정 | [ADR-0001](adr/0001-use-dotnet8.md). LTS 지원 종료(2026-11-10)를 알고 **현재 버전 유지**로 결정(2026-09-27, 사용자) |
| 아키텍처 스타일 | MSA | 🟢 확정 | [ADR-0002](adr/0002-adopt-msa.md) |
| 서비스 내부 구조 | Clean Architecture + DDD | 🟢 확정 | [ADR-0003](adr/0003-clean-architecture-and-ddd.md) |
| 서비스 간 통신 | Event-Driven Architecture (Outbox / Inbox) | 🟢 확정 | [ADR-0004](adr/0004-adopt-event-driven-architecture.md). 브로커 · Outbox / Inbox 구현은 보류([ADR-0023](adr/0023-deferred-adoptions.md)), ADR-0004는 유지 |
| 유스케이스 구조 | CQRS (같은 DB, 모델 분리) | 🟢 확정 | [ADR-0007](adr/0007-adopt-cqrs.md) |
| DI | 기본 컨테이너 + 규칙 기반 자동 등록(Scoped) | 🟢 확정 | [ADR-0010](adr/0010-convention-based-di-registration.md), 구현은 Scrutor([ADR-0017](adr/0017-scrutor-for-convention-based-di.md)) |
| 로컬 오케스트레이션 | .NET Aspire 9.5.2 AppHost | 🟢 확정 | [ADR-0011](adr/0011-use-aspire-local-orchestration.md). docker compose 없음, 지원 종료 상태로 사용(TD-005) |
| 웹 프레임워크 | ASP.NET Core (Controller) | 🟢 확정 | [ADR-0016](adr/0016-use-controllers-for-api.md): `[ApiController]` Controller |
| API Gateway | YARP / Ocelot | 보류 | [ADR-0023](adr/0023-deferred-adoptions.md). 재검토 추천안 YARP |

## 데이터

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| RDBMS | PostgreSQL | 🟢 확정 | [ADR-0005](adr/0005-use-postgresql.md) |
| ORM | EF Core 8 + Npgsql | 🟢 확정 | [ADR-0009](adr/0009-separate-read-write-db-context.md): 읽기 / 쓰기 DbContext 분리, Repository는 람다 LINQ만 |
| 명명 규칙 변환 | EFCore.NamingConventions (snake_case) | 🟢 확정 | [데이터베이스](../04-development/database.md) |
| 코드값 | 정수(`smallint`) + C# enum, 조합은 비트 마스킹 | 🟢 확정 | [ADR-0008](adr/0008-integer-codes-and-bitmask.md) |
| 기본 키 | UUID v7 (UUIDNext) | 🟢 확정 | [ADR-0013](adr/0013-uuid-v7-with-uuidnext.md): `IIdGenerator` + `Uuid.NewDatabaseFriendly(Database.PostgreSql)`, Handler가 생성 |
| 마이그레이션 적용 | MigrationService(Worker) + 운영 전 리셋 허용 | 🟢 확정 | [ADR-0012](adr/0012-migration-apply-and-pre-production-reset.md) |
| 캐시 | Redis | 이후 토픽 | 필요 시 |

## 메시징

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 메시지 브로커 | RabbitMQ / Kafka | 보류 | [ADR-0023](adr/0023-deferred-adoptions.md). 재검토 추천안 RabbitMQ, [EDA](event-driven-architecture.md) 비교 참고 |
| 메시징 추상화 | MassTransit 8.x / 브로커 클라이언트 직접 사용 | 보류 | [ADR-0023](adr/0023-deferred-adoptions.md). MassTransit v9+는 상용 라이선스라 제외 |
| Outbox / Inbox | 직접 구현 / MassTransit EF Core Outbox | 보류 | [ADR-0023](adr/0023-deferred-adoptions.md). 확장 지점은 UnitOfWork 커밋 전([ADR-0014](adr/0014-command-transaction-boundary-and-unit-of-work.md)) |

## 공통 라이브러리

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| Mediator (CQRS 파이프라인) | 직접 구현 | 🟢 확정 | [ADR-0015](adr/0015-custom-mediator-pipeline.md): 데코레이터 파이프라인(로깅 → 검증 → 트랜잭션 → Handler). MediatR v13+는 상용이라 제외 |
| Command 트랜잭션 | UnitOfWork + 실행 전략 | 🟢 확정 | [ADR-0014](adr/0014-command-transaction-boundary-and-unit-of-work.md): SaveChanges · 커밋만 재시도, Handler는 저장하지 않음 |
| 유효성 검사 | FluentValidation | 🟢 확정 | [ADR-0018](adr/0018-use-fluentvalidation.md): Mediator 검증 데코레이터에서 실행 |
| DI 타입 검색 | Scrutor | 🟢 확정 | [ADR-0017](adr/0017-scrutor-for-convention-based-di.md): ADR-0010 구체화(`Scan` + `Decorate`) |
| 로깅 | Serilog (`ILogger<T>` 공급자) + OTLP 싱크 | 🟢 확정 | [ADR-0020](adr/0020-logging-with-serilog-and-otlp.md), [로깅 & 관측성](../04-development/logging-observability.md): 콘솔 텍스트 / 파일 JSON(CLEF) / Aspire 대시보드 |
| 로그 수집기 | Seq / Loki / ELK | 보류 | [ADR-0023](adr/0023-deferred-adoptions.md). 로컬 관측은 Aspire 대시보드 |
| 관측성 | OpenTelemetry (추적 · 메트릭, W3C Trace Context) | 🟢 확정 | [ADR-0020](adr/0020-logging-with-serilog-and-otlp.md): OTLP로 Aspire 대시보드에 내보냄. 추적 수집 백엔드(Jaeger / Tempo)는 보류([ADR-0023](adr/0023-deferred-adoptions.md)) |
| API 문서 | OpenAPI (Swashbuckle) | 🟢 확정 | [ADR-0019](adr/0019-use-swashbuckle-openapi.md): Development에서만 노출 |
| 회복성 | Polly | 이후 토픽 | 나가는 HTTP 호출이 생길 때. ServiceDefaults의 `AddStandardResilienceHandler`는 템플릿대로 포함([ADR-0011](adr/0011-use-aspire-local-orchestration.md)), DB 재시도는 `EnableRetryOnFailure` |
| 시간 | `TimeProvider` | 🟢 확정 | `DateTime.UtcNow` 직접 호출 금지 |

## 테스트 (TDD)

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 개발 방법론 | TDD | 🟢 확정 | [ADR-0006](adr/0006-adopt-tdd.md), [테스트 전략](../04-development/testing-strategy.md) |
| 테스트 프레임워크 | xUnit v3 (`xunit.v3`) | 🟢 확정 | [ADR-0021](adr/0021-test-tooling-xunit-v3-and-awesomeassertions.md). SDK 8.0.4xx 이상 필요 |
| 단언 | AwesomeAssertions | 🟢 확정 | [ADR-0021](adr/0021-test-tooling-xunit-v3-and-awesomeassertions.md). FluentAssertions는 어떤 버전도 쓰지 않음(v8+ 상용) |
| Test Double | NSubstitute | 🟢 확정 | [TDD 가이드](../04-development/tdd-guide.md#test-double-사용-기준), 버전은 ADR-0021 |
| 통합 테스트 | Testcontainers (PostgreSQL) + Respawn | 🟢 확정 | [ADR-0022](adr/0022-respawn-and-coverage-tooling.md). InMemory / SQLite 대체 금지. 브로커 컨테이너는 보류(ADR-0023) |
| 시간 고정 | Microsoft.Extensions.TimeProvider.Testing | 🟢 확정 | `FakeTimeProvider` |
| 아키텍처 테스트 | NetArchTest.Rules | 🟢 확정 | 레이어 의존, 마커 상속, record 규칙 |
| 커버리지 | coverlet.collector + ReportGenerator | 🟢 확정 | [ADR-0022](adr/0022-respawn-and-coverage-tooling.md). BuildingBlocks · Employee Domain / Application 80% 목표(보고만) |

## 인프라 & DevOps

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 형상관리 | GitHub, Git Flow | 🟢 확정 | [Git 워크플로우](../04-development/git-workflow.md) |
| CI | GitHub Actions | 🟢 확정 | PRD-001 FR-10(Q7), 워크플로는 S01-T07. CD(배포)는 Phase 4 |
| 컨테이너 | Docker | 🟢 확정 | 로컬 인프라(Aspire가 컨테이너 실행)와 Testcontainers에 필수. docker compose는 쓰지 않음([ADR-0011](adr/0011-use-aspire-local-orchestration.md)). 서비스 이미지 빌드는 Phase 4 |
| 오케스트레이션 | Kubernetes | 이후 토픽 | 필요 시(Phase 4) |

## 남은 항목 처리 방식

확정되지 않은 항목과 처리 방식입니다([PRD-001](../10-delivery/prd/PRD-001-foundation.md) FR-01). 보류 항목의 재검토 시점(트리거)은 [ADR-0023](adr/0023-deferred-adoptions.md)이 원본입니다.

| 항목 | 처리 | 근거 · 시점 |
|---|---|---|
| 메시지 브로커 · 메시징 추상화 | 보류 | ADR-0023. 두 번째 서비스가 통합 이벤트를 받는 토픽 |
| Outbox / Inbox | 보류 | ADR-0023. 브로커 도입과 같은 토픽 |
| API Gateway | 보류 | ADR-0023. 공개 Api 2개 이상, Identity 토픽, Phase 4 배포 설계 |
| 로그 수집기 · 추적 수집 백엔드 | 보류 | ADR-0023. 로컬 밖 공유 환경(Phase 4), 알림 기준 설계 |
| 캐시(Redis) | 이후 토픽 | 조회 성능 요구가 생길 때 |
| 회복성(Polly) | 이후 토픽 | 나가는 HTTP 호출이 생길 때 |
| 오케스트레이션(Kubernetes) · CD | 이후 토픽 | Phase 4 배포 |
| 계약 테스트 도구 | 이후 토픽 | 서비스 간 연동 시작 시([테스트 전략](../04-development/testing-strategy.md#계약-테스트-api--이벤트)) |
| GitHub Actions 메이저 태그 / SHA 고정 | S01-T07 | [패키지 버전 · 라이선스](package-versions.md#github-actions) |

## 패키지 버전 · 라이선스

도입 패키지의 고정 버전, 라이선스, 대상 프레임워크, 출처는 [패키지 버전 · 라이선스](package-versions.md)에 표로 정리합니다(S01-T01). 요약: Aspire 9.5.2(지원 종료 상태, TD-005), EF Core 8.0.31 · Npgsql.EFCore 8.0.11, PostgreSQL 이미지 `17`, SDK 하한 8.0.400(`rollForward: latestFeature`), 상용 라이선스 0건.

## 개발 도구

| 항목 | 선택 | 비고 |
|---|---|---|
| AI 개발 흐름 | Claude Code 서브에이전트 + 스킬 | [개발 관리](../10-delivery/README.md), [에이전트 워크플로우](../10-delivery/agents.md) |
| 스크립트 | Node.js | Windows 환경에서 `python`은 스토어 스텁이라 사용하지 않음 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 (확정 사항 + 초안 기본값) |
| 2026-09-27 | - | .NET 8 유지 결정 기록 |
| 2026-09-27 | - | ADR 0007~0010과 기준 문서 결정 반영: CQRS, EF Core, 정수 코드, Serilog, NSubstitute, Testcontainers 등 확정 / MediatR · FluentAssertions 라이선스로 후보 전환 |
| 2026-09-27 | developer | 패키지 버전 · 라이선스 문서 링크 추가 (S01-T01) |
| 2026-09-27 | developer | ADR 0011~0023 반영: 후보(미정) 항목을 확정 / 보류 / 이후 토픽으로 정리, 남은 항목 처리 방식 표 추가 (S01-T04) |
