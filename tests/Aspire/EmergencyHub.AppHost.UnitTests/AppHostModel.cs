using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.AppHost.UnitTests;

/// <summary>
/// AppHost 애플리케이션 모델을 DCP · Docker 없이 만든다. 빌더에 리소스만 추가하고 Build · Run은 하지 않는다.
/// </summary>
/// <remarks>
/// <see cref="DistributedApplicationOptions.AssemblyName"/>을 AppHost로 두어 AppHost 디렉터리(초기화 스크립트 폴더 기준 경로)가 실제와 같게 한다.
/// 매개변수 값은 요청하지 않으므로(식만 비교) 비밀번호 생성 · user-secrets 저장이 일어나지 않는다.
/// </remarks>
internal static class AppHostModel
{
    internal const string AppHostAssemblyName = "EmergencyHub.AppHost";

    internal static string PostgresImageTag { get; } = AppHost.PostgresImageTag.Read(typeof(EmergencyHubApplication).Assembly);

    internal static IDistributedApplicationBuilder CreateBuilder(
        DistributedApplicationOperation operation = DistributedApplicationOperation.Run,
        string? assemblyName = AppHostAssemblyName,
        string? environmentName = null)
    {
        string[] args = operation == DistributedApplicationOperation.Publish
            ? ["--publisher", "manifest", "--output-path", Path.GetTempPath()]
            : [];

        if (environmentName is not null)
        {
            args = [.. args, "--environment", environmentName];
        }

        return DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = args,
            AssemblyName = assemblyName,
            DisableDashboard = true,
        });
    }

    internal static (IDistributedApplicationBuilder Builder, EmergencyHubResources Resources) Create(DistributedApplicationOperation operation = DistributedApplicationOperation.Run)
    {
        var builder = CreateBuilder(operation);
        var resources = builder.AddEmergencyHub(PostgresImageTag);
        return (builder, resources);
    }

    /// <summary>환경 변수를 게시 모드 식(<c>{리소스.value}</c> 등)으로 얻는다. 비밀 값은 풀지 않는다.</summary>
    internal static async Task<Dictionary<string, string>> EnvironmentExpressionsAsync(IResource resource) =>
        await ((IResourceWithEnvironment)resource).GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);

    /// <summary>
    /// 실행 모드 환경 변수 콜백만 돌려 이름 → 값(문자열 또는 식 객체)을 얻는다. 값을 풀지 않으므로 엔드포인트 할당 · 비밀 값이 필요 없다.
    /// launchSettings 프로필 환경 변수처럼 실행 모드에서만 붙는 값을 확인할 때 쓴다.
    /// OTLP 콜백이 실행 컨텍스트의 서비스 공급자를 쓰므로 빌더 서비스로 공급자를 만든다(Build · Run은 하지 않음).
    /// </summary>
    internal static async Task<Dictionary<string, object>> RunEnvironmentAsync(IDistributedApplicationBuilder builder, IResource resource)
    {
        await using var services = builder.Services.BuildServiceProvider();
        var executionContext = new DistributedApplicationExecutionContext(
            new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Run) { ServiceProvider = services });
        var context = new EnvironmentCallbackContext(executionContext);
        foreach (var callback in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await callback.Callback(context);
        }

        return context.EnvironmentVariables;
    }

    /// <summary>
    /// 리소스의 Database 생성 스크립트 목록. 9.5.2의 PostgresCreateDatabaseScriptAnnotation은 internal이라 형식 이름 · Script 속성을 리플렉션으로 읽는다.
    /// </summary>
    internal static List<string> CreationScripts(IResource resource) =>
        resource.Annotations
            .Where(annotation => annotation.GetType().Name == "PostgresCreateDatabaseScriptAnnotation")
            .Select(annotation => (string)annotation.GetType().GetProperty("Script")!.GetValue(annotation)!)
            .ToList();

    internal static ParameterResource Parameter(IDistributedApplicationBuilder builder, string name) =>
        builder.Resources.OfType<ParameterResource>().Single(parameter => parameter.Name == name);
}
