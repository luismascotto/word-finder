using System.Buffers;
using System.Text.RegularExpressions;

namespace WordFinder;
public class SearchCriteria
{
    public SearchCriteria(string strRegex)
    {
        Regex = new Regex(strRegex);
    }
    public SearchCriteria(string? inputIncludeAll, string? inputIncludeOnly, string? inputInclude, string? inputExclude, bool includeOrder = false)
    {
        IncludeAll = inputIncludeAll?.Select(c => SearchValues.Create(c.ToString().AsSpan())).ToList();
        Exclude = inputExclude != null ? SearchValues.Create(inputExclude.AsSpan()) : null;
        Include = inputInclude != null ? SearchValues.Create(inputInclude.AsSpan()) : null;
        InputIncludeOnly = inputIncludeOnly;
        IncludeOrder = includeOrder;
    }
    public Regex? Regex { get; internal init; }
    public List<SearchValues<char>>? IncludeAll { get; internal init; }
    public SearchValues<char>? Exclude { get; internal init; }
    public SearchValues<char>? Include { get; internal init; }
    public bool IncludeOrder { get; internal init; }

    private readonly string? InputIncludeOnly;

    public ReadOnlySpan<char> SpanIncludeOnly() => InputIncludeOnly != null ? InputIncludeOnly.AsSpan() : null;
}
