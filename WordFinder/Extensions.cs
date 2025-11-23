namespace WordFinder;

public static class Extensions
{
    public static string Detailed(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }
        return string.Join(", ", input!.Trim().Select(s => s));
    }
    public static string Detailed(this string[]? input)
    {
        if (input == null || input.Length == 0)
        {
            return string.Empty;
        }
        return string.Join(", ", input.Select(s => s));
    }
    public static string Detailed(this IEnumerable<string> input)
    {
        if (input == null || !input.Any())
        {
            return string.Empty;
        }
        return string.Join(", ", input.Select(s => s));
    }

}