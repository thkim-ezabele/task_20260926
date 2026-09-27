namespace EmergencyHub.BuildingBlocks.Api.DependencyInjection;

/// <summary>
/// <see cref="ApiServiceCollectionExtensions.AddBuildingBlocksApi"/>를 이미 불렀는지 표시하는 등록입니다(중복 호출 검출용).
/// </summary>
internal sealed class ApiRegistrationMarker;
