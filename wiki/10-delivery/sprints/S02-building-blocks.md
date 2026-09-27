---
title: "S02: BuildingBlocks Application · Infrastructure와 공통 API 처리"
type: sprint
sprint: "S02"
status: active
prd: [PRD-001]
started: 2026-09-27
finished:
adrs: [ADR-0024]
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

> 직접 구현한 디스패처와 파이프라인(로깅 → 검증 → 트랜잭션), 규칙 기반 DI 등록, UUID v7 생성, EF 공통 규칙, 실행 전략 UoW와 23505 · 동시성 예외 변환, BuildingBlocks.Api의 ProblemDetails 변환이 단위 테스트로 증명되고(DB 없이 완결), 아키텍처 테스트가 CI에서 돈다.

## 작업

실행 순서: T08 → T01 → T02 → T03 → T04 → T07 → T06 → T05 (계획 리뷰에서 T07 · T08 추가, T05를 맨 뒤로 이동)

| ID | 작업 | 요구사항 | 완료 조건 | 의존 | 상태 | 커밋 |
|---|---|---|---|---|---|---|
| S02-T08 | ADR-0024 공통 API 처리 계층 BuildingBlocks.Api 신설(N3) | FR-07, FR-09 | ADR-0024 `accepted`: `EmergencyHub.BuildingBlocks.Api`(FrameworkReference `Microsoft.AspNetCore.App`, Swashbuckle)는 Application · Domain만 참조하고 Infrastructure는 참조하지 않음, 서비스 Api 레이어만 이 프로젝트를 참조. Infrastructure에 ASP.NET을 넣는 대안은 MigrationService · Infrastructure 테스트까지 웹 API 코드와 Swashbuckle이 번지고 레이어 책임이 섞여 배제. 공개 진입점은 확장 메서드 2개(서비스 등록 · 파이프라인, 이름은 T06). Infrastructure 예외 분류는 Application 포트 + Infrastructure 구현. 의존성 규칙 표(초안)에 Api 행 추가(clean-architecture 반영은 S04-T02). PRD FR-09 테스트 프로젝트 목록에 `Api.UnitTests` 비고. check-docs 결함 증가 없음 | 없음 | doing | |
| S02-T01 | BuildingBlocks.Application 계약과 직접 구현 디스패처 | FR-05, FR-09 | `ICommand` / `ICommand<T>`(`ICommand : ICommand<Unit>`), `IQuery<T>`, Handler 인터페이스, `Unit`, `ISender`(`Command` · `Command<T>` · `Query<T>` Send), `IUnitOfWork`(`CommitAsync` → `Task<Result>`), 마커 `IReadRepository` / `IService`, `IIdGenerator`, 예외 분류 포트(예외 → `Error?`, 분류 못 하면 null. ADR-0024) 정의. 디스패처가 Handler 타입을 캐시(같은 요청 타입을 두 번 해석해도 캐시 항목 1개, 테스트 이름에 판정 방법 표시)하고 Handler가 없으면 요청 타입 이름을 담은 예외. 동시 Send · CancellationToken 전달 엣지. `Microsoft.Extensions.Logging.Abstractions` · `DependencyInjection.Abstractions` 버전 고정과 package-versions 기록(TD-008 관찰). 단위 테스트가 성공 / 실패 / 엣지를 다룸 | S01-T06 | todo | |
| S02-T02 | 데코레이터 동작: 로깅 · 검증(FluentValidation) · 트랜잭션 | FR-05 | 데코레이터별 동작을 단위 테스트(전체 순서는 T03). 검증 실패면 inner 미호출 · 실패 로그. 트랜잭션 데코레이터는 Handler 성공 Result일 때만 `IUnitOfWork.CommitAsync`를 부르고, 실패 Result는 커밋 없이 전달, `CommitAsync`의 실패 Result도 그대로 전달(ADR-0015). 데코레이터는 실행 전략 · 트랜잭션 · SaveChanges를 다루지 않음(T07). CustomState에 Error가 없는 실패는 `Error.Validation`(1001)로 감쌈(BL-053), `WithError` 확장. 로그: 메시지 템플릿, 성공 Debug · 실패 Result Information · 예외는 기록하지 않음, 이벤트 ID 하위 범위(1 전역 예외, 101~199 Mediator, 201~299 영속성, 301~399 API)를 error-codes에 기록(BL-028). `Microsoft.Extensions.Diagnostics.Testing`(FakeLogger) 추가 · package-versions 기록, FakeLogger로 개인정보 없음 단언 | T01 | todo | |
| S02-T03 | 규칙 기반 DI 등록(Scrutor), UUIDNext 기반 `IIdGenerator`, `TimeProvider` 등록, 파이프라인 순서 | FR-06, FR-05 | Application public 진입점 `AddBuildingBlocksApplication()`(데코레이터 open generic 목록 안쪽 → 바깥, `ISender` Scoped. InternalsVisibleTo 확대 금지). `AddConventionalServices`가 Scrutor `Scan`(internal 포함, Scoped, `RegistrationStrategy.Throw`)으로 마커 구현 타입 등록, Handler는 두 인자 형태만, `TryDecorate`(ADR-0017), Validator는 FluentValidation 검색. `TimeProvider.System` Singleton. `ValidateOnBuild` · `ValidateScopes`에서 모두 해석. 해석된 Command 체인이 로깅 → 검증 → 트랜잭션 → Handler, Query 체인에는 트랜잭션 없음. 중복 등록 시 시작 실패. UUID v7 버전 · variant 비트, 문자열(빅 엔디언) 기준 생성 순서 정렬, 대량 생성에서 같은 밀리초 쌍이 실제로 있었음을 단언한 뒤 단조 증가 확인. `IIdGenerator` 등록 방식은 ADR-0013 원문 확인 후 명시 | T01, T02 | todo | |
| S02-T04 | EF Core 공통 모델 규칙: 명명 · 변환 · 감사 · 동시성, Repository 기반, 읽기 전용 DbContext | FR-06 | snake_case, `ux_`(`HasDatabaseName` 덮어쓰기, 이름 상수는 T07 매핑과 공유) · `ck_<table>_<rule>`(enum IN 목록, `[Flags]`는 마스크 조건, 컬럼 이름은 메타데이터로 해석) 도우미. Domain에 `IStronglyTypedId<TSelf>` 추가, `ConfigureConventions`로 강타입 ID 변환 공통 등록 + `ValueGeneratedNever`, `DomainEvents` Ignore 공통 처리(TD-015). 감사 shadow property(루트 엔티티만, owned 변경 시 소유자 `updated_at` 갱신) + `SaveChangesInterceptor`(`TimeProvider`, +09:00에서도 UTC 저장, DB 없이 테스트 가능한 구조). `xmin`은 **EF shadow property**(`IsRowVersion`, xid, 컬럼 이름 `xmin` 확인)로 매핑하고 database.md 190행 수정. `RepositoryBase` / `ReadRepositoryBase`(람다 LINQ만), 읽기 전용 DbContext 기반(NoTracking, SaveChanges 오버로드 전부 예외). `IDesignTimeModel` 기준 메타데이터 단위 테스트(이름 · 제약 · 동시성 토큰 · ValueGenerated · DomainEvents 미매핑) | T01, T03 | todo | |
| S02-T07 | 실행 전략 UnitOfWork, 23505 / 동시성 예외 변환, 공통 DbContext 등록 확장 | FR-06, FR-07 | UoW: 실행 전략 안에서 Read Committed 트랜잭션 → SaveChanges(accept false) → Outbox 훅(재시도 때 다시 호출될 수 있어 멱등이어야 함을 XML 주석 · 테스트 이름에 명시) → 커밋 → AcceptAllChanges → ClearDomainEvents(Domain에 비제네릭 `IHasDomainEvents` 추가). 순수 번역기: 23505는 `ConstraintName`을 서비스 매핑 레지스트리(Infrastructure 계약)로 찾고 없으면 **3003 `Common.UniqueConstraintViolated`**(Conflict 409, BL-019. CommonErrors · CommonErrorsTests · error-codes.md 같은 커밋 갱신), `DbUpdateConcurrencyException` → 3001, 23514 · 기타 SqlState · Postgres가 아닌 inner는 변환하지 않음. `PostgresException` public 생성자로 규칙별 성공 · 실패 · 엣지 테스트. 23505 Detail 값이 Result 메시지 · 로그에 없음을 단언(로그는 제약 이름 · SqlState · 엔티티 형식만). 예외 분류 포트 구현(`RetryLimitExceededException` → 9003, 그 밖은 null, ADR-0024)과 등록. `AddUnitOfWork<TContext>()`와 공통 DbContext 등록(AddDbContext Scoped, UseNpgsql + EnableRetryOnFailure, snake_case, 인터셉터, 읽기 NoTracking) | T02, T04 | todo | |
| S02-T06 | 공통 API 처리(BuildingBlocks.Api): Result → ProblemDetails, 바인딩 오류 1001, enum 1002, 전역 예외 처리 | FR-07 | CommonErrors 재사용, 새 코드 할당 없음(BL-052). Result → RFC 9457 ProblemDetails(정수 `code`는 JSON 숫자, `traceId`는 `Activity.Current.TraceId`, 없으면 `TraceIdentifier`), ErrorType 9종 → HTTP 전수 `[Theory]`(None · 미정의는 500), ValidationError → `errors`(camelCase 필드, 정수 code). `InvalidModelStateResponseFactory`(1001, ActionContext 직접 구성 테스트). enum 1002 규칙은 Application에 둠(`IsInEnum().WithError(CommonErrors.InvalidCode)` 확장, `[Flags]`는 정의된 비트 조합 허용, 비Flags 0 거부). `IExceptionHandler`(예외 분류 포트를 먼저 묻고 결과가 없으면 9001, 타입 이름 문자열 판별 금지. RetryLimitExceeded → 9003은 포트 결과, 23514 · 25006 → 9001, BadHttpRequest → 400, 요청 중단은 기록 수준만 낮춤, 9001에 스택 미노출, 이벤트 ID 1은 이곳에서만). Controller 기본 설정 확장, Swashbuckle 설정. 테스트 프로젝트 `EmergencyHub.BuildingBlocks.Api.UnitTests`(sln 등록). Employee 코드 할당은 S03 | T01, T02, T08 | todo | |
| S02-T05 | 아키텍처 테스트(NetArchTest): 의존성 규칙과 컨벤션 규칙 | FR-09, NFR-02 | 규칙 원본은 clean-architecture 의존성 규칙 표(Api 행은 T08 초안). 의존성 규칙마다 테스트 1개 + BuildingBlocks 고유 규칙(Domain 외부 무참조, Application ↛ EF Core · Scrutor · ASP.NET Core, Api ↛ Infrastructure · EF Core · Npgsql, Infrastructure ↛ ASP.NET Core · BuildingBlocks.Api, Application ↛ Api 계열. 원본은 ADR-0024 의존성 규칙 표) + 컨벤션 규칙(sealed, Command / Query / Response는 record(`ICustomRule`), Repository 인터페이스의 마커 상속) + BL-029(Handler는 ISender 주입 금지, Validator는 Repository · Service 주입 금지, Error / Result 파생 금지, Entity 파생은 sealed). 규칙마다 대상 타입 1개 이상 단언(공허 통과 방지), 테스트 어셈블리 안 위반 예시 타입으로 실패 확인. 대상 어셈블리 목록은 한곳에서 관리(Employee는 S03). 첫 단계에서 NetArchTest 1.3.2 스파이크, 막히면 ArchUnitNET 전환 판단(TD-013) | T04, T06, T07, T08 | todo | |

