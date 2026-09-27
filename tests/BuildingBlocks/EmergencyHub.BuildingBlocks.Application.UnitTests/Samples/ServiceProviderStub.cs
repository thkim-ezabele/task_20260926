namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>
/// 등록한 서비스만 돌려주는 최소 <see cref="IServiceProvider"/>. 해석 횟수를 형식별로 셉니다.
/// </summary>
public sealed class ServiceProviderStub : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = [];
    private readonly Dictionary<Type, int> _resolveCounts = [];
    private readonly object _gate = new();

    public ServiceProviderStub Add<TService>(TService service)
        where TService : class
    {
        _services[typeof(TService)] = service;
        return this;
    }

    public int ResolveCount<TService>()
    {
        lock (_gate)
        {
            return _resolveCounts.GetValueOrDefault(typeof(TService));
        }
    }

    public object? GetService(Type serviceType)
    {
        lock (_gate)
        {
            _resolveCounts[serviceType] = _resolveCounts.GetValueOrDefault(serviceType) + 1;
            return _services.GetValueOrDefault(serviceType);
        }
    }
}
