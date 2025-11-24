using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Disassemblers;

namespace Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class Benchmarks
{
    private const int min = 2;
    private const int max = 32;
    private readonly string _filePath1 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_1.txt";
    private readonly string _filePath2 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_2.txt";
    private readonly string _filePathB1 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_big_1.txt";
    private readonly string _filePathB2 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_big_2.txt";
    private readonly string _filePathB3 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_big_3.txt";
    private readonly string _filePathB4 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_big_4.txt";

    [Params(8, 12)]
    public int MinWordLength { get; set; }
    [Params(13, 14)]
    public int MaxWordLength { get; set; }



    [GlobalSetup]
    public void SetupAndTest()
    {
        WordDictionary._fileStreamOptions = WordDictionary.GetFileStreamReadOptions(_filePath1);
        WordDictionary._fileStreamOptionsB = WordDictionary.GetFileStreamReadOptions(_filePathB1);
        if (!File.Exists(_filePath1))
        {
            throw new FileNotFoundException($"Word file not found at: {_filePath1}");
        }
        if (!File.Exists(_filePath2))
        {
            throw new FileNotFoundException($"Word file not found at: {_filePath2}");
        }
    }

    [Benchmark(Baseline = true)]
    public void FileReadAllLines()
    {
        var dictionary = new WordDictionary(_filePathB1);
        dictionary.LoadWordsFileReadAllLines(MinWordLength, MaxWordLength);
    }

    [Benchmark]
    public async Task StreamReadLinesAsync()
    {
        var dictionary = new WordDictionary(_filePathB2);
        await dictionary.LoadWordsStreamReadLinesAsync(MinWordLength, MaxWordLength);
    }

    [Benchmark]
    public async Task FilesReadAllLinesAsync()
    {
        var dictionary = new WordDictionary(_filePathB3);
        await dictionary.LoadWordsFileReadAllLinesAsync(MinWordLength, MaxWordLength);
    }

    //[Benchmark]
    public async Task StreamReadToEndSplitArrayAsync()
    {
        var dictionary = new WordDictionary(_filePathB4);
        await dictionary.LoadWordsStreamReadToEndSplitArrayAsync(MinWordLength, MaxWordLength);
    }
    //[Benchmark]
    public async Task StreamReadToEndSpanLinesAsync()
    {
        var dictionary = new WordDictionary(_filePathB4);
        await dictionary.LoadWordsStreamReadToEndSpanLinesAsync(MinWordLength, MaxWordLength);
    }
}

public class WordDictionary
{
    private readonly Dictionary<int, List<string>> _wordsByLength;
    private readonly string _filePath;

    public static FileStreamOptions _fileStreamOptions;
    public static FileStreamOptions _fileStreamOptionsB;


    public WordDictionary(string filePath)
    {
        _filePath = filePath;
        _wordsByLength = [];
    }

    public void LoadWordsFileReadAllLines(int min = 2, int max = 32)
    {
        //if (!File.Exists(_filePath))
        //{
        //    throw new FileNotFoundException($"Word file not found at: {_filePath}");
        //}

        var words = File.ReadAllLines(_filePath);

        foreach (var word in words)
        {
            var length = word.Length;
            if (length < min || length > max)
            {
                continue;
            }
            AddWordDummy(word);
        }
    }

    public async Task LoadWordsStreamReadLinesAsync(int min = 2, int max = 32)
    {
        await foreach (var word in ReadLinesAsync(_filePath))
        {
            int length = word.Length;
            if (length < min || length > max)
            {
                continue;
            }
            AddWordDummy(word);
        }
    }
    public async IAsyncEnumerable<string> ReadLinesAsync(string path)
    {
        using var reader = new StreamReader(path, _fileStreamOptionsB);
        string line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            yield return line;
        }
    }

    public async Task LoadWordsFileReadAllLinesAsync(int min = 2, int max = 32)
    {
        var words = await File.ReadAllLinesAsync(_filePath);
        foreach (var word in words)
        {
            int length = word.Length;
            if (length < min || length > max)
            {
                continue;
            }
            AddWordDummy(word);
        }
    }

    public async Task LoadWordsStreamReadToEndSplitArrayAsync(int min = 2, int max = 32)
    {
        var words = await ReadToEndAsync(_filePath);
        var wordsArray = words.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in wordsArray)
        {
            int length = word.Length;
            if (length < min || length > max)
            {
                continue;
            }
            AddWordDummy(word);
        }
    }
    public async Task LoadWordsStreamReadToEndSpanLinesAsync(int min = 2, int max = 32)
    {
        var words = await ReadToEndAsync(_filePath);
        var wordsAsSpan = words.AsSpan();

        foreach (var word in wordsAsSpan.EnumerateLines())
        {
            int length = word.Length;
            if (length < min || length > max)
            {
                continue;
            }
            AddWordDummy(word.ToString());
        }
    }
    public async Task<string> ReadToEndAsync(string path)
    {
        using var reader = new StreamReader(path, _fileStreamOptionsB);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    public static FileStreamOptions GetFileStreamReadOptions(string filePath)
    {
        long fileLength = new FileInfo(filePath).Length;
        return new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = GetFileStreamBufferSize(fileLength),
            Options = fileLength < 10737418240 ? FileOptions.None : FileOptions.SequentialScan
        };
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

    public void AddWordDummy(string word)
    {
        int length = word.Length;
        if (!_wordsByLength.TryGetValue(length, out List<string> currLengthWords))
        {
            currLengthWords = [];
            _wordsByLength[length] = currLengthWords;
        }
        if (currLengthWords.Count == 0)
        {
            currLengthWords.Add(word);
        }
        else
        {
            currLengthWords[0] = word;
        }
    }
}