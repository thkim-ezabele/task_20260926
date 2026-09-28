using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Api.UnitTests.TestDoubles;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace EmergencyHub.Employee.Api.UnitTests.Acceptance;

// S07-T02 tester 보강(PRD-002 FR-08 · NFR-04, ADR-0025): 이름 값이 경로에 들어오는 형태(NFC · NFD × 대문자 · 소문자 퍼센트 인코딩)마다
// 응답 본문 · instance · 요청 안의 모든 로그에 이름 값이 어떤 형태(NFC · NFD 원문, 대문자 · 소문자 인코딩)로도 없는지 본다.
// GetEmployeeByNameHttpTests는 NFC 대문자 인코딩 한 형태만 부재를 단언한다(NFD는 원문 비교가 서수라 NFC 단언으로 잡히지 않음).
[Trait("FR", "PRD-002/FR-08")]
[Trait("NFR", "PRD-002/NFR-04")]
public sealed class GetEmployeeByNameEncodedFormsHttpTests : IAsyncLifetime
{
    private const string Template = "/api/employee/{name}";

    private static readonly string Nfc = "홍길동".Normalize(NormalizationForm.FormC);
    private static readonly string Nfd = "홍길동".Normalize(NormalizationForm.FormD);

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly CollectingLogSink _sink = new();
    private EmployeeApiTestHost _host = null!;

    public static TheoryData<string> EncodedPaths => new()
    {
        Uri.EscapeDataString(Nfc),
        Uri.EscapeDataString(Nfc).ToLowerInvariant(),
        Uri.EscapeDataString(Nfd),
        Uri.EscapeDataString(Nfd).ToLowerInvariant(),
        "%20" + Uri.EscapeDataString(Nfd) + "%20",
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static IEnumerable<string> ForbiddenForms =>
    [
        Nfc,
        Nfd,
        Uri.EscapeDataString(Nfc),
        Uri.EscapeDataString(Nfc).ToLowerInvariant(),
        Uri.EscapeDataString(Nfd),
        Uri.EscapeDataString(Nfd).ToLowerInvariant(),
    ];

    public async ValueTask InitializeAsync()
    {
        _host = await EmployeeApiTestHost.StartAsync(_sender, "Production", CancellationToken, builder =>
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning",
            });
            builder.Services.AddSingleton<ILogEventSink>(_sink);
        });
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    // ---- 실패: 404 ----

    [Theory]
    [MemberData(nameof(EncodedPaths))]
    public async Task Get_NotFound_AnyEncodedForm_LeavesNoNameInBodyInstanceOrLogs(string encodedName)
    {
        _sender.QueryAsync(Arg.Any<GetEmployeeByNameQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<EmployeeResponse>(EmployeeErrors.NotFound));

        using var response = await _host.Client.GetAsync($"/api/employee/{encodedName}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AssertTemplateInstanceAndNoNameAsync(response);
    }

    // ---- 500 경로 ----

    [Theory]
    [MemberData(nameof(EncodedPaths))]
    public async Task Get_UnexpectedException_AnyEncodedForm_LeavesNoNameInBodyInstanceOrLogs(string encodedName)
    {
        _sender.QueryAsync(Arg.Any<GetEmployeeByNameQuery>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<EmployeeResponse>>>(_ => throw new InvalidOperationException("boom"));

        using var response = await _host.Client.GetAsync($"/api/employee/{encodedName}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        await AssertTemplateInstanceAndNoNameAsync(response);
    }

    private async Task AssertTemplateInstanceAndNoNameAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(CancellationToken);
        using (var json = JsonDocument.Parse(body))
        {
            json.RootElement.GetProperty("instance").GetString().Should().Be(Template);
        }

        AssertNoName(body, "응답 본문");
        _sink.Events.Should().NotBeEmpty();
        foreach (var logEvent in _sink.Events)
        {
            var text = logEvent.RenderMessage(CultureInfo.InvariantCulture)
                + string.Join('|', logEvent.Properties.Select(pair => $"{pair.Key}={pair.Value}"))
                + logEvent.Exception;
            AssertNoName(text, $"'{logEvent.MessageTemplate.Text}' 로그");
        }
    }

    private static void AssertNoName(string text, string where)
    {
        foreach (var form in ForbiddenForms)
        {
            text.Should().NotContain(form, $"{where}에 이름 값이 어떤 형태로도 없어야 한다");
        }
    }
}
