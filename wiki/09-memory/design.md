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
| [0001](../03-architecture/adr/0001-use-dotnet8.md) | .NET 8 (C#) | Target Framework `net8.0` |
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

## 검토 중 (초안 기본값)

- **메시지 브로커**: RabbitMQ / Kafka (미정), 추상화는 MassTransit
- **API Gateway**: YARP / Ocelot (미정)
- **공통 라이브러리**: ASP.NET Core(API 스타일 미정, Minimal API 기본안), Mediator(MediatR v13+ 상용 → 구현체 미정), FluentValidation, Serilog, OpenTelemetry, Swashbuckle, Polly, Redis(필요 시)
- **테스트 도구**: xUnit, 단언 라이브러리(FluentAssertions v8+ 상용 → v7 / Shouldly / AwesomeAssertions 미정), NSubstitute, Testcontainers, Respawn, NetArchTest
- **인프라**: GitHub Actions, Docker / docker compose, Kubernetes(필요 시)

## 기준 문서 (draft)

[코딩 컨벤션](../04-development/coding-conventions.md) · [데이터베이스](../04-development/database.md) · [테스트 전략](../04-development/testing-strategy.md)(성공 / 실패 / 엣지 필수) · [Clean Architecture](../03-architecture/clean-architecture.md) · [로깅](../04-development/logging-observability.md)(콘솔 텍스트 / 파일 JSON) · [TDD 가이드](../04-development/tdd-guide.md) · [API 설계](../04-development/api-guidelines.md)(ProblemDetails `code`, `Idempotency-Key`) · [에러 코드](../05-api/error-codes.md)(`S T NNN`, 로그 이벤트 ID `S0NNN`) · [기술 스택](../03-architecture/tech-stack.md)

미정(기반 구축 토픽에서 결정): Mediator 구현체, 단언 라이브러리, UUID v7 생성, API 스타일, 타입 검색 구현, 로그 수집기

## 미착수

솔루션 코드(PRD-001 기반 구축 예정), 도메인 모델(유비쿼터스 언어, 바운디드 컨텍스트, Aggregate), 서비스 상세와 의존 관계, API / 이벤트 명세
