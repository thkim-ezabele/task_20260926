using System.Text.Json;

namespace EmergencyHub.BuildingBlocks.Api.Errors;

/// <summary>
/// 필드 경로(속성 이름 · 모델 상태 키)를 <c>errors</c>의 JSON 키(camelCase)로 바꿉니다(ADR-0018 "JSON 키 변환은 API 계층").
/// </summary>
internal static class FieldErrorKeys
{
    private const string JsonRoot = "$";
    private const string JsonRootPrefix = "$.";

    /// <summary>
    /// 점(<c>.</c>)으로 나눈 경로 조각마다 System.Text.Json 웹 기본값(camelCase)과 같은 규칙으로 바꿉니다.
    /// 예: <c>Items[0].Name</c> → <c>items[0].name</c>. 빈 문자열(객체 수준)은 그대로입니다.
    /// </summary>
    /// <param name="path">속성 경로.</param>
    /// <returns>JSON 키.</returns>
    public static string ToJsonKey(string path)
    {
        if (path.Length == 0)
        {
            return path;
        }

        var segments = path.Split('.');
        for (var index = 0; index < segments.Length; index++)
        {
            segments[index] = JsonNamingPolicy.CamelCase.ConvertName(segments[index]);
        }

        return string.Join('.', segments);
    }

    /// <summary>
    /// 모델 상태 키를 JSON 키로 바꿉니다. System.Text.Json 입력 포맷터의 JSON 경로 접두사(<c>$.</c>)는 떼고,
    /// 본문 전체를 가리키는 <c>$</c>는 객체 수준(빈 문자열)으로 봅니다.
    /// </summary>
    /// <param name="modelStateKey">모델 상태 키.</param>
    /// <returns>JSON 키.</returns>
    public static string FromModelStateKey(string modelStateKey)
    {
        if (modelStateKey == JsonRoot)
        {
            return string.Empty;
        }

        var path = modelStateKey.StartsWith(JsonRootPrefix, StringComparison.Ordinal) ? modelStateKey[JsonRootPrefix.Length..] : modelStateKey;
        return ToJsonKey(path);
    }
}
