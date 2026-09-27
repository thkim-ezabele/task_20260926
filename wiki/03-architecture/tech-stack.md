# 기술 스택

> 백엔드에서 사용하는 기술과 선정 사유를 정리합니다. 결정 배경은 [ADR](adr/README.md)을 참고합니다.
>
> 상태: 🟡 검토 중 · [위키 홈](../README.md)

## 런타임 & 아키텍처

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 런타임 | .NET 8 (LTS) / C# | 🟢 확정 | [ADR-0001](adr/0001-use-dotnet8.md) |
| 아키텍처 스타일 | MSA | 🟢 확정 | [ADR-0002](adr/0002-adopt-msa.md) |
| 서비스 내부 구조 | Clean Architecture + DDD | 🟢 확정 | [ADR-0003](adr/0003-clean-architecture-and-ddd.md) |
| 서비스 간 통신 | Event-Driven Architecture | 🟢 확정 | [ADR-0004](adr/0004-adopt-event-driven-architecture.md) |
| 웹 프레임워크 | ASP.NET Core Web API | 🟡 검토 중 | |
| API Gateway | YARP / Ocelot | 🟡 검토 중 | |

## 데이터

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| RDBMS | PostgreSQL | 🟢 확정 | [ADR-0005](adr/0005-use-postgresql.md) |
| ORM | EF Core 8 (Npgsql) | 🟡 검토 중 | |
| 캐시 | Redis | 🟡 검토 중 | 필요 시 |

## 메시징

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 메시지 브로커 | RabbitMQ / Kafka | 🟡 검토 중 | |
| 메시징 추상화 | MassTransit | 🟡 검토 중 | |

## 공통 라이브러리

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| CQRS / Mediator | MediatR | 🟡 검토 중 | |
| 유효성 검사 | FluentValidation | 🟡 검토 중 | |
| 로깅 | Serilog | 🟡 검토 중 | |
| 관측성 | OpenTelemetry | 🟡 검토 중 | |
| API 문서 | Swashbuckle (Swagger) | 🟡 검토 중 | |
| 회복성 | Polly | 🟡 검토 중 | |

## 테스트 (TDD)

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 개발 방법론 | TDD | 🟢 확정 | [ADR-0006](adr/0006-adopt-tdd.md) |
| 테스트 프레임워크 | xUnit | 🟡 검토 중 | |
| Assertion | FluentAssertions | 🟡 검토 중 | |
| Mocking | NSubstitute / Moq | 🟡 검토 중 | |
| 통합 테스트 | Testcontainers (PostgreSQL / 브로커) | 🟡 검토 중 | |
| 아키텍처 테스트 | NetArchTest | 🟡 검토 중 | 레이어 의존성 규칙 검증 |

## 인프라 & DevOps

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 형상관리 | GitHub | 🟢 확정 | |
| CI/CD | GitHub Actions | 🟡 검토 중 | |
| 컨테이너 | Docker / docker compose | 🟡 검토 중 | |
| 오케스트레이션 | Kubernetes | 🟡 검토 중 | 필요 시 |

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 (확정 사항 + 초안 기본값) |
