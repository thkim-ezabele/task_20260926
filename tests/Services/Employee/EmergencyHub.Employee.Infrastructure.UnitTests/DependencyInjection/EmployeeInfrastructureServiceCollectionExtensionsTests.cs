using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;
using EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.Infrastructure.Persistence.ReadRepositories;
using EmergencyHub.Employee.Infrastructure.Persistence.Repositories;
using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.DependencyInjection;

// S03-T02: AddEmployeeInfrastructure = 공통 인프라 → 규칙 기반 등록(Application · Infrastructure 어셈블리) → 쓰기 · 읽기 DbContext(재시도 인자)
// → UnitOfWork(ux_employees_normalized_email → 23001, S05-T04). MigrationService용 AddEmployeeWriteDbContext는 쓰기만, 같은 재시도 인자 경로(dba 구현 사양 5).
// 연결은 열지 않는다(더미 연결 문자열).
[Trait("FR", "PRD-001/FR-06")]
[Trait("FR", "PRD-001/FR-08")]
public sealed class EmployeeInfrastructureServiceCollectionExtensionsTests
{
    private const string Connection = EmployeeDbContexts.DummyConnectionString;

    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    // ---- AddEmployeeInfrastructure ----

    [Fact]
    public void AddEmployeeInfrastructure_Called_ResolvesRepositoriesContextsUnitOfWorkAndSender()
    {
        using var provider = CreateServices().AddEmployeeInfrastructure(Configuration()).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Should().BeOfType<EmployeeRepository>();
        scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>().Should().BeOfType<EmployeeReadRepository>();
        scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<EmployeeReadDbContext>().ChangeTracker.QueryTrackingBehavior
            .Should().Be(QueryTrackingBehavior.NoTracking);
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ISender>().Should().NotBeNull();
    }

