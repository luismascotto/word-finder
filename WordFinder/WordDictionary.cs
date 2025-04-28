using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WordFinder
{
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
    }
} 