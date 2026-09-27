using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>응답 본문을 읽을 수 있는 <see cref="DefaultHttpContext"/>를 만들고 응답 JSON을 읽습니다.</summary>
internal static class HttpContexts
{
    public const string TraceIdentifier = "0HN7SAMPLE:00000001";

    public static DefaultHttpContext Create(string path = "/api/v1/employees", IServiceProvider? services = null)
    {
        var context = new DefaultHttpContext { TraceIdentifier = TraceIdentifier };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (services is not null)
        {
            context.RequestServices = services;
        }

        return context;
    }

    public static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return reader.ReadToEnd();
    }

    public static JsonElement ReadJson(HttpContext context)
    {
        using var document = JsonDocument.Parse(ReadBody(context));
        return document.RootElement.Clone();
    }
}
