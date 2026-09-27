using Swashbuckle.AspNetCore.SwaggerGen;

namespace EmergencyHub.ArchitectureTests.Samples.InfrastructureAspNetCoreDependencies;

/// <summary>위반 예: Swashbuckle(웹 API 문서화) 형식을 쓴다.</summary>
public sealed class SwaggerOptionsInfrastructureType
{
    /// <summary>Swagger 옵션.</summary>
    public SwaggerGenOptions? Options { get; init; }
}
