namespace EmergencyHub.Employee.IntegrationTests.Http;

/// <summary>
/// Kestrel 호스트로 요청을 보내고, 서버가 연결을 먼저 닫은 경우를 예외 대신 결과로 돌려줍니다(S06-T06 1 MiB 초과 판정).
/// </summary>
/// <remarks>
/// 판정 방법: 클라이언트 쪽은 <see cref="KestrelSendOutcome.Response"/>(413 응답을 읽음) 또는 <see cref="KestrelSendOutcome.ConnectionError"/>(업로드 중 끊김) 중 하나,
/// 서버 쪽 최종 상태는 <see cref="Fixtures.SerilogEventCollector.RequestStatusCodes"/>(요청 완료 로그), 저장 결과는 DB 행 수로 봅니다.
/// 취소(<see cref="OperationCanceledException"/>)와 그 밖의 예외는 그대로 전파합니다.
/// </remarks>
public static class KestrelSend
{
    /// <summary>요청을 보냅니다(응답 헤더까지 읽음).</summary>
    /// <param name="client">Kestrel 클라이언트(<see cref="Fixtures.EmployeeApiFactory.CreateKestrelClient"/>).</param>
    /// <param name="request">요청.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>응답 또는 연결 끊김.</returns>
    public static async Task<KestrelSendOutcome> SendAsync(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            return new KestrelSendOutcome(response, ConnectionError: null);
        }
        catch (HttpRequestException exception)
        {
            return new KestrelSendOutcome(Response: null, exception);
        }
    }
}
