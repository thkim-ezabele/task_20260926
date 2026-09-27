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

형상관리는 GitHub로 확정했습니다(ADR 없음).

## 검토 중 (초안 기본값)

- **메시지 브로커**: RabbitMQ / Kafka (미정), 추상화는 MassTransit
- **API Gateway**: YARP / Ocelot (미정)
- **공통 라이브러리**: ASP.NET Core Web API, MediatR, FluentValidation, Serilog, OpenTelemetry, Swashbuckle, Polly, Redis(필요 시)
- **테스트 도구**: xUnit, FluentAssertions, NSubstitute / Moq, Testcontainers, NetArchTest
- **인프라**: GitHub Actions, Docker / docker compose, Kubernetes(필요 시)

## 미착수

도메인 모델(유비쿼터스 언어, 바운디드 컨텍스트, Aggregate), 서비스 상세와 의존 관계, API / 이벤트 명세
