using Serilog.Events;

namespace EmergencyHub.Employee.IntegrationTests.Http;

/// <summary>로그 이벤트 하나에서 찾은 입력 값 하나입니다(<see cref="PersonalDataScan.FindInLogs"/>, NFR-04).</summary>
/// <param name="EventId">이벤트 ID(<c>EventId.Id</c>). 없으면 <see langword="null"/>.</param>
/// <param name="Level">로그 수준.</param>
/// <param name="SourceContext">로거 범주.</param>
/// <param name="MessageTemplate">메시지 템플릿.</param>
/// <param name="Value">찾은 입력 값.</param>
public sealed record LogExposure(int? EventId, LogEventLevel Level, string SourceContext, string MessageTemplate, string Value);
