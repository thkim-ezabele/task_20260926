---
title: "ADR-0023: 도입 보류 (메시지 브로커, Outbox / Inbox, API Gateway, 로그 수집기)"
type: adr
adr: "0023"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0023]
tags: [adr, architecture, messaging, infrastructure]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0023: 도입 보류 (메시지 브로커, Outbox / Inbox, API Gateway, 로그 수집기)

## 배경 (Context)

- 서비스 간 상태 전파는 통합 이벤트 비동기 메시징과 Outbox / Inbox로 하기로 했다([ADR-0004](0004-adopt-event-driven-architecture.md)). 브로커(RabbitMQ / Kafka), 메시징 추상화(MassTransit), API Gateway(YARP / Ocelot), 로그 수집기(Seq / Loki / ELK)는 [기술 스택](../tech-stack.md)에서 후보로 남아 있었다.
- [PRD-001](../../10-delivery/prd/PRD-001-foundation.md) 기반 구축의 범위는 **로컬 환경, BuildingBlocks + Employee 서비스 1개**다. 서비스 간 통신이 없고, 외부에 공개하는 진입점이나 로컬 밖 공유 환경도 없다([질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q2 · Q4).
- PRD Q5에서 "브로커 · Gateway · 로그 수집기 미도입", Q11에서 "Outbox / Inbox는 보류 ADR에 포함", Q18에서 "도입 보류 항목만 1건으로 묶음"으로 정했다. 도메인 이벤트는 수집까지만 한다(Q15).
- MassTransit은 v9부터 상용 라이선스다(9.0.0 nuspec의 라이선스 URL이 `massient.com/license`, 8.x 줄은 Apache-2.0, 2026-09-27 NuGet 확인). 이 프로젝트는 상용 라이선스를 쓰지 않는다(NFR-05).
- 로컬 관측은 Aspire 대시보드(OTLP 로그 · 트레이스 · 메트릭)로 하기로 했다([ADR-0011](0011-use-aspire-local-orchestration.md), [ADR-0020](0020-logging-with-serilog-and-otlp.md)).

## 검토한 대안 (Options)

1. **지금 모두 도입**: 장점: 나중에 붙일 때의 구조 변경이 없다. 단점: 소비자가 없는 브로커 · 발행 대상이 없는 Outbox · 라우팅할 서비스가 하나뿐인 Gateway · 로컬에서 대시보드와 겹치는 수집기를 만들고 유지해야 한다. 기반 구축 범위와 일정(스프린트 4개)을 넘는다.
2. **ADR-0004를 대체해 EDA를 취소**: 장점: 결정과 구현이 일치한다. 단점: 서비스가 늘면 다시 필요해질 결정을 뒤집는 것이고, 긴급 전파 · 알림 흐름(ADR-0004 배경)은 그대로다.
3. **항목별로 도입을 보류하고, 대신 쓰는 것과 재검토 시점(트리거)을 적어 둠. ADR-0004는 유지**: 장점: 지금 범위에 필요한 것만 만들고, 확장 지점과 재검토 조건이 문서로 남는다. 단점: 재검토 전까지 ADR-0004의 결정이 구현되지 않은 상태로 남는다.

## 결정 (Decision)

**대안 3: 네 항목의 도입을 보류한다. ADR-0004(EDA, Outbox / Inbox)는 유지하며 이 ADR은 ADR-0004를 대체하지 않는다.** 보류는 "도입 시점을 늦춤"이고, 서비스 간 통신 방식에 대한 결정은 그대로 유효하다. ADR-0004 본문은 고치지 않는다.

| 항목 | 지금 하지 않는 것 | 대신 쓰는 것 / 남겨 두는 확장 지점 | 재검토 시점(트리거) | 재검토 때 후보 |
|---|---|---|---|---|
| 메시지 브로커 · 메시징 추상화 | 브로커 컨테이너, 브로커 클라이언트, 메시징 추상화 패키지, 통합 이벤트 발행 · 소비 | 도메인 이벤트는 Aggregate가 수집하고 커밋 뒤 `ClearDomainEvents`로 비운다([ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md)). 통합 이벤트 기반 타입 · 봉투 설계는 [EDA](../event-driven-architecture.md) 문서의 설계안으로 둔다 | 두 번째 서비스가 다른 서비스의 상태 변경을 받아야 하는 토픽(예: Emergency · Notification 서비스)의 PRD 작성 시 | 브로커: RabbitMQ(추천안) / Kafka. 추상화: MassTransit 8.x(Apache-2.0, 지원 기간 확인 필요) / 브로커 클라이언트 직접 사용. MassTransit v9+는 상용이라 제외 |
| Outbox / Inbox | `outbox_messages` · `inbox_messages` 테이블, 발행 처리기, 소비 측 멱등 처리 | UnitOfWork의 **커밋 전 · 같은 트랜잭션** 확장 지점(ADR-0014). [데이터베이스 · Outbox 테이블](../../04-development/database.md#outbox-테이블) 표는 도입 시 설계안 | 브로커 도입과 같은 토픽(첫 통합 이벤트 발행 작업 전). Outbox 없이 브로커만 도입하지 않는다 | 직접 구현(데이터베이스 문서 설계안) / MassTransit EF Core Outbox(브로커 추상화 결정과 함께) |
| API Gateway | Gateway 프로젝트, 라우팅 · 인증 전달 설정 | 클라이언트가 서비스 Api를 직접 호출한다(로컬은 Aspire가 노출한 Employee Api 엔드포인트). 공통 API 처리(ProblemDetails, 예외 처리)는 서비스 공통 코드에 둔다([ADR-0016](0016-use-controllers-for-api.md)) | 외부에 공개하는 서비스 Api가 2개 이상이 되거나, 인증 · 인가를 한곳에서 처리해야 하는 토픽(Identity 서비스) 착수 시, 또는 Phase 4 배포 설계 시 | YARP(추천안, MIT, net8.0 지원) / Ocelot |
| 로그 수집기 · 추적 수집 백엔드 | Seq · Loki · ELK 같은 로그 저장소, Jaeger · Tempo 같은 추적 백엔드, 알림 규칙 | 로컬 관측은 **Aspire 대시보드**(Serilog OTLP 싱크 로그, OpenTelemetry 트레이스 · 메트릭, ADR-0020). 파일 로그는 CLEF(JSON)로 남겨 수집기 도입 시 그대로 읽힌다. 콘솔 텍스트 로그 | 로컬 밖에 지속되는 공유 환경(스테이징 · 운영, Phase 4)을 구성하거나, 로그 기반 알림 기준을 정하는 작업 착수 시 | 로그: Seq / Loki / ELK(ELK면 ECS 포매터로 교체). 추적: Jaeger / Tempo. 애플리케이션은 OTLP 표준 출력을 유지하므로 수집기 선택은 설정 변경 위주다 |

- **재검토 방법**: 트리거가 된 토픽의 PRD 리뷰에서 해당 항목을 다시 다루고, 도입을 결정하면 항목마다 새 ADR을 쓴다. 새 ADR은 "ADR-0023의 ○○ 보류를 해소한다"고 적는다. 네 항목이 모두 해소되면 이 ADR의 상태를 `superseded`로 바꾸고 `superseded_by`에 해당 ADR들을 적는다.
- **보류 중 금지**: 보류 항목의 패키지(브로커 클라이언트, MassTransit, YARP, Ocelot, 수집기 싱크)를 참조하지 않는다. 확장 지점(UnitOfWork 커밋 전 단계)에 임시 구현을 넣지 않는다.
- **테스트 범위**: 통합 테스트 컨테이너는 PostgreSQL만 쓴다. 브로커 컨테이너 · Inbox 멱등 테스트는 브로커 도입 토픽에서 추가한다.

## 결과 (Consequences)

- 긍정: 기반 구축 범위에 필요한 것만 만들어 스프린트 4개 안에 끝낼 수 있다. 상용 라이선스 위험(MassTransit v9+)을 피한다. 확장 지점(UnitOfWork 커밋 전, OTLP 표준 출력)이 정해져 있어 도입할 때 트랜잭션 · 로깅 구조를 바꾸지 않는다.
- 부정: ADR-0004의 결정(통합 이벤트, Outbox / Inbox)이 재검토 전까지 구현되지 않는다. 도메인 이벤트는 수집한 뒤 버려지므로, 이벤트로 이어져야 하는 후속 처리는 지금은 만들 수 없다.
- 부정: 로컬 밖 로그 보관 · 검색 · 알림이 없다. 파일 로그는 각 실행 환경에만 남는다.
- 후속: [기술 스택](../tech-stack.md), [데이터베이스](../../04-development/database.md#outbox-테이블), [로깅 & 관측성](../../04-development/logging-observability.md)의 해당 🟡를 "보류(ADR-0023)"로 정리한다(S01-T04). 재검토 트리거는 다음 토픽 PRD 작성 때 orchestrator가 확인한다.
