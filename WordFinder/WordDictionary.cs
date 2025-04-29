using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    public void LoadWords()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException($"Word file not found at: {_filePath}");
        }

        var words = File.ReadAllLines(_filePath);

        foreach (var word in words)
        {
            var length = word.Length;
            if (!_wordsByLength.TryGetValue(length, out List<string>? value))
            {
                value = [];
                _wordsByLength[length] = value;
            }

            value.Add(word);
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

    public List<string> Search(string? inputInclude, string? inputExclude, string? inputLength, string? inputMaxLength, bool includeOrder = false)
    {
        if (inputInclude == null && inputExclude == null && inputLength == null && inputMaxLength == null)
        {
            throw new ArgumentException("No search criteria provided");
        }

        int minLength = 0;
        int maxLength = 0;

        if (inputLength != null)
        {
            minLength = int.Parse(inputLength);
        }

        if (inputMaxLength != null)
        {
            maxLength = int.Parse(inputMaxLength);
        }

        if (minLength > maxLength)
        {
            throw new ArgumentException("Minimum length cannot be greater than maximum length");
        }

        var srcInclude = inputInclude?.Select(c => SearchValues.Create(c.ToString().AsSpan())).ToList();
        //Print srcInclude
        if (srcInclude != null)
        {
            Console.WriteLine($"inputInclude: {string.Join(", ", inputInclude!.Select(s => s))}");
        }
        var srcExclude = inputExclude != null ? SearchValues.Create(inputExclude.AsSpan()) : null;

        var matches = new List<string>();

        foreach (var length in GetAvailableLengths().Where(l => l >= minLength && l <= maxLength))
        {
            var words = GetWordsByLength(length);
            foreach (var word in words)
            {
                if (srcExclude != null && word.AsSpan().IndexOfAny(srcExclude) != -1)
                {
                    continue;
                }

                if (srcInclude != null)
                {
                    int lastIndex = -1;
                    int index;
                    bool allLettersFound = true;
                    foreach (var searchValue in srcInclude)
                    {
                        index = word.AsSpan()[(lastIndex + 1)..].IndexOfAny(searchValue);
                        if (index == -1)
                        {
                            allLettersFound = false;
                            break;
                        }
                        if (!includeOrder)
                        {
                            lastIndex = -1; 
                        }
                        else
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