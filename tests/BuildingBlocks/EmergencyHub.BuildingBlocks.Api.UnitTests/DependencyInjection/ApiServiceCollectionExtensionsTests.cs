using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using EmergencyHub.BuildingBlocks.Api.DependencyInjection;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Api.Exceptions;
using EmergencyHub.BuildingBlocks.Api.OpenApi;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.DependencyInjection;

// 공개 진입점 1: AddBuildingBlocksApi(ADR-0024). Controller 기본 설정(ADR-0016), 바인딩 오류 1001, 전역 예외 처리기, Swashbuckle(ADR-0019).
public sealed class ApiServiceCollectionExtensionsTests
{
    private const string Title = "Sample API";

    // ---- 성공 ----

    [Fact]
    public async Task AddBuildingBlocksApi_ConfiguresControllerOptionsFromAdr0016()
    {
        await using var provider = CreateProvider();

        var mvc = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        mvc.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes.Should().BeTrue();
        var json = provider.GetRequiredService<IOptions<MvcJsonOptions>>().Value.JsonSerializerOptions;
        json.Converters.Should().NotContain(converter => converter is JsonStringEnumConverter);
        json.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.Never);
        json.PropertyNamingPolicy.Should().NotBeNull();
    }

    [Fact]
    public async Task AddBuildingBlocksApi_InvalidModelStateResponseFactory_Returns1001Problem()
    {
        await using var provider = CreateProvider();
        var factory = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value.InvalidModelStateResponseFactory;
        var context = new ActionContext(HttpContexts.Create(services: provider), new RouteData(), new ActionDescriptor());
        context.ModelState.AddModelError("$.email", "invalid");

        var result = factory(context).Should().BeOfType<ObjectResult>().Subject;

        result.StatusCode.Should().Be(400);
        result.Value.Should().BeOfType<ProblemDetails>().Which.Extensions[ErrorProblemDetails.CodeExtension].Should().Be(1001);
    }

    [Fact]
    public async Task AddBuildingBlocksApi_ClientErrorFactory_Maps415To1005AndWrapsFrameworkFactoryOnce()
    {
        // ADR-0028: [FromBody] 액션에 Content-Type 없음 415(ClientErrorFactory 경로)는 1005. 프레임워크 기본 팩토리는 안쪽에 남긴다(등록 1개).
        var services = CreateServices();
        await using var provider = services.BuildServiceProvider();

        services.Count(descriptor => descriptor.ServiceType == typeof(IClientErrorFactory)).Should().Be(1);
        var factory = provider.GetRequiredService<IClientErrorFactory>().Should().BeOfType<UnsupportedMediaTypeClientErrorFactory>().Subject;
        var context = new ActionContext(HttpContexts.Create(services: provider), new RouteData(), new ActionDescriptor());
        factory.GetClientError(context, new UnsupportedMediaTypeResult()).Should().BeOfType<ErrorProblemResult>()
            .Which.Error.Code.Should().Be(1005);
        var notFound = factory.GetClientError(context, new NotFoundResult()).Should().BeOfType<ObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
        notFound.Value.Should().BeOfType<ProblemDetails>().Which.Extensions.Should().NotContainKey(ErrorProblemDetails.CodeExtension);
    }

    [Fact]
    public async Task AddBuildingBlocksApi_RegistersGlobalExceptionHandlerAsSingleSingleton()
    {
        await using var provider = CreateProvider();

        provider.GetServices<IExceptionHandler>().Should().ContainSingle().Which.Should().BeOfType<GlobalExceptionHandler>();
        provider.GetRequiredService<IExceptionHandler>().Should().BeSameAs(provider.GetRequiredService<IExceptionHandler>());
    }

    [Fact]
    public async Task AddBuildingBlocksApi_ExceptionHandlerUsesRegisteredClassifiers()
    {
        var classifier = Substitute.For<IExceptionClassifier>();
        classifier.Classify(Arg.Any<Exception>()).Returns(CommonErrors.TemporarilyUnavailable);
        var services = CreateServices();
        services.AddSingleton(classifier);
        await using var provider = services.BuildServiceProvider();
        var context = HttpContexts.Create(services: provider);

        await provider.GetRequiredService<IExceptionHandler>().TryHandleAsync(context, new TimeoutException(), CancellationToken.None);

        context.Response.StatusCode.Should().Be(503);
    }

    [Fact]
    public void AddBuildingBlocksApi_RegistersScopedSenderFromApplication()
    {
        // Controller는 ISender만 받는다(ADR-0016). AddConventionalServices는 부르지 않는다(Infrastructure 비참조, ADR-0024).
        CreateServices().Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(ISender))
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task AddBuildingBlocksApi_ConfiguresSwaggerDocumentV1AndSchemaFilters()
    {
        await using var provider = CreateProvider();

        var swagger = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value;
        swagger.SwaggerGeneratorOptions.SwaggerDocs.Should().ContainKey(ApiServiceCollectionExtensions.DocumentName);
        swagger.SwaggerGeneratorOptions.SwaggerDocs[ApiServiceCollectionExtensions.DocumentName].Title.Should().Be(Title);
        swagger.SwaggerGeneratorOptions.SwaggerDocs[ApiServiceCollectionExtensions.DocumentName].Version.Should().Be("v1");
        swagger.SchemaFilterDescriptors.Select(descriptor => descriptor.Type)
            .Should().Contain([typeof(IntegerEnumSchemaFilter), typeof(ProblemDetailsSchemaFilter)]);
    }

    [Fact]
    public async Task AddBuildingBlocksApi_SuppressesFrameworkExceptionHandlerMiddlewareLog()
    {
        // 프레임워크 미들웨어도 이벤트 ID 1(Error)로 원본 예외(메시지 포함)를 남긴다. 이벤트 ID 1은 전역 예외 처리기에서만 쓴다.
        await using var provider = CreateProvider();

        var rules = provider.GetRequiredService<IOptions<LoggerFilterOptions>>().Value.Rules;
        rules.Should().Contain(rule =>
            rule.CategoryName == ApiServiceCollectionExtensions.ExceptionHandlerMiddlewareCategory
            && rule.LogLevel == LogLevel.None
            && rule.ProviderName == null);
    }

    [Fact]
    public void AddBuildingBlocksApi_ReturnsSameCollection()
    {
        var services = new ServiceCollection();

        services.AddBuildingBlocksApi(Title).Should().BeSameAs(services);
    }

    // ---- 실패 ----

    [Fact]
    public void AddBuildingBlocksApi_CalledTwice_ThrowsInvalidOperationException()
    {
        // 두 번 부르면 Swagger 문서 v1이 중복되고 예외 처리기가 두 개가 된다.
        var services = CreateServices();

        var act = () => services.AddBuildingBlocksApi(Title);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddBuildingBlocksApi_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddBuildingBlocksApi(Title);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddBuildingBlocksApi_BlankTitle_ThrowsArgumentException(string title)
    {
        var act = () => new ServiceCollection().AddBuildingBlocksApi(title);

        act.Should().Throw<ArgumentException>().WithParameterName("apiTitle");
    }

    [Fact]
    public void AddBuildingBlocksApi_NullTitle_ThrowsArgumentNullException()
    {
        var act = () => new ServiceCollection().AddBuildingBlocksApi(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("apiTitle");
    }

    [Fact]
    public async Task CreateInner_DescriptorWithoutInstanceFactoryOrType_ThrowsInvalidOperationException()
    {
        // 공개 생성자로는 만들 수 없는 등록(인스턴스 · 팩토리 · 형식이 모두 없음)은 null-forgiving 없이 명시적으로 실패한다.
        var descriptor = (ServiceDescriptor)RuntimeHelpers.GetUninitializedObject(typeof(ServiceDescriptor));
        await using var provider = new ServiceCollection().BuildServiceProvider();

        var act = () => ApiServiceCollectionExtensions.CreateInner(provider, descriptor);

        act.Should().Throw<InvalidOperationException>().WithMessage("*IClientErrorFactory*");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task AddBuildingBlocksApi_WebHost_PassesValidateOnBuildAndValidateScopes()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
        builder.Services.AddBuildingBlocksApi(Title);

        var act = builder.Build;

        await using var app = act.Should().NotThrow().Subject;
        app.Services.GetRequiredService<IExceptionHandler>().Should().BeOfType<GlobalExceptionHandler>();
    }

    [Fact]
    public async Task AddBuildingBlocksApi_ClassifierRegisteredAfterward_IsStillUsed()
    {
        // 등록 순서(Api 먼저, Infrastructure 나중)와 무관하게 분류기를 해석한다.
        var classifier = Substitute.For<IExceptionClassifier>();
        classifier.Classify(Arg.Any<Exception>()).Returns(CommonErrors.TemporarilyUnavailable);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocksApi(Title);
        services.AddSingleton(classifier);
        await using var provider = services.BuildServiceProvider();
        var context = HttpContexts.Create(services: provider);

        await provider.GetRequiredService<IExceptionHandler>().TryHandleAsync(context, new TimeoutException(), CancellationToken.None);

        context.Response.StatusCode.Should().Be(503);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocksApi(Title);
        return services;
    }

    private static ServiceProvider CreateProvider() => CreateServices().BuildServiceProvider();
}
