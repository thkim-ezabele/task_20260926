---
title: "ADR-0007: CQRS 적용"
type: adr
adr: "0007"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0007]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0007: CQRS 적용

## 배경 (Context)

- 긴급연락망은 **조회가 많고**(연락망 조회, 응답 현황 집계) 쓰기는 도메인 규칙이 복잡하다(전파, 응답 수집, 상태 전이).
- 도메인 모델([ADR-0003](0003-clean-architecture-and-ddd.md))로 조회까지 처리하면 Aggregate 전체를 불러와 DTO로 바꾸는 비용이 크고, 조회 요구 때문에 도메인 모델이 오염된다.
- 에이전트(developer / reviewer)가 일관되게 따를 수 있는 유스케이스 구조가 필요하다.

## 검토한 대안 (Options)

1. **서비스 클래스 방식**(`EmployeeService`에 조회 · 변경 메서드를 함께 둠): 익숙하지만, 서비스가 비대해지고 조회와 변경의 관심사가 섞인다.
2. **CQRS (같은 DB, 모델 분리)**: Command와 Query를 타입 수준에서 분리한다. 조회는 도메인 모델을 거치지 않고 프로젝션한다. 저장소 분리 없이 시작할 수 있다.
3. **CQRS + 읽기 저장소 분리**(별도 Read DB, 이벤트로 동기화): 확장성은 가장 좋지만, 과제 규모에 비해 운영 복잡도(동기화 지연, 재구축)가 크다.

## 결정 (Decision)

**대안 2: 같은 Database 안에서 Command와 Query를 분리하는 CQRS를 적용한다.**

- Command: 상태 변경, `Result` / `Result<TId>` 반환, Write Repository → Aggregate → Unit of Work
- Query: 부작용 없음, 응답 `record` 반환, Read Repository가 읽기 DbContext에서 프로젝션
- 요청 하나 = Handler 하나. 검증 · 로깅 · 트랜잭션은 파이프라인 동작으로 처리한다.
- 읽기 저장소는 물리적으로 나누지 않지만 설정은 나눈다([ADR-0009](0009-separate-read-write-db-context.md)).

## 결과 (Consequences)

- 긍정: 유스케이스 단위로 파일이 나뉘어 변경 범위가 작고, 에이전트가 따르기 쉬운 반복 구조가 된다. 조회 성능을 도메인 모델과 무관하게 최적화할 수 있다.
- 부정: 파일 수가 늘어난다(Command, Handler, Validator, Query, Response). Mediator 계층이 추가된다.
- 후속: Mediator 구현체(MediatR v13+ 상용 라이선스 / 소스 생성기 기반 Mediator / 직접 구현)는 기반 구축 토픽에서 결정한다. 규칙 상세는 [코딩 컨벤션 · CQRS 규칙](../../04-development/coding-conventions.md#cqrs-규칙).
