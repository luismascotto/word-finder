using System.Linq;

namespace WordFinder;

public static class Extensions
{
    public static string GetAsSeparator(this string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return ", ";
        }
        return input;
    }
    public static string Detailed(this string? input, string separator = ", ")
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }
        return string.Join(separator.GetAsSeparator(), input!.Trim().Select(s => s));
    }
    public static string Detailed(this string[]? input, string separator = ", ")
    {
        if (input == null || input.Length == 0)
        {
            return string.Empty;
        }
        return string.Join(separator.GetAsSeparator(), input.Select(s => s));
    }
    public static string Detailed(this IEnumerable<string> input, string separator = ", ")
    {
        if (input == null || !input.Any())
        {
            return string.Empty;
        }
        return string.Join(separator.GetAsSeparator(), input.Select(s => s));
    }

}