    [Fact]
    public void AddEmployeeInfrastructure_Called_RegistersRepositoriesAsScoped()
    {
        var services = CreateServices().AddEmployeeInfrastructure(Configuration());

        services.Should().ContainSingle(d => d.ServiceType == typeof(IEmployeeRepository)).Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
        services.Should().ContainSingle(d => d.ServiceType == typeof(IEmployeeReadRepository)).Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEmployeeInfrastructure_EmployeeApplication_RegistersRegisterListAndNameHandlersAndValidatorsScoped()
    {
        // S06-T04: 일괄 등록 Command Handler · Validator가 규칙 기반 등록(Scrutor · FluentValidation 어셈블리 검색)으로 Scoped 등록된다.
        // S07-T01 · T02: 목록 · 이름 조회 Query Handler · Validator도 같은 경로로 등록된다. AddConventionalServices는 한 번만 부를 수 있어 Application 어셈블리도 여기서 넘긴다.
        var services = CreateServices().AddEmployeeInfrastructure(Configuration());

        // 데코레이터(Scrutor)가 원래 등록을 키 있는 서비스로 옮기므로 키 없는 등록만 센다.
        services.Where(d => !d.IsKeyedService && d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
            .Should().ContainSingle()
            .Which.Should().Match<ServiceDescriptor>(d =>
                d.ServiceType == typeof(ICommandHandler<RegisterEmployeesCommand, RegisterEmployeesResponse>) && d.Lifetime == ServiceLifetime.Scoped);
        services.Where(d => !d.IsKeyedService && d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
            .Should().HaveCount(2)
            .And.Contain(d => d.ServiceType == typeof(IQueryHandler<ListEmployeesQuery, ListEmployeesResponse>) && d.Lifetime == ServiceLifetime.Scoped)
            .And.Contain(d => d.ServiceType == typeof(IQueryHandler<GetEmployeeByNameQuery, EmployeeResponse>) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().ContainSingle(d => d.ServiceType == typeof(IValidator<RegisterEmployeesCommand>))
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
        services.Should().ContainSingle(d => d.ServiceType == typeof(IValidator<ListEmployeesQuery>))
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
        services.Should().ContainSingle(d => d.ServiceType == typeof(IValidator<GetEmployeeByNameQuery>))
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEmployeeInfrastructure_GetEmployeeByNameHandler_ResolvesDecoratedInScope()
    {
        using var provider = CreateServices().AddEmployeeInfrastructure(Configuration()).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetEmployeeByNameQuery, EmployeeResponse>>();

        handler.GetType().Name.Should().StartWith("LoggingQueryHandlerDecorator");
        scope.ServiceProvider.GetRequiredService<IValidator<GetEmployeeByNameQuery>>().GetType().Name
            .Should().Be("GetEmployeeByNameQueryValidator");
    }

    [Fact]
    public void AddEmployeeInfrastructure_ListEmployeesHandler_ResolvesDecoratedInScope()
    {
        using var provider = CreateServices().AddEmployeeInfrastructure(Configuration()).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<ListEmployeesQuery, ListEmployeesResponse>>();

        // Query는 로깅 → 검증 → Handler다(트랜잭션 없음, ADR-0015).
        handler.GetType().Name.Should().StartWith("LoggingQueryHandlerDecorator");
        scope.ServiceProvider.GetRequiredService<IValidator<ListEmployeesQuery>>().GetType().Name
            .Should().Be("ListEmployeesQueryValidator");
    }

    [Fact]
    public void AddEmployeeInfrastructure_RegisterEmployeesHandler_ResolvesDecoratedInScope()
    {
        using var provider = CreateServices().AddEmployeeInfrastructure(Configuration()).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RegisterEmployeesCommand, RegisterEmployeesResponse>>();

        // 가장 바깥은 로깅 데코레이터다(로깅 → 검증 → 트랜잭션 → Handler, ADR-0015).
        handler.GetType().Name.Should().StartWith("LoggingCommandHandlerDecorator");
        scope.ServiceProvider.GetRequiredService<IValidator<RegisterEmployeesCommand>>().GetType().Name
            .Should().Be("RegisterEmployeesCommandValidator");
    }

    [Fact]
    public void AddEmployeeInfrastructure_UniqueConstraintRegistry_MapsOnlyNormalizedEmailIndexToDuplicateEmailInstance()
    {
        using var provider = CreateServices().AddEmployeeInfrastructure(Configuration()).BuildServiceProvider(StrictOptions);

        var registry = provider.GetRequiredService<UniqueConstraintErrorRegistry>();

        registry.IndexNames.Should().Equal(EmployeeDbNames.NormalizedEmailUniqueIndex);
        registry.IndexNames.Select(name => name.Value).Should().Equal("ux_employees_normalized_email");
        registry.Find("ux_employees_normalized_email").Should().BeSameAs(EmployeeErrors.DuplicateEmail);
        EmployeeErrors.DuplicateEmail.Code.Should().Be(23001);
    }

    [Fact]
    public void AddEmployeeInfrastructure_UniqueConstraintRegistry_KeysAreAllUniqueIndexNamesOfModel()
    {
        // 레지스트리 키와 매핑의 HasUniqueIndex가 같은 상수라 모델 인덱스 이름에 모두 있어야 한다(오타 · 잘림이면 23505가 3003이 됨).
        using var provider = CreateServices().AddEmployeeInfrastructure(Configuration()).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();
        var model = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().GetService<IDesignTimeModel>().Model;
        var uniqueIndexNames = model.GetEntityTypes().SelectMany(type => type.GetIndexes()).Where(index => index.IsUnique)
            .Select(index => index.GetDatabaseName());

        var registry = provider.GetRequiredService<UniqueConstraintErrorRegistry>();

        registry.IndexNames.Select(name => name.Value).Should().NotBeEmpty().And.BeSubsetOf(uniqueIndexNames);
    }

    [Theory]
    [InlineData("pk_employees")]
    [InlineData("UX_EMPLOYEES_NORMALIZED_EMAIL")]
    [InlineData("ux_employees_normalized_email ")]
    [InlineData("ix_employees_name_joined_on_id")]
    public void AddEmployeeInfrastructure_UniqueConstraintRegistry_OtherOrNonExactNames_AreNotMapped(string constraintName)
    {
        // 실패 · 엣지: 매핑 없는 제약(pk_ · ix_)과 대소문자 · 공백이 다른 이름은 매핑되지 않는다(→ 3003, Ordinal 비교).
        using var provider = CreateServices().AddEmployeeInfrastructure(Configuration()).BuildServiceProvider(StrictOptions);

        provider.GetRequiredService<UniqueConstraintErrorRegistry>().Find(constraintName).Should().BeNull();
    }

    [Fact]
    public void AddEmployeeInfrastructure_WithoutRetry_BothContextsUseNpgsqlDefaultRetrySettings()
    {
        using var provider = CreateServices().AddEmployeeInfrastructure(Configuration()).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        MaxRetryCount(scope.ServiceProvider.GetRequiredService<EmployeeDbContext>()).Should().Be(6);
        MaxRetryCount(scope.ServiceProvider.GetRequiredService<EmployeeReadDbContext>()).Should().Be(6);
    }

    [Fact]
    public void AddEmployeeInfrastructure_WithRetry_BothContextsUseGivenRetrySettings()
    {
        using var provider = CreateServices()
            .AddEmployeeInfrastructure(Configuration(), new DbRetryOptions(3, TimeSpan.FromSeconds(5)))
            .BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        MaxRetryCount(scope.ServiceProvider.GetRequiredService<EmployeeDbContext>()).Should().Be(3);
        MaxRetryCount(scope.ServiceProvider.GetRequiredService<EmployeeReadDbContext>()).Should().Be(3);
    }

    [Fact]
    public void AddEmployeeInfrastructure_Resolved_UsesConfiguredConnectionStrings()
    {
        const string ReadConnection = "Host=localhost;Database=emergency_hub_employee;Username=employee_app;Options=-c default_transaction_read_only=on";
        using var provider = CreateServices().AddEmployeeInfrastructure(Configuration(read: ReadConnection)).BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().Database.GetConnectionString().Should().Be(Connection);
        scope.ServiceProvider.GetRequiredService<EmployeeReadDbContext>().Database.GetConnectionString().Should().Be(ReadConnection);
    }

    [Fact]
    public void AddEmployeeInfrastructure_MissingWriteConnectionString_ThrowsNamingTheKey()
    {
        var act = () => CreateServices().AddEmployeeInfrastructure(Configuration(write: null));

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings:Write*");
    }

    [Fact]
    public void AddEmployeeInfrastructure_MissingReadConnectionString_ThrowsNamingTheKey()
    {
        var act = () => CreateServices().AddEmployeeInfrastructure(Configuration(read: " "));

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings:Read*");
    }

    [Fact]
    public void AddEmployeeInfrastructure_CalledTwice_ThrowsInvalidOperationException()
    {
        var services = CreateServices().AddEmployeeInfrastructure(Configuration());

        var act = () => services.AddEmployeeInfrastructure(Configuration());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddEmployeeInfrastructure_NullArguments_ThrowArgumentNullException()
    {
        var nullServices = () => EmployeeInfrastructureServiceCollectionExtensions.AddEmployeeInfrastructure(null!, Configuration());
        var nullConfiguration = () => CreateServices().AddEmployeeInfrastructure(null!);

        nullServices.Should().Throw<ArgumentNullException>();
        nullConfiguration.Should().Throw<ArgumentNullException>();
    }

    // ---- AddEmployeeWriteDbContext (MigrationService) ----

    [Fact]
    public void AddEmployeeWriteDbContext_WithoutReadConnection_RegistersOnlyWriteContext()
    {
        var services = new ServiceCollection().AddEmployeeWriteDbContext(Configuration(read: null));
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().Database.IsNpgsql().Should().BeTrue();
        scope.ServiceProvider.GetService<EmployeeReadDbContext>().Should().BeNull();
        services.Should().NotContain(d => d.ServiceType == typeof(IUnitOfWork));
    }

    [Fact]
    public void AddEmployeeWriteDbContext_WithRetry_UsesGivenRetrySettings()
    {
        using var provider = new ServiceCollection()
            .AddEmployeeWriteDbContext(Configuration(), new DbRetryOptions(0, TimeSpan.Zero))
            .BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        MaxRetryCount(scope.ServiceProvider.GetRequiredService<EmployeeDbContext>()).Should().Be(0);
    }

    [Fact]
    public void AddEmployeeWriteDbContext_MissingWriteConnectionString_ThrowsNamingTheKey()
    {
        var act = () => new ServiceCollection().AddEmployeeWriteDbContext(Configuration(write: ""));

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings:Write*");
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    private static IConfiguration Configuration(string? write = Connection, string? read = Connection) =>
        ConnectionStringsConfiguration.Create(write, read);

    private static int MaxRetryCount(DbContext context) =>
        (int)typeof(ExecutionStrategy)
            .GetProperty("MaxRetryCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!
            .GetValue(context.Database.CreateExecutionStrategy())!;
}
