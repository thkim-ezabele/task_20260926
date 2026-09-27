---
title: "ADR-0018: 입력 검증에 FluentValidation 사용"
type: adr
adr: "0018"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0018]
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0018: 입력 검증에 FluentValidation 사용

## 배경 (Context)

- [코딩 컨벤션 · CQRS 규칙](../../04-development/coding-conventions.md#cqrs-규칙)은 Command 검증을 "FluentValidation Validator(파이프라인에서 자동 실행)"로 적어 두었고, [PRD-001 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q11에서 FluentValidation 도입을 확정했다.
- 파이프라인은 로깅 → 검증 → 트랜잭션 → Handler이고, 검증 실패 시 Handler가 호출되지 않아야 한다([ADR-0015](0015-custom-mediator-pipeline.md), FR-05).
- 검증 실패는 BuildingBlocks.Domain의 `ValidationError`(`Error` 파생 non-sealed record, 필드별 상세)로 표현한다([S01 계획 리뷰](../../10-delivery/sprints/S01-decisions-build-ci.md#계획-리뷰) Q2). API는 이를 `400` `ProblemDetails`(`code` `1001`, 필드별 `errors`의 정수 `code`)로 바꾼다([API 설계 · 에러 응답 포맷](../../04-development/api-guidelines.md#에러-응답-포맷-problemdetails)).
- 에러 코드는 정수이고 문자열 코드는 금지다([ADR-0008](0008-integer-codes-and-bitmask.md)). FluentValidation의 `ErrorCode`는 문자열이다.
- FR-07은 정의되지 않은 enum 정수를 Validator에서 `1002`(`Common.InvalidCode`)로 거부하라고 요구한다.

## 검토한 대안 (Options)

1. **데이터 어노테이션(`System.ComponentModel.DataAnnotations`)**: 장점: 프레임워크 내장. 단점: 규칙이 요청 모델 특성에 붙어 Command(Application)와 섞이고, 규칙별 정수 코드 · 조건부 규칙 표현이 어렵다. Controller를 거치지 않는 호출은 검증되지 않는다.
2. **FluentValidation 12.1.1(Apache-2.0) + Mediator 검증 데코레이터**: 장점: 규칙을 Command 옆 Validator 클래스로 분리해 단위 테스트가 쉽고, HTTP 여부와 관계없이 파이프라인에서 실행된다. 단점: 외부 패키지 의존, 규칙별 정수 코드를 붙이는 방식을 따로 정해야 한다.
3. **FluentValidation.AspNetCore 자동 검증(MVC 필터)**: 장점: 설정이 적다. 단점: 패키지 제작자가 더는 권장하지 않고 비동기 규칙을 막으며, Controller 경로에서만 동작해 파이프라인 순서(Q14)와 맞지 않는다.
4. **직접 구현한 검증 규칙**: 장점: 의존이 없다. 단점: 규칙 조합 · 메시지 · 컬렉션 규칙을 다시 만들어야 한다.

## 결정 (Decision)

**대안 2: FluentValidation 12.1.1과 FluentValidation.DependencyInjectionExtensions 12.1.1을 쓰고, 검증은 Mediator 검증 데코레이터에서만 실행한다.**

- **위치 · 형태**: Validator는 Application의 Command / Query와 같은 기능 폴더에 두고(`RegisterEmployeeCommandValidator`), `internal sealed class ... : AbstractValidator<TRequest>`로 만든다. Query에는 필요할 때만 둔다.
- **등록**: `AddConventionalServices` 안에서 `AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Scoped, includeInternalTypes: true)`([ADR-0010](0010-convention-based-di-registration.md), [ADR-0017](0017-scrutor-for-convention-based-di.md)).
- **실행(검증 데코레이터)**: 요청 타입의 `IEnumerable<IValidator<TRequest>>`를 받아, 없으면 그대로 통과하고, 있으면 순서대로 `ValidateAsync(request, cancellationToken)`를 실행해 실패를 모은다. 실패가 하나라도 있으면 Handler를 부르지 않고 `ValidationError` 실패 `Result`를 반환한다. 예외(`ValidateAndThrow`)는 쓰지 않는다(예상 가능한 실패는 `Result`).
- **`ValidationError` 구성**: 대표 코드 `1001`(`Common.ValidationFailed`, `ErrorType` Validation)과 필드별 상세 목록(속성 경로, 정수 코드, 메시지). 속성 경로는 FluentValidation의 `PropertyName`(Command 속성 이름)을 그대로 담고, JSON 키(camelCase) 변환은 API의 `ProblemDetails` 변환기가 한다(S02-T06).
- **규칙별 정수 코드**: 규칙에 BuildingBlocks 확장 메서드 `WithError(Error error)`를 붙인다. 이 메서드는 `WithState(_ => error)`로 `Error`를 `CustomState`에 싣고 `WithMessage(error.Message)`로 메시지를 맞춘다. 데코레이터는 `ValidationFailure.CustomState`의 `Error.Code`를 쓴다. `CustomState`에 `Error`가 없는 규칙의 실패는 `1001`로 담는다. FluentValidation의 문자열 `ErrorCode`는 쓰지 않는다(ADR-0008).
- **정의되지 않은 코드값**: 공통 규칙 확장 `MustBeDefinedEnum()`(`Enum.IsDefined`)이 `1002`(`Common.InvalidCode`)로 거부한다. `[Flags]` 조합 검사 규칙은 비트 플래그 샘플을 다루는 Phase 2에서 추가한다.
- **Validator는 DB에 접근하지 않는다(입력 형식 · 범위만).** Repository · DbContext · `IService`를 주입받지 않고 `MustAsync`로 DB를 조회하지 않는다. 이메일 중복 같은 유일성 검사는 Handler 사전 조회 + 유니크 인덱스로 한다([ADR-0014](0014-command-transaction-boundary-and-unit-of-work.md)의 두 겹 방어).
- **중단 방식**: 한 속성의 규칙 체인은 첫 실패에서 멈춘다(`RuleLevelCascadeMode = CascadeMode.Stop`). 여러 속성의 실패는 모두 모은다. 설정 위치(전역 기본값 / 공통 기반)는 S02-T02에서 정한다.
- **요청 모델의 역할**: 모델 바인딩은 JSON 형식 · 형식 변환 오류만 맡고, 필수 값을 포함한 입력 규칙은 모두 Validator가 맡는다([ADR-0016](0016-use-controllers-for-api.md)).

## 결과 (Consequences)

- 긍정: 검증 규칙이 Application에 모여 HTTP 밖(배치, 메시지 소비자)에서도 같은 규칙이 적용된다. 규칙별 정수 코드가 `ProblemDetails.errors`까지 이어진다. Validator를 단위 테스트로 규칙마다 검증할 수 있다.
- 부정: `WithError`를 빠뜨린 규칙은 `1001`로 뭉개진다(Validator 단위 테스트에서 코드까지 단언해 막는다). FluentValidation 12는 net8.0 전용이므로 .NET 업그레이드 때 함께 확인한다(TD-002).
- 후속: `ValidationError`는 S01-T06(BuildingBlocks.Domain), 검증 데코레이터 · `WithError` · `MustBeDefinedEnum`은 S02-T02 · S02-T06, 샘플 Validator(이메일 형식, enum `1002`)는 S03-T01에서 구현한다.