상태: `todo` · `doing` · `done` · `blocked`(반려 3회) · `moved:BL-NNN`(백로그로 이관)

### 작업별 파이프라인

| 작업 | dba | developer | reviewer | tester |
|---|---|---|---|---|
| S02-T08 | 해당 없음 | ADR 1차(초안) → 사용자 확인 → 2차(파일) | 문서 규칙 | 명령 기반 점검표(check-docs, ADR 목록) |
| S02-T01 | 해당 없음 | TDD | 표준 진입 점검 | 인수 조건 대조, 단위 테스트 |
| S02-T02 | `IUnitOfWork.CommitAsync` 계약(Result 반환, 실패 전파)이 ADR-0014 · 0015와 맞는지 검토 | TDD(가짜 IUnitOfWork, FakeLogger) | 표준 진입 점검, 로그 규칙 | 미호출 · 미커밋 · CommitAsync 실패 전달 시나리오 |
| S02-T03 | 해당 없음(UUID의 DB 정렬은 S03-T05) | TDD | 표준 진입 점검, ADR-0017 검증 항목 | 인수 조건 대조, 파이프라인 순서 |
| S02-T04 | 명명 · 타입 매핑 · `xmin` · 감사 컬럼 · `ck_` 규칙 작성 / 검토(마이그레이션 없음), database.md 갱신 | TDD | 표준 진입 점검 | 모델 메타데이터 테스트. 인터셉터 실제 DB 동작 · 읽기 거부는 S03-T05 |
| S02-T07 | 제약 이름 · 매핑 계약 · 3003 · 예외 변환 규칙 검토 | TDD(순수 번역기) | 표준 진입 점검, 로그 개인정보 | 번역기 규칙표 대조. 실행 전략 + 트랜잭션 실제 동작은 S03-T05 |
| S02-T06 | 해당 없음 | TDD | api-guidelines · error-codes 대조 | 변환 규칙 확인. HTTP 전 구간은 S03-T05 |
| S02-T05 | 해당 없음 | 규칙 테스트 작성 | 의존성 규칙 표와 테스트 1:1 대응 | 위반 예시 타입으로 실패 확인 |

