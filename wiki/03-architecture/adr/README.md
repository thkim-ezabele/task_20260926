---
title: "아키텍처 결정 기록 (ADR)"
type: index
status: stable
tags: [adr, architecture]
created: 2026-09-27
updated: 2026-09-28
---

# 아키텍처 결정 기록 (ADR)

> 주요 기술/아키텍처 의사결정의 배경과 결과를 기록합니다.
>
> [위키 홈](../../README.md)

## 작성 방법

1. [ADR 템플릿](../../_templates/adr.md)으로 새 문서를 만듭니다 (Obsidian: Templates 플러그인).
2. 파일명은 `NNNN-kebab-case-title.md` 형식으로 하고, frontmatter의 `adr`, `aliases`, 제목의 번호를 맞춥니다.
3. 작성한 ADR을 아래 목록에 추가합니다.
4. 결정이 바뀌면 기존 ADR의 본문은 수정하지 않습니다. 새 ADR에 `supersedes`를 적고, 기존 ADR의 frontmatter만 `status: superseded`, `superseded_by`로 바꿉니다.

## ADR 목록

| 번호 | 제목 | 상태 | 날짜 |
|---|---|---|---|
| [0001](0001-use-dotnet8.md) | .NET 8 채택 | 승인 | 2026-09-27 |
| [0002](0002-adopt-msa.md) | 마이크로서비스 아키텍처(MSA) 채택 | 승인 | 2026-09-27 |
| [0003](0003-clean-architecture-and-ddd.md) | Clean Architecture + DDD 적용 | 승인 | 2026-09-27 |
| [0004](0004-adopt-event-driven-architecture.md) | 이벤트 기반 아키텍처(EDA) 채택 | 승인 | 2026-09-27 |
| [0005](0005-use-postgresql.md) | PostgreSQL 채택 | 승인 | 2026-09-27 |
| [0006](0006-adopt-tdd.md) | TDD 개발 방법론 채택 | 승인 | 2026-09-27 |
| [0007](0007-adopt-cqrs.md) | CQRS 적용 | 승인 | 2026-09-27 |
| [0008](0008-integer-codes-and-bitmask.md) | 코드값 정수화와 비트 마스킹 | 승인 | 2026-09-27 |
| [0009](0009-separate-read-write-db-context.md) | 읽기 / 쓰기 DB 연결 분리와 Repository 규칙 | 승인 | 2026-09-27 |
| [0010](0010-convention-based-di-registration.md) | 규칙 기반 DI 자동 등록 | 승인 | 2026-09-27 |
| [0011](0011-use-aspire-local-orchestration.md) | .NET Aspire 로컬 오케스트레이션 | 승인 | 2026-09-27 |
| [0012](0012-migration-apply-and-pre-production-reset.md) | 마이그레이션 적용 방식과 운영 전 리셋 정책 | 승인 | 2026-09-27 |
| [0013](0013-uuid-v7-with-uuidnext.md) | UUID v7 식별자 (IIdGenerator + UUIDNext) | 승인 | 2026-09-27 |
| [0014](0014-command-transaction-boundary-and-unit-of-work.md) | Command 트랜잭션 경계와 Unit of Work | 승인 | 2026-09-27 |
| [0015](0015-custom-mediator-pipeline.md) | Mediator 직접 구현과 데코레이터 파이프라인 | 승인 | 2026-09-27 |
| [0016](0016-use-controllers-for-api.md) | API 스타일로 Controller 사용 | 승인 | 2026-09-27 |
| [0017](0017-scrutor-for-convention-based-di.md) | Scrutor로 규칙 기반 DI 자동 등록 구체화 | 승인 | 2026-09-27 |
| [0018](0018-use-fluentvalidation.md) | 입력 검증에 FluentValidation 사용 | 승인 | 2026-09-27 |
| [0019](0019-use-swashbuckle-openapi.md) | OpenAPI 문서에 Swashbuckle 사용 | 승인 | 2026-09-27 |
| [0020](0020-logging-with-serilog-and-otlp.md) | 로깅 · 관측 구현 (Serilog + OTLP) | 승인 | 2026-09-27 |
| [0021](0021-test-tooling-xunit-v3-and-awesomeassertions.md) | 테스트 도구 (xUnit v3 · AwesomeAssertions와 주변 도구 고정) | 승인 | 2026-09-27 |
| [0022](0022-respawn-and-coverage-tooling.md) | 통합 테스트 DB 초기화(Respawn)와 커버리지 도구(coverlet + ReportGenerator) | 승인 | 2026-09-27 |
| [0023](0023-deferred-adoptions.md) | 도입 보류 (메시지 브로커, Outbox / Inbox, API Gateway, 로그 수집기) | 승인 | 2026-09-27 |
| [0024](0024-building-blocks-api-for-common-http-handling.md) | 공통 API 처리 계층 BuildingBlocks.Api 신설 | 승인 | 2026-09-27 |
| [0025](0025-api-rule-exceptions-for-assignment-endpoints.md) | 과제 API 명세에 따른 API 규칙 예외 (`/api/employee` 3개 엔드포인트) | 승인 | 2026-09-28 |
| [0026](0026-employee-bulk-import-input-processing.md) | 직원 일괄 가져오기 입력 처리 (바인더, 파서, 행 검증, 단일 트랜잭션 예외) | 승인 | 2026-09-28 |
| [0027](0027-case-insensitive-unique-email-with-normalized-column.md) | 이메일 대소문자 무시 유일 (정규화 컬럼 + 일반 유니크 인덱스) | 승인 | 2026-09-28 |
| [0028](0028-building-blocks-error-contract-extension.md) | BuildingBlocks 오류 계약 확장 (상세 Conflict 오류, 413 · 415) | 승인 | 2026-09-28 |
