using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;

namespace EmergencyHub.ArchitectureTests.Samples.BuildingBlocksApiDependencies;

/// <summary>규칙을 지킨 예: ASP.NET Core와 Domain만 쓴다(BuildingBlocks.Api 허용).</summary>
public sealed class ResultMappingApiType
{
    /// <summary>표본 메서드.</summary>
    /// <param name="context">HTTP 컨텍스트.</param>
    /// <param name="error">오류.</param>
    public static void Apply(HttpContext context, Error error) => context.Response.StatusCode = error.Code;
}