## 완료 기준 (DoD)

- [ ] 모든 작업이 `done`이거나 백로그로 이관되었다
- [ ] 빌드와 모든 테스트(단위 · 통합 · 아키텍처)가 통과했다
- [ ] 관련 위키 문서(API, 이벤트, DB)를 갱신했다
- [ ] 백로그 / 기술부채를 정리했다 (`new` 항목 없음)
- [ ] 토픽 브랜치를 push하고 `sprint/S02` 태그를 붙였다
- [ ] S03-T05로 이관한 DB 동작 검증 항목(실행 전략 + 트랜잭션, 감사 인터셉터 UTC, 읽기 연결 거부 25006, UUID DB 정렬, 23505 실제 발생과 제약 이름 → 3003 / 서비스 코드, RetryLimitExceeded → 9003, EF Error 로그 중복(BL-023))과 S03-T02 dba 검토 항목(idempotent SQL에 `xmin` 컬럼 생성 없음, `ux_` · `ck_` 이름 일치)을 결과 리뷰에 기록했다

## 진행 기록

> 단계 판정(PASS / REJECT / BLOCKED), 반려와 재작업, 계획 변경을 시간 순서로 적습니다.

| 날짜 | 작업 | 단계 | 판정 | 내용 (반려 시 되돌린 단계와 사유) |
|---|---|---|---|---|
| 2026-09-27 | S02 | 계획 리뷰 | 승인 | 작업 6 → 8개(T07 · T08 추가), T02 · T04 축소, T05 맨 뒤. N3 = BuildingBlocks.Api + ADR-0024, xmin = shadow property, 3003 = `Common.UniqueConstraintViolated` |
| 2026-09-27 | S02-T08 | dba | PASS | 해당 없음(DB 변경 없음). handoff → developer: 23505 변환은 Infrastructure · Api는 Npgsql / EF 타입 비참조, 23514는 500 · 제약 이름 미노출, MigrationService는 Api 비참조(배제 근거), DbContext 등록은 Api에 두지 않음 |
| 2026-09-27 | S02-T08 | developer | PASS | 1차 초안 → 사용자 확인(9003 = 분류 포트 A, Infrastructure ↛ ASP.NET Core는 아키텍처 테스트로 강제, 배제 근거 · 진입점 초안대로) → 2차 ADR-0024 accepted, ADR 목록 · PRD FR-09 비고 · frontmatter adrs. check-docs 기준선 유지(2건). handoff: T01 분류 포트, T07 구현(RetryLimitExceeded → 9003), T06 진입점 2개 · 분류기 우선, T05 규칙 원본 = ADR-0024 표(완료 조건에 반영됨) |
| 2026-09-27 | S02-T08 | reviewer | PASS | 템플릿 · frontmatter · 링크 결함 0, 0001~0023 불변, 완료 조건 · 사용자 확인 · dba 입력 4건 반영, ADR-0014 · 0016 · 0017 · 0019 · 0020 정합. handoff: T06 Api에 EF · Npgsql · Infrastructure 참조 시 반려, 분류기 0개 엣지 테스트 / T05 규칙마다 테스트 1개 · 대상 1개 이상 단언 |

