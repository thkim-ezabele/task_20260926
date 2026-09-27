namespace EmergencyHub.AppHost;

/// <summary>
/// AppHost 진입점입니다. <c>dotnet run --project src/Aspire/EmergencyHub.AppHost</c> 한 번으로 로컬 리소스를 띄웁니다(PRD-001 FR-03 · NFR-04).
/// </summary>
internal static class Program
{
    /// <summary>애플리케이션 모델을 구성하고 실행합니다.</summary>
    /// <param name="args">명령줄 인자.</param>
    /// <returns>AppHost가 끝날 때 완료되는 작업.</returns>
    public static async Task Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        builder.AddEmergencyHub(PostgresImageTag.Read(typeof(Program).Assembly));

        await using var app = builder.Build();
        await app.RunAsync();
    }
}
