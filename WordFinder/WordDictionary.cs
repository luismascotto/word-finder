using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.RegularExpressions;


namespace WordFinder;

public class WordDictionary
{
    private const int MAX_WORD_LEN = 64;
    private static ReadOnlySpan<byte> NewLine => "\r\n"u8;
    private readonly Dictionary<int, List<string>> _wordsByLength;
    private readonly string _filePath;
    readonly int minWordLen;
    readonly int maxWordLen;


    public WordDictionary(string filePath, int minWordLen, int maxWordLen)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Dictionary file not found at: {_filePath}");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minWordLen);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxWordLen);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minWordLen, maxWordLen, nameof(minWordLen));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxWordLen, MAX_WORD_LEN, nameof(maxWordLen));

        this.minWordLen = minWordLen;
        this.maxWordLen = maxWordLen;

        _filePath = filePath;
        _wordsByLength = [];
        PreAlloc();
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

    private void PreAlloc()
    {
        for (int c = minWordLen; c <= maxWordLen; c++)
        {
            int pre = GetSizePreAlloc(c);
            if (pre <= 0)
            {
                pre = 32; // default pre-allocation size for lengths without specific data
            }
#if preview_
            _wordsByLength[c] = [with(pre)];
#else
            _wordsByLength[c] = new(pre);
#endif
        }
    }

    public void LoadWords()
    {
        ReadLineUsingPipelineVer2Async().Wait();
    }
    public void LoadWordsReadAll()
    {
        var words = File.ReadAllLines(_filePath);

        foreach (var word in words)
        {
            AddString(word);
        }
    }

    public async Task LoadWordsAsync()
    {
        await foreach (var word in ReadLinesAsync(_filePath))
        {
            AddString(word);
        }
    }

    private static int GetFileStreamBufferSize(long fileLength)
    {
        return fileLength switch
        {
            <= 262144 => 0,
            <= 5242880 => 81920,
            < 104857600 => 131072,
            >= 104857600 => 1048576
        };
    }
    private static async IAsyncEnumerable<string> ReadLinesAsync(string path)
    {
        using var stream = GetFileStreamAsyncRead(path);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync() is { } line)
        {
            yield return line;
        }
    }

    private void AddSpan(ReadOnlySpan<byte> word)
    {
        if (word.Length > maxWordLen || word.Length < minWordLen)
        {
            return;
        }
        _wordsByLength[word.Length].Add(Encoding.UTF8.GetString(word));
    }
    private void AddString(string word)
    {
        if (word.Length > maxWordLen || word.Length < minWordLen)
        {
            return;
        }
        _wordsByLength[word.Length].Add(word);
    }

    private void ProcessLine(ref ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment)
        {
            var span = buffer.FirstSpan;
            int consumed;
            while (span.Length > 0)
            {
                var newLine = span.IndexOf(NewLine);

                if (newLine == -1)
                {
                    break;
                }

                var line = span[..newLine];

                AddSpan(line);

                consumed = line.Length + NewLine.Length;
                span = span[consumed..];
                buffer = buffer.Slice(consumed);
            }
        }
        else
        {
            var sequenceReader = new SequenceReader<byte>(buffer);

            while (!sequenceReader.End)
            {
                while (sequenceReader.TryReadTo(out ReadOnlySpan<byte> line, NewLine))
                {
                    // simulate string processing
                    AddSpan(line);
                }

                buffer = buffer.Slice(sequenceReader.Position);
                sequenceReader.Advance(buffer.Length);
            }
        }
    }

    public async Task ReadLineUsingPipelineVer2Async()
    {
        using FileStream stream = GetFileStreamAsyncRead(_filePath);

        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions());


        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            ProcessLine(ref buffer);

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync();

    }

    private static FileStream GetFileStreamAsyncRead(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: GetFileStreamBufferSize(new FileInfo(path).Length), useAsync: true);
    }

    public List<string> GetWordsByLength(int length) => _wordsByLength.TryGetValue(length, out var words) ? words : [];

    public IEnumerable<int> GetAvailableLengths() => _wordsByLength.Keys.OrderBy(k => k);

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