## 계획 리뷰

> `/sprint` 시작 시 에이전트별 계획 리뷰와 orchestrator 통합 결과, 사용자 승인 내용을 요약합니다.

분할 시 남긴 메모 (계획 리뷰에서 확정):

- **N3 공통 API 처리 위치**: BuildingBlocks는 Domain / Application / Infrastructure 3개뿐이라 FR-07(ProblemDetails, `IExceptionHandler`, Controller 설정)을 둘 곳이 없다. Infrastructure에 ASP.NET Core 의존을 넣을지, `BuildingBlocks.Api`를 새로 만들지 정하고 S04-T02에서 clean-architecture에 반영한다. → **확정: BuildingBlocks.Api 신설, ADR-0024(S02-T08)**
- 이 스프린트는 DB 없이 완결한다. 실제 DB 동작 검증은 S03-T05로 이관하고 결과 리뷰에서 추적한다(위험: 재시도 실행 전략과 트랜잭션 충돌, Write / Read 매핑 미검증).

### 에이전트 리뷰 요약

| 에이전트 | 핵심 지적 |
|---|---|
| dba | BL-019 코드를 소비 작업(T04)보다 늦은 T06에서 할당, 23505 변환 · 매핑 레지스트리 누락, xmin 매핑 방식이 database.md와 불일치, 공통 DbContext 등록 담당 작업 없음, T04 분할 권장 |
| developer | T02가 ADR-0015 책임 분담과 다름(CommitAsync만 호출), 순서 테스트는 DI 조합(T03)에서만 가능, internal 데코레이터 등록 경로 필요(public 진입점), 강타입 ID 추상화 없음, T04 과대 |
| reviewer | T06 코드 중복 할당(BL-052), N3 미결이면 T06 판정 불가, T05 공허 통과 · 규칙 원본 미지정, T03 `Decorate` → `TryDecorate`, TD-015 · BL-028 · BL-053 완료 조건 누락 |
| tester | 로그 검증 도구 필요(FakeLogger), 23505 Detail 개인정보 유출 단언, ErrorType 전수 · enum `[Flags]` 엣지, UUID 같은 밀리초 쌍 존재 단언, NetArchTest record 판별 스파이크 |

