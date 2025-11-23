using System.Buffers;
using System.Text.RegularExpressions;

namespace WordFinder;

public class WordDictionary
{
    private readonly Dictionary<int, List<string>> _wordsByLength;
    private readonly string _filePath;


    public WordDictionary(string filePath)
    {
        _filePath = filePath;
        _wordsByLength = [];
    }

    public void LoadWords(int min = 1, int max = 99)
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"Word file not found at: {_filePath}");
        }

        var words = File.ReadAllLines(_filePath);

        foreach (var word in words)
        {
            var length = word.Length;
            if (length < min || length > max)
            {
                continue;
            }
            if (!_wordsByLength.TryGetValue(length, out List<string>? currLengthWords))
            {
                currLengthWords = [];
                _wordsByLength[length] = currLengthWords;
            }

            currLengthWords.Add(word);
        }
    }

    public List<string> GetWordsByLength(int length)
    {
        return _wordsByLength.TryGetValue(length, out var words) ? words : [];
    }

    public IEnumerable<int> GetAvailableLengths()
    {
        return _wordsByLength.Keys.OrderBy(k => k);
    }

    public List<string> Search(string? inputIncludeAll, string? inputIncludeOnly, string? inputInclude, string? inputExclude, bool includeOrder = false)
    {
        if (inputIncludeAll != null)
        {
            Console.WriteLine($"inputIncludeAll: {inputIncludeAll.Detailed()} - ordered: {includeOrder}");
        }
        if (inputIncludeOnly != null)
        {
            Console.WriteLine($"inputIncludeOnly: {inputIncludeOnly.Detailed()}");
        }
        if (inputInclude != null)
        {
            Console.WriteLine($"inputInclude: {inputInclude.Detailed()}");
        }
        if (inputExclude != null)
        {
            Console.WriteLine($"inputExclude: {inputExclude.Detailed()}");
        }
        var srchIncludeAll = inputIncludeAll?.Select(c => SearchValues.Create(c.ToString().AsSpan())).ToList();
        var srchExclude = inputExclude != null ? SearchValues.Create(inputExclude.AsSpan()) : null;
        var srchInclude = inputInclude != null ? SearchValues.Create(inputInclude.AsSpan()) : null;
        var spanIncludeOnly = inputIncludeOnly != null ? inputIncludeOnly.AsSpan() : null;
        var matches = new List<string>();

        foreach (var length in GetAvailableLengths())
        {
            var words = GetWordsByLength(length);
            foreach (var word in words)
            {
                var wordAsSpan = word.AsSpan();

                if (!spanIncludeOnly.IsEmpty)
                {
                    if (wordAsSpan.ContainsAnyExcept(spanIncludeOnly))
                    {
                        continue;
                    }
                }

                if (srchExclude != null && wordAsSpan.IndexOfAny(srchExclude) != -1)
                {
                    continue;
                }
                if (srchInclude != null && wordAsSpan.IndexOfAny(srchInclude) == -1)
                {
                    continue;
                }

                if (srchIncludeAll != null)
                {
                    int lastIndex = -1;
                    int index;
                    bool allLettersFound = true;
                    foreach (var searchValue in srchIncludeAll)
                    {
                        index = wordAsSpan[(lastIndex + 1)..].IndexOfAny(searchValue);
                        if (index == -1)
                        {
                            allLettersFound = false;
                            break;
                        }
                        if (includeOrder)
                        {
                            lastIndex += index + 1; // Move past the found letter
                        }
                    }
                    if (!allLettersFound)
                    {
                        continue;
                    }
                }
                matches.Add(word);
            }
        }
        return matches;
    }

    public List<string> SearchWithRegex(string strRegex)
    {
        if (string.IsNullOrWhiteSpace(strRegex))
        {
            throw new ArgumentException("Regex cannot be null or whitespace");
        }

        var regex = new Regex(strRegex);
        var matches = new List<string>();

        foreach (var length in GetAvailableLengths())
        {
            var words = GetWordsByLength(length);
            foreach (var word in words)
            {
                if (regex.IsMatch(word))
                {
                    matches.Add(word);
                }
            }
        }
        return matches;
    }

    public List<string> Search(SearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var matches = new List<string>();

        foreach (var length in GetAvailableLengths())
        {
            //Replicate foreach to avoid multiple checks and add/continue flow for each word
            List<string> words = GetWordsByLength(length);

            if (criteria.Regex != null)
            {
                foreach (var word in words)
                {
                    if (criteria.Regex.IsMatch(word))
                    {
                        matches.Add(word);
                    }
                }
                continue;
            }

            if (!criteria.SpanIncludeOnly().IsEmpty)
            {
                foreach (var word in words)
                {
                    var wordAsSpan = word.AsSpan();
                    if (wordAsSpan.ContainsAnyExcept(criteria.SpanIncludeOnly()) == false)
                    {
                        matches.Add(word);
                    }
                }
                continue;
            }

            // Complementary criteria
            foreach (var word in words)
            {
                var wordAsSpan = word.AsSpan();

                if (criteria.Exclude != null && wordAsSpan.ContainsAny(criteria.Exclude))
                {
                    continue;
                }
                if (criteria.Include != null && !wordAsSpan.ContainsAny(criteria.Include))
                {
                    continue;
                }

                if (criteria.IncludeAll != null)
                {
                    int lastIndex = -1;
                    int index;
                    bool allLettersFound = true;
                    foreach (var searchValue in criteria.IncludeAll)
                    {
                        index = wordAsSpan[(lastIndex + 1)..].IndexOfAny(searchValue);
                        if (index == -1)
                        {
                            allLettersFound = false;
                            break;
                        }
                        if (criteria.IncludeOrder)
                        {
                            lastIndex += index + 1; // Move past the found letter
                        }
                    }
                    if (!allLettersFound)
                    {
                        continue;
                    }
                }
                matches.Add(word);
            }
        }
        return matches;
    }
}