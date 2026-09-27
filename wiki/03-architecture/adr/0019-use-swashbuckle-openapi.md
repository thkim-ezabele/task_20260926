---
title: "ADR-0019: OpenAPI 문서에 Swashbuckle 사용"
type: adr
adr: "0019"
status: accepted
date: 2026-09-27
deciders: []
supersedes:
superseded_by:
aliases: [ADR-0019]
tags: [adr, architecture, api]
created: 2026-09-27
updated: 2026-09-27
---

# ADR-0019: OpenAPI 문서에 Swashbuckle 사용

## 배경 (Context)

- API는 OpenAPI 문서로 계약을 제공하고, 정수 코드값의 의미를 문서 설명으로 알려야 한다([API 설계 · 요청 / 응답 포맷](../../04-development/api-guidelines.md#요청--응답-포맷)). [PRD-001 질문과 답변](../../10-delivery/prd/PRD-001-foundation.md#질문과-답변) Q11에서 Swashbuckle 도입을 확정했고, FR-07이 Swashbuckle(OpenAPI)을 공통 API 처리에 포함한다.
- 대상 프레임워크는 net8.0이다([ADR-0001](0001-use-dotnet8.md)). API 스타일은 Controller다([ADR-0016](0016-use-controllers-for-api.md)).
- 코드값은 API에서도 정수로 주고받는다([ADR-0008](0008-integer-codes-and-bitmask.md)).

## 검토한 대안 (Options)

1. **Microsoft.AspNetCore.OpenApi**: 장점: Microsoft 공식. 단점: 문서 생성(`AddOpenApi` / `MapOpenApi`)은 .NET 9부터이고, .NET 8 버전은 Minimal API용 `WithOpenApi` 보조 기능만 있어 단독으로 문서를 만들 수 없다.
2. **Swashbuckle.AspNetCore 10.2.3(MIT)**: 장점: net8.0을 지원하고 Controller · `[ProducesResponseType]` · 스키마 필터로 문서화가 쉬우며 Swagger UI를 함께 제공한다. 단점: 10.x는 Microsoft.OpenApi 2.x를 쓰므로 예전(1.x) 예제와 네임스페이스 · API가 다르다.
3. **NSwag(MIT)**: 장점: 문서 생성과 클라이언트 코드 생성까지 한다. 단점: 이번 범위에 클라이언트 생성이 필요 없고 설정 면이 더 넓다.

## 결정 (Decision)

**대안 2: Swashbuckle.AspNetCore 10.2.3으로 OpenAPI 문서와 Swagger UI를 제공한다.**

- **노출 범위**: OpenAPI JSON과 Swagger UI는 **Development 환경에서만** 연다(S03-T03 "개발 환경 Swagger UI"). 다른 환경에서는 엔드포인트를 매핑하지 않는다.
- **문서 단위**: 주 버전 경로와 맞춰 문서 이름 `v1` 하나를 둔다(`/api/v1/...`, [API 버저닝](../../04-development/api-guidelines.md#api-버저닝)).
- **코드값**: `JsonStringEnumConverter`를 쓰지 않으므로 enum 스키마는 정수로 나온다. 정수 값의 의미(값 = 이름)는 스키마 필터로 스키마 설명에 넣는다. 문자열 enum 스키마로 바꾸지 않는다.
- **실패 응답**: 액션의 `[ProducesResponseType]`으로 실패 상태 코드마다 `ProblemDetails` 형식을 선언하고, 확장 필드 `code`(정수) · `traceId` · `errors`가 스키마에 드러나게 한다([에러 응답 포맷](../../04-development/api-guidelines.md#에러-응답-포맷-problemdetails)).
- **설정 위치**: Swashbuckle 설정은 공통 API 처리와 같은 공통 위치에 둔다(메모 N3, S02-T06). 서비스 Api는 공통 확장 메서드만 부른다.
- **XML 문서 주석**: Api 프로젝트의 XML 문서 생성(`GenerateDocumentationFile`)은 켜지 않는다(FR-02는 BuildingBlocks만 CS1591 오류화). 설명이 필요한 곳은 특성과 스키마 필터로 넣는다.

## 결과 (Consequences)

- 긍정: net8.0에서 추가 작업 없이 Controller 기반 문서와 UI를 얻는다. 정수 코드 규칙을 문서에서도 지킨다.
- 부정: .NET 9 이상으로 올리면 Microsoft.AspNetCore.OpenApi로 옮길지 다시 검토해야 한다(TD-002와 함께). Microsoft.OpenApi 2.x API로 필터를 작성해야 한다([패키지 버전 · 라이선스 · 애플리케이션](../package-versions.md#애플리케이션)). 문서 출력 OpenAPI 버전(3.0 / 3.1) 기본값은 S02-T06에서 확인해 기록한다.
- 후속: 공통 Swashbuckle 설정(정수 enum 설명 필터, `ProblemDetails` 스키마)은 S02-T06, Employee 문서 · Development 전용 UI는 S03-T03에서 구현한다. 문서의 "OpenAPI 설명" 연결은 [API 레퍼런스](../../05-api/api-reference.md) 작성 토픽에서 다룬다.
