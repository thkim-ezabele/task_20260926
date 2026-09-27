---
title: "ADR-0016: API 스타일로 Controller 사용"
type: adr
adr: "0016"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0016]
tags: [adr, architecture, api]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0016: API 스타일로 Controller 사용

## 배경 (Context)

- [Clean Architecture](../clean-architecture.md#서비스별-프로젝트-구성) 문서는 API 스타일을 🟡로 두고 기본안을 Minimal API(엔드포인트 그룹)로 적어 두었다.
- [PRD-001](../../10-delivery/prd/PRD-001-foundation.md) 인터뷰와 [질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q5에서 API 스타일을 **Controller**로 정했다.
- FR-07은 `Result` → RFC 9457 `ProblemDetails`(정수 `code`, `traceId`) 변환, `InvalidModelStateResponseFactory`로 바인딩 오류를 `1001`로 변환, 전역 예외 처리(`IExceptionHandler`), Controller 기반 설정, Swashbuckle을 요구한다([API 설계](../../04-development/api-guidelines.md), [에러 코드](../../05-api/error-codes.md)).
- Api 레이어는 요청을 Command / Query로 바꾸고 `Result`를 HTTP 응답으로 바꾸는 진입점이며, Infrastructure는 DI 등록에만 쓴다([ADR-0003](0003-clean-architecture-and-ddd.md)).

## 검토한 대안 (Options)

1. **Minimal API(엔드포인트 그룹)**: 장점: 코드가 짧고 시작 비용이 작다. 단점: .NET 8에서 모델 바인딩 오류 응답을 `InvalidModelStateResponseFactory` 한 곳으로 모을 수 없어 FR-07의 `1001` 변환을 엔드포인트 필터로 따로 만들어야 하고, 필터 · 규칙 적용 위치가 엔드포인트마다 흩어지기 쉽다.
2. **Controller(`[ApiController]`)**: 장점: 특성 라우팅 · 모델 바인딩 · 자동 400 응답 · `InvalidModelStateResponseFactory` · 필터가 한 체계로 갖춰져 있고, Swashbuckle과 `[ProducesResponseType]`으로 문서화가 쉽다. 에이전트가 따르기 쉬운 반복 구조다. 단점: Minimal API보다 코드와 시작 비용이 조금 크다.

## 결정 (Decision)

**대안 2: 서비스 Api는 `[ApiController]` Controller로 만든다.**

- **위치와 형태**: Api 프로젝트의 `Controllers/` 폴더(Clean Architecture 문서의 `Endpoints/`를 대체). Controller는 `public sealed class`, `ControllerBase` 상속, primary constructor로 `ISender`만 받는다([ADR-0015](0015-custom-mediator-pipeline.md)).
- **Controller는 얇게 둔다**: 요청 `record` → Command / Query 변환, `ISender` 호출, `Result` → HTTP 응답 변환만 한다. 업무 판단 · 검증 · DB 접근을 두지 않는다. Infrastructure 타입과 Repository를 주입받지 않는다(아키텍처 테스트, S03-T03).
- **라우팅**: 특성 라우팅, 경로에 주 버전을 쓴다(`[Route("api/v1/employees")]`, [API 설계 · API 버저닝](../../04-development/api-guidelines.md#api-버저닝)). 버저닝 패키지는 쓰지 않는다.
- **응답**: 성공은 [HTTP 메서드 & 상태 코드](../../04-development/api-guidelines.md#http-메서드--상태-코드) 표대로(`POST` 생성은 `201` + `Location` + `{ "id": ... }`, `CreatedAtAction`). 실패 `Result`는 공통 변환기가 `ErrorType`으로 HTTP 상태를 정하고 `ProblemDetails`(`code`, `traceId`, 검증 실패는 `errors`)로 만든다. 액션은 `ActionResult<T>` + `[ProducesResponseType]`으로 성공 · 실패 형식을 선언한다.
- **바인딩 오류**: `[ApiController]`의 자동 400을 유지하고 `InvalidModelStateResponseFactory`로 `1001`(`Common.ValidationFailed`) `ProblemDetails`로 바꾼다. 모델 바인딩은 JSON 형식 · 형식 변환 오류만 맡도록 `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`로 두고, 필수 값 · 형식 · 범위 · 정의되지 않은 enum(`1002`) 검증은 FluentValidation이 규칙별 코드로 한다([ADR-0018](0018-use-fluentvalidation.md)). 요청 모델에 데이터 어노테이션을 쓰지 않는다.
- **JSON**: System.Text.Json 웹 기본값(camelCase). `JsonStringEnumConverter`를 등록하지 않아 코드값은 정수로 주고받는다([ADR-0008](0008-integer-codes-and-bitmask.md)). `null` 속성도 생략하지 않는다([요청 / 응답 포맷](../../04-development/api-guidelines.md#요청--응답-포맷)).
- **취소**: 액션은 `CancellationToken`을 마지막 매개변수로 받아 `ISender`에 넘긴다.
- **공통 설정**: `AddControllers` 옵션, `InvalidModelStateResponseFactory`, `Result` → `ProblemDetails` 변환기, `IExceptionHandler`(예상 못한 예외 → `9001`), Swashbuckle 설정은 서비스마다 복사하지 않고 공통 위치 하나에 둔다. 위치(메모 N3)는 S02-T06에서 정한다.

## 결과 (Consequences)

- 긍정: 바인딩 오류 · 검증 실패 · 업무 실패 · 예상 못한 예외가 모두 같은 `ProblemDetails` 형식으로 나간다(FR-07). 문서화와 테스트(`WebApplicationFactory`) 경로가 표준적이다.
- 부정: Minimal API보다 시작 시간과 코드가 조금 늘어난다. `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`이므로 null 불가 참조 속성에 `null`이 들어올 수 있고, Validator가 반드시 필수 값을 검사해야 한다(Validator 단위 테스트로 확인).
- 후속: [Clean Architecture](../clean-architecture.md)의 `Endpoints/` · Minimal API 기본안을 S01-T04에서 Controller로 고친다. 공통 API 처리는 S02-T06, Employee Controller는 S03-T03, 응답 형식 통합 검증은 S03-T05.
