---
title: "ADR-0014: Command 트랜잭션 경계와 Unit of Work"
type: adr
adr: "0014"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0014]
tags: [adr, architecture, database]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0014: Command 트랜잭션 경계와 Unit of Work

## 배경 (Context)

- EF Core 등록에 재시도 실행 전략(`EnableRetryOnFailure`)을 쓴다([ADR-0011](0011-use-aspire-local-orchestration.md)). 재시도 실행 전략은 전략 **밖에서** 연 사용자 트랜잭션을 거부한다(`InvalidOperationException`). 따라서 트랜잭션은 `CreateExecutionStrategy().ExecuteAsync` 안에서 열어야 한다.
- 전략 안에서 무엇을 재실행할지 정해야 한다. Handler까지 재실행하면 Handler가 멱등이어야 하고, ID · 시간이 다시 만들어진다. [PRD-001 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q9에서 "트랜잭션 데코레이터가 실행 전략 안에서 트랜잭션 → SaveChanges → 커밋, Handler는 저장하지 않음, 재시도 유지"로 정했고, S01 계획 리뷰 Q3에서 "SaveChanges · 커밋만 재시도, Handler 1회 실행, Read Committed"로 확정했다.
- 기존 규칙: Command 하나 = 트랜잭션 하나 = Aggregate 하나, 격리 수준 Read Committed, `xmin` 낙관적 잠금([데이터베이스 · 트랜잭션 & 동시성 제어](../../04-development/database.md#트랜잭션--동시성-제어)). Repository는 `SaveChanges`를 부르지 않는다([ADR-0009](0009-separate-read-write-db-context.md)). Domain · Application은 EF · Npgsql에 의존하지 않는다([ADR-0003](0003-clean-architecture-and-ddd.md)).
- Outbox는 도입을 보류하지만, 나중에 비즈니스 데이터와 같은 트랜잭션에 이벤트를 저장할 자리는 남겨야 한다([ADR-0004](0004-adopt-event-driven-architecture.md)).

## 검토한 대안 (Options)

1. **Handler까지 실행 전략 안에서 재실행**: 장점: 재시도 때 최신 상태로 다시 판단하므로 정합성이 높다. 단점: Handler 멱등성이 필요하고, ID · 시간이 다시 만들어지며 외부 부수 효과가 반복될 수 있다(Q3에서 기각).
2. **재시도 없음**: 장점: 가장 단순하다. 단점: 일시적 연결 오류에 약하다.
3. **SaveChanges의 암묵 트랜잭션만 사용**: 장점: 코드가 적다. 단점: Outbox 저장을 끼울 확장 지점이 없다.
4. **Handler는 전략 밖에서 1회 실행, SaveChanges · 커밋만 전략 안에서 재시도**: 장점: 재시도 범위가 DB 왕복으로 한정되고 외부 부수 효과 · ID 재생성이 없다. 단점: 커밋 결과를 모르는 상태에서 재시도하면 오보가 날 수 있다(아래 결과).

## 결정 (Decision)

**대안 4를 채택한다.**

- **트랜잭션 데코레이터(Command 전용)**: Handler를 실행 전략 밖에서 **한 번** 실행한다. 결과가 실패 `Result`면 저장하지 않고 그대로 반환하고, 성공이면 `IUnitOfWork.CommitAsync(ct)`를 부른다. 파이프라인 순서는 로깅 → 검증 → 트랜잭션 → Handler다(S01-T03 Mediator ADR과 같음).
- **UnitOfWork(Infrastructure)**: 다음 순서로 커밋한다.
  1. `strategy = db.Database.CreateExecutionStrategy()`
  2. `strategy.ExecuteAsync` 안에서 `BeginTransactionAsync(IsolationLevel.ReadCommitted)` → `SaveChangesAsync(acceptAllChangesOnSuccess: false)` → **[Outbox 확장 지점: 커밋 전, 같은 트랜잭션]** → `CommitAsync`
  3. 전략 밖에서 커밋이 성공한 뒤 `ChangeTracker.AcceptAllChanges()`
- **`acceptAllChangesOnSuccess: false`인 이유**: 재시도할 때 엔티티 상태(Added / Modified)가 남아 있어야 같은 변경을 다시 보낸다. 감사 인터셉터의 `created_at`(Added 판정)도 재시도 때 같게 동작한다.
- **격리 수준**: Read Committed를 **명시한다**(서버 기본값에 기대지 않음). 더 높은 수준이 필요하면 [데이터베이스](../../04-development/database.md#트랜잭션--동시성-제어) 규칙대로 이유를 작업 문서에 남긴다.
- **Handler · Repository는 `SaveChanges`를 부르지 않는다.** Handler는 DbContext를 받지 않고 Write Repository와 `IIdGenerator` 등만 쓴다.
- **예외 변환은 Infrastructure(UnitOfWork)에서 한다.** BuildingBlocks.Application은 EF · Npgsql 형식을 참조하지 않는다.
  - `DbUpdateException` 안의 `PostgresException` SqlState `23505`(유니크 위반)는 `ConstraintName`으로 서비스별 매핑을 찾아 `Result` 실패로 바꾼다(예: `ux_employees_email` → Employee 이메일 중복 Error). 매핑이 없으면 공통 Conflict Error로 바꾼다.
  - `DbUpdateConcurrencyException`은 공통 Conflict Error(`Common.ConcurrencyConflict`, [에러 코드](../../05-api/error-codes.md))로 바꾼다(FR-07).
  - `23514`(체크 제약 위반)는 **변환하지 않는다.** Validator를 통과한 뒤의 체크 제약 위반은 프로그래밍 오류이므로 전역 예외 처리(Internal)로 보낸다.
  - 변환 메시지와 로그에는 **제약 이름만** 남기고 값(이메일 등)은 남기지 않는다(개인정보, S01-T03 로깅 ADR).
- **이메일 중복은 두 겹으로 막는다**: Handler의 사전 조회 + 유니크 인덱스 `ux_employees_email`의 최종 방어. 사전 조회는 트랜잭션 밖 Read Committed이므로, 동시 요청 경합에서는 `23505`가 정상 경로다.
- **도메인 이벤트는 수집만 한다**: `ClearDomainEvents`는 커밋 뒤에 부른다. Outbox를 도입하면 커밋 전 확장 지점에서 이벤트를 저장한다(ADR-0004 유지).
- **Query**는 트랜잭션 없이 읽기 DbContext(NoTracking)로 조회한다([ADR-0007](0007-adopt-cqrs.md)).

## 결과 (Consequences)

- 긍정: 재시도 범위가 DB 왕복으로 한정되어 Handler는 멱등일 필요가 없고, ID · 시간이 재생성되지 않는다. 저장 · 커밋 위치가 한 곳이라 Handler 테스트는 가짜 `IUnitOfWork`로 충분하다. Outbox를 도입할 때 트랜잭션 구조를 바꾸지 않고 확장 지점만 채운다. 영속성 예외가 Application으로 새지 않는다.
- 부정: Handler의 판단(사전 조회 등)은 재시도 때 다시 하지 않으므로, 경합 방어는 DB 제약과 `xmin`이 맡는다.
- 알려진 한계: 커밋 응답을 받는 중 연결이 끊기면 실행 전략이 SaveChanges를 다시 실행해, 실제로는 성공한 커밋을 `pk_` `23505` 또는 `xmin` 충돌로 잘못 보고할 수 있다. 운영 전에 `verifySucceeded` 또는 멱등 키로 해소한다(TD-010).
- 후속: `IUnitOfWork` 계약과 트랜잭션 데코레이터는 S02-T01 · S02-T02, 실행 전략 UnitOfWork · 예외 변환은 S02-T04, 실제 DB 동작(재시도 · 23505 변환)은 S03-T05에서 검증한다. 매핑이 없는 `23505`용 공통 Conflict 코드 번호는 S02-T06(공통 에러 코드)에서 정한다. [코딩 컨벤션](../../04-development/coding-conventions.md)의 Handler `SaveChanges` 예시와 [TDD 가이드](../../04-development/tdd-guide.md)의 "저장" 표현은 S01-T04에서 이 ADR에 맞춘다.
