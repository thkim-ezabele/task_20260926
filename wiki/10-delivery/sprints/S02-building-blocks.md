---
title: "S02: BuildingBlocks Application · Infrastructure와 공통 API 처리"
type: sprint
sprint: "S02"
status: planned
prd: [PRD-001]
started:
finished:
adrs: []
worklogs: []
aliases: [S02]
tags: [delivery, sprint]
created: 2026-09-27
updated: 2026-09-27
---

# S02: BuildingBlocks Application · Infrastructure와 공통 API 처리

- PRD: [PRD-001](../prd/PRD-001-foundation.md)
- 토픽 브랜치: `feature/prd-001-foundation` · 스프린트 종료 태그: `sprint/S02`

## 목표

> 직접 구현한 디스패처와 파이프라인(로깅 → 검증 → 트랜잭션), 규칙 기반 DI 등록, UUID v7 생성, EF 공통 규칙, ProblemDetails 변환이 단위 테스트로 증명되고(DB 없이 완결), 아키텍처 테스트가 CI에서 돈다.

## 작업

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S02-T01 | BuildingBlocks.Application 계약과 직접 구현 디스패처 | FR-05, FR-09 | `ICommand` / `ICommand<T>`(`ICommand : ICommand<Unit>`), `IQuery<T>`, Handler 인터페이스, `Unit`, `IUnitOfWork`, 마커 `IReadRepository` / `IService`, `IIdGenerator` 정의. 디스패처가 Handler 타입을 캐시하고 Handler가 없으면 명확한 예외. 단위 테스트가 성공 / 실패 / 엣지를 다룸 | S01-T06 | todo | |
| S02-T02 | 데코레이터 파이프라인: 로깅 → 검증(FluentValidation) → 트랜잭션 | FR-05 | Command는 로깅 → 검증 → 트랜잭션 → Handler, Query는 로깅 → 검증 → Handler 순서를 테스트로 확인. 검증 실패면 Handler 미호출 · 실패 로그 기록. 트랜잭션 데코레이터는 `IUnitOfWork`의 실행 전략 감싸기 API로 트랜잭션 → SaveChanges → 커밋, 실패 Result면 미커밋. Handler는 SaveChanges를 부르지 않음. Outbox 확장 지점(커밋 전 훅) 존재. 로그는 메시지 템플릿, 개인정보 없음 | T01 | todo | |
| S02-T03 | 규칙 기반 DI 등록(Scrutor), UUIDNext 기반 `IIdGenerator`, `TimeProvider` 등록 | FR-06, FR-05 | `AddConventionalServices`가 Scrutor `Scan`으로 마커 구현 타입을 Scoped 등록, `Decorate`로 데코레이터 적용. `ValidateOnBuild` · `ValidateScopes`에서 모두 해석. UUID v7 버전 비트, 문자열(빅 엔디언) 기준 생성 순서 정렬, 같은 밀리초 내 단조 증가를 테스트 | T01, T02 | todo | |
| S02-T04 | EF Core 공통 인프라: 명명 · 변환 · 감사 · 동시성, Repository 기반, 읽기 전용 DbContext, 실행 전략 UoW | FR-06, FR-07 | snake_case, `ux_` · `ck_<table>_<rule>` 명명 도우미, 강타입 ID 변환 + `ValueGeneratedNever`, 감사 shadow property + `SaveChangesInterceptor`(`TimeProvider`, `DateTimeOffset` UTC), `xmin`, `RepositoryBase` / `ReadRepositoryBase`(람다 LINQ만), 읽기 전용 DbContext 기반(NoTracking, SaveChanges 예외), 실행 전략 UnitOfWork, `DbUpdateConcurrencyException` → 충돌 Result. Npgsql 제공자 테스트 모델의 `IModel` 메타데이터로 이름 · 제약 · 동시성 토큰 · ValueGenerated를 단위 테스트 | T01, T03 | todo | |
| S02-T05 | 아키텍처 테스트(NetArchTest): 의존성 규칙과 컨벤션 규칙 | FR-09, NFR-02 | 의존성 규칙마다 테스트 1개 + 컨벤션 규칙(sealed, Command / Query / Response는 record, Repository 인터페이스의 마커 상속)이 BuildingBlocks 어셈블리에 대해 통과. 대상 어셈블리 목록은 한곳에서 관리. 일부러 규칙을 어기면 실패함을 한 번 확인 · 기록 | T01, T04 | todo | |
| S02-T06 | 공통 API 처리: Result → ProblemDetails, 바인딩 오류 1001, enum 1002, 전역 예외 처리, 공통 에러 코드 | FR-07 | Result → RFC 9457 ProblemDetails(정수 `code`, `traceId`) 변환기, `InvalidModelStateResponseFactory`(1001), 정의되지 않은 enum 거부 공통 Validator 규칙(1002), `IExceptionHandler`, Controller 기본 설정 확장 메서드, Swashbuckle 설정을 공통 위치(메모 N3)에 구현. 공통 에러 코드를 `S T NNN` 형식으로 할당. ErrorType별 HTTP 상태와 code를 단위 테스트 | T01, T02 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S02-T01 | 해당 없음 | TDD | 표준 진입 점검 | 인수 조건 대조, 단위 테스트 |
| S02-T02 | 트랜잭션 데코레이터 · `IUnitOfWork` 계약이 S01-T02 ADR과 맞는지 검토 | TDD(가짜 IUnitOfWork) | 표준 진입 점검, 로그 규칙 | 순서 · 미호출 · 미커밋 시나리오. 실제 실행 전략 · DB 동작은 S03-T05로 이관 |
| S02-T03 | 해당 없음(UUID의 DB 정렬은 S03-T05) | TDD | 표준 진입 점검 | 인수 조건 대조 |
| S02-T04 | 명명 · 타입 매핑 · `xmin` · 감사 컬럼 규칙 작성 / 검토(마이그레이션 없음) | TDD | 표준 진입 점검 | 모델 메타데이터 테스트. 인터셉터 · UoW · 읽기 거부 실제 DB 동작은 S03-T05 |
| S02-T05 | 해당 없음 | 규칙 테스트 작성 | 의존성 규칙 표와 테스트 1:1 대응 | 위반 코드로 실패 확인(기록만, 커밋 안 함) |
| S02-T06 | 해당 없음 | TDD | api-guidelines · error-codes 대조 | 변환 규칙 확인. HTTP 전 구간은 S03-T05 |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S02` 태그를 붙였다
- [ ] S03-T05로 이관한 DB 동작 검증 항목(실행 전략 + 트랜잭션, 감사 인터셉터, 읽기 연결 거부, UUID DB 정렬)을 결과 리뷰에 기록했다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| | | | | |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모 (계획 리뷰에서 확정):

- **N3 공통 API 처리 위치**: BuildingBlocks는 Domain / Application / Infrastructure 3개뿐이라 FR-07(ProblemDetails, `IExceptionHandler`, Controller 설정)을 둘 곳이 없다. Infrastructure에 ASP.NET Core 의존을 넣을지, `BuildingBlocks.Api`를 새로 만들지 정하고 S04-T02에서 clean-architecture에 반영한다.
- 이 스프린트는 DB 없이 완결한다. 실제 DB 동작 검증은 S03-T05로 이관하고 결과 리뷰에서 추적한다(위험: 재시도 실행 전략과 트랜잭션 충돌, Write / Read 매핑 미검증).

## 결과 리뷰

> 스프린트 종료 시 orchestrator의 결과 리뷰(계획 대비 실제, 완료 조건 · FR 충족, 반려 분석)를 요약합니다.

-

## 생긴 백로그 / 기술부채

| ID | 제목 | 발생 작업 | 정리 결과 |
|---|---|---|---|
| | | | open / planned:SNN / dropped |

## 회고

### 잘된 점

-

### 문제

-

### 다음에 바꿀 것

-

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 스프린트 계획 (`/prd` PRD-001 분할) |
