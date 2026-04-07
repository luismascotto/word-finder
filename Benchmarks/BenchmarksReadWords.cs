using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;


[MemoryDiagnoser]
public class BenchmarksReadWords
{
    private static ReadOnlySpan<byte> NewLine => "\r\n"u8;

    private FileStream _stream;

    private const int min = 2;
    private const int max = 32;
    private readonly string _filePath1 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_1.txt";
    private readonly string _filePath2 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_2.txt";
    private readonly string _filePathB1 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_big_1.txt";
    private readonly string _filePathB2 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_big_2.txt";
    private readonly string _filePathB3 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_big_3.txt";
    private readonly string _filePathB4 = @"K:\Develop\word-finder\Benchmarks\words_full_ordered_big_4.txt";

    //[Params(2, 12)]
    [Params(2)]
    public int MinWordLength { get; set; }
    //[Params(24, 14)]
    [Params(24)]
    public int MaxWordLength { get; set; }


    [GlobalSetup]
    public void GlobalSetup()
    {
        _stream = PrepareFileStream();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _stream.Dispose();
    }
    public FileStream PrepareFileStream()
    {
        long fileLength = new FileInfo(_filePath1).Length;
        var stream = new FileStream(_filePath1, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: GetFileStreamBufferSize(fileLength), useAsync: true);

        return stream;
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

    private static void AddStringDummy(string word, int MinWordLength, int MaxWordLength)
    {
        if (word.Length > MaxWordLength || word.Length < MinWordLength)
        {
            return;
        }
        // Dummy method to prevent optimizations
        word = word.AsSpan()[..].ToString();
    }
    private static void AddSpanDummy(ReadOnlySpan<char> word, int MinWordLength, int MaxWordLength)
    {
        if (word.Length > MaxWordLength || word.Length < MinWordLength)
        {
            return;
        }
        // Dummy method to prevent optimizations
        var str = word[..].ToString();
    }


    [Benchmark(Baseline = true)]
    public void FileReadAllLines()
    {
        //_ = _stream.Seek(0, SeekOrigin.Begin);

        var words = File.ReadAllLines(_filePath1);

        foreach (var word in words)
        {
            AddStringDummy(word, MinWordLength, MaxWordLength);
        }

    }

    [Benchmark]
    public async Task<string> ReadLineUsingStringReaderAsync()
    {
        _ = _stream.Seek(0, SeekOrigin.Begin);

        var sr = new StreamReader(_stream, Encoding.UTF8);
        string str;
        while ((str = await sr.ReadLineAsync()) is { })
        {
            AddStringDummy(str, MinWordLength, MaxWordLength);
        }

        return str;
    }

    //[Benchmark]
    public async Task<string> ReadAllUsingStringReaderAsync()
    {
        _ = _stream.Seek(0, SeekOrigin.Begin);
        var sr = new StreamReader(_stream, Encoding.UTF8);

        var words = await sr.ReadToEndAsync();
        var wordsAsSpan = words.AsSpan();

        foreach (var word in wordsAsSpan.EnumerateLines())
        {
            AddSpanDummy(word, MinWordLength, MaxWordLength);
        }


        return default;
    }

    [Benchmark]
    public async Task<string> ReadLineUsingPipelineVer2Async()
    {
        _ = _stream.Seek(0, SeekOrigin.Begin);

        var reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));
        string str;

        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            str = ProcessLine(ref buffer, MinWordLength, MaxWordLength);

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ReadLine(ref ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);

        if (reader.TryReadTo(out ReadOnlySpan<byte> line, NewLine))
        {
            buffer = buffer.Slice(reader.Position);
            return Encoding.UTF8.GetString(line);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    private static string ProcessLine(ref ReadOnlySequence<byte> buffer, int min, int max)
    {
        string str = null;

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
                str = Encoding.UTF8.GetString(line);

                // simulate string processing
                AddStringDummy(str, min, max);

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
                    str = Encoding.UTF8.GetString(line);

                    // simulate string processing
                    AddStringDummy(str, min, max);
                }

                buffer = buffer.Slice(sequenceReader.Position);
                sequenceReader.Advance(buffer.Length);
            }
        }

        return str;
    }



}
