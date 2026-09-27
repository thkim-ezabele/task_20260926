using EmergencyHub.BuildingBlocks.Application.Cqrs;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.ArchitectureTests.Samples.ControllerDependencies;

/// <summary>규칙을 지킨 예: Controller가 <see cref="ISender"/>만 쓴다(ADR-0016 얇은 Controller).</summary>
/// <param name="sender">디스패처.</param>
[ApiController]
[Route("api/v1/sender-only-samples")]
public sealed class SenderOnlySampleController(ISender sender) : ControllerBase
{
    /// <summary>디스패처.</summary>
    public ISender Sender { get; } = sender;
}
