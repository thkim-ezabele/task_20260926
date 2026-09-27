using Microsoft.Extensions.Configuration;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;

/// <remarks>
/// <c>ConnectionStrings:Write</c> · <c>ConnectionStrings:Read</c>만 가진 설정 대역이다(<c>GetConnectionString</c>은 이 섹션을 읽음).
/// </remarks>
internal static class ConnectionStringsConfiguration
{
    public static IConfiguration Create(string? write, string? read)
    {
        var section = Substitute.For<IConfigurationSection>();
        section["Write"].Returns(write);
        section["Read"].Returns(read);

        var configuration = Substitute.For<IConfiguration>();
        configuration.GetSection("ConnectionStrings").Returns(section);
        return configuration;
    }
}
