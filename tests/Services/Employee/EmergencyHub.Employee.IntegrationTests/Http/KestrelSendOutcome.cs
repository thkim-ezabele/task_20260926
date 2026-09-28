namespace EmergencyHub.Employee.IntegrationTests.Http;

/// <summary>
/// 실제 소켓으로 보낸 요청의 클라이언트 쪽 결과입니다(<see cref="KestrelSend"/>). 둘 중 정확히 하나가 있습니다.
/// </summary>
/// <param name="Response">응답을 받았으면 응답(호출자가 폐기).</param>
/// <param name="ConnectionError">응답 전에 연결이 끊겼으면 그 예외(예: 서버가 413을 쓰고 업로드 중 연결을 닫음).</param>
public sealed record KestrelSendOutcome(HttpResponseMessage? Response, HttpRequestException? ConnectionError);
