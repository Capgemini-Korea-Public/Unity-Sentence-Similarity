using System.Text.RegularExpressions;
public static class StringUtility
{
    /// <summary>
    /// 부정적인 표현을 제거하는 메서드
    /// </summary>
    public static string RemoveNegativeParts(string input)
    {
        string pattern = @"(I |We |They |He |She |It )?(don't|doesn't|won't|can't|shouldn't|not|never).*?(,|\.|but)";
        string result = Regex.Replace(input, pattern, "");
        result = Regex.Replace(result, @"\s+", " ").Trim();
        return result;
    }
    
    /// <summary>
    /// 문자열을 소문자로 변환하고 앞뒤 공백을 제거하는 메서드
    /// </summary>
    public static string NormalizeText(string input)
    {
        return input.ToLower().Trim();
    }
    
    /// <summary>
    /// 문자열이 null 또는 빈 문자열인지 확인하는 메서드
    /// </summary>
    public static bool IsNullOrEmpty(string input)
    {
        return string.IsNullOrEmpty(input);
    }
}
