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
| S02-T08 | ADR-0024 공통 API 처리 계층 BuildingBlocks.Api 신설(N3) | FR-07, FR-09 | ADR-0024 `accepted`: `EmergencyHub.BuildingBlocks.Api`(FrameworkReference `Microsoft.AspNetCore.App`, Swashbuckle)는 Application · Domain만 참조하고 Infrastructure는 참조하지 않음, 서비스 Api 레이어만 이 프로젝트를 참조. Infrastructure에 ASP.NET을 넣는 대안은 MigrationService · Infrastructure 테스트까지 웹 API 코드와 Swashbuckle이 번지고 레이어 책임이 섞여 배제. 공개 진입점은 확장 메서드 2개(서비스 등록 · 파이프라인, 이름은 T06). Infrastructure 예외 분류는 Application 포트 + Infrastructure 구현. 의존성 규칙 표(초안)에 Api 행 추가(clean-architecture 반영은 S04-T02). PRD FR-09 테스트 프로젝트 목록에 `Api.UnitTests` 비고. check-docs 결함 증가 없음 | 없음 | done | 6bf97e0, 05ac2ea, d29199f, 38b86d5 |
| S02-T01 | BuildingBlocks.Application 계약과 직접 구현 디스패처 | FR-05, FR-09 | `ICommand` / `ICommand<T>`(`ICommand : ICommand<Unit>`), `IQuery<T>`, Handler 인터페이스, `Unit`, `ISender`(`Command` · `Command<T>` · `Query<T>` Send), `IUnitOfWork`(`CommitAsync` → `Task<Result>`), 마커 `IReadRepository` / `IService`, `IIdGenerator`, 예외 분류 포트(예외 → `Error?`, 분류 못 하면 null. ADR-0024) 정의. 디스패처가 Handler 타입을 캐시(같은 요청 타입을 두 번 해석해도 캐시 항목 1개, 테스트 이름에 판정 방법 표시)하고 Handler가 없으면 요청 타입 이름을 담은 예외. 동시 Send · CancellationToken 전달 엣지. `Microsoft.Extensions.Logging.Abstractions` · `DependencyInjection.Abstractions` 버전 고정과 package-versions 기록(TD-008 관찰). 단위 테스트가 성공 / 실패 / 엣지를 다룸 | S01-T06 | done | 0739674, d4fb085, 7e76050, 1e04b66 |
| S02-T02 | 데코레이터 동작: 로깅 · 검증(FluentValidation) · 트랜잭션 | FR-05 | 데코레이터별 동작을 단위 테스트(전체 순서는 T03). 검증 실패면 inner 미호출 · 실패 로그. 트랜잭션 데코레이터는 Handler 성공 Result일 때만 `IUnitOfWork.CommitAsync`를 부르고, 실패 Result는 커밋 없이 전달, `CommitAsync`의 실패 Result도 그대로 전달(ADR-0015). 데코레이터는 실행 전략 · 트랜잭션 · SaveChanges를 다루지 않음(T07). CustomState에 Error가 없는 실패는 `Error.Validation`(1001)로 감쌈(BL-053), `WithError` 확장. 로그: 메시지 템플릿, 성공 Debug · 실패 Result Information · 예외는 기록하지 않음, 이벤트 ID 하위 범위(1 전역 예외, 101~199 Mediator, 201~299 영속성, 301~399 API)를 error-codes에 기록(BL-028). `Microsoft.Extensions.Diagnostics.Testing`(FakeLogger) 추가 · package-versions 기록, FakeLogger로 개인정보 없음 단언 | T01 | done | 0417789, b3a0039, 1385b4c, 2b2e6b5 |
| S02-T03 | 규칙 기반 DI 등록(Scrutor), UUIDNext 기반 `IIdGenerator`, `TimeProvider` 등록, 파이프라인 순서 | FR-06, FR-05 | Application public 진입점 `AddBuildingBlocksApplication()`(데코레이터 open generic 목록 안쪽 → 바깥, `ISender` Scoped. InternalsVisibleTo 확대 금지). `AddConventionalServices`가 Scrutor `Scan`(internal 포함, Scoped, `RegistrationStrategy.Throw`)으로 마커 구현 타입 등록, Handler는 두 인자 형태만, `TryDecorate`(ADR-0017), Validator는 FluentValidation 검색. `TimeProvider.System` Singleton. `ValidateOnBuild` · `ValidateScopes`에서 모두 해석. 해석된 Command 체인이 로깅 → 검증 → 트랜잭션 → Handler, Query 체인에는 트랜잭션 없음. 중복 등록 시 시작 실패. UUID v7 버전 · variant 비트, 문자열(빅 엔디언) 기준 생성 순서 정렬, 대량 생성에서 같은 밀리초 쌍이 실제로 있었음을 단언한 뒤 단조 증가 확인. `IIdGenerator` 등록 방식은 ADR-0013 원문 확인 후 명시 | T01, T02 | done | 98a9b11, 1a7c5ad, 9764055, f88e79d, 8064223, e8c151d |
| S02-T04 | EF Core 공통 모델 규칙: 명명 · 변환 · 감사 · 동시성, Repository 기반, 읽기 전용 DbContext | FR-06 | snake_case, `ux_`(`HasDatabaseName` 덮어쓰기, 이름 상수는 T07 매핑과 공유) · `ck_<table>_<rule>`(enum IN 목록, `[Flags]`는 마스크 조건, 컬럼 이름은 메타데이터로 해석) 도우미. Domain에 `IStronglyTypedId<TSelf>` 추가, `ConfigureConventions`로 강타입 ID 변환 공통 등록 + `ValueGeneratedNever`, `DomainEvents` Ignore 공통 처리(TD-015). 감사 shadow property(루트 엔티티만, owned 변경 시 소유자 `updated_at` 갱신) + `SaveChangesInterceptor`(`TimeProvider`, +09:00에서도 UTC 저장, DB 없이 테스트 가능한 구조). `xmin`은 **EF shadow property**(`IsRowVersion`, xid, 컬럼 이름 `xmin` 확인)로 매핑하고 database.md 190행 수정. `RepositoryBase` / `ReadRepositoryBase`(람다 LINQ만), 읽기 전용 DbContext 기반(NoTracking, SaveChanges 오버로드 전부 예외). `IDesignTimeModel` 기준 메타데이터 단위 테스트(이름 · 제약 · 동시성 토큰 · ValueGenerated · DomainEvents 미매핑) | T01, T03 | done | d51d302, 3fe392a, 4e7624e, a35094f, bdc4d62 |
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
| 2026-09-27 | S02-T08 | tester | PASS | 명령 기반 점검 9개 통과: check-docs 81개 · 결함 2건(기준선), ADR 템플릿 4절 · wikilink 0, 목록 행, 0001~0023 불변, 완료 조건 7항목, PRD FR-09 비고 · 변경 이력, FR-07 구성 요소 배치 대조. handoff: T05 타입 의존 기준 단언, T06 tester 확인 항목 |
| 2026-09-27 | S02-T01 | dba | PASS | 해당 없음(DB 변경 없음). CommitAsync → Task<Result>가 ADR-0014와 맞음. 주의: IUnitOfWork에 SaveChanges · BeginTransaction 공개 금지, 분류 포트는 System.Exception만, 실패 Result는 롤백 완료 전제를 XML 주석에. handoff: T02 실패 Result 전달, T07 UoW 순서(원문 T04 → T07로 정정) |
| 2026-09-27 | S02-T01 | developer | PASS | Red(컴파일 오류 34) → Green. 계약(Unit, ICommand · IQuery · Handler, ISender, IUnitOfWork.CommitAsync → Task<Result>, IReadRepository, IService, IIdGenerator, IExceptionClassifier.Classify(Exception) → Error?), internal sealed Sender + RequestInvokerCache(키 = 요청 · 응답 형식, Lazy). 테스트 51건(캐시 판정 ByCount · ByReference 등, 동시 64건, 누락 예외, 토큰 전달), Application 커버리지 line · branch 100%. Abstractions 10.0.0 고정(8.0.2는 Scrutor 7로 NU1109). build 경고 0, test 253 통과, format 통과. 후보 BL-065(IService 마커 위치). handoff: T02 · T03 · T05 · T06 · T07 |
| 2026-09-27 | S02-T01 | reviewer | PASS | 진입 점검 15개 통과(build 경고 0, test 253, format, 레이어 의존, dba 조건). 경고 억제 승인 목록: CA1812 2건(CommandInvoker{TCommand,TResponse}.cs:12, QueryInvoker{TQuery,TResponse}.cs:12, [SuppressMessage] + Justification, Activator 생성), 전역 NoWarn · pragma 없음. BL-065 유지. handoff: T03 Sender 생성자 해석 · 비마커 제외 확인, T05 수락 테스트 중복 정리 |
| 2026-09-27 | S02-T01 | tester | PASS | 인수 조건 대조, 인수 테스트 7건 추가(DispatcherAcceptanceTests: 인터페이스 변수 → 런타임 형식 캐시, 한 인자 Handler, Unit 실패 전달, 다른 응답 형식만 등록 시 누락 예외, Query 취소 토큰, Query 동시 64건, 형식 혼합 동시 첫 요청 48건). build 경고 0, test 260 통과, format 통과, 새 테스트 5회 반복 통과. handoff: T03 실제 ServiceCollection 확인 항목, T05 중복 기준 |
| 2026-09-27 | S02-T02 | dba | PASS | IUnitOfWork 계약(CommitAsync → Task<Result>, 실패 = 롤백 완료, 23514 · 재시도 한도는 예외)이 ADR-0014 · 0015와 일치, 변경 없음. handoff → developer / tester: 트랜잭션 데코레이터 조건 11개(Command만, Handler 1회, 실패 Result면 커밋 0회, 성공만 커밋 1회 · 토큰 전달, 커밋 실패 Error 그대로, Handler · 커밋 예외 미포착, IUnitOfWork만 의존, 로그 값 미기록). T07 롤백 · AcceptAllChanges · ClearDomainEvents 순서 |
| 2026-09-27 | S02-T02 | developer | PASS | Red(컴파일 오류 22) → Green. internal sealed 데코레이터 5개(Logging Command · Query, Validation Command · Query, Transaction Command), dba 조건 11개 충족. MediatorLogs 101~104([LoggerMessage], 성공 Debug · 실패 Information, 예외 미기록). BL-053 1001 감싸기, WithError 확장, 공통 기반 RequestValidator<TRequest>(RuleLevelCascadeMode = Stop, ADR-0018이 이 작업에 넘긴 설정 위치). 비검증 유형 Error를 CustomState에 넣으면 ArgumentException. FakeLogger(Diagnostics.Testing 10.10.0) 추가, 개인정보 없음 단언. error-codes 이벤트 ID 하위 범위(BL-028) · package-versions · coding-conventions 갱신. CA1812 억제 5건. test 363 통과(+103), build 경고 0, format 통과, check-docs 기준선. 후보 BL-066 · TD-019. handoff: T03 · T05 · T06 · T07 |
| 2026-09-27 | S02-T02 | reviewer | PASS | 진입 점검 15개 통과(-warnaserror 빌드, test 363, format, 레이어 의존, 로그 템플릿 · 값 미기록, dba 조건 11개). 경고 억제 승인: CA1812 5건(Logging Command:17 · Query:14, Validation Command:18 · Query:17, Transaction:23), 새 NoWarn · pragma 없음, 누적 CA1812 7건. RequestValidator는 ADR-0018:44가 이 작업에 맡긴 범위 안, AbstractValidator 상속이라 충돌 없음(문구 정리는 BL-066). handoff: T03 · T05 |
| 2026-09-27 | S02-T02 | tester | PASS | dba 조건 · 권장 시나리오는 developer 테스트로 충족 확인. 수동 조립 체인 인수 테스트 11건 추가(DecoratorChainAcceptanceTests: Received.InOrder 순서, 경과 시간에 커밋 포함, 검증 · Handler · 커밋 실패 각각 102 1건 · 커밋 0회, 예외 시 로그 0건, 토큰, 동시 32건, Query 104 · BL-053). -warnaserror 빌드 경고 0, test 374 통과, format 통과, check-docs 기준선 2건. handoff: T03 DI로 순서 재확인해야 FR-05 순서 조건 닫힘 |
| 2026-09-27 | S02-T03 | dba | PASS | 해당 없음(DB 변경 없음). handoff → developer: NewDatabaseFriendly(Database.PostgreSql)만, 정렬 단언은 ToString("D") Ordinal 또는 ToByteArray(bigEndian: true)(Guid.CompareTo 금지), 버전 7 · variant 8/9/a/b, 타임스탬프는 허용 오차, 1만 건 연속 생성 단조성, 병렬은 중복만, DB 검증은 S03-T05. handoff → S03-T05: ORDER BY id 정렬 · uuid 타입 · 기본값 없음 |
| 2026-09-27 | S02 | 운영 변경 | 승인 | T04부터 적용(사용자 결정): (1) 작업별 파이프라인 표에서 dba가 '해당 없음'인 작업은 dba를 호출하지 않고 진행 기록 한 줄만 남겨 developer 단계 커밋에 포함 (2) reviewer PASS는 따로 커밋하지 않고 tester 단계 커밋에 포함(REJECT · BLOCKED는 즉시 커밋). 작업당 커밋 5 → 3(developer, tester, 완료). developer 테스트 범위를 완료 조건 중심으로 제한할지는 S02 회고에서 결정 |
| 2026-09-27 | S02-T03 | developer | PASS | Red → Green(첫 실행 5건 실패: Scrutor 7 Decorate의 keyed 안쪽 등록 → 판정 수정). Application: AddBuildingBlocksApplication(ISender Scoped, TimeProvider.System Singleton), public PipelineDecorators(안쪽 → 바깥), CA1812 Justification 등록 경로로 갱신. Infrastructure 프로젝트 신설(ASP.NET 비참조): AddConventionalServices(두 번째 호출 예외, 서비스 인터페이스 1개 선검사, Scan internal 포함 Scoped · Throw, Handler 두 인자 닫힌 형식만, TryDecorate, Validator 검색) · AddBuildingBlocksInfrastructure. IIdGenerator는 ADR-0017 따라 명시 등록(Scoped), UuidV7IdGenerator(버전 · variant · 타임스탬프, 1만 건 같은 밀리초 쌍 선단언 후 단조성, 병렬 4만 건 중복 없음). PipelineOrderAcceptanceTests로 실제 DI에서 FR-05 순서 확인. DI 10.0.0 테스트 고정. test 469 통과(+95), build 경고 0, format, check-docs 기준선. 후보 BL-067 · BL-068 · TD-020 |
| 2026-09-27 | S02-T03 | reviewer | REJECT → developer | 점검 6 위반(coding-conventions:51 파일 하나에 최상위 타입 하나): Infrastructure.UnitTests/Samples의 MarkerSamples.cs(10) · RequestSamples.cs(11) · PortSamples.cs(3) · ViolationSamples.cs(3). 나머지 14개 통과(-warnaserror 빌드, test 469, ADR-0013 대 0017 해석 타당). T02 SampleValidators.cs(2개 형식)는 T02 리뷰 누락, 함께 분리 권장. 후보 BL-069(테스트 Samples 파일 규칙) |
| 2026-09-27 | S02-T03 | developer | PASS | 재작업(반려 1): Infrastructure.UnitTests Samples 4개 파일 → 형식별 27개, Application.UnitTests SampleValidators.cs → 2개(권장 항목). 설명 주석은 XML remarks로, 동작 · 테스트 불변. CA1812 새 위치: InternalSampleService.cs:11, CreateSampleCommandValidator.cs:9. build 경고 0, test 469 통과, format 통과, 최상위 형식 2개 이상 파일 0 |
| 2026-09-27 | S02-T03 | reviewer | PASS | 재판정: 반려 사유 해결(src · tests 전체에 최상위 형식 2개 이상 파일 0), -warnaserror 빌드 경고 0, test 469, format 통과. 경고 억제 승인: 테스트 CA1812 2건(InternalSampleService.cs:10, CreateSampleCommandValidator.cs:8 — 특성 시작 줄, 앞 행의 :11 · :9 정정), 누적 CA1812 제품 7 + 테스트 2 |
| 2026-09-27 | S02-T03 | tester | PASS | 인계 · 계획 리뷰 항목 대조(PipelineOrderAcceptanceTests 등 기존 테스트로 충족). 보강 9건: ServiceCompositionAcceptanceTests(전 등록 Scoped — Singleton은 TimeProvider · 표식만, 엄격 스코프 전수 해석, 루트 해석 예외, 등록 순서 뒤바꿈, 스코프 간 UUID 순서), ConcurrentPipelineAcceptanceTests(64 스코프 동시 Send, 결과 혼합, 동시 Query IUnitOfWork 미해석, 5회 반복 안정). -warnaserror 경고 0, test 478 통과, format 통과. handoff: T07 조합 테스트 갱신, T04 샘플 의존 분리 |
| 2026-09-27 | S02-T04 | dba | PASS | database.md: xmin 190행 정정(shadow property Property<uint>.IsRowVersion + HasColumnName("xmin") + xid 명시, Npgsql DLL로 IsSystemColumn 확인), 'EF Core 공통 모델 규칙' 절(규칙 8개) · '감사 컬럼' 절(상태별 표, 상수 공개, 재시도 재계산 허용), [Flags] ck_ 마스크 조건, 식별자 63바이트 · ux_ 덮어쓰기 · 이름 상수 규칙. check-docs 기준선. handoff → developer: 구현 규칙 9개 + 메타데이터 단언 A1~A13 + 동작 단언, T07 · S03-T02 · S03-T05. 후보 BL-070 |
| 2026-09-27 | S02-T04 | developer | PASS | TDD. Domain IStronglyTypedId<TSelf>. Infrastructure.Persistence: Write/ReadDbContextBase(단일 진입점 CommonModelConventions, 같은 IDbModelDefinition), IgnoreAny<IDomainEvent>, 강타입 ID 변환기(CS8927 회피) + 키 ValueGeneratedNever, 감사 shadow · xmin(uint, xid), ck_ 코드 · Flags(모델 확정 규칙에서 최종 이름으로 생성, 63바이트 검사), UniqueIndexName record + HasUniqueIndex, AuditSaveChangesInterceptor(중첩 owned → 소유자), RepositoryBase · ReadRepositoryBase(NoTracking, SaveChanges 4개 차단). 메타데이터 A1~A13 · 동작 단언 전부 테스트. 실측: FakeTimeProvider +09:00 → ToUniversalTime 정규화 추가, EF 8 기본 동작 때문에 방어 규칙 판별용 샘플 · 테스트 추가(규칙 제거 시 실패 확인). CA1812 억제 2건. test 600 통과(+122), build 경고 0, format, check-docs 기준선. coding-conventions · package-versions 갱신. 후보 BL-071 · BL-072 |
| 2026-09-27 | S02-T04 | reviewer | REJECT → developer | 점검 7 위반(coding-conventions:78 enum 기반 형식 명시): 테스트 샘플 Samples/Persistence/DeliveryChannels.cs:5 · NoBitChannels.cs:5 · SignBitChannels.cs:5 · IntBackedStatus.cs:4 · PlainIntCode.cs:4에 `: int` 누락. 나머지 14개 통과(-warnaserror 빌드, test 600, dba 단언 A1~A13 · 동작 단언 전부, database.md · coding-conventions와 구현 일치) |
| 2026-09-27 | S02-T04 | developer | PASS | 재작업(반려 1): 테스트 샘플 enum 5개에 `: int` 명시. src · tests 전체 enum 기반 형식 누락 0. -warnaserror 빌드 경고 0, test 600 통과, format 통과 |
| 2026-09-27 | S02-T04 | reviewer | PASS | 재판정: 반려 사유 해결(기반 형식 없는 enum 0), -warnaserror 경고 0, test 600, format 통과. 경고 억제 승인: CA1812 StronglyTypedIdValueConverter.cs:16(EF 리플렉션 생성), AuditSaveChangesInterceptor.cs:18(DI 생성, T07에서 제거 예정). 누적 CA1812 제품 9 + 테스트 2 |
| 2026-09-27 | S02-T04 | tester | PASS | 계획 리뷰 tester 항목은 기존 테스트로 충족. 인수 테스트 22건 보강(CommonModelRulesAcceptanceTests + Dispatch 샘플): 이름 덮어쓰기 · owned 속성 ck_ 최종 이름, ADR-0008 저장 형식(smallint · integer · bigint, 문자열 변환기 없음), 모델 등록 변환기 왕복(Guid.Empty 포함), 두 번째 Aggregate 공통 뒤처리 · 읽기/쓰기 스크립트 동일 · 저장 차단, +09:00 → UTC, owned만 변경 시 소유자만 갱신. 제품 결함 없음. -warnaserror 경고 0, test 622 통과, format 통과. handoff: T07 등록 확장으로 조립 교체 · 레지스트리 키 단언, S03-T05 실측 항목 |
| 2026-09-27 | S02-T07 | dba | PASS | database.md: 공통 DbContext 등록 규칙, UnitOfWork 커밋 순서 · 재시도 때 상태(accept false), 영속성 예외 변환 규칙표 1~9, 23505 매핑 레지스트리 계약(UniqueIndexName 키, Conflict만, 중복 키 예외, Ordinal 조회), 변환 로그 필드 · 수준(201 Debug · 202 Warning · 203 Debug). 발견: AcceptAllChanges가 Deleted를 Detached로 빼므로 IHasDomainEvents 대상은 그 전에 수집. 변환은 ExecuteAsync 바깥에서. 3003 · 201~203 error-codes 행은 developer가 코드와 같은 커밋에. 후보: EF 23505 Error 로그는 BL-023에 해당(새 행 없음), BL-073 · BL-074. check-docs 기준선 |
| 2026-09-27 | S02-T07 | developer | PASS | TDD. Domain IHasDomainEvents(AggregateRoot 구현), CommonErrors 3003 UniqueConstraintViolated(CommonErrorsTests · error-codes 같은 커밋). UseBuildingBlocksNpgsql(옵션 한 경로, 샘플 직접 조립 교체), AddWriteDbContext(감사 인터셉터 Singleton) · AddReadDbContext · AddUnitOfWork(레지스트리 Map: Conflict만 · 중복 키 예외, FrozenDictionary Ordinal). PersistenceExceptionTranslator(3001 · 23505 매핑/3003 · 미변환 규칙), UnitOfWork<TContext>(전략 안 ReadCommitted → SaveChanges(false) → IPreCommitHook(멱등) → Commit, 전략 바깥 변환, 이벤트 대상 선수집 → AcceptAllChanges → Clear), PersistenceLogs 201 Debug · 202 Warning · 203 Debug(예외 객체 미전달, Detail 미기록 단언), PersistenceExceptionClassifier(RetryLimitExceeded → 9003, TryAddEnumerable). FakeDatabase 인터셉터 대역으로 실제 Npgsql 재시도 전략 경로 검증(InMemory 미사용). AuditSaveChangesInterceptor CA1812 억제 제거, 테스트 EF1001 억제 1건. ServiceCompositionAcceptanceTests 실제 등록으로 교체. test 744 통과(+122), build 경고 0, format, check-docs 기준선. accept false 실제 효과는 S03-T05 인계 |
| 2026-09-27 | S02-T07 | reviewer | PASS | 진입 점검 15개 통과(-warnaserror 경고 0, test 744, format, database.md 규칙표 · 레지스트리 계약 · 로그 필드와 구현 일치, dba 조건 7개, ADR-0014 · 0015 · 0020 · 0024 정합). 경고 억제 승인: 테스트 EF1001 1건(SamplePostgresExceptions.cs:40-43, Justification), CA1812 AuditSaveChangesInterceptor 제거 확인, 새 제품 억제 0. 누적 CA1812 제품 8 + 테스트 2. 제안: UnitOfWork{TContext}.cs:86-97 Log switch에 모든 PersistenceFailureKind 대응 테스트 |
| 2026-09-27 | S02-T07 | tester | PASS | 규칙표 1~9를 실제 UnitOfWork 경로(실제 Npgsql 재시도 전략 + FakeDatabase)로 인수 테스트 28건 보강(PersistenceExceptionRulesAcceptanceTests): 변환 행 코드 · 로그 · 재시도 · 커밋 없음, 미변환 행 같은 인스턴스 재전파 · 로그 0 · 상태 유지, COMMIT 시점 23505, 201~203 세 경로 비밀 값 · 예외 객체 미기록, Save · COMMIT 일시 오류 재시도 때 엔트리 상태 동일, 실제 재시도 전략 형식, PersistenceFailureKind 값 고정. reviewer 대조 4항목 확인. 제품 결함 없음. -warnaserror 경고 0, test 772 통과, format, check-docs 기준선. handoff: T06 409 응답 인수 테스트, S03-T05 실측 6항목 |

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
