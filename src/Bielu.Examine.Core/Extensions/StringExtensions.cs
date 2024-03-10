namespace Bielu.Examine.Core.Extensions;

public static class StringExtensions
{
    public static string FormatFieldName(this string fieldName)
    {
        return $"{fieldName.Replace(".", "_")}";
    }
}
