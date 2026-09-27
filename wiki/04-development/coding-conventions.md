---
title: "코딩 컨벤션"
type: doc
status: draft
tags: [development]
created: 2026-09-27
updated: 2026-09-27
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

- **파일 하나에 최상위 타입 하나**, 파일 이름 = 타입 이름.
- **file-scoped namespace**를 쓴다: `namespace EmergencyHub.Employee.Domain.Employees;`
- 네임스페이스 = 프로젝트 루트 네임스페이스 + 폴더 경로.
- 공통 `using`은 프로젝트별 `GlobalUsings.cs`에 둔다. `ImplicitUsings`를 켠다.

## C# 언어 기능 (C# 12)

| 규칙 | 설명 |
|---|---|
| Nullable 참조 형식 | 전 프로젝트 `enable`. `!`(null-forgiving)는 테스트 외에는 쓰지 않는다. |
| 클래스는 기본 `sealed` | 상속을 의도한 타입만 `sealed`를 뺀다(`abstract` 기반 클래스 등). |
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

## 코드값 (enum) 규칙

DB의 코드값 규칙([데이터베이스 · 코드값](database.md#코드값-규칙))과 한 쌍입니다.

- 코드는 `enum`으로 정의하고 **모든 멤버에 값을 명시**한다. 한 번 배포된 값은 바꾸거나 재사용하지 않는다.
- 기반 형식을 명시한다. 일반 코드는 `short`, 비트 플래그는 `int`(31개까지) 또는 `long`(63개까지).
- `0`은 `None` / `Unknown` 용도로 예약한다. 유효한 업무 값으로 쓰지 않는다.
- API 요청 / 응답과 이벤트에서도 코드는 **정수로 직렬화**한다. `JsonStringEnumConverter`는 쓰지 않는다.
- 외부에서 들어온 정수는 `Enum.IsDefined`(일반 코드) 또는 정의된 비트 마스크 범위 검사(플래그)로 검증한다.

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
| 트랜잭션 | Command 하나 = 트랜잭션 하나 = Aggregate 하나. 저장 · 커밋은 트랜잭션 데코레이터 → `IUnitOfWork`가 한다(Handler는 저장하지 않음) | 없음 |
| 검증 | FluentValidation Validator (파이프라인에서 자동 실행) | 필요 시 Validator |

- Handler 하나는 요청 하나만 처리한다.
- Command Handler는 다른 Command를 직접 호출하지 않는다. Handler는 `ISender`를 주입받지 않는다(중첩 `SendAsync`는 커밋이 두 번 일어남). 후속 처리는 도메인 이벤트 / 통합 이벤트로 연결한다.
- 공통 관심사(로깅, 검증, 트랜잭션)는 **Handler 데코레이터 파이프라인**으로 처리한다. 순서는 Command가 로깅 → 검증 → 트랜잭션 → Handler, Query가 로깅 → 검증 → Handler다(트랜잭션은 Command에만).
- **Handler와 Repository는 `SaveChanges`를 부르지 않는다.** Handler가 성공 `Result`를 돌려주면 트랜잭션 데코레이터가 `IUnitOfWork.CommitAsync`를 부르고, 실패 `Result`면 저장하지 않는다. Handler는 DbContext를 받지 않는다([ADR-0014](../03-architecture/adr/0014-command-transaction-boundary-and-unit-of-work.md)).
- Application 코드는 BuildingBlocks의 추상화(`ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `ISender`)에만 의존한다. 반환 값 없는 Command는 `ICommand : ICommand<Unit>`이고 `Result<Unit>`을 돌려준다.

> **Mediator는 직접 구현한다**([ADR-0015](../03-architecture/adr/0015-custom-mediator-pipeline.md)). MediatR는 v13부터 상용 라이선스라 쓰지 않는다. 디스패처(`ISender`)는 Handler 타입을 캐시하고, 등록되지 않은 Handler는 `InvalidOperationException`을 던진다.

기능 폴더 구조 (Application)

```
EmergencyHub.Employee.Application/
└── Employees/
    ├── Commands/
    │   └── RegisterEmployee/
    │       ├── RegisterEmployeeCommand.cs
    │       ├── RegisterEmployeeCommandHandler.cs
    │       └── RegisterEmployeeCommandValidator.cs
    ├── Queries/
    │   └── GetEmployeeById/
    │       ├── GetEmployeeByIdQuery.cs
    │       ├── GetEmployeeByIdQueryHandler.cs
    │       └── EmployeeResponse.cs
    └── IEmployeeReadRepository.cs
```

```csharp
public sealed record RegisterEmployeeCommand(string Name, string Email, NotificationChannels Channels)
    : ICommand<EmployeeId>;

// 저장 · 커밋은 이 Handler가 아니라 트랜잭션 데코레이터 → IUnitOfWork가 한다(ADR-0014).
// ID는 Handler가 IIdGenerator로 만든다(ADR-0013). IIdGenerator의 메서드 이름은 S02에서 확정한다.
internal sealed class RegisterEmployeeCommandHandler(
    IEmployeeRepository repository,
    IIdGenerator idGenerator,
    TimeProvider timeProvider) : ICommandHandler<RegisterEmployeeCommand, EmployeeId>
{
    public async Task<Result<EmployeeId>> Handle(RegisterEmployeeCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.IsFailure)
        {
            return email.Error;
        }

        // 사전 조회는 1차 방어이고, 동시 요청 경합은 유니크 인덱스(ux_employees_email) → 23505 변환이 막는다.
        if (await repository.ExistsByEmailAsync(email.Value, cancellationToken))
        {
            return EmployeeErrors.EmailAlreadyInUse;
        }

        var id = new EmployeeId(idGenerator.NewId());
        var result = Employee.Register(id, command.Name, email.Value, command.Channels, timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return result.Error;
        }

        repository.Add(result.Value);
        return result.Value.Id;
    }
}
```

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

```csharp
internal sealed class EmployeeRepository(EmployeeDbContext db)
    : RepositoryBase<EmployeeDbContext>(db), IEmployeeRepository
{
    public Task<Employee?> GetByIdAsync(EmployeeId id, CancellationToken cancellationToken) =>
        Db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Db.Employees.AnyAsync(e => e.Email == email, cancellationToken);

    public void Add(Employee employee) => Db.Employees.Add(employee);
}

internal sealed class EmployeeReadRepository(EmployeeReadDbContext db)
    : ReadRepositoryBase<EmployeeReadDbContext>(db), IEmployeeReadRepository
{
    public Task<EmployeeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Db.Employees
            .Where(e => e.Id == new EmployeeId(id))
            .Select(e => new EmployeeResponse(e.Id.Value, e.Name, e.EmployeeStatus, e.NotificationChannels))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<EmployeeSummaryResponse>> SearchAsync(EmployeeSearchCondition condition, CancellationToken cancellationToken) =>
        Db.Employees
            .Where(e => condition.Status == null || e.EmployeeStatus == condition.Status)   // 선택 조건은 람다 안에서
            .Where(e => condition.Channels == NotificationChannels.None
                        || (e.NotificationChannels & condition.Channels) != 0)
            .OrderBy(e => e.Name)
            .Skip(condition.Offset)
            .Take(condition.Limit)
            .Select(e => new EmployeeSummaryResponse(e.Id.Value, e.Name))
            .ToListAsync(cancellationToken);
}
```

## 의존성 주입 (DI) 규칙

**서비스와 Repository는 `Scoped`로, 타입 검색(assembly scanning)을 통해 자동 등록**합니다. `Program.cs`나 `DependencyInjection.cs`에서 구현 타입을 하나씩 등록하지 않습니다.

BuildingBlocks에 등록 기준이 되는 **마커 인터페이스와 기반 클래스**를 둡니다.

| 종류 | 상속 대상 | 등록 |
|---|---|---|
| Write Repository | 인터페이스: `IRepository` 상속 / 구현: `RepositoryBase<TDbContext>` 상속 | Scoped |
| Read Repository | 인터페이스: `IReadRepository` 상속 / 구현: `ReadRepositoryBase<TDbContext>` 상속 | Scoped |
| 서비스 (도메인 서비스, 외부 연동 어댑터 등) | 인터페이스: `IService` 상속 | Scoped |
| Command / Query Handler, Validator | `ICommandHandler<,>` / `IQueryHandler<,>` / `AbstractValidator<T>` | `AddConventionalServices` 안에서 Scoped 등록(Handler는 Scrutor `Scan`, Validator는 `AddValidatorsFromAssemblies`). 데코레이터는 Scrutor `Decorate` |

```csharp
// BuildingBlocks: 마커와 기반 클래스
public interface IRepository;
public interface IReadRepository;
public interface IService;

public abstract class RepositoryBase<TDbContext>(TDbContext db) where TDbContext : DbContext
{
    protected TDbContext Db { get; } = db;
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
- `Singleton` / `Transient`가 필요한 인프라 요소(`TimeProvider`, `HttpClient` 등)는 BuildingBlocks의 공통 등록 코드에서만 등록한다. 서비스 코드에서 직접 등록하지 않는다.
- 등록 누락은 통합 테스트(모든 마커 구현 타입이 컨테이너에서 해석되는지)로 검증하고, `ValidateOnBuild` / `ValidateScopes`를 개발 환경에서 켠다.

> **타입 검색은 Scrutor로 구현한다**([ADR-0017](../03-architecture/adr/0017-scrutor-for-convention-based-di.md), ADR-0010 구체화). Scrutor는 BuildingBlocks.Infrastructure만 참조한다. 같은 서비스 인터페이스를 두 구현이 등록하면 시작 시 실패한다(`RegistrationStrategy.Throw`). Handler는 두 제네릭 인자 형태(`ICommandHandler<,>` / `IQueryHandler<,>`)로만 등록해 데코레이터를 우회하는 경로를 만들지 않는다.

## 비동기 프로그래밍 규칙

- I/O는 끝까지 비동기로 처리한다. `.Result`, `.Wait()`, `GetAwaiter().GetResult()` 금지.
- `async void` 금지(이벤트 핸들러 제외).
- 비동기 메서드는 `CancellationToken`을 **마지막 매개변수**로 받고 하위 호출에 전달한다.
- ASP.NET Core 애플리케이션 코드에서는 `ConfigureAwait(false)`를 쓰지 않는다.
- 결과를 그대로 반환하는 메서드도 `async` / `await`로 쓴다(스택 추적 보존). 성능이 입증된 핫패스만 예외.

## 예외 처리 규칙

- **예상 가능한 실패는 예외가 아니라 `Result`로 반환한다.** 비즈니스 규칙 위반, 검증 실패, 대상 없음, 충돌이 여기에 해당한다.
- 예외는 예상하지 못한 오류(인프라 장애, 프로그래밍 오류)에만 쓴다.
- `Error`는 **정수 코드**와 메시지, 유형(검증 / 없음 / 충돌 / 규칙 위반 / 권한)을 가진다. 코드 범위는 서비스별로 나누며 [에러 코드](../05-api/error-codes.md)에서 관리한다.
- API는 `Result`를 RFC 9457 `ProblemDetails`로 변환한다. 전역 예외 처리(`IExceptionHandler`)는 예상하지 못한 예외만 500으로 변환하고 로그를 남긴다.
- `catch (Exception)`으로 삼키지 않는다. 잡았으면 처리하거나 로그와 함께 다시 던진다.

## DDD 구현 규칙 (Aggregate / Value Object)

- Aggregate Root는 **팩토리 메서드**(`Register`, `Create`)로만 생성한다. 생성자는 `private`.
- 속성 setter는 `private`. 상태는 의미 있는 도메인 메서드(`ChangeStatus`, `AssignTo`)로만 바꾼다.
- 불변식은 Aggregate 안에서 검증하고, 위반 시 `Result` 실패를 반환한다.
- 다른 Aggregate는 **ID로만** 참조한다.
- ID는 강타입 `record struct`(`EmployeeId`)를 쓴다.
- Value Object는 `record`로 만들고, 생성 시 검증한다(`Email.Create(string)` → `Result<Email>`).
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
