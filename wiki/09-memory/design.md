---
title: "설계 요약"
type: memory
tags: [memory]
created: 2026-09-27
updated: 2026-09-27
---

# 설계 요약

> [장기기억](README.md) · 원본: [ADR 목록](../03-architecture/adr/README.md), [기술 스택](../03-architecture/tech-stack.md)

## 확정 (ADR 승인)

| ADR | 결정 | 핵심 결과 |
|---|---|---|
| [0001](../03-architecture/adr/0001-use-dotnet8.md) | .NET 8 (C#) | Target Framework `net8.0`. 지원 종료(2026-11-10)를 알고 유지하기로 결정 |
| [0002](../03-architecture/adr/0002-adopt-msa.md) | MSA | 바운디드 컨텍스트 단위 서비스, 서비스별 독립 배포와 DB(Database per Service) |
| [0003](../03-architecture/adr/0003-clean-architecture-and-ddd.md) | Clean Architecture + DDD | Domain 레이어는 프레임워크 비의존, 레이어 규칙은 아키텍처 테스트로 검증 |
| [0004](../03-architecture/adr/0004-adopt-event-driven-architecture.md) | EDA | 서비스 간 상태 전파는 통합 이벤트 비동기 메시징, Outbox 패턴, 멱등성 검토 |
| [0005](../03-architecture/adr/0005-use-postgresql.md) | PostgreSQL | EF Core + Npgsql, 서비스별 Database(또는 Schema), 테스트는 컨테이너 DB |
| [0006](../03-architecture/adr/0006-adopt-tdd.md) | TDD | 도메인/애플리케이션 로직은 테스트 먼저, PR은 테스트 통과 필수 |
| [0007](../03-architecture/adr/0007-adopt-cqrs.md) | CQRS | 같은 DB에서 Command / Query 분리, Query는 Read Repository 프로젝션 |
| [0008](../03-architecture/adr/0008-integer-codes-and-bitmask.md) | 정수 코드 · 비트 마스킹 | 문자열 코드 절대 금지(`smallint` + enum), 조합 코드는 `[Flags]` 비트 마스킹, API · 이벤트 · 에러 코드도 정수 |
| [0009](../03-architecture/adr/0009-separate-read-write-db-context.md) | 읽기 / 쓰기 분리 | 연결 문자열 · DbContext 분리(현재 같은 DB), Repository는 람다 LINQ 쿼리만 |
| [0010](../03-architecture/adr/0010-convention-based-di-registration.md) | DI 자동 등록 | 마커 인터페이스 / 기반 클래스 + 어셈블리 검색, Scoped |

형상관리는 GitHub로 확정했습니다(ADR 없음).

## 방향 결정 (PRD-001, S01에서 ADR로 확정 예정)

원본: [PRD-001 질문과 답변](../10-delivery/prd/PRD-001-foundation.md#질문과-답변)

- **로컬 인프라**: .NET Aspire 9.x AppHost(docker compose 없음), SDK .NET 8. PostgreSQL + MigrationService(Api는 완료 대기), DB `emergency_hub_employee` / 롤 `employee_app`, Write / Read 연결 주입(Read는 `default_transaction_read_only=on`)
- **도입 보류**: 메시지 브로커, Outbox / Inbox, API Gateway, 로그 수집기(로컬 관측은 Aspire 대시보드)
- **애플리케이션**: Mediator 직접 구현(로깅 → 검증 → 트랜잭션 → Handler), 트랜잭션 데코레이터가 실행 전략 안에서 SaveChanges · 커밋(Handler는 저장 안 함), Controller, Scrutor(0010 구체화), FluentValidation, Serilog + OTLP, Swashbuckle
- **ID · 테스트**: UUID v7은 `IIdGenerator` + UUIDNext(Handler가 생성), AwesomeAssertions, Respawn, coverlet + ReportGenerator
- **정책**: 운영 배포(Phase 4) 전까지 마이그레이션 리셋 허용, 도메인 이벤트는 수집만
- 미해결: 고정할 Aspire 9.x 마이너 버전(S01-T01)

## 검토 중 (초안 기본값)

- **메시지 브로커**(보류 후 재검토): RabbitMQ 추천안 / Kafka. MassTransit은 v9부터 상용
- **API Gateway**(보류 후 재검토): YARP 추천안 / Ocelot
- **공통 라이브러리**: OpenTelemetry, Polly, Redis(필요 시)
- **테스트 도구**: xUnit, NSubstitute, Testcontainers, NetArchTest
- **인프라**: GitHub Actions, Docker, Kubernetes(필요 시, Phase 4)

## 기준 문서 (draft)

[코딩 컨벤션](../04-development/coding-conventions.md) · [데이터베이스](../04-development/database.md) · [테스트 전략](../04-development/testing-strategy.md)(성공 / 실패 / 엣지 필수) · [Clean Architecture](../03-architecture/clean-architecture.md) · [로깅](../04-development/logging-observability.md)(콘솔 텍스트 / 파일 JSON) · [TDD 가이드](../04-development/tdd-guide.md) · [API 설계](../04-development/api-guidelines.md)(ProblemDetails `code`, `Idempotency-Key`) · [에러 코드](../05-api/error-codes.md)(`S T NNN`, 로그 이벤트 ID `S0NNN`) · [기술 스택](../03-architecture/tech-stack.md)


## 미착수

솔루션 코드(PRD-001 S01~S04에서 작성), 도메인 모델(유비쿼터스 언어, 바운디드 컨텍스트, Aggregate), 서비스 상세와 의존 관계, API / 이벤트 명세
