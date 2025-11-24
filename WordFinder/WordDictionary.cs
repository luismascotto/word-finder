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

    static int GetSizePreAlloc(int c)
    {
        return c switch
        {
            2 => 440,
            3 => 2140,
            4 => 7200,
            5 => 15920,
            6 => 29880,
            7 => 42000,
            8 => 51636,
            9 => 53412,
            10 => 45880,
            11 => 37556,
            12 => 29132,
            13 => 20956,
            14 => 14156,
            15 => 8856,
            16 => 5190,
            17 => 2980,
            18 => 1480,
            19 => 770,
            20 => 370,
            21 => 180,
            22 => 80,
            23 => 40,
            24 => 12,
            25 => 8,
            27 => 4,
            28 => 4,
            29 => 4,
            31 => 4,
            _ => 0
        };

    }
    public void LoadWordsReadAll(int min = 2, int max = 32)
    {
        //if (!File.Exists(_filePath))
        //{
        //    throw new FileNotFoundException($"Word file not found at: {_filePath}");
        //}
        for (int c = min; c <= max; c++)
        {
            int pre = GetSizePreAlloc(c);
            if (pre > 0)
            {
                _wordsByLength[c] = new(256);
            }
        }

        var words = File.ReadAllLines(_filePath);

        foreach (var word in words)
        {
            var length = word.Length;
            if (word.Length > max || word.Length < min)
            {
                continue;
            }
            _wordsByLength[length].Add(word);
            //if (!_wordsByLength.TryGetValue(length, out List<string>? currLengthWords))
            //{
            //    currLengthWords = [];
            //    _wordsByLength[length] = currLengthWords;
            //}

            //currLengthWords.Add(word);
        }
    }

    public async Task LoadWordsAsync(int min = 2, int max = 32)
    {
        await foreach (var word in ReadLinesAsync(_filePath))
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

    public static async IAsyncEnumerable<string> ReadLinesAsync(string path)
    {
        using var reader = new StreamReader(path);
        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            yield return line;
        }
    }

    public List<string> GetWordsByLength(int length)
    { return _wordsByLength.TryGetValue(length, out var words) ? words : []; }

    public IEnumerable<int> GetAvailableLengths() { return _wordsByLength.Keys.OrderBy(k => k); }

    public List<string> Search(
        string? inputIncludeAll,
        string? inputIncludeOnly,
        string? inputInclude,
        string? inputExclude,
        bool includeOrder = false)
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