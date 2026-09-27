---
title: "ADR-0015: Mediator 직접 구현과 데코레이터 파이프라인"
type: adr
adr: "0015"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0015]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0015: Mediator 직접 구현과 데코레이터 파이프라인

## 배경 (Context)

- [ADR-0007](0007-adopt-cqrs.md)은 Command / Query를 나누고 검증 · 로깅 · 트랜잭션을 파이프라인 동작으로 처리하기로 했고, Mediator 구현체는 기반 구축 토픽에서 정하기로 남겼다.
- MediatR는 v13부터 상용 라이선스로 바뀌었고, 이 프로젝트는 상용 라이선스 패키지를 쓰지 않는다([PRD-001](../../10-delivery/prd/PRD-001-foundation.md) NFR-05, [패키지 버전 · 라이선스 · 애플리케이션](../package-versions.md#애플리케이션)).
- [PRD-001 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q5에서 "Mediator 직접 구현", Q14에서 파이프라인 순서 "로깅 → 검증 → 트랜잭션 → Handler"로 정했다. FR-05는 `ICommand : ICommand<Unit>`로 통일, 직접 구현한 디스패처(Handler 타입 캐시), 트랜잭션은 Command에만 적용을 요구한다.
- 트랜잭션 데코레이터와 `IUnitOfWork.CommitAsync`의 책임은 [ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md)에서 정했다. 이 ADR은 그 데코레이터가 들어가는 파이프라인 전체를 정한다.
- 예상 가능한 실패는 `Result`(정수 에러 코드)로 돌려준다([ADR-0008](0008-integer-codes-and-bitmask.md), [에러 코드](../../05-api/error-codes.md)). 검증 실패는 BuildingBlocks.Domain의 `ValidationError`(`Error` 파생, 필드별 상세)로 표현한다([S01 계획 리뷰](../../10-delivery/sprints/S01-decisions-build-ci.md#계획-리뷰) Q2).

## 검토한 대안 (Options)

1. **MediatR 12.x(마지막 Apache-2.0 버전)에 고정**: 장점: 널리 알려진 API, 파이프라인 동작 기능이 이미 있다. 단점: 더는 무료 버전이 갱신되지 않아 보안 · 호환 수정을 받을 수 없고, 나중에 상용 버전으로 올라갈 압력이 생긴다.
2. **소스 생성기 기반 Mediator(martinothamar/Mediator, MIT)**: 장점: 리플렉션 없이 빠르고 컴파일 시점에 Handler 누락을 잡는다. 단점: 생성 코드가 동작을 가려 과제 증빙 · 학습 관점에서 흐름이 덜 보이고, 생성기 버전과 분석기 경고(경고 = 오류)를 따로 관리해야 한다.
3. **Mediator 없이 Controller가 Handler 인터페이스를 직접 주입**: 장점: 코드가 가장 적다. 데코레이터는 그대로 적용된다. 단점: Controller가 유스케이스마다 Handler 타입에 의존하고, FR-05의 디스패처 요구와 맞지 않는다.
4. **직접 구현**: 작은 디스패처 + Handler 인터페이스를 감싸는 데코레이터. 장점: 의존이 없고, 파이프라인 순서가 코드와 등록에서 그대로 보이며, 필요한 기능만 둔다. 단점: 디스패처 · 데코레이터 · 등록 코드를 직접 만들고 테스트해야 한다.

파이프라인 적용 방식: (가) **Handler 인터페이스를 데코레이터로 감싸기**(DI의 데코레이터 등록, [ADR-0017](0017-scrutor-for-convention-based-di.md)): Command와 Query가 서로 다른 인터페이스라 "트랜잭션은 Command에만"이 타입으로 정해진다. (나) **MediatR식 `IPipelineBehavior<,>` 열린 제네릭 목록**: Command 전용 동작을 제네릭 제약으로 거르려면 기본 DI 컨테이너의 제약 처리 동작에 기대야 하고, 순서가 등록 순서에 숨는다.

## 결정 (Decision)

**대안 4(직접 구현), 파이프라인은 (가) Handler 데코레이터로 한다.**

계약 (BuildingBlocks.Application):

| 타입 | 형태 |
|---|---|
| `Unit` | 값이 없는 응답을 나타내는 `readonly record struct`(`Unit.Value`) |
| `ICommand<TResponse>` | 상태를 바꾸는 요청 |
| `ICommand` | `ICommand : ICommand<Unit>`. 반환 값이 없는 Command. ADR-0007의 "`Result` 반환 Command"는 `Result<Unit>`으로 표현한다 |
| `IQuery<TResponse>` | 조회 요청 |
| `ICommandHandler<TCommand, TResponse>` | `Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)` (`TCommand : ICommand<TResponse>`) |
| `ICommandHandler<TCommand>` | `ICommandHandler<TCommand, Unit>`을 상속하는 편의 인터페이스 |
| `IQueryHandler<TQuery, TResponse>` | `Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)` |
| `ISender` | `SendAsync<TResponse>(ICommand<TResponse>, CancellationToken)`, `QueryAsync<TResponse>(IQuery<TResponse>, CancellationToken)`. 둘 다 `Task<Result<TResponse>>` |

- Command / Query / 응답은 `record`, Handler는 `internal sealed class`(primary constructor)다([코딩 컨벤션](../../04-development/coding-conventions.md#cqrs-규칙)). Handler 하나는 요청 하나만 처리한다.
- `Unit`을 둔 이유: 반환 값이 있는 Command와 없는 Command가 같은 제네릭 인터페이스 · 같은 데코레이터 하나로 처리된다. 반환 값 없는 Command용 데코레이터를 따로 만들지 않는다.

디스패처 (`ISender` 구현, BuildingBlocks.Application):

- **Handler 타입 캐시**: 요청의 런타임 타입을 키로 하는 정적 `ConcurrentDictionary`에, 요청 타입마다 한 번 만든 호출기(`CommandInvoker<TCommand, TResponse>` 등, `MakeGenericType` + `Activator`로 한 번만 생성)를 담는다. 이후 호출은 캐시된 호출기가 `IServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>()`로 Handler를 꺼내 강타입으로 부른다. 요청마다 리플렉션 `Invoke`를 하지 않는다. 캐시는 타입 정보만 담고 인스턴스를 담지 않으므로 스레드 · 스코프와 무관하다.
- Handler가 등록되어 있지 않으면 요청 타입 이름을 담은 `InvalidOperationException`을 던진다. 등록 누락은 예상할 수 없는 프로그래밍 오류이므로 `Result`로 돌려주지 않는다(S02-T01).
- `ISender`는 Scoped로 등록하고 현재 스코프의 `IServiceProvider`로 Handler를 해석한다(Handler · 데코레이터 · Repository · DbContext가 같은 스코프를 쓴다).

파이프라인:

- **순서는 로깅 → 검증 → 트랜잭션 → Handler이며 [ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md)의 트랜잭션 데코레이터와 같은 것이다. 트랜잭션 데코레이터는 `ICommand` / `ICommand<T>`에만 적용하고 `IQuery<T>`에는 적용하지 않는다.** 구현상 트랜잭션 데코레이터는 `ICommandHandler<,>`만 감싼다(`ICommand : ICommand<Unit>`이므로 두 형태를 모두 덮는다).

| 요청 | 파이프라인 (바깥 → 안) |
|---|---|
| Command | 로깅 → 검증 → 트랜잭션 → Handler |
| Query | 로깅 → 검증 → Handler |

- **로깅 데코레이터(가장 바깥)**: 요청 형식 이름, 결과(성공 / 실패 `Error` 코드와 `ErrorType`), 경과 시간만 기록한다. 요청 · 응답 객체를 통째로 구조화 기록하지 않는다(개인정보). 경과 시간은 `TimeProvider.GetTimestamp()` / `GetElapsedTime`으로 잰다. 가장 바깥에 두어 검증 실패와 커밋 단계의 실패(영속성 충돌)까지 한 줄로 남는다(FR-05 "검증 실패도 로그에 남음"). 로그는 `[LoggerMessage]`로 정의하고 이벤트 ID는 공통 범위(1~999)에서 S02-T02가 할당한다. 수준은 성공 `Debug`, 실패 `Result` `Information`이다(예상 가능한 실패는 `Error`가 아니다, [로깅 & 관측성](../../04-development/logging-observability.md#로그-레벨-기준)). 예외는 잡지 않고 그대로 올려 보내며 여기서 로그를 남기지 않는다(예외 로그는 전역 예외 처리기 한 곳).
- **검증 데코레이터**: 트랜잭션 데코레이터보다 먼저 실행한다. 해당 요청의 FluentValidation Validator를 모두 실행해 실패가 있으면 **Handler · Repository · UoW에 닿기 전에 `ValidationError` 실패 `Result`를 반환**한다. Validator가 없으면 그대로 통과한다. 상세는 [ADR-0018](0018-use-fluentvalidation.md).
- **Validator는 DB에 접근하지 않는다(입력 형식 · 범위만).** 이메일 중복 같은 유일성 검사는 Handler 사전 조회 + 유니크 인덱스로 한다(ADR-0014의 두 겹 방어).
- **트랜잭션 데코레이터(Command 전용)**: **Handler를 실행 전략 밖에서 한 번 실행한다. 결과가 실패 `Result`면 `IUnitOfWork.CommitAsync`를 부르지 않고 그대로 반환하고, 성공일 때만 `CommitAsync(ct)`를 부른다.** 데코레이터는 DbContext · 트랜잭션 · 실행 전략 · `SaveChanges`를 직접 다루지 않는다(모두 UoW 안, ADR-0014).
- **`CommitAsync`가 영속성 충돌(`23505`, `DbUpdateConcurrencyException`)을 바꾼 실패 `Result`를 돌려주면, 데코레이터는 Handler의 성공 값 대신 그 실패를 반환한다.** 영속성 예외 형식은 Application으로 새지 않는다.
- 커밋 뒤 `ClearDomainEvents`는 UoW가 부른다(ADR-0014). 데코레이터는 부르지 않는다.
- **Query Handler는 트랜잭션 없이 읽기 DbContext(NoTracking)의 Read Repository로만 조회한다**([ADR-0007](0007-adopt-cqrs.md) · [ADR-0009](0009-separate-read-write-db-context.md) · ADR-0014).
- **Handler 안에서 Mediator로 다른 Command를 보내지 않는다**(Command 하나 = 트랜잭션 하나. 중첩 `SendAsync`는 커밋이 두 번 일어난다). Handler는 `ISender`를 주입받지 않는다. 후속 처리는 도메인 이벤트 / 통합 이벤트로 잇는다.

등록:

- Handler 등록과 데코레이터 적용은 BuildingBlocks.Infrastructure의 `AddConventionalServices`가 Scrutor `Scan` · `Decorate`로 한다([ADR-0017](0017-scrutor-for-convention-based-di.md)). Handler는 두 제네릭 인자 형태(`ICommandHandler<,>` / `IQueryHandler<,>`)로만 등록해, 데코레이터를 거치지 않는 경로가 생기지 않게 한다.
- 데코레이터는 안쪽부터 등록한다(마지막에 등록한 것이 가장 바깥): Command는 트랜잭션 → 검증 → 로깅, Query는 검증 → 로깅.
- 데코레이터 클래스는 BuildingBlocks.Application에 두고 `internal sealed`로 만든다. BuildingBlocks.Application은 `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, FluentValidation만 참조하고 Scrutor · EF Core는 참조하지 않는다([ADR-0003](0003-clean-architecture-and-ddd.md)).

## 결과 (Consequences)

- 긍정: 상용 라이선스와 외부 Mediator 의존이 없다. "트랜잭션은 Command에만"이 인터페이스 종류로 정해져 실수할 여지가 없다. 파이프라인 순서가 등록 코드에 그대로 드러나 증빙과 리뷰가 쉽다. 데코레이터마다 가짜 안쪽 Handler로 단위 테스트할 수 있다.
- 부정: 디스패처 · 데코레이터 · 등록을 직접 유지보수한다. Command용 · Query용 로깅 · 검증 데코레이터가 인터페이스마다 따로 있어 클래스 수가 늘어난다(공통 로직은 내부 도우미로 모은다). 알림(Publish) · 스트리밍 같은 기능은 없으며 필요해지면 새 ADR로 추가한다.
- 부정: 반환 값 없는 Command도 `Result<Unit>`을 돌려준다. [코딩 컨벤션 · CQRS 규칙](../../04-development/coding-conventions.md#cqrs-규칙)의 반환 표기(`Result`)와 Handler `SaveChanges` 예시는 S01-T04에서 이 ADR과 ADR-0014에 맞춘다.
- 검증: 계약 · 디스패처(캐시, 누락 시 예외)는 S02-T01, 순서(Command 4단계 · Query 3단계), 검증 실패 시 Handler 미호출 · 실패 로그, 실패 `Result`면 미커밋, 커밋 실패 `Result` 전달은 S02-T02 단위 테스트(가짜 `IUnitOfWork`), 데코레이터 적용 · 해석은 S02-T03, 실제 DB 동작은 S03-T05에서 확인한다. "Handler는 `ISender`를 주입받지 않는다"는 S02-T05 아키텍처 테스트 후보다.
