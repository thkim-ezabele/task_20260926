using EmergencyHub.BuildingBlocks.Application.Cqrs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.Api.UnitTests.TestDoubles;

/// <summary>
/// 운영 등록(<see cref="Program.ConfigureServices"/> · <see cref="Program.ConfigurePipeline"/>)으로 만든 Api를 TestServer로 띄웁니다.
/// <see cref="ISender"/>만 대역으로 바꾸므로 DB에 연결하지 않고 라우팅 · 필터 · 바인더 · 예외 처리 전 구간을 지납니다.
/// </summary>
/// <remarks>TestServer는 <c>RequestSizeLimit</c>(<c>IHttpMaxRequestBodySizeFeature</c>)를 적용하지 않습니다. Kestrel 413은 S06-T06 범위입니다.</remarks>
internal sealed class EmployeeApiTestHost : IAsyncDisposable
{
    private const string DummyWrite = "Host=localhost;Database=emergency_hub_employee;Username=employee_app";
    private const string DummyRead = "Host=localhost;Database=emergency_hub_employee;Username=employee_app;Options=-c default_transaction_read_only=on";

    private readonly string _contentRoot;
    private readonly WebApplication _app;

    private EmployeeApiTestHost(string contentRoot, WebApplication app, HttpClient client)
    {
        _contentRoot = contentRoot;
        _app = app;
        Client = client;
    }

    /// <summary>TestServer 클라이언트.</summary>
    public HttpClient Client { get; }

    /// <summary>호스트를 만들고 시작합니다.</summary>
    /// <param name="sender">Controller가 받을 <see cref="ISender"/> 대역.</param>
    /// <param name="environment">환경 이름. Swagger는 Development에서만 노출됩니다.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    public static async Task<EmployeeApiTestHost> StartAsync(ISender sender, string environment, CancellationToken cancellationToken)
    {
        // 테스트 출력 폴더의 appsettings.json(파일 싱크)을 읽지 않도록 빈 폴더를 콘텐츠 루트로 쓴다(ProgramTests와 같음).
        var contentRoot = Directory.CreateTempSubdirectory("employee-api-host-").FullName;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
            ContentRootPath = contentRoot,
            ApplicationName = typeof(Program).Assembly.GetName().Name,
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Write"] = DummyWrite,
            ["ConnectionStrings:Read"] = DummyRead,
        });

        Program.ConfigureServices(builder);
        builder.Services.AddSingleton(sender);

        var app = builder.Build();
        Program.ConfigurePipeline(app);
        await app.StartAsync(cancellationToken);
        return new EmployeeApiTestHost(contentRoot, app, app.GetTestClient());
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
        Directory.Delete(_contentRoot, recursive: true);
    }
}
