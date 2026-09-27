---
title: "ADR-0013: UUID v7 식별자 (IIdGenerator + UUIDNext)"
type: adr
adr: "0013"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0013]
tags: [adr, architecture, database]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0013: UUID v7 식별자 (IIdGenerator + UUIDNext)

## 배경 (Context)

- Aggregate ID는 `uuid` 강타입 ID로 쓰고, 애플리케이션에서 **먼저** 정해야 한다. 도메인 이벤트 · 응답이 저장 전에 ID를 알아야 하고, 실행 전략이 SaveChanges를 재시도할 때 같은 INSERT를 다시 보내려면 ID가 이미 정해져 있어야 한다([ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md)). Aggregate 팩토리는 ID를 인자로 받고, ID는 Handler가 만든다([PRD-001](../../10-delivery/prd/PRD-001-foundation.md) FR-04).
- 무작위 UUID(v4)는 B-tree 인덱스 곳곳에 삽입되어 단편화가 크다. 시간 순서 UUID(v7)는 삽입이 인덱스 한쪽 끝에 몰린다.
- .NET 8에는 `Guid.CreateVersion7()`이 없다(.NET 9부터). PostgreSQL `uuidv7()`는 PostgreSQL 18부터이고, 사용하는 이미지는 17이다([PostgreSQL 이미지](../package-versions.md#postgresql-이미지)). [데이터베이스](../../04-development/database.md) 문서의 UUID 생성 방식은 🟡로 남아 있다.

## 검토한 대안 (Options)

1. **UUIDNext 라이브러리**: 장점: 0BSD, 의존 없음, PostgreSQL용 v7 생성과 단조성 처리가 이미 있다. 단점: 외부 패키지 의존, 내부 시계가 `TimeProvider`가 아니다.
2. **RFC 9562 직접 구현**: 장점: 의존이 없다. 단점: 단조성 · 카운터 넘침 · 시계 역행 처리를 직접 만들고 테스트해야 한다.
3. **PostgreSQL `uuidv7()` 기본값**: 장점: 애플리케이션 코드가 없다. 단점: PostgreSQL 18이 필요하고, ID를 애플리케이션에서 먼저 정할 수 없어 `ValueGeneratedNever`와 충돌한다.
4. **UUID v4 (`Guid.NewGuid()` / `gen_random_uuid()`)**: 장점: 표준 기능만 쓴다. 단점: 정렬되지 않고 인덱스 단편화가 크다.
5. **순차 `bigint`(identity)**: 장점: 작고 빠르다. 단점: 서비스 간 충돌 가능, INSERT 뒤에야 값이 정해진다.

## 결정 (Decision)

**대안 1(UUIDNext 4.2.4)을 채택한다.**

- **API**: `IIdGenerator`(BuildingBlocks.Application)의 구현(BuildingBlocks.Infrastructure)이 `Uuid.NewDatabaseFriendly(Database.PostgreSql)`를 호출한다. 생성기 상태는 라이브러리의 정적 생성기에 있으므로, [ADR-0010](0010-convention-based-di-registration.md)에 따라 Scoped로 자동 등록해도 단조성에 영향이 없다.
- **생성 위치**: Handler가 `IIdGenerator`로 ID를 만들어 Aggregate 팩토리에 넘긴다. EF 매핑은 `ValueGeneratedNever`다.
- **단조성**: 한 프로세스 안에서는 같은 밀리초 안에서도 단조 증가한다(12비트 카운터 + lock, 밀리초마다 최상위 비트가 0인 무작위 시드, 카운터가 넘치면 다음 밀리초를 빌려 씀, 시계 역행 방지). 프로세스 사이(Api 여러 개, 다른 서비스)에서는 밀리초 단위 시간 순서만 보장한다.
- **바이트 순서 전제**: UUIDNext는 Guid를 빅 엔디언으로 조립한다(`ToString()`이 RFC 표기와 같다). Npgsql은 Guid를 RFC(네트워크) 바이트 순서로 `uuid`에 기록하므로, PostgreSQL `uuid` 정렬(바이트 비교) = 생성 순서가 된다. 이 전제는 S03-T05 통합 테스트(DB `ORDER BY id` = 생성 순서)로 검증한다.
- **메모리 안 정렬**: .NET `Guid.CompareTo`는 바이트 순서가 달라 생성 순서와 다를 수 있으므로, 메모리 안 정렬 검증은 문자열(빅 엔디언) 기준으로 한다(FR-06).
- **DB 기본값은 쓰지 않는다**: `gen_random_uuid()`는 v4이고, `uuidv7()`는 PostgreSQL 17에 없다. 애플리케이션이 ID를 먼저 정해야 한다는 배경과도 맞지 않는다.

## 결과 (Consequences)

- 긍정: ID가 저장 전에 정해져 도메인 이벤트 · 응답 · 재시도가 단순하다. 삽입이 인덱스 끝에 몰려 단편화가 적다. 외부 의존은 의존성 없는 0BSD 패키지 하나다([패키지 버전 · 라이선스 · 데이터](../package-versions.md#데이터)).
- 부정: UUIDNext는 `TimeProvider`가 아니라 `DateTimeOffset.UtcNow`를 직접 쓴다. 따라서 단위 테스트에서는 `IIdGenerator` 대역을 쓰고, 실제 v7 형식 · 단조성은 FR-06 테스트(S02-T03)와 통합 테스트(S03-T05)에서 검증한다.
- 부정: v7에는 생성 시각(밀리초)이 드러난다. ID를 외부에 노출하는 것이 문제가 되면 이 결정을 다시 검토한다.
- 확인 범위: 알고리즘 분석은 UUIDNext GitHub HEAD 소스 기준이다. 4.2.4가 같은 동작인지는 위 테스트로 확인한다.
- 후속: [데이터베이스](../../04-development/database.md)의 UUID 🟡를 이 ADR로 해소한다(S01-T04). .NET 9 이상으로 올리면 `Guid.CreateVersion7()`으로 바꿀지 검토할 수 있다(TD-002).
