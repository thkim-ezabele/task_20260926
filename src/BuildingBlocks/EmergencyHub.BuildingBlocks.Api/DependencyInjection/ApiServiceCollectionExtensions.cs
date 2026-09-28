using System.Text.Json.Serialization;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Api.Exceptions;
using EmergencyHub.BuildingBlocks.Api.OpenApi;
using EmergencyHub.BuildingBlocks.Api.Validation;
using EmergencyHub.BuildingBlocks.Application.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace EmergencyHub.BuildingBlocks.Api.DependencyInjection;

/// <summary>
/// 공통 API 처리의 서비스 등록 진입점입니다(ADR-0024 공개 진입점 1). 서비스 Api 호스트의 <c>Program.cs</c>가 한 번 부릅니다.
/// </summary>
/// <remarks>
/// <para>서비스 Api 호스트의 등록 순서 예(서비스 Infrastructure 등록은 S03):</para>
/// <code>
/// builder.Services.AddBuildingBlocksInfrastructure();                  // IIdGenerator, 예외 분류기, ISender, TimeProvider
/// builder.Services.AddConventionalServices(applicationAssembly, infrastructureAssembly); // 한 번만
/// builder.Services.AddWriteDbContext&lt;EmployeeDbContext&gt;(builder.Configuration.GetConnectionString("Write"));
/// builder.Services.AddReadDbContext&lt;EmployeeReadDbContext&gt;(builder.Configuration.GetConnectionString("Read"));
/// builder.Services.AddUnitOfWork&lt;EmployeeDbContext&gt;(errors =&gt; ...);
/// builder.Services.AddBuildingBlocksApi("Employee API");               // 이 메서드
/// var app = builder.Build();
/// app.UseBuildingBlocksApi();
/// </code>
/// <para>
/// 이 메서드는 <c>AddConventionalServices</c>를 부르지 않습니다(BuildingBlocks.Api는 Infrastructure를 참조하지 않음, ADR-0024).
/// 예외 분류기는 등록 순서와 무관하게 처리기 생성 시점에 모두 해석합니다.
/// </para>
/// </remarks>
public static class ApiServiceCollectionExtensions
{
    /// <summary>OpenAPI 문서 이름입니다. 경로의 주 버전(<c>/api/v1</c>)과 맞춥니다(ADR-0019).</summary>
    public const string DocumentName = "v1";

    /// <summary>
    /// 프레임워크 <c>ExceptionHandlerMiddleware</c>의 로그 범주입니다. 이 미들웨어는 처리기를 부르기 전에 원본 예외(메시지 포함)를
    /// 이벤트 ID 1 · Error로 남기므로 이 범주를 끕니다. 이벤트 ID 1은 전역 예외 처리기에서만 씁니다(error-codes "공통 하위 범위").
    /// </summary>
    public const string ExceptionHandlerMiddlewareCategory = "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware";

    /// <summary>
    /// Controller 기본 설정, 바인딩 오류 응답(1001), 전역 예외 처리기, Swashbuckle, Application 공통 등록(<c>ISender</c>)을 합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션.</param>
    /// <param name="apiTitle">OpenAPI 문서 제목(예: <c>Employee API</c>).</param>
    /// <returns>같은 <paramref name="services"/>(체이닝용).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 또는 <paramref name="apiTitle"/>이 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException"><paramref name="apiTitle"/>이 비었거나 공백만 있는 경우.</exception>
    /// <exception cref="InvalidOperationException">이미 부른 경우(문서 <c>v1</c> · 예외 처리기 중복).</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Controller(ADR-0016): <c>SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true</c>(필수 값은 Validator),
    /// <c>InvalidModelStateResponseFactory</c> → 1001 <c>ProblemDetails</c>, System.Text.Json 웹 기본값(camelCase, 정수 enum,
    /// <c>JsonStringEnumConverter</c> 없음, <c>null</c> 속성 생략 안 함).</description></item>
    /// <item><description>클라이언트 오류 결과 변환(<c>IClientErrorFactory</c>)을 감싸 <c>415</c>만 <c>1005</c> <c>ProblemDetails</c>로(ADR-0028). 그 밖은 프레임워크 기본 그대로.</description></item>
    /// <item><description>전역 예외 처리기(<c>IExceptionHandler</c>, Singleton) + 프레임워크 예외 미들웨어 자체 로그 끄기(Microsoft.Extensions.Logging 필터).
    /// Serilog를 쓰는 호스트는 같은 범주를 Serilog <c>MinimumLevel.Override</c>로도 꺼야 합니다(ServiceDefaults).</description></item>
    /// <item><description>Swashbuckle(ADR-0019): 문서 <c>v1</c>, 정수 enum 설명 필터, <c>ProblemDetails</c> 확장 필드 스키마 필터. 노출은 <c>UseBuildingBlocksApi</c>가 Development에서만 합니다.</description></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddBuildingBlocksApi(this IServiceCollection services, string apiTitle)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiTitle);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(ApiRegistrationMarker)))
        {
            throw new InvalidOperationException(
                "AddBuildingBlocksApi는 한 번만 부릅니다. 두 번 부르면 OpenAPI 문서 v1과 전역 예외 처리기가 중복됩니다.");
        }

        services.AddSingleton<ApiRegistrationMarker>();
        services.AddBuildingBlocksApplication();

        services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponses.Create)
            .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never);
        DecorateClientErrorFactory(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IExceptionHandler, GlobalExceptionHandler>());
        services.AddLogging(logging => logging.AddFilter(ExceptionHandlerMiddlewareCategory, LogLevel.None));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo { Title = apiTitle, Version = DocumentName });
            options.SchemaFilter<IntegerEnumSchemaFilter>();
            options.SchemaFilter<ProblemDetailsSchemaFilter>();
        });

        return services;
    }

    /// <summary>
    /// <c>AddControllers</c>가 등록한 프레임워크 기본 <see cref="IClientErrorFactory"/>를 <see cref="UnsupportedMediaTypeClientErrorFactory"/>로 감쌉니다.
    /// 등록은 하나로 유지하고, 기본 팩토리는 등록 방식(형식 · 팩토리 · 인스턴스) 그대로 안쪽에 만듭니다.
    /// </summary>
    private static void DecorateClientErrorFactory(IServiceCollection services)
    {
        var descriptor = services.Last(candidate => candidate.ServiceType == typeof(IClientErrorFactory));
        services.Remove(descriptor);
        services.AddSingleton<IClientErrorFactory>(provider => new UnsupportedMediaTypeClientErrorFactory(CreateInner(provider, descriptor)));
    }

    private static IClientErrorFactory CreateInner(IServiceProvider provider, ServiceDescriptor descriptor) =>
        (IClientErrorFactory)(descriptor.ImplementationInstance
            ?? descriptor.ImplementationFactory?.Invoke(provider)
            ?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!));
}