### 사용자 승인 (2026-09-27)

- **N3**: `EmergencyHub.BuildingBlocks.Api` 신설, ADR-0024로 기록(S02-T08). PRD FR-09에 `Api.UnitTests` 비고
- **xmin**: EF shadow property. Domain 무변경, database.md 수정(S02-T04)
- **3003**: `Common.UniqueConstraintViolated`(Conflict 409), S02-T07에서 추가
- 계획 수정안 전체 승인: 작업 8개, 실행 순서 T08 → T01 → T02 → T03 → T04 → T07 → T06 → T05

### 백로그 / 기술부채 편입

| 항목 | 처리 |
|---|---|
| BL-019 | planned:S02 → S02-T07(3003) |
| BL-028 · BL-053 | S02-T02 |
| BL-052 | S02-T06 |
| TD-015 | S02-T04 |
| BL-029 | planned:S02 → S02-T05(BL-054 병합분 포함) |
| BL-023 | 편입하지 않음, S03-T05 실측 후 결정 |
| BL-055 | 편입하지 않음. 아래 reviewer 기준으로 먼저 합의, 명문화는 S04-T02 |

### 경고 억제 기준 (BL-055, reviewer 판정용)

- 억제는 해당 코드에 `[SuppressMessage]` + `Justification`을 필수로 붙인다.
- 전역 `NoWarn` · `.editorconfig` 수준 끄기는 금지한다(기존 설정 제외).
- 승인된 억제 목록은 reviewer가 진행 기록에 남긴다.

### 비차단 결정 (추천안 채택)

- 감사 시각이 재시도 때 다시 계산되는 것을 허용한다.
- 비`[Flags]` enum의 0은 거부(1002), `[Flags]`의 0은 "없음"으로 허용한다(T06 확정, coding-conventions는 S04-T02).
- `IHasDomainEvents`(T07) · `IStronglyTypedId<TSelf>`(T04)를 BuildingBlocks.Domain에 테스트와 함께 추가한다.
- FakeLogger는 ADR 없이 package-versions에 기록한다. ADR-0021 확장 여부는 결과 리뷰에서 판단한다.

### 위험

- DB 없이 완결하므로 UoW · 23505 · 감사 인터셉터의 실제 동작은 S03-T05에서야 확인된다(DoD 이관 목록으로 추적).
- T07은 규칙이 많아 반려 한도에 걸릴 수 있다(순수 번역기, 규칙표 선고정으로 완화).
- ADR-0024 확인 전에는 T06 · T05를 시작할 수 없다.
- NetArchTest 1.3.2 유지 중단(TD-013), Scrutor 7의 Microsoft.Extensions.* 10.0.0 전이 의존(TD-008).

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
| 2026-09-27 | orchestrator | 계획 리뷰 반영: T07 · T08 추가, T02 · T03 · T04 · T05 · T06 완료 조건 수정, 실행 순서 변경, `active` |
| 2026-09-27 | - | ADR-0024 초안 확인 반영: 예외 분류 포트(T01 계약, T07 구현, T06 사용), T05 규칙 추가, T08 배제 근거 문구 |
