---
title: "코딩 컨벤션"
type: doc
status: draft
tags: [development]
created: 2026-09-27
updated: 2026-09-28
---

# 코딩 컨벤션

> C# 12 / .NET 8 코드 작성 규칙입니다. **developer 에이전트는 이 문서를 반드시 준수**하고, reviewer 에이전트는 이 문서를 기준으로 판정합니다(위반은 반려 사유).
> 솔루션 구조와 레이어 의존 규칙은 [Clean Architecture](../03-architecture/clean-architecture.md), DB 규칙은 [데이터베이스](database.md)를 따릅니다.
>
> [위키 홈](../README.md)

## 기본 원칙

- 컴파일러와 분석기가 잡을 수 있는 것은 규칙 문서가 아니라 **빌드 설정으로 강제**한다(`Nullable`, `TreatWarningsAsErrors`, `.editorconfig`, 아키텍처 테스트).
- 식별자는 영어로, 주석과 문서는 한국어로 쓴다.
- **코드값은 문자열로 다루지 않는다.** 상태, 유형, 권한 같은 코드는 명시적 정수 값을 가진 `enum`으로 정의한다([코드값 규칙](#코드값-enum-규칙)).

## 네이밍 규칙

| 대상 | 규칙 | 예 |
|---|---|---|
| 클래스, record, struct, enum, 메서드, 속성, 이벤트 | PascalCase | `EmployeeStatus`, `RegisterAsync` |
| 인터페이스 | `I` + PascalCase | `IEmployeeRepository` |
| 지역 변수, 매개변수 | camelCase | `employeeId` |
| private 필드 | `_camelCase` | `_repository` |
| 상수, static readonly | PascalCase | `MaxNameLength` |
| 비동기 메서드 | `Async` 접미사 | `GetByIdAsync` |
| 제네릭 타입 매개변수 | `T` 또는 `T` + 설명 | `TResponse` |
| `[Flags]` enum | 복수형 | `Permissions`, `NotificationChannels` |
| 일반 enum | 단수형 | `EmployeeStatus` |

CQRS 타입 이름

| 대상 | 규칙 | 예 |
|---|---|---|
| Command | 동사 + 대상 + `Command` | `RegisterEmployeeCommand` |
| Query | `Get` / `Search` + 대상 + `Query` | `GetEmployeeByIdQuery` |
| Handler | 요청 이름 + `Handler` | `RegisterEmployeeCommandHandler` |
| Validator | 요청 이름 + `Validator` | `RegisterEmployeeCommandValidator` |
| 응답 DTO | 대상 + `Response` / `Dto` | `EmployeeResponse` |
| 도메인 이벤트 | 과거형 + `DomainEvent` | `EmployeeRegisteredDomainEvent` |
| 통합 이벤트 | 과거형 + `IntegrationEvent` | `EmployeeRegisteredIntegrationEvent` |

## 파일 및 네임스페이스 규칙

- **파일 하나에 최상위 타입 하나**, 파일 이름 = 타입 이름. **테스트 코드(`tests/**`)에도 예외 없이 적용한다**: 테스트 클래스, `Samples/`의 샘플 형식, 테스트 대역(fake · stub), fixture, 테스트 전용 `record`도 형식마다 파일을 나눈다. 시나리오 묶음 파일 같은 예외는 두지 않는다(BL-069).
- **file-scoped namespace**를 쓴다: `namespace EmergencyHub.Employee.Domain.Employees;`
- 네임스페이스 = 프로젝트 루트 네임스페이스 + 폴더 경로.
- 공통 `using`은 프로젝트별 `GlobalUsings.cs`에 둔다. `ImplicitUsings`를 켠다.

## C# 언어 기능 (C# 12)

| 규칙 | 설명 |
|---|---|
| Nullable 참조 형식 | 전 프로젝트 `enable`. `!`(null-forgiving)는 테스트 외에는 쓰지 않는다. |
| 클래스는 기본 `sealed` | 상속을 의도한 타입만 `sealed`를 뺀다(`abstract` 기반 클래스 등). 아키텍처 테스트가 검사하는 예외 목록과 검사 범위의 원본은 [테스트 전략 · 아키텍처 테스트](testing-strategy.md#아키텍처-테스트)(`ClassesAreSealed` 범위)다. `Error`(non-sealed `record`, `ValidationError` · `ConflictError`가 파생)는 값을 받는 생성자가 `private protected`라 어셈블리 밖에서는 유형별 팩토리로만 새 값을 만든다. 다만 non-sealed `record`의 컴파일러 생성 복사 생성자는 `protected`여야 하므로(더 좁히면 CS8878) **어셈블리 밖에서도 복사 생성자로 파생할 수 있다**. 이때도 이미 검증된 인스턴스에서 복사하고 `init` 접근자가 없어 코드 · 메시지 · 유형 불변식은 유지된다(`Error.cs` remarks와 같은 내용, TD-016). |
| `record` | **데이터를 담는 모델 클래스는 모두 `record`로 만든다**: DTO, API Request / Response, Command, Query, 조회 모델(Read Model), 도메인 / 통합 이벤트, Value Object. 위치 기반 생성자(positional record)를 기본으로 하고 불변으로 둔다. 모델에 `class`를 쓰면 반려 사유다. |
| `record struct` | 강타입 ID에 쓴다: `public readonly record struct EmployeeId(Guid Value);` |
| primary constructor | DI를 받는 서비스와 Handler에 쓴다. Entity / Aggregate에는 쓰지 않는다(불변식 검증이 필요하므로 팩토리 메서드 사용). |
| `required` | DTO / 옵션 클래스의 필수 속성에 쓴다. |
| 컬렉션 식 | `[]`, `[.. items]`를 쓴다. |
| 패턴 매칭 | `is null`, `is not null`, switch 식을 쓴다. `== null`은 쓰지 않는다. |
| `var` | 우변에서 타입이 드러날 때만 쓴다. |
| 식 본문 멤버 | 한 줄로 끝나는 멤버에 쓴다. |
| 컬렉션 노출 | 외부에는 `IReadOnlyCollection<T>` / `IReadOnlyList<T>`로 노출한다. |
| 시간 | `DateTime.UtcNow` 직접 호출 금지. `TimeProvider`를 주입받는다(테스트 가능성). |

**생성 코드의 sealed (`*.Sealed.cs`)**: EF Core 도구가 만드는 마이그레이션 · 모델 스냅샷 클래스는 `sealed`가 아니지만 `ClassesAreSealed`의 대상이다(가시성과 관계없음, [테스트 전략 · ClassesAreSealed 범위](testing-strategy.md#아키텍처-테스트)). 생성 파일은 고치지 않고, 같은 폴더에 직접 작성한 partial 선언 파일로 `sealed`를 붙인다(절차 원본: [데이터베이스 · 마이그레이션 규칙](database.md#마이그레이션-규칙)).

- 파일 이름: 마이그레이션마다 `<마이그레이션 ID>.Sealed.cs`, 서비스마다 `<DbContext>ModelSnapshot.Sealed.cs`. 선언의 가시성은 생성 파일과 같게 둔다(마이그레이션 `public`, 스냅샷 `internal`).
- 새 마이그레이션 · ADR-0012 리셋으로 ID가 바뀌면 같은 작업에서 선언 파일을 추가하거나 이름을 맞춘다. 누락은 `ClassesAreSealed_ProductAssemblies_Holds` 실패로 드러난다.
- 이 파일은 `.editorconfig`의 `[**/Persistence/Migrations/*.Sealed.cs]` 섹션이 생성 코드에서 빼므로(`generated_code = false`, CS1591 warning) XML 문서 주석을 단다.
- 생성 형식을 규칙 예외로 두는 기준은 정하지 않았다(BL-090). 정하기 전까지 예외 목록에 넣지 않고 이 선언 파일로 지킨다.

실제 코드(`src/Services/Employee/EmergencyHub.Employee.Infrastructure/Persistence/Migrations/`의 두 파일에서 namespace · XML 문서 주석을 빼고, 파일 이름을 `//` 주석으로 앞에 붙인 것. 남은 선언 줄은 소스와 같다):

```csharp
// 20260928090646_InitialCreate.Sealed.cs
public sealed partial class InitialCreate;

// EmployeeDbContextModelSnapshot.Sealed.cs
internal sealed partial class EmployeeDbContextModelSnapshot;
```

## 코드값 (enum) 규칙

DB의 코드값 규칙([데이터베이스 · 코드값](database.md#코드값-규칙))과 한 쌍입니다.

- 코드는 `enum`으로 정의하고 **모든 멤버에 값을 명시**한다. 한 번 배포된 값은 바꾸거나 재사용하지 않는다.
- 기반 형식을 명시한다. 일반 코드는 `short`, 비트 플래그는 `int`(31개까지) 또는 `long`(63개까지).
  - **테스트 코드(`tests/**`)의 enum에도 적용한다.** 테스트 샘플 · 대역 enum도 기반 형식을 적고, 기본값과 같은 `: int`도 생략하지 않는다. 일부러 규칙 밖 형식을 쓰는 샘플(예: `Samples/Persistence/IntBackedStatus.cs`의 `public enum IntBackedStatus : int`, 도우미의 형식 검사를 확인하는 용도)도 그 형식을 명시한다. 아키텍처 규칙 `CodeEnumsUseConventionalUnderlyingTypes`는 제품 어셈블리만 보고, `: int` 생략은 메타데이터로 구별되지 않아 reviewer가 판정한다(S02-T04 반려, BL-119).
  - 예외는 이 규칙의 위반을 재현하는 아키텍처 테스트 표본 하나다: `tests/EmergencyHub.ArchitectureTests/Samples/EnumTypes/ImplicitIntStatus.cs`(XML 주석 "위반 예: 기반 형식을 빠뜨린 일반 코드"). 새 위반 표본을 만들 때도 XML 주석에 "위반 예"를 적는다.
  - 점검(Git Bash): `git ls-files src tests | grep '\.cs$' | xargs grep -nE "^\s*(public |internal |private )?enum [A-Za-z0-9_]+\s*$"`. 2026-09-28 현재 출력은 위 표본 1줄뿐이다.
- `0`은 `None` / `Unknown` 용도로 예약한다. 유효한 업무 값으로 쓰지 않는다.
  - 예외: 외부 규약이 `0`의 의미를 정해 둔 **`internal` enum**은 그 규약을 따른다. 예: 프로세스 종료 코드 `MigrationExitCode`(`Succeeded = 0`, 운영체제 · AppHost `WaitForCompletion`이 0을 성공으로 봄). 기반 형식 명시 · 모든 멤버 값 명시 규칙은 그대로 적용한다(BL-091).
- API 요청 / 응답과 이벤트에서도 코드는 **정수로 직렬화**한다. `JsonStringEnumConverter`는 쓰지 않는다.
- 외부에서 들어온 정수는 `Enum.IsDefined`(일반 코드) 또는 정의된 비트 마스크 범위 검사(플래그)로 검증한다. 요청 검증은 Validator의 `MustBeDefinedEnum()`(1002)으로 한다.

```csharp
public enum EmployeeStatus : short
{
    Unknown = 0,
    Active = 1,
    OnLeave = 2,
    Retired = 3,
}
```

### 비트 마스킹 (조합 코드)

권한, 알림 채널처럼 **여러 값을 조합**해야 하는 코드는 `[Flags]` enum으로 정의하고 정수 컬럼 하나에 저장한다.

```csharp
[Flags]
public enum NotificationChannels : int
{
    None  = 0,
    Sms   = 1 << 0,  // 1
    Push  = 1 << 1,  // 2
    Email = 1 << 2,  // 4

    All = Sms | Push | Email,  // 조합 별칭은 코드에서만 정의하고 저장 값으로 따로 두지 않는다
}

// 사용
var channels = NotificationChannels.Sms | NotificationChannels.Push;
bool usesSms = (channels & NotificationChannels.Sms) != 0;   // 포함 여부
channels &= ~NotificationChannels.Push;                       // 제거
```

- 값은 `1 << n`으로 적는다. **비트 자리는 재사용하지 않는다.** 폐기한 플래그는 `[Obsolete]`로 남기고 자리를 비워 둔다.
- 포함 여부는 비트 연산으로 확인한다. 핫패스에서 박싱이 생길 수 있는 `HasFlag` 대신 `(value & flag) != 0`을 쓴다.
- 도메인 모델에서 조합 규칙(예: 최소 채널 하나)은 Aggregate / Value Object가 검증한다.

## CQRS 규칙

Application 레이어는 **Command(상태 변경)와 Query(조회)를 분리**합니다.

| 구분 | Command | Query |
|---|---|---|
| 목적 | 상태 변경 | 데이터 조회 |
| 반환 | `Result<Unit>`(`ICommand`, 반환 값 없음) 또는 `Result<TId>`(`ICommand<TId>`, 생성한 ID 정도만) | `Result<TResponse>` (DTO) |
| 부작용 | 있음 | **없음** |
| 도메인 모델 | Repository로 Aggregate를 불러와 도메인 메서드 호출 | 도메인 모델을 거치지 않는다. **Read Repository**가 읽기 전용 DbContext에서 DTO로 바로 프로젝션한다 |
| DB 연결 | 쓰기 DbContext (`ConnectionStrings:Write`) | 읽기 전용 DbContext (`ConnectionStrings:Read`) |
| 트랜잭션 | Command 하나 = 트랜잭션 하나 = Aggregate 하나(예외: `RegisterEmployeesCommand`는 Employee 최대 1,000개를 한 트랜잭션에, [ADR-0026](../03-architecture/adr/0026-employee-bulk-import-input-processing.md) 9절). 저장 · 커밋은 트랜잭션 데코레이터 → `IUnitOfWork`가 한다(Handler는 저장하지 않음) | 없음 |
| 검증 | FluentValidation Validator (파이프라인에서 자동 실행). 예외: `RegisterEmployeesCommand`는 행 검증을 Handler가 하고 `ValidationError`를 만든다([ADR-0026](../03-architecture/adr/0026-employee-bulk-import-input-processing.md) 7절, 이 Handler에만) | 필요 시 Validator |

- Handler 하나는 요청 하나만 처리한다.
- Command Handler는 다른 Command를 직접 호출하지 않는다. Handler는 `ISender`를 주입받지 않는다(중첩 `SendAsync`는 커밋이 두 번 일어남). 후속 처리는 도메인 이벤트 / 통합 이벤트로 연결한다.
- 공통 관심사(로깅, 검증, 트랜잭션)는 **Handler 데코레이터 파이프라인**으로 처리한다. 순서는 Command가 로깅 → 검증 → 트랜잭션 → Handler, Query가 로깅 → 검증 → Handler다(트랜잭션은 Command에만).
- Validator는 `internal sealed class <요청>Validator : RequestValidator<요청>`(BuildingBlocks.Application.Validation)으로 만든다. 공통 기반의 동작 설명 원본은 [테스트 전략 · 단위 테스트](testing-strategy.md#단위-테스트-domain--application)의 "Validator 기반"이다([ADR-0018](../03-architecture/adr/0018-use-fluentvalidation.md)). 규칙마다 `WithError(Error)`로 정수 코드를 붙이고(검증 실패 유형만), 붙이지 않은 규칙의 실패는 1001로 담긴다. 코드값(enum) 속성은 `MustBeDefinedEnum()`으로 정의되지 않은 값을 1002로 거부한다(일반 enum은 0도 거부, `[Flags]`는 정의된 비트의 조합과 0 허용). FluentValidation의 문자열 `ErrorCode`는 쓰지 않는다.
- **Handler와 Repository는 `SaveChanges`를 부르지 않는다.** Handler가 성공 `Result`를 돌려주면 트랜잭션 데코레이터가 `IUnitOfWork.CommitAsync`를 부르고, 실패 `Result`면 저장하지 않는다. Handler는 DbContext를 받지 않는다([ADR-0014](../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md)).
- Application 코드는 BuildingBlocks의 추상화(`ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `ISender`)에만 의존한다. 반환 값 없는 Command는 `ICommand : ICommand<Unit>`이고 `Result<Unit>`을 돌려준다.

> **Mediator는 직접 구현한다**([ADR-0015](../03-architecture/adr/0015-custom-mediator-pipeline.md)). MediatR는 v13부터 상용 라이선스라 쓰지 않는다. 디스패처(`ISender`)는 Handler 타입을 캐시하고, 등록되지 않은 Handler는 `InvalidOperationException`을 던진다.

기능 폴더 구조 (Application)

- 규칙: 기능마다 `Employees/Commands/<기능>/`에 Command · Handler · Validator를, `Employees/Queries/<기능>/`에 Query · Handler · 응답 `record`를 한 폴더로 둔다. 기능 폴더 밖에는 여러 기능이 함께 쓰는 로그 정의 · Read Repository 인터페이스 · Read Repository가 돌려주는 프로젝션 `record`(여러 Query가 함께 쓰는 것)만 둔다. Query 하나만 쓰는 프로젝션은 그 기능 폴더 안에 둔다.
- 현재 구성(S05-T06, PRD-001 샘플 Command · Query 제거 뒤): 기능 폴더가 없다. Read Repository와 그 프로젝션 `record`는 S05-T06에서 생겼고, 일괄 등록(`Commands/RegisterEmployees/`)은 S06-T04, 목록 · 이름 조회 Query는 S07에서 생긴다.

```
EmergencyHub.Employee.Application/
├── EmployeeApplicationAssembly.cs
└── Employees/
    ├── EmployeeContactResponse.cs   # Read Repository 프로젝션 record(목록 · 이름 조회 공용)
    ├── EmployeeLogs.cs              # 로그 이벤트 20001(배포된 ID라 유지, S06 등록 Handler가 다시 씀)
    └── IEmployeeReadRepository.cs   # 목록 · 개수 · 이름 단건
```

> 아래 Handler 예시는 PRD-001 샘플(`v0.1.0` 태그의 `Employees/Commands/RegisterEmployee/`)이고 S05-T04에서 코드에서 지웠다. 예시는 S06-T04에서 일괄 등록(`RegisterEmployeesCommand`, 처리 순서와 검증 위치는 [ADR-0026](../03-architecture/adr/0026-employee-bulk-import-input-processing.md))의 실제 코드로 바꾼다. 그때까지 예시는 Handler 형태(주입 · 저장하지 않음 · `Result` 반환)의 설명용이고, `Employee.Register` 호출 모양은 지금 코드와 다르다(아래 목록 참고).

아래 예시는 `v0.1.0`의 `RegisterEmployeeCommand.cs`와 `RegisterEmployeeCommandHandler.cs` 두 파일을 이어 붙이고, using · namespace · XML 문서 주석 · `[SuppressMessage]`(CA1812, [경고 억제 규칙](#경고-억제-규칙))를 뺀 것이다.

```csharp
public sealed record RegisterEmployeeCommand(string DisplayName, string Email, EmployeeStatus? EmployeeStatus)
    : ICommand<EmployeeId>;

internal sealed class RegisterEmployeeCommandHandler(
    IEmployeeRepository repository,
    IIdGenerator idGenerator,
    ILogger<RegisterEmployeeCommandHandler> logger) : ICommandHandler<RegisterEmployeeCommand, EmployeeId>
{
    public async Task<Result<EmployeeId>> Handle(RegisterEmployeeCommand command, CancellationToken cancellationToken)
    {
        // 누락(21006)은 검증 데코레이터가 먼저 막는다. 여기까지 null이 오면 예약 값 0으로 넘겨 도메인 불변식이 예외로 막게 한다.
        var employee = Domain.Employees.Employee.Register(
            new EmployeeId(idGenerator.NewId()),
            command.DisplayName,
            command.Email,
            command.EmployeeStatus ?? EmployeeStatus.Unknown);

        if (await repository.ExistsByEmailAsync(employee.Email, cancellationToken))
        {
            return EmployeeErrors.DuplicateEmail;
        }

        repository.Add(employee);
        logger.EmployeeRegistered(employee.Id.Value);

        return employee.Id;
    }
}
```

- 저장 · 커밋은 이 Handler가 아니라 트랜잭션 데코레이터 → `IUnitOfWork`가 한다([ADR-0014](../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md)). Handler는 `SaveChanges`를 부르지 않는다.
- ID는 Handler가 `IIdGenerator.NewId()`로 만든다([ADR-0013](../03-architecture/adr/0013-uuid-v7-with-uuidnext.md)).
- `Employee.Register`는 `Result`가 아니라 `Employee`를 돌려주고, 불변식을 어기면 예외를 던진다([DDD 구현 규칙](#ddd-구현-규칙-aggregate--value-object)). S05-T04부터 시그니처는 `Register(EmployeeId id, Name name, Email email, PhoneNumber phoneNumber, JoinedOn joinedOn)`이다. 필드 규칙은 호출한 쪽이 Value Object `Create`의 `Result`로 먼저 판정하고, 상태는 입력으로 받지 않고 Active(1)로 고정한다.
- 이메일은 Email Value Object(입력 표기 `Value`, `NormalizedEmail` = `Value.ToLowerInvariant()`)이고, Aggregate는 `NormalizedEmail` 문자열 속성을 가진다. 사전 중복 검사는 정규화 값 목록으로 한 번에 하고(`IEmployeeRepository.ListExistingNormalizedEmailsAsync`, `= ANY` 배열 매개변수 1개, S05-T06), 동시 요청 경합은 유니크 인덱스 `ux_employees_normalized_email` → 23505 → Infrastructure 매핑이 같은 `EmployeeErrors.DuplicateEmail`(23001) 인스턴스로 막는다([ADR-0027](../03-architecture/adr/0027-case-insensitive-unique-email-with-normalized-column.md)). 위 예시의 `employee.Email`(정규화 `string`, PRD-001)은 지금 `employee.NormalizedEmail`이다.
- 시각이 필요한 Handler는 `TimeProvider`를 주입받는다. 이 Handler는 시각을 쓰지 않는다(감사 시각은 Infrastructure 감사 인터셉터가 채움).

## Repository 규칙 (EF Core)

데이터 접근은 EF Core로 하고, Repository는 **쿼리만** 담습니다.

| 구분 | Write Repository | Read Repository |
|---|---|---|
| 용도 | Command: Aggregate 조회 · 추가 · 삭제 | Query: DTO 조회 |
| 인터페이스 위치 | Domain (`IEmployeeRepository`) | Application (`IEmployeeReadRepository`) |
| 구현 위치 | Infrastructure | Infrastructure |
| DbContext | 쓰기 (`EmployeeDbContext`) | 읽기 전용 (`EmployeeReadDbContext`) |
| 반환 | Aggregate | 응답 `record` (Select 프로젝션) |

**Repository 클래스에 허용하는 것은 EF Core 쿼리뿐이다.**

- 메서드 하나 = **식 본문(`=>`) 하나의 LINQ 메서드 체인**. 쿼리는 **람다식(메서드 구문)**으로 작성한다.
- 다음은 Repository에 두지 않는다: `if` / `switch` / 삼항 · null 병합 연산자 같은 분기, 반복문, `try` / `catch`, 로깅, 검증, 매핑 코드, 여러 쿼리 조합, `SaveChangesAsync` 호출(Unit of Work가 담당).
- 판단과 분기는 Handler(Application) 또는 Aggregate(Domain)가 한다.
- LINQ 쿼리 구문(`from x in ... select`)과 원시 SQL(`FromSql`, `ExecuteSql`)은 쓰지 않는다. 원시 SQL이 꼭 필요하면 작업 문서에 사유를 남기고 사용자 승인을 받는다.
- 선택적 조건은 코드 분기가 아니라 **람다 안의 조건식**으로 쓴다(SQL로 번역됨).

- 값 변환기로 매핑한 Value Object 속성(Employee의 `Name` · `Email` · `PhoneNumber` · `JoinedOn`)은 조건 · 정렬에서 **Value Object끼리** 비교한다(`employee.Name == name`, `OrderBy(employee => employee.JoinedOn)`). 조건식 안의 `employee.Name.Value`는 번역되지 않는다(`InvalidOperationException` "could not be translated"). 최상위 `Select`의 `.Value` 프로젝션은 컬럼을 읽은 뒤 클라이언트에서 계산되므로 쓸 수 있다(S05-T04 실측: `ToQueryString` · 가로챈 명령).
- 목록 조회 쿼리 형태(S05-T06 실측, 가로챈 명령 원문): 정렬은 `OrderBy(JoinedOn).ThenBy(Id)`를 함께 써야 `ORDER BY e.joined_on, e.id`가 복합 인덱스와 맞고 결과가 안정적이다. 값 목록 조건은 람다 `Contains`로 쓰면 배열 매개변수 1개의 `= ANY (@...)`로 번역된다(`IN (...)` 나열 아님). 목록과 개수는 메서드를 나누고 `COUNT(*) OVER()`를 쓰지 않는다. `Distinct` · 빈 목록 처리 같은 판단은 Handler가 한다.

실제 코드(`src/Services/Employee/EmergencyHub.Employee.Infrastructure/Persistence/Repositories/EmployeeRepository.cs`, `.../ReadRepositories/EmployeeReadRepository.cs` 두 파일을 이어 붙이고, using(별칭 `EmployeeAggregate`만 남김) · namespace · XML 문서 주석을 뺀 것. 남은 줄은 소스와 같다):

```csharp
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

internal sealed class EmployeeRepository(EmployeeDbContext db) : RepositoryBase<EmployeeDbContext>(db), IEmployeeRepository
{
    public Task<List<string>> ListExistingNormalizedEmailsAsync(IReadOnlyCollection<string> normalizedEmails, CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>()
            .Where(employee => normalizedEmails.Contains(employee.NormalizedEmail))
            .Select(employee => employee.NormalizedEmail)
            .ToListAsync(cancellationToken);

    public void Add(EmployeeAggregate employee) => Db.Set<EmployeeAggregate>().Add(employee);

    public void AddRange(IEnumerable<EmployeeAggregate> employees) => Db.Set<EmployeeAggregate>().AddRange(employees);
}

internal sealed class EmployeeReadRepository(EmployeeReadDbContext db)
    : ReadRepositoryBase<EmployeeReadDbContext>(db), IEmployeeReadRepository
{
    public Task<List<EmployeeContactResponse>> ListOrderedByJoinedOnAsync(int skip, int take, CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>()
            .OrderBy(employee => employee.JoinedOn)
            .ThenBy(employee => employee.Id)
            .Skip(skip)
            .Take(take)
            .Select(employee => new EmployeeContactResponse(
                employee.Id.Value, employee.Name.Value, employee.Email.Value, employee.PhoneNumber.Value, employee.JoinedOn.Value))
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>().CountAsync(cancellationToken);

    public Task<EmployeeContactResponse?> FindFirstByNameAsync(Name name, CancellationToken cancellationToken) =>
        Db.Set<EmployeeAggregate>()
            .Where(employee => employee.Name == name)
            .OrderBy(employee => employee.JoinedOn)
            .ThenBy(employee => employee.Id)
            .Select(employee => new EmployeeContactResponse(
                employee.Id.Value, employee.Name.Value, employee.Email.Value, employee.PhoneNumber.Value, employee.JoinedOn.Value))
            .FirstOrDefaultAsync(cancellationToken);
}
```

선택 조건을 람다 안에 쓰는 형태(설명용, 현재 코드에는 검색 쿼리가 없음):

```csharp
public Task<List<EmployeeSummaryResponse>> SearchAsync(EmployeeSearchCondition condition, CancellationToken cancellationToken) =>
    Db.Set<EmployeeAggregate>()
        .Where(employee => condition.Status == null || employee.EmployeeStatus == condition.Status)   // 분기 대신 조건식
        .OrderBy(employee => employee.DisplayName)
        .Skip(condition.Offset)
        .Take(condition.Limit)
        .Select(employee => new EmployeeSummaryResponse(employee.Id.Value, employee.DisplayName))
        .ToListAsync(cancellationToken);
```

### 영속성 기반 형식 (BuildingBlocks.Infrastructure.Persistence)

DB 규칙의 원본은 [데이터베이스 · EF Core 공통 모델 규칙](database.md#ef-core-공통-모델-규칙-buildingblocksinfrastructure)이고, 아래는 서비스 코드가 쓰는 형식입니다(S02-T04).

| 형식 | 서비스 코드에서 쓰는 방법 |
|---|---|
| `WriteDbContextBase` / `ReadDbContextBase` | `<Service>DbContext` / `<Service>ReadDbContext`가 상속하고 `ModelDefinition`만 재정의한다. `ConfigureConventions` · `OnModelCreating`은 봉인되어 있다. 읽기 기반은 `NoTracking` 기본값이고 `SaveChanges` 오버로드 4개가 `InvalidOperationException`을 던진다 |
| `IDbModelDefinition` | 서비스 Infrastructure에 구현 하나를 두고 두 DbContext가 **같은 인스턴스**를 돌려준다. `StronglyTypedIdAssemblies`(보통 Domain), `ConfigureModel`(`ApplyConfigurationsFromAssembly`) |
| `IStronglyTypedId<TSelf>`(Domain) | `public readonly record struct EmployeeId(Guid Value) : IStronglyTypedId<EmployeeId>;` 값 변환기 · 키 `ValueGeneratedNever`는 공통 규칙이 건다(엔티티마다 `HasConversion` 금지) |
| `HasUniqueIndex(e => ..., 이름 상수)` · `UniqueIndexName` | 이름 상수는 `public static readonly UniqueIndexName EmployeesEmail = new("ux_employees_email");`처럼 서비스가 한 곳에 둔다(`ux_` 접두사 · 소문자 snake_case · 63바이트 검사) |
| `HasCodeCheckConstraint()` / `HasFlagsCheckConstraint()` | `builder.Property(e => e.EmployeeStatus).HasCodeCheckConstraint();` 이름 · SQL은 공통 규칙이 enum 정의와 최종 컬럼 이름으로 만든다 |
| `ShadowPropertyNames` | `CreatedAt` · `UpdatedAt` · `Version`. 프로젝션은 `EF.Property<DateTimeOffset>(e, ShadowPropertyNames.CreatedAt)` |

- 강타입 ID는 `Guid` 하나를 받는 public 생성자가 있어야 한다(위치 기반 `record struct`면 자동). 없으면 그 형식을 쓰는 모델 생성이 실패한다.
- 도메인 이벤트(`IDomainEvent` 구현 형식)는 속성 · 탐색에서 모두 빠진다. Aggregate에 구체 이벤트 형식의 속성을 두어도 매핑되지 않는다.
- 감사 인터셉터 · snake_case · 실행 전략은 공통 등록 확장이 붙인다(S02-T07). 서비스 코드에서 직접 붙이지 않는다.

## 의존성 주입 (DI) 규칙

**서비스와 Repository는 `Scoped`로, 타입 검색(assembly scanning)을 통해 자동 등록**합니다. `Program.cs`나 `DependencyInjection.cs`에서 구현 타입을 하나씩 등록하지 않습니다.

BuildingBlocks에 등록 기준이 되는 **마커 인터페이스와 기반 클래스**를 둡니다.

| 종류 | 상속 대상 | 등록 |
|---|---|---|
| Write Repository | 인터페이스: `IRepository` 상속 / 구현: `RepositoryBase<TContext>`(`TContext : WriteDbContextBase`) 상속 | Scoped |
| Read Repository | 인터페이스: `IReadRepository` 상속 / 구현: `ReadRepositoryBase<TContext>`(`TContext : ReadDbContextBase`) 상속 | Scoped |
| 서비스 (도메인 서비스, 외부 연동 어댑터 등) | 인터페이스: `IService` 상속 | Scoped |
| Command / Query Handler, Validator | `ICommandHandler<,>` / `IQueryHandler<,>` / `AbstractValidator<T>` | `AddConventionalServices` 안에서 Scoped 등록(Handler는 Scrutor `Scan`, Validator는 `AddValidatorsFromAssemblies`). 데코레이터는 Scrutor `TryDecorate` |
| 호스티드 서비스 (`IHostedService` / `BackgroundService`) | 마커 없음 | 호스트 `Program`에서 `builder.Services.AddHostedService<T>()`로 **명시 등록한다**(허용, 수명은 프레임워크가 정한 Singleton). 예: MigrationService `Program.Configure`의 `AddHostedService<MigrationWorker>()`. 어셈블리 검색 대상이 아니다(BL-092) |

```csharp
// BuildingBlocks: 마커와 기반 클래스
public interface IRepository;
public interface IReadRepository;
public interface IService;

// 쓰기 기반은 WriteDbContextBase, 읽기 기반은 ReadDbContextBase만 받는다(컴파일 시점에 섞이지 않음, S02-T04)
public abstract class RepositoryBase<TContext> where TContext : WriteDbContextBase
{
    protected RepositoryBase(TContext db) { ArgumentNullException.ThrowIfNull(db); Db = db; }
    protected TContext Db { get; }
}

// Domain / Application: 인터페이스가 마커를 상속
public interface IEmployeeRepository : IRepository { /* ... */ }
public interface IEmployeeReadRepository : IReadRepository { /* ... */ }

// 서비스 초기화: 어셈블리만 넘기면 마커를 구현한 타입을 찾아 Scoped로 등록
builder.Services.AddConventionalServices(
    typeof(EmployeeApplicationAssembly).Assembly,
    typeof(EmployeeInfrastructureAssembly).Assembly);
```

- `AddConventionalServices`는 BuildingBlocks가 제공한다. 지정한 어셈블리에서 **추상이 아닌 클래스 중 마커를 상속한 인터페이스를 구현한 타입**을 찾아, 그 인터페이스(마커 제외)로 `Scoped` 등록한다. `internal` 클래스도 찾는다.
- 구현 클래스는 `sealed`, 인터페이스 이름은 `I` + 구현 클래스 이름을 원칙으로 한다(`EmployeeRepository` ↔ `IEmployeeRepository`).
- 구현 클래스가 서비스 인터페이스를 **하나만** 구현하도록 한다. 두 개 이상이면 등록이 모호해지므로 설계를 나눈다.
- `Singleton` / `Transient`가 필요한 인프라 요소(`TimeProvider`, `HttpClient` 등)는 BuildingBlocks의 공통 등록 코드에서만 등록한다. 서비스 코드에서 직접 등록하지 않는다(예외: 위 표의 호스티드 서비스 `AddHostedService<T>()`).
- 등록 누락은 통합 테스트(모든 마커 구현 타입이 컨테이너에서 해석되는지)로 검증하고, `ValidateOnBuild` / `ValidateScopes`를 개발 환경에서 켠다.

> **타입 검색은 Scrutor로 구현한다**([ADR-0017](../03-architecture/adr/0017-scrutor-for-convention-based-di.md), ADR-0010 구체화). Scrutor는 BuildingBlocks.Infrastructure만 참조한다. 같은 서비스 인터페이스를 두 구현이 등록하면 시작 시 실패한다(`RegistrationStrategy.Throw`). Handler는 두 제네릭 인자 형태(`ICommandHandler<,>` / `IQueryHandler<,>`)로만 등록해 데코레이터를 우회하는 경로를 만들지 않는다.

BuildingBlocks 공통 등록 진입점 (S02-T03)

| 진입점 | 위치 | 등록 |
|---|---|---|
| `AddBuildingBlocksApplication()` | BuildingBlocks.Application | `ISender`(Scoped), `TimeProvider.System`(Singleton). `TryAdd`라 여러 번 불러도 하나, 먼저 등록한 대역(`FakeTimeProvider` 등)은 유지 |
| `AddBuildingBlocksInfrastructure()` | BuildingBlocks.Infrastructure | `IIdGenerator`(UUID v7, Scoped) + `AddBuildingBlocksApplication()`. `TryAdd` |
| `AddConventionalServices(어셈블리...)` | BuildingBlocks.Infrastructure | 마커 구현 · Handler · Validator(모두 Scoped), 데코레이터(`PipelineDecorators` 목록 순서로 `TryDecorate`) + `AddBuildingBlocksApplication()` |
| `AddBuildingBlocksApi(apiTitle)` / `app.UseBuildingBlocksApi()` | BuildingBlocks.Api | Controller 기본 설정 · 바인딩 오류 1001 · 전역 예외 처리기(Singleton) · Swashbuckle(`v1`) + `AddBuildingBlocksApplication()` / 예외 처리 미들웨어 → (Development만) Swagger → `MapControllers`. 서비스 Api 호스트만 부르고, 두 번 부르면 `InvalidOperationException`. `AddConventionalServices`는 부르지 않는다([ADR-0024](../03-architecture/adr/0024-building-blocks-api-for-common-http-handling.md), S02-T06) |

- `IIdGenerator`는 `IService`를 상속하지 않고 `AddBuildingBlocksInfrastructure()`가 **명시 등록**한다. ADR-0017이 공통 인프라(`TimeProvider`, `ISender`, `IIdGenerator`)를 공통 등록 코드에서 명시 등록한다고 정했고, 구현이 서비스 어셈블리 밖(BuildingBlocks.Infrastructure)에 있어 어셈블리 검색으로 찾을 수 없기 때문이다. 수명은 ADR-0013대로 Scoped다. `IUnitOfWork` · `IExceptionClassifier`도 마커를 상속하지 않는다(등록은 S02-T07).
- `AddConventionalServices`는 **한 번만** 부르고 검색할 어셈블리를 한 번에 모두 넘긴다. 두 번 부르면 Handler가 두 겹으로 감싸져 커밋 · 로그가 중복되므로 두 번째 호출은 `InvalidOperationException`이다. 같은 어셈블리를 중복해 넘기는 것은 한 번으로 처리한다.
- 마커를 구현한 클래스가 서비스 인터페이스(마커를 상속한 인터페이스)를 구현하지 않거나 둘 이상 구현하면, 등록 전에 `InvalidOperationException`으로 시작이 실패한다(위반 형식을 모두 메시지에 담음).
- open generic 정의는 Handler로 등록하지 않는다(데코레이터 제외). 파이프라인 순서의 원본은 `PipelineDecorators`(BuildingBlocks.Application, 안쪽 → 바깥) 하나다.
- Scrutor 7의 `Decorate`는 감싼 안쪽 단계를 같은 서비스 형식의 **keyed 등록**으로 남긴다. 등록을 세는 테스트는 키 없는 등록(가장 바깥) 하나와 전체 단계 수(Command 4, Query 3)를 나눠 확인한다.

### Controller 의존성 (서비스 로케이터 금지)

Controller는 primary constructor로 **`ISender`만** 받는다([ADR-0016](../03-architecture/adr/0016-use-controllers-for-api.md)). 다른 의존성을 생성자 밖에서 꺼내는 **서비스 로케이터도 금지**한다.

- 금지: Controller 본문 · 액션의 `HttpContext.RequestServices`, `IServiceProvider` 주입, `GetService<T>()` · `GetRequiredService<T>()`, 액션 매개변수 `[FromServices]`.
- 이유: 아키텍처 규칙 `ControllersDoNotUseInfrastructureOrRepositories`는 생성자 · 액션 매개변수 같은 시그니처만 보므로, 메서드 본문에서 Repository를 꺼내는 코드는 잡지 못한다(TD-025). 그래서 reviewer가 판정하고, 규칙 기계화는 BL-119에서 판단한다.
- 점검(Git Bash): `git ls-files src/Services | grep 'Controllers/.*\.cs$' | xargs grep -nE "IServiceProvider|RequestServices|GetRequiredService|GetService|FromServices"`. 2026-09-28 현재 출력 없음(S05-T04에서 PRD-001 샘플 Controller를 지워 Controller 파일이 0개다. `/api/employee` Controller는 S06-T05).
- Controller가 아닌 곳의 `GetService` · `GetRequiredService`(DI 등록 코드, Mediator 디스패처, MigrationService Worker의 스코프, BuildingBlocks.Api 바인딩 오류 응답 `InvalidModelStateResponses`의 로거 조회)는 이 규칙의 대상이 아니다.

## 비동기 프로그래밍 규칙

- I/O는 끝까지 비동기로 처리한다. `.Result`, `.Wait()`, `GetAwaiter().GetResult()` 금지.
- `async void` 금지(이벤트 핸들러 제외).
- 비동기 메서드는 `CancellationToken`을 **마지막 매개변수**로 받고 하위 호출에 전달한다.
- ASP.NET Core 애플리케이션 코드에서는 `ConfigureAwait(false)`를 쓰지 않는다.
- 결과를 그대로 반환하는 메서드도 `async` / `await`로 쓴다(스택 추적 보존). 성능이 입증된 핫패스만 예외.

## 예외 처리 규칙

- **예상 가능한 실패는 예외가 아니라 `Result`로 반환한다.** 비즈니스 규칙 위반, 검증 실패, 대상 없음, 충돌이 여기에 해당한다.
- 예외는 예상하지 못한 오류(인프라 장애, 프로그래밍 오류)에만 쓴다. Validator를 거친 뒤의 Aggregate 불변식 위반은 프로그래밍 오류라 예외다([DDD 구현 규칙](#ddd-구현-규칙-aggregate--value-object)의 실패 처리 경계).
- `Error`는 **정수 코드**와 메시지, 유형(`ErrorType`: 검증 / 본문 크기 초과 / 지원하지 않는 Content-Type / 없음 / 충돌 / 규칙 위반 / 인증 / 권한 / 내부 / 외부 연동 / 일시 장애)을 가진다. 유형별 팩토리(`Error.NotFound(22001, "...")`)로만 만들고, 코드 규칙(범위, T ↔ `ErrorType`)을 어기면 생성 시 예외가 난다. 코드 범위는 서비스별로 나누며 [에러 코드](../05-api/error-codes.md)에서 관리한다.
- `Result` / `Result<T>`: 성공은 `Result.Success()` / `Result.Success(value)`, 실패는 `Result.Failure(error)`. `Error` → `Result` / `Result<T>`, 값 → `Result<T>` 암시적 변환이 있어 `return EmployeeErrors.NotFound;`, `return result.Error;`, `return result.Value.Id;`로 쓴다. 성공 값은 `null`일 수 없다(대상이 없으면 NotFound 실패). 실패 결과의 `Value`, 성공 결과의 `Error`를 읽으면 `InvalidOperationException`이므로 `IsFailure`를 먼저 확인한다. `Error.None` 같은 빈 오류 값은 두지 않는다.
- API는 `Result`를 RFC 9457 `ProblemDetails`로 변환한다. 전역 예외 처리(`IExceptionHandler`)는 예상하지 못한 예외만 500으로 변환하고 로그를 남긴다.
- `catch (Exception)`으로 삼키지 않는다. 잡았으면 처리하거나 로그와 함께 다시 던진다.

## DDD 구현 규칙 (Aggregate / Value Object)

- Aggregate Root는 **팩토리 메서드**(`Register`, `Create`)로만 생성한다. 생성자는 `private`.
- 속성 setter는 `private`. 상태는 의미 있는 도메인 메서드(`ChangeStatus`, `AssignTo`)로만 바꾼다.
- **실패 처리 경계: Aggregate 불변식 위반은 예외, 입력 검증 실패는 `Result`다.** 판정 기준은 "요청 값만으로 판정할 수 있는 규칙인가"다. 입력 검증 실패를 판정하는 곳은 기본이 Validator이고, Employee 필드 규칙은 Value Object `Create`다(아래 두 번째 항목).
  - 요청 값만으로 판정할 수 있는 규칙(필수 · 길이 · 형식 · 정의된 코드값)은 Validator가 먼저 `ValidationError`(1001 + 필드 코드) `Result`로 돌려주고, Aggregate는 같은 규칙을 불변식으로 다시 검사해 어기면 예외(`ArgumentException` 계열)를 던진다. 요청 값이 아닌 인자(Handler가 만든 ID가 비어 있음 등)의 불변식 위반도 예외다. Validator를 통과한 값이 Aggregate 예외를 일으키면 Validator 누락 · 기준 불일치인 프로그래밍 오류다(전역 예외 처리기 → 9001). 그래서 두 곳은 같은 판정 코드를 쓴다(PRD-001 샘플은 `EmployeeEmail.Normalize` · `IsWellFormed`와 `Employee.DisplayNameMaxLength`를 함께 썼고, S05-T04에서 샘플과 함께 지웠다).
  - **Employee 필드 규칙(name · email · tel · joined)의 판정 원본은 Value Object(`Name` · `Email` · `PhoneNumber` · `JoinedOn`)의 `Create(string?)`가 돌려주는 `Result`다**(필드 코드 `Error` 하나, [ADR-0026](../03-architecture/adr/0026-employee-bulk-import-input-processing.md) 8절). 세 분류는 그대로이고 첫 분류의 판정 위치만 Validator에서 Value Object로 옮긴다. 규칙과 길이 · 자리 수 상수는 Value Object 한 곳에만 두고(`public const`), Handler · Validator · EF 설정은 호출 · 참조만 한다. 호출한 쪽이 결과를 `FieldError`로 옮긴다(`RegisterEmployeesCommand`는 Handler, 다른 Command는 그 Validator). `Employee.Register`는 검증된 Value Object만 받아 문자열 규칙을 다시 검사하지 않고, `null` 인자 · 빈 ID 같은 불변식 위반만 예외로 막는다. **적용 범위는 Employee Value Object 4개다.** 다른 Aggregate의 Value Object가 같은 방식을 쓰려면 그 작업에서 이 규칙에 기준을 추가한다.
  - 저장된 데이터나 현재 상태에 따라 달라지는 실패(대상 없음, 중복, 허용되지 않은 상태 전이 같은 업무 규칙 위반)는 Handler 또는 Aggregate 메서드가 `Result`(유형 2 · 3 · 4)로 돌려준다([예외 처리 규칙](#예외-처리-규칙)).
  - 예: `Employee.Register(id, name, email, phoneNumber, joinedOn)`(S05-T04)은 `Employee`를 돌려주고, 빈 ID · `null` Value Object면 예외를 던진다. 필드 규칙 위반은 Value Object `Create`의 `Result`(21003 ~ 21005, 21007 ~ 21017), 이메일 중복(23001)은 Handler가 `Result`로 돌려준다. PRD-001 샘플(`Register(id, displayName, email, status)`)은 S05-T04에서 지웠다.
- 다른 Aggregate는 **ID로만** 참조한다.
- ID는 강타입 `record struct`(`EmployeeId`)를 쓴다.
- Value Object는 get-only `sealed record`로 만든다. `Create`로만 만들어야 하는 Value Object(Employee 4개)는 위치 기반 record가 아니라 **private 생성자 + get-only 속성**으로 둔다(위치 기반이면 public 생성자와 `init`이 생겨 `Create` 판정을 우회한다). 예: `public sealed record Name { private Name(string value) { Value = value; } public string Value { get; } public static Result<Name> Create(string? value) ... }`. PRD-001 샘플은 이메일을 값 객체로 만들지 않고 Aggregate가 정규화한 `string` 속성으로 두었다(`Employee.Email`, `EmployeeEmail.Normalize` = Trim + `ToLowerInvariant`, S03 결정, S05-T04에서 제거). PRD-002부터 이메일은 Email Value Object이고(S05-T03), 입력 표기와 정규화 값(`NormalizedEmail`, NFC 없음)을 따로 가진다. Aggregate는 `Email`(VO)과 `NormalizedEmail`(`string`, 유니크 인덱스 대상)을 함께 가진다(S05-T04)([ADR-0027](../03-architecture/adr/0027-case-insensitive-unique-email-with-normalized-column.md)).
- 도메인 이벤트는 Aggregate가 발생시켜 수집한다. 지금은 **수집까지만** 하고 커밋 뒤 UnitOfWork가 `ClearDomainEvents`로 비운다([ADR-0014](../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md)). 디스패치는 이후 토픽, 다른 서비스로 알릴 통합 이벤트 · Outbox는 도입 보류다([ADR-0023](../03-architecture/adr/0023-deferred-adoptions.md), [ADR-0004](../03-architecture/adr/0004-adopt-event-driven-architecture.md) 유지).
- Domain 프로젝트는 EF Core, ASP.NET Core 등 프레임워크를 참조하지 않는다(데이터 어노테이션 금지).

## 코드 스타일 (.editorconfig)

저장소 루트의 `.editorconfig`로 강제하고, CI에서 `dotnet format --verify-no-changes`로 검사합니다. 파일은 기반 구축 토픽에서 만듭니다.

| 항목 | 값 |
|---|---|
| 들여쓰기 | 공백 4칸 (XML / JSON / YAML은 2칸) |
| 줄 끝 / 인코딩 | LF, UTF-8 |
| 중괄호 | Allman(새 줄), 한 줄 `if`도 중괄호 필수 |
| `using` | 네임스페이스 밖, `System` 먼저 정렬 |
| namespace | file-scoped (`csharp_style_namespace_declarations = file_scoped:error`) |
| 분석기 | `AnalysisLevel = latest-recommended`, 경고는 오류로 처리 |
| 빌드 강제 | `옵션 = 값:심각도`의 심각도는 빌드에서 적용되지 않으므로 강제할 IDE 규칙은 `dotnet_diagnostic.<ID>.severity`로 다시 지정한다(IDE0161 · IDE0011 · IDE0065 · IDE0055 · IDE1006 등) |
| 테스트 예외 | `tests/**.cs`는 CA1707(밑줄 이름) · CA1822 · `Async` 접미사 규칙을 끈다(`<메서드>_<조건>_<기대 결과>`) |
| 생성 코드 | `**/Persistence/Migrations/*.cs`는 `generated_code = true`와 **`dotnet_diagnostic.CS1591.severity = none`을 함께** 둔다. `generated_code`는 분석기 · 스타일 규칙만 끄고 컴파일러 경고 CS1591은 끄지 못한다. `WarningsAsErrors`에 CS1591을 넣으면 이 설정보다 우선하므로 넣지 않는다(`TreatWarningsAsErrors`로 충분). 직접 작성하는 sealed partial 선언 `*.Sealed.cs`는 뒤따르는 섹션에서 `generated_code = false`로 되돌려 분석 대상에 둔다([데이터베이스](database.md#마이그레이션-규칙)) |

## 경고 억제 규칙

빌드는 `TreatWarningsAsErrors`라 경고 하나가 빌드 실패입니다. 경고는 코드를 고쳐 없애는 것이 기본이고, 억제는 규칙이 이 코드에 맞지 않는 이유를 적을 수 있을 때만 합니다(BL-055, S02 reviewer 판정 기준을 명문화).

- 억제는 경고가 난 선언에 **`[SuppressMessage("<범주>", "<ID>:<제목>", Justification = "<이유>")]`**를 붙여서만 한다. `Justification`은 필수이고, 이 코드가 규칙의 전제와 다른 이유(예: "DI가 만든다")를 적는다. 같은 이유를 여러 곳에 쓰면 문자열 상수로 둘 수 있다(예: ArchitectureTests `SampleSuppressions.MetadataOnly`).
- 다음은 쓰지 않는다: 전역 `NoWarn` · `WarningsNotAsErrors`(프로젝트 · `Directory.Build.props`), `#pragma warning disable`, `.editorconfig`의 `dotnet_diagnostic.<ID>.severity` 낮추기.
  - 기존 설정만 예외다: `.editorconfig`의 `tests/**.cs` CA1707 · CA1822 · `Async` 접미사 규칙 끄기와 `**/Persistence/Migrations/*.cs`의 `generated_code = true` · CS1591 none([코드 스타일](#코드-스타일-editorconfig)), EF Core 도구가 생성한 마이그레이션 · 스냅샷 `Designer`의 `#pragma warning disable 612, 618`(생성 코드라 직접 고치지 않음).
- 새 억제는 developer가 억제를 추가한 같은 작업에서 아래 승인 목록에 행을 추가하고(승인 기록 칸은 해당 작업 reviewer), reviewer가 진행 기록에 승인을 남긴다. 목록에 행이 없거나 reviewer 승인 기록이 없는 억제는 반려 사유다.
  - **억제와 승인 목록 행은 함께 제출한다.** 승인 목록 행 없이 억제만 제출하면 reviewer는 PASS할 수 없다. 승인을 다음 작업으로 미루지 않는다(PRD-001에서 Employee CA1812 3건이 S03-T01에 들어오고 S04-T05에서 사후 승인됨).
  - **앞선 작업의 같은 유형을 grep한다.** 새 억제의 규칙 ID로 저장소 전체를 찾아, 같은 ID의 억제가 모두 승인 목록에 있는지 대조한다. 목록에 없는 것이 나오면 developer는 제출 내용에 적고, reviewer는 그 억제를 들여온 작업을 밝혀 승인 여부를 판정받는다.
  - 규칙 ID 검색(Git Bash, 속성이 여러 줄이라 ID 문자열로 찾음): `git ls-files src tests | grep '\.cs$' | xargs grep -n '"<ID>:'`. 예: `"CA1812:`은 2026-09-28 현재 13줄이고 승인 목록 CA1812 행의 파일 수 합계(2 + 5 + 1 + 2 + 3)와 같다(S05-T04에서 Employee.Application 샘플 3개 파일과 함께 3줄이 없어짐).

승인 목록 (2026-09-28 현재 코드 전수, 17건 = 제품 11 + 테스트 6. S05-T04에서 Employee.Application CA1812 3건은 억제한 파일과 함께 삭제)

| ID | 위치 (파일) | 사유 요약 | 승인 기록 |
|---|---|---|---|
| CA1716 | BuildingBlocks.Domain `Errors/Error.cs` | `Error`는 PRD-001 FR-04 · ADR-0018이 정한 도메인 용어, VB 소비자 없음 | S01-T06 reviewer |
| CA1812 | BuildingBlocks.Application `Cqrs/CommandInvoker{TCommand,TResponse}.cs`, `Cqrs/QueryInvoker{TQuery,TResponse}.cs` | `RequestInvokerCache`가 `MakeGenericType` + `Activator`로 생성 | S02-T01 reviewer |
| CA1812 | BuildingBlocks.Application `Pipeline/`의 `LoggingCommandHandlerDecorator.cs`, `LoggingQueryHandlerDecorator.cs`, `ValidationCommandHandlerDecorator.cs`, `ValidationQueryHandlerDecorator.cs`, `TransactionCommandHandlerDecorator.cs` | Scrutor `TryDecorate`로 DI가 생성 | S02-T02 reviewer |
| CA1812 | BuildingBlocks.Infrastructure `Persistence/Conventions/StronglyTypedIdValueConverter.cs` | EF Core가 형식으로 받아 생성 | S02-T04 reviewer |
| CA1032, CA1064 | BuildingBlocks.Api `Exceptions/RedactedException.cs` | 던지지 않는 로그 전용 내부 사본(메시지 제거가 목적) | S02-T06 reviewer |
| CA1812 | 테스트 BuildingBlocks.Infrastructure.UnitTests `Samples/InternalSampleService.cs`, `Samples/CreateSampleCommandValidator.cs` | 어셈블리 검색 등록을 검증하는 샘플, DI가 생성 | S02-T03 reviewer |
| EF1001 | 테스트 BuildingBlocks.Infrastructure.UnitTests `Samples/Persistence/SamplePostgresExceptions.cs` | `DbUpdateConcurrencyException` 모양 재현에 EF 내부 엔트리 필요(테스트 전용) | S02-T07 reviewer |
| CA1812 | 테스트 ArchitectureTests `Samples/ImplementationVisibility/InternalSampleClassifier.cs`, `InternalSampleCommandHandler.cs`, `Samples/ValidatorBases/InternalSampleCommandValidator.cs` | 아키텍처 규칙 검증용 샘플, 인스턴스를 만들지 않음 | S02-T05 reviewer |

목록 대조 명령(Git Bash): `git ls-files src tests | grep '\.cs$' | xargs grep -c "SuppressMessage(" | grep -v ':0$'`(파일별 건수), `git ls-files | grep -v '^wiki/' | xargs grep -n -E "NoWarn|WarningsNotAsErrors|#pragma warning"`(생성 마이그레이션 2개 파일의 `612, 618`만 나와야 함).

## 로깅

로그는 [로깅 & 관측성](logging-observability.md) 규칙을 따른다. `ILogger<T>` + 메시지 템플릿, 반복 로그는 `[LoggerMessage]`, 개인정보 금지.

## 주석 및 문서화

- 주석은 **"왜"**를 적는다. 코드로 알 수 있는 "무엇"은 적지 않는다.
- BuildingBlocks의 public API에는 XML 문서 주석(`///`)을 단다.
- `TODO`는 기술부채 ID와 함께 쓴다: `// TODO(TD-003): 재시도 정책 적용`. ID 없는 `TODO`는 남기지 않는다.
- 주석 처리한 코드는 커밋하지 않는다.

---

## 변경 이력

| 날짜 | 작성자 | 내용 |
|---|---|---|
| 2026-09-27 | - | 문서 생성 |
| 2026-09-27 | - | 기본 컨벤션 초안: 네이밍, C# 12 기능, 정수 코드 / 비트 마스킹, CQRS, 비동기, Result 기반 예외 처리, DDD |
| 2026-09-27 | - | 모델은 모두 `record`, Repository 규칙(쿼리만, 람다 식), 읽기 / 쓰기 DbContext 분리, DI 자동 등록(마커 + Scoped) 추가 |
| 2026-09-27 | developer | ADR 0013~0015 · 0017 · 0023 반영: Handler 예시에서 `SaveChanges` 제거, Command 반환 `Result<Unit>`, Mediator 직접 구현 · Scrutor 확정, 파이프라인 순서, 도메인 이벤트 수집만 (S01-T04) |
| 2026-09-27 | developer | `.editorconfig` 표에 빌드 강제 · 테스트 예외 · 생성 코드(CS1591 none 병기, BL-047) 행 추가 (S01-T05) |
| 2026-09-27 | developer | BuildingBlocks.Domain `sealed` 예외(Entity · AggregateRoot · Error · Result), `Error` 팩토리 · 생성 시 검증, `Result` 사용 규칙과 암시적 변환(BL-040) (S01-T06) |
| 2026-09-27 | developer | CQRS 규칙에 Validator 공통 기반 `RequestValidator<T>` · `WithError` 규칙 추가 (S02-T02) |
| 2026-09-27 | developer | DI 규칙에 BuildingBlocks 공통 등록 진입점 표, `IIdGenerator` 명시 등록 결정, `AddConventionalServices` 1회 호출 · 서비스 인터페이스 1개 검사 · keyed 데코레이터 등록 추가, Handler 예시 주석의 `NewId()` 확정 (S02-T03) |
| 2026-09-27 | developer | Repository 규칙에 영속성 기반 형식 절 추가(DbContext 기반 · 모델 정의 · 강타입 ID · `ux_` / `ck_` 도우미 · shadow property 이름), DI 예시의 `RepositoryBase` 제약을 `WriteDbContextBase`로 정정 (S02-T04) |
| 2026-09-27 | developer | 공통 등록 진입점 표에 `AddBuildingBlocksApi` / `UseBuildingBlocksApi` 추가, 코드값 검증 규칙 `MustBeDefinedEnum()`(1002) (S02-T06) |
| 2026-09-27 | developer | `.editorconfig` 생성 코드 행에 직접 작성하는 sealed partial 선언(`*.Sealed.cs`)을 분석 대상으로 되돌리는 섹션 추가 (S03-T02 재작업) |
| 2026-09-28 | developer | S03 결정 · 실제 코드에 맞춤: DDD 실패 처리 경계(불변식 위반 예외 · 입력 검증 실패 Result, BL-089), Email 값 객체 예시 제거(정규화한 `string`), CQRS Handler · Repository 예시를 실제 코드로(`IIdGenerator.NewId()`, SaveChanges 미호출, BL-039), 경고 억제 규칙 · 승인 목록 20건(BL-055), 한 파일 한 형식 테스트 코드 적용(BL-069), 외부 규약 0 의미 internal enum 예외(BL-091), `AddHostedService` 명시 등록 허용(BL-092), `Error` 파생 문구 CS8878 정정(TD-016 문서분), sealed · Validator 기반 설명은 testing-strategy 링크로 (S04-T05) |
| 2026-09-28 | developer | S04-T05 재작업(reviewer 반려 1회): CQRS · Repository 예시 도입 문장을 실제로 뺀 범위(두 파일 합침, using · namespace · XML 문서 주석 · `[SuppressMessage]`)에 맞추고 소스에 없는 설명 주석 2줄을 코드 블록 밖 목록으로 이동, 경고 억제 승인 절차의 주체 · 순서 명시, Employee CA1812 3건 승인 기록을 S04-T05 reviewer 사후 승인으로 (S04-T05) |
| 2026-09-28 | - | RETRO-PRD-001 개선안 #14 반영: 새 경고 억제는 승인 목록 행과 함께 제출(행 없으면 reviewer PASS 불가), 앞선 작업의 같은 규칙 ID grep 대조와 검색 명령 |
| 2026-09-28 | - | RETRO-PRD-001 개선안 #15 반영: 테스트 enum 기반 형식 명시(`: int` 포함, 위반 표본 예외, 점검 명령), 생성 코드 `*.Sealed.cs` 규칙(BL-090, 실제 선언), Controller 서비스 로케이터 금지(TD-025, 점검 명령) |
| 2026-09-28 | developer | ADR-0026 · 0027 반영: 실패 처리 경계에 Employee 필드 규칙 판정 원본(Value Object `Create` → `Result`, 적용 범위 Employee Value Object 4개), CQRS 표 트랜잭션 · 검증 행의 `RegisterEmployeesCommand` 예외, 이메일 `string` 문구를 PRD-001 샘플 설명으로 한정하고 Email Value Object · `ux_employees_normalized_email` 추가, Register 예시가 샘플(S05-T04에서 제거, S06-T04에서 교체)임을 표시 (S05-T02) |
| 2026-09-28 | developer | Value Object 규칙에 `Create`로만 만드는 Value Object는 private 생성자 + get-only 속성(위치 기반 record 아님) 추가 (S05-T03) |
| 2026-09-28 | developer | PRD-001 샘플 제거 반영: 기능 폴더 구조를 규칙 + 현재 구성으로, Handler 예시를 `v0.1.0` 샘플 표시(교체는 S06-T04), `Employee.Register` VO 시그니처 · `NormalizedEmail`, Repository 실제 코드 · 값 변환기 VO 비교 규칙(실측), Controller 0개, 경고 억제 승인 목록 17건(Employee.Application CA1812 3건 삭제) (S05-T04) |
| 2026-09-28 | dba | sealed partial 선언 실제 코드 예시의 파일 이름 주석을 리셋 뒤 `20260928090646_InitialCreate.Sealed.cs`로 교체(선언 줄 그대로) (S05-T05) |
| 2026-09-28 | developer | Repository 실제 코드를 S05-T06 구현으로(`ListExistingNormalizedEmailsAsync` · `AddRange`, Read Repository 목록 · 개수 · 이름 단건, `ExistsByNormalizedEmailAsync` 제거), 목록 쿼리 형태(`ThenBy(Id)` · `Contains` → `= ANY` · 개수 분리, 실측), 기능 폴더 밖에 둘 수 있는 것에 Read Repository 프로젝션 `record` 추가 (S05-T06) |
| 2026-09-28 | orchestrator | S05 결과 리뷰: 기능 폴더 밖 프로젝션 `record` 규칙 추인(대리 승인), 조건 "Query 하나만 쓰는 프로젝션은 기능 폴더 안" 추가 (S05) |
| 2026-09-28 | developer | `Error` 파생에 `ConflictError`, `ErrorType` 목록에 본문 크기 초과 · 지원하지 않는 Content-Type 추가(ADR-0028) (S06-T01) |
