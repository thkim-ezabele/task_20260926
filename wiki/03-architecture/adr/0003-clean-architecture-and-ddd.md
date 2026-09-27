---
title: "ADR-0003: Clean Architecture + DDD 적용"
type: adr
adr: "0003"
status: accepted
date: 2026-09-27
deciders: []
aliases: [ADR-0003]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0003: Clean Architecture + DDD 적용

## 배경 (Context)

- 긴급 상황 전파, 응답 수집, 연락망(Call Tree) 구성은 상태 전이와 업무 규칙이 복잡하다. 규칙이 코드 곳곳에 흩어지면 검증과 변경이 어렵다.
- 서비스가 여러 개이므로, 서비스마다 같은 내부 구조를 가져야 사람과 AI 에이전트 모두 코드를 빠르게 이해하고 리뷰할 수 있다.
- TDD(ADR-0006)를 하려면 업무 규칙을 DB · 웹 프레임워크 없이 테스트할 수 있어야 한다.

## 검토한 대안 (Options)

1. **전통적 계층형(Controller / Service / Repository)**: 장점: 익숙하고 단순하다. 단점: 업무 규칙이 Service에 몰리고(빈약한 도메인 모델), 도메인이 ORM · 프레임워크에 의존하기 쉽다.
2. **Vertical Slice Architecture**: 장점: 기능 단위로 코드가 모여 변경이 쉽다. 단점: 기능 간 공유 도메인 규칙의 위치가 모호해지고, 구조의 일관성을 규칙으로 강제하기 어렵다.
3. **Clean Architecture + DDD 전술적 패턴**: 장점: 의존성이 도메인으로만 향해 업무 규칙을 독립적으로 테스트할 수 있고, Aggregate로 불변식을 한곳에 모은다. 단점: 레이어 · 타입 수가 늘어 초기 코드량이 많다.

## 결정 (Decision)

각 서비스 내부는 **Clean Architecture** 레이어 구조를 따르고, 도메인 모델은 **DDD** 전술적 패턴(Aggregate, Entity, Value Object, Domain Event)으로 설계합니다.

선정 사유:

- 업무 규칙을 Domain 레이어(Aggregate, Value Object)에 모아 **프레임워크 없이 단위 테스트**할 수 있다(ADR-0006).
- 레이어 의존 규칙을 **아키텍처 테스트로 자동 검증**할 수 있어, 에이전트가 만든 코드도 같은 구조를 지키게 할 수 있다.
- 바운디드 컨텍스트 · Aggregate 개념이 MSA의 서비스 경계(ADR-0002) 설정 근거가 된다.
- 애플리케이션 레이어 안에서는 기능 폴더(Commands / Queries)로 나눠, Vertical Slice의 장점도 일부 취한다(ADR-0007).

## 결과 (Consequences)

- Domain 레이어는 외부 프레임워크에 의존하지 않습니다.
- 레이어 의존성 규칙은 아키텍처 테스트로 검증합니다.
- 상세 구조는 [Clean Architecture & 솔루션 구조](../clean-architecture.md)를 참고합니다.
- 부정: 단순 CRUD 기능에도 레이어별 타입이 필요해 코드량이 늘어난다. 공통 부분은 BuildingBlocks로 줄인다.

> 2026-09-27: 작성 당시 비워 둔 배경 · 대안 · 선정 사유를 보완했습니다(결정 변경 없음).
