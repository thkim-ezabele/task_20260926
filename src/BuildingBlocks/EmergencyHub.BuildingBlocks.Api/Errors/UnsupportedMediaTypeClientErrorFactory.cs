using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace EmergencyHub.BuildingBlocks.Api.Errors;

/// <summary>
/// <c>[ApiController]</c>의 클라이언트 오류 결과 변환(<see cref="IClientErrorFactory"/>)을 감싸 <c>415</c>만 공통 <c>ProblemDetails</c>
/// (<c>415</c> · <c>1005</c>, <see cref="CommonErrors.UnsupportedMediaType"/>)로 바꿉니다(ADR-0028 "[Consumes] 불일치 415").
/// </summary>
/// <remarks>
/// <para>
/// 경로(S06-T01 실측): 액션까지 왔지만 입력 포맷터를 고를 수 없는 요청(예: <c>[FromBody]</c> 액션에 Content-Type 없음) →
/// <c>UnsupportedContentTypeFilter</c> → <see cref="UnsupportedMediaTypeResult"/> → <c>ClientErrorResultFilter</c> → 이 팩토리.
/// 라우팅 단계에서 거부되는 <c>[Consumes]</c> 불일치(본문 없는 415)는 <see cref="UnsupportedMediaTypeStatusCodeResponses"/>가 맡습니다.
/// </para>
/// <para>415가 아닌 클라이언트 오류 결과(<c>NotFound()</c> 등)는 안쪽 프레임워크 기본 팩토리에 그대로 맡깁니다(기존 동작 유지).</para>
/// </remarks>
internal sealed class UnsupportedMediaTypeClientErrorFactory : IClientErrorFactory
{
    private readonly IClientErrorFactory _inner;

    /// <summary>안쪽 팩토리를 감쌉니다.</summary>
    /// <param name="inner">프레임워크 기본 팩토리.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/>가 <see langword="null"/>인 경우.</exception>
    public UnsupportedMediaTypeClientErrorFactory(IClientErrorFactory inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc/>
    public IActionResult? GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        ArgumentNullException.ThrowIfNull(clientError);

        return clientError.StatusCode == StatusCodes.Status415UnsupportedMediaType
            ? CommonErrors.UnsupportedMediaType.ToProblemResult()
            : _inner.GetClientError(actionContext, clientError);
    }
}
