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
> 🟢 확정: ADR 또는 병합된 기준 문서(코딩 · DB · 테스트 · 로깅, 현재 `draft`)에서 채택 · 🟡 후보: 미정. 결정 시점이 정해진 항목은 비고에 적습니다.
>
> [위키 홈](../README.md)

## 런타임 & 아키텍처

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 런타임 | .NET 8 (LTS) / C# 12 | 🟢 확정 | [ADR-0001](adr/0001-use-dotnet8.md). LTS 지원 종료(2026-11-10)를 알고 **현재 버전 유지**로 결정(2026-09-27, 사용자) |
| 아키텍처 스타일 | MSA | 🟢 확정 | [ADR-0002](adr/0002-adopt-msa.md) |
| 서비스 내부 구조 | Clean Architecture + DDD | 🟢 확정 | [ADR-0003](adr/0003-clean-architecture-and-ddd.md) |
| 서비스 간 통신 | Event-Driven Architecture (Outbox / Inbox) | 🟢 확정 | [ADR-0004](adr/0004-adopt-event-driven-architecture.md) |
| 유스케이스 구조 | CQRS (같은 DB, 모델 분리) | 🟢 확정 | [ADR-0007](adr/0007-adopt-cqrs.md) |
| DI | 기본 컨테이너 + 규칙 기반 자동 등록(Scoped) | 🟢 확정 | [ADR-0010](adr/0010-convention-based-di-registration.md) |
| 웹 프레임워크 | ASP.NET Core | 🟢 확정 | API 스타일(Minimal API 기본안 / Controller)은 🟡 기반 구축 토픽에서 결정 |
| API Gateway | YARP / Ocelot | 🟡 후보 | |

## 데이터

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| RDBMS | PostgreSQL | 🟢 확정 | [ADR-0005](adr/0005-use-postgresql.md) |
| ORM | EF Core 8 + Npgsql | 🟢 확정 | [ADR-0009](adr/0009-separate-read-write-db-context.md): 읽기 / 쓰기 DbContext 분리, Repository는 람다 LINQ만 |
| 명명 규칙 변환 | EFCore.NamingConventions (snake_case) | 🟢 확정 | [데이터베이스](../04-development/database.md) |
| 코드값 | 정수(`smallint`) + C# enum, 조합은 비트 마스킹 | 🟢 확정 | [ADR-0008](adr/0008-integer-codes-and-bitmask.md) |
| 기본 키 | UUID v7 | 🟢 확정 | 생성 방식(라이브러리 / 직접 구현)은 🟡 기반 구축 토픽에서 결정 (.NET 8에는 내장 없음) |
| 캐시 | Redis | 🟡 후보 | 필요 시 |

## 메시징

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 메시지 브로커 | RabbitMQ / Kafka | 🟡 후보 | [EDA](event-driven-architecture.md) 비교 참고 |
| 메시징 추상화 | MassTransit | 🟡 후보 | v9부터 상용 라이선스 전환 예정 여부를 결정 시 확인 |

## 공통 라이브러리

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| Mediator (CQRS 파이프라인) | MediatR / 소스 생성기 기반 Mediator / 직접 구현 | 🟡 후보 | MediatR v13+ 상용 라이선스. 기반 구축 토픽에서 ADR로 결정 |
| 유효성 검사 | FluentValidation | 🟢 확정 | 파이프라인 동작에서 자동 실행 |
| DI 타입 검색 | Scrutor / 직접 구현 | 🟡 후보 | 기반 구축 토픽에서 결정 |
| 로깅 | Serilog (`ILogger<T>` 공급자) | 🟢 확정 | [로깅 & 관측성](../04-development/logging-observability.md): 콘솔 텍스트 / 파일 JSON(CLEF) |
| 로그 수집기 | Seq / Loki / ELK | 🟡 후보 | 결정 시 로그 설계와 함께 ADR |
| 관측성 | OpenTelemetry (추적, W3C Trace Context) | 🟢 확정 | 수집 백엔드(Jaeger / Tempo 등)는 🟡 |
| API 문서 | OpenAPI (Swashbuckle) | 🟡 후보 | API 스타일과 함께 결정 |
| 회복성 | Polly | 🟡 후보 | |
| 시간 | `TimeProvider` | 🟢 확정 | `DateTime.UtcNow` 직접 호출 금지 |

## 테스트 (TDD)

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 개발 방법론 | TDD | 🟢 확정 | [ADR-0006](adr/0006-adopt-tdd.md), [테스트 전략](../04-development/testing-strategy.md) |
| 테스트 프레임워크 | xUnit | 🟢 확정 | |
| 단언 | FluentAssertions v7 고정 / Shouldly / AwesomeAssertions | 🟡 후보 | FluentAssertions v8+ 상용 라이선스. 기반 구축 토픽에서 결정 |
| Test Double | NSubstitute | 🟢 확정 | [TDD 가이드](../04-development/tdd-guide.md#test-double-사용-기준) |
| 통합 테스트 | Testcontainers (PostgreSQL / 브로커) + Respawn | 🟢 확정 | InMemory / SQLite 대체 금지 |
| 시간 고정 | Microsoft.Extensions.TimeProvider.Testing | 🟢 확정 | `FakeTimeProvider` |
| 아키텍처 테스트 | NetArchTest | 🟢 확정 | 레이어 의존, 마커 상속, record 규칙 |
| 커버리지 | coverlet + ReportGenerator | 🟢 확정 | Domain / Application 80% 목표 |

## 인프라 & DevOps

| 항목 | 선택 | 상태 | 비고 |
|---|---|---|---|
| 형상관리 | GitHub, Git Flow | 🟢 확정 | [Git 워크플로우](../04-development/git-workflow.md) |
| CI/CD | GitHub Actions | 🟡 후보 | 기반 구축 토픽에서 구성 |
| 컨테이너 | Docker / docker compose | 🟡 후보 | 로컬 인프라, Testcontainers에는 Docker 필수 |
| 오케스트레이션 | Kubernetes | 🟡 후보 | 필요 시 |

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
