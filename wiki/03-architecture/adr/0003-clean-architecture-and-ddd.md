# ADR-0003: Clean Architecture + DDD 적용

- 상태: 승인
- 날짜: 2026-09-27
- 결정자:

## 배경 (Context)

> TODO: 결정이 필요했던 배경

## 검토한 대안 (Options)

> TODO: 검토한 대안과 장단점

## 결정 (Decision)

각 서비스 내부는 **Clean Architecture** 레이어 구조를 따르고, 도메인 모델은 **DDD** 전술적 패턴(Aggregate, Entity, Value Object, Domain Event)으로 설계합니다.

> TODO: 선정 사유 상세

## 결과 (Consequences)

- Domain 레이어는 외부 프레임워크에 의존하지 않습니다.
- 레이어 의존성 규칙은 아키텍처 테스트로 검증합니다.
- 상세 구조는 [Clean Architecture & 솔루션 구조](../clean-architecture.md)를 참고합니다.
