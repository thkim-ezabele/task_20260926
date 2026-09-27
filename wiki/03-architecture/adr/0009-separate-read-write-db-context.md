---
title: "ADR-0009: 읽기 / 쓰기 DB 연결 분리와 Repository 규칙"
type: adr
adr: "0009"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0009]
tags: [adr, architecture, database]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0009: 읽기 / 쓰기 DB 연결 분리와 Repository 규칙

## 배경 (Context)

- CQRS([ADR-0007](0007-adopt-cqrs.md))를 적용하지만, 과제 규모에서 읽기 전용 DB(복제본)를 운영할 필요는 아직 없다.
- 긴급 상황에는 응답 현황 조회가 몰리므로, **나중에 복제본을 붙일 수 있는 구조**는 처음부터 갖춰야 한다.
- 데이터 접근 코드에 업무 판단이 섞이면 테스트가 어렵고, 에이전트가 만든 코드의 품질 편차가 커진다.

## 검토한 대안 (Options)

1. **DbContext 하나**: 단순하지만 복제본 도입 시 조회 코드 전체를 손봐야 하고, 조회 경로에서 실수로 쓰기가 가능하다.
2. **읽기 / 쓰기 DbContext와 연결 문자열 분리, 대상은 같은 DB**: 지금은 비용이 거의 없고, 복제본 도입 시 설정만 바꾼다.
3. **복제본까지 즉시 도입**: 운영 복잡도(복제 지연, 인프라)가 과제 범위를 넘는다.

Repository 형태: (a) Repository 없이 Handler에서 DbContext 직접 사용, (b) **쿼리만 담는 얇은 Repository**, (c) 로직을 포함한 두꺼운 Repository

## 결정 (Decision)

**대안 2와 (b)를 채택한다.**

- 연결 문자열 `ConnectionStrings:Write` / `ConnectionStrings:Read`, DbContext `<Service>DbContext` / `<Service>ReadDbContext`로 나눈다. 지금은 같은 Database를 가리키고, 읽기 연결은 `default_transaction_read_only=on`으로 연다.
- 마이그레이션은 쓰기 DbContext에서만 만든다. 두 DbContext는 엔티티 매핑을 공유한다.
- 데이터 접근은 EF Core Repository로만 한다. Write Repository(인터페이스는 Domain)는 쓰기 DbContext, Read Repository(인터페이스는 Application)는 읽기 DbContext를 쓴다.
- **Repository에는 람다식 LINQ 쿼리만 둔다.** 메서드 하나 = 식 본문 하나의 메서드 체인. 분기 · 반복 · 예외 처리 · 로깅 · 매핑 · 검증 · `SaveChanges` 금지, 쿼리 구문과 원시 SQL 금지(원시 SQL은 사용자 승인 시 예외).

## 결과 (Consequences)

- 긍정: 복제본 도입 시 코드 변경이 없다. 조회 경로의 실수 쓰기를 DB가 막는다. Repository가 단순해 리뷰와 테스트가 쉽고, 업무 판단이 Handler / Aggregate로 모인다.
- 부정: DbContext와 Repository가 두 벌이 된다. 복잡한 조회도 LINQ 람다로 표현해야 하므로, 번역이 어려운 쿼리는 원시 SQL 승인 절차가 필요하다. 복제본 도입 후에는 복제 지연에 따른 "쓰고 바로 읽기" 문제를 고려해야 한다.
- 후속: 규칙 상세는 [데이터베이스 · 읽기 / 쓰기 연결 분리](../../04-development/database.md#읽기--쓰기-연결-분리), [코딩 컨벤션 · Repository 규칙](../../04-development/coding-conventions.md#repository-규칙-ef-core).
