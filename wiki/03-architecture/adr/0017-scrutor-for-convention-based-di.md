---
title: "ADR-0017: Scrutor로 규칙 기반 DI 자동 등록 구체화"
type: adr
adr: "0017"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0017]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0017: Scrutor로 규칙 기반 DI 자동 등록 구체화

## 배경 (Context)

- [ADR-0010](0010-convention-based-di-registration.md)은 마커 인터페이스(`IRepository`, `IReadRepository`, `IService`)와 기반 클래스를 상속한 타입을 어셈블리 검색으로 찾아 `Scoped`로 자동 등록하기로 했고, **타입 검색 구현(Scrutor / 직접 구현)은 기반 구축 토픽에서 정한다**고 후속으로 남겼다.
- [PRD-001 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q5에서 DI 타입 검색을 Scrutor로 정했고, FR-01은 이 결정을 "ADR-0010을 구체화하는 새 ADR, 대체 아님"으로 남기라고 요구한다. FR-06은 `AddConventionalServices`(Scrutor `Scan` + `Decorate`)를 요구한다.
- [ADR-0015](0015-custom-mediator-pipeline.md)의 파이프라인은 Handler 인터페이스를 데코레이터로 감싸므로, 열린 제네릭 데코레이터를 등록할 수단이 필요하다. 기본 DI 컨테이너(`Microsoft.Extensions.DependencyInjection`)에는 어셈블리 검색과 데코레이터 등록이 없다.

## 검토한 대안 (Options)

1. **리플렉션 직접 구현**: 장점: 외부 의존이 없다. 단점: 타입 검색 필터, 열린 제네릭 데코레이터(기존 등록을 찾아 수명을 유지한 채 감싸기)를 직접 만들고 테스트해야 한다.
2. **Scrutor(MIT)**: 장점: `Scan`(어셈블리 검색 · 필터 · 수명 · 중복 처리 전략)과 `Decorate`(열린 제네릭 포함)를 기본 컨테이너 위에서 제공한다. 단점: 외부 패키지 의존. 7.0.0은 net8.0에서 `Microsoft.Extensions.DependencyInjection.Abstractions` · `DependencyModel` 10.0.0을 전이로 끌어온다.
3. **서드파티 DI 컨테이너(Autofac 등)**: ADR-0010에서 이미 기각했다(기본 컨테이너 외 의존 증가).

## 결정 (Decision)

**대안 2: Scrutor 7.0.0으로 ADR-0010의 타입 검색과 데코레이터 등록을 구현한다.**

**이 ADR은 ADR-0010을 대체하지 않고 구체화한다.** ADR-0010의 결정(마커 · 기반 클래스, `Scoped`, 초기화 코드는 `AddConventionalServices`만 호출, 구현 클래스 `sealed` · 서비스 인터페이스 하나)은 그대로 유효하고, ADR-0010 본문은 고치지 않는다. 이 ADR은 ADR-0010 후속의 "타입 검색 구현"을 Scrutor로 정한 것이다.

- **진입점**: BuildingBlocks.Infrastructure의 `AddConventionalServices(params Assembly[] assemblies)` 하나. 서비스의 `Program.cs`는 이 메서드와 BuildingBlocks 공통 등록만 부르고, 구현 타입을 개별 등록하지 않는다. Scrutor는 BuildingBlocks.Infrastructure만 참조한다(Domain · Application은 Scrutor를 모른다).
- **마커 구현 타입 등록**: 마커(`IRepository`, `IReadRepository`, `IService`)마다 `Scan` → `FromAssemblies(assemblies)` → `AddClasses(c => c.AssignableTo(마커), publicOnly: false)`(internal 포함, 추상 제외) → `As(t => 마커를 상속한 인터페이스 중 마커 자신을 뺀 것)` → `WithScopedLifetime()`.
  - 중복 처리 전략은 `RegistrationStrategy.Throw`: 같은 서비스 인터페이스를 두 구현이 등록하면 시작 시 실패한다(ADR-0010 "인터페이스 하나 = 구현 하나").
  - 기반 클래스(`RepositoryBase<TDbContext>`, `ReadRepositoryBase<TDbContext>`)는 추상이라 등록 대상이 아니며, 구현 클래스가 마커 인터페이스를 구현해 등록된다.
- **Handler 등록**: `AddClasses(c => c.AssignableToAny(typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)), publicOnly: false)`를 두 제네릭 인자 형태의 닫힌 인터페이스로만 등록한다(`ICommandHandler<TCommand>` 편의 인터페이스로는 등록하지 않음, 데코레이터 우회 방지). 수명은 `Scoped`.
- **데코레이터**: Handler 등록 뒤 `Decorate(typeof(ICommandHandler<,>), ...)` / `Decorate(typeof(IQueryHandler<,>), ...)`를 안쪽부터 부른다(Command: 트랜잭션 → 검증 → 로깅, Query: 검증 → 로깅). 데코레이터는 감싼 서비스의 수명(`Scoped`)을 따른다. Command가 없는 서비스에서도 시작이 실패하지 않도록 `TryDecorate`를 쓰고, 데코레이터가 실제로 적용됐는지는 테스트로 확인한다(아래 검증).
- **Validator**: ADR-0010대로 FluentValidation의 `AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Scoped, includeInternalTypes: true)`로 등록한다([ADR-0018](0018-use-fluentvalidation.md)). 이 호출도 `AddConventionalServices` 안에서 한다.
- **공통 인프라 등록**: `TimeProvider`(Singleton), `ISender`(Scoped), `IIdGenerator` 등은 BuildingBlocks 공통 등록 코드에서 명시적으로 등록한다(ADR-0010의 Singleton / Transient 예외 규칙).
- **검증 옵션**: Development와 테스트 호스트에서 `ValidateOnBuild` · `ValidateScopes`를 켠다.

## 결과 (Consequences)

- 긍정: 새 Repository · 서비스 · Handler · Validator를 추가할 때 초기화 코드를 고치지 않는다. 파이프라인 순서가 `Decorate` 호출 순서 한 곳에 모인다. 중복 구현은 시작 시점에 드러난다.
- 부정: 등록이 암묵적이라 규칙을 알아야 디버깅할 수 있다(ADR-0010과 같음). `TryDecorate`는 대상이 없어도 조용히 지나가므로 테스트로 보완한다. Scrutor 7.0.0의 10.0.0 전이 의존은 Serilog · OpenTelemetry도 같은 전이를 가져오므로 따로 피하지 않는다(TD-008, [패키지 버전 · 라이선스 · 애플리케이션](../package-versions.md#애플리케이션)).
- 검증: S02-T03 테스트로 `ValidateOnBuild` · `ValidateScopes` 상태에서 모든 마커 구현 타입이 `Scoped`로 해석되는지, 해석한 Command Handler의 가장 바깥이 로깅 데코레이터이고 안쪽 순서가 ADR-0015와 같은지, 같은 인터페이스 중복 구현 시 시작이 실패하는지 확인한다. 등록 누락 통합 검증은 S03-T05.
- 후속: [코딩 컨벤션 · DI 규칙](../../04-development/coding-conventions.md#의존성-주입-di-규칙)의 타입 검색 🟡를 S01-T04에서 이 ADR로 해소한다.
