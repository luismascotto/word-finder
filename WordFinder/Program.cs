using System.CommandLine;
using System.Diagnostics;

namespace WordFinder;

class Program
{
    //--includeAll "gce" --include "st" --exclude "jzx" --min-length 8 --max-length 16 --include-ordered true
    const string _spaces = "                                                                                                                                                                         ";
    private const int PRINTING_COLUMNS = 5;
    static int Main(string[] args)
    {
        var rootCommand = new RootCommand("Word Finder - A tool to search for words based on various criteria");
        rootCommand.Aliases.Add("word-finder");


        var fileOption = new Option<string>("--file", ["-p"])
        {
            Description = "Path to the words/dictionary file (text content, one word per line)",
            HelpName = "file-path",
            DefaultValueFactory = parseResult => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "words_alpha.txt")
        };

        var regexOption = new Option<string?>("--regex", ["-r"])
        {
            Description = "Regular expression for word matching",
            HelpName = "expression"
        };

        // "str": Valids: strike, sectoring, retires. Invalids: stop, tried, syrup
        var includeAllOption = new Option<string?>("--includeAll", ["-all"])
        {
            Description = "Letters that must be all present in the word",
            HelpName = "letters"
        };

        // "abefkst": Valids: task, beak, kebab, skate. Invalids: strike, beekeeper
        var includeOnlyOption = new Option<string?>("--includeOnly", ["-only"])
        {
            Description = "Only letters that must be all present in the word",
            HelpName = "letters"
        };

        // "abefkst": Valids: beetle, test, sorry, fly. Invalids: cloth, dull
        var includeOption = new Option<string?>("--include", ["-any"])
        {
            Description = "Letters that must appear at least one time in the word",
            HelpName = "letters"
        };

        var excludeOption = new Option<string?>("--exclude", ["-not"])
        {
            Description = "Letters that cannot exist in the word",
            HelpName = "letters"
        };

        var minLengthOption = new Option<int?>("--min-length", ["-min", "-n"])
        {
            Description = "Minimum word length",
            HelpName = "length",
            DefaultValueFactory = _ => 4
        };

        var maxLengthOption = new Option<int?>("--max-length", ["-max", "-x"])
        {
            Description = "Maximum word length",
            HelpName = "length",
            DefaultValueFactory = _ => 12
        };

        // "str": Valids: strike, streak. Invalids: sectoring, retires, rooster
        var includeAllOrderedOption = new Option<bool>(name: "--ordered", ["-o"])
        {
            Description = "When using withIncludeAll, letters must appear in the same order as provided, including with repeated values ('ss', 'rr')"
        };

        var resultSortByLen = new Option<bool>(name: "--result-length", ["-rlen"])
        {
            Description = "Sort results by word length, then alphabetically"
        };

        rootCommand.Options.Add(fileOption);
        rootCommand.Options.Add(regexOption);
        rootCommand.Options.Add(includeAllOption);
        rootCommand.Options.Add(includeOnlyOption);
        rootCommand.Options.Add(includeOption);
        rootCommand.Options.Add(excludeOption);
        rootCommand.Options.Add(minLengthOption);
        rootCommand.Options.Add(maxLengthOption);
        rootCommand.Options.Add(includeAllOrderedOption);
        rootCommand.Options.Add(resultSortByLen);


        rootCommand.SetAction(context =>
        {
            var file = context.GetValue(fileOption);
            var regex = context.GetValue(regexOption);
            var includeAll = context.GetValue(includeAllOption);
            var includeOnly = context.GetValue(includeOnlyOption);
            var include = context.GetValue(includeOption);
            var exclude = context.GetValue(excludeOption);
            var minLength = context.GetValue(minLengthOption);
            var maxLength = context.GetValue(maxLengthOption);
            var includeAllOrdered = context.GetValue(includeAllOrderedOption);
            var resultSortByLength = context.GetValue(resultSortByLen);

            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"Error: Word file not found at: {file}");
                return;
            }

            if (regex == null &&
               includeAll == null &&
               includeOnly == null &&
               include == null &&
               exclude == null)
            {
                Console.Error.WriteLine("Error: At least one search criteria must be provided.");
                return;
            }

            if (regex != null && !(
               includeAll == null &&
               includeOnly == null &&
               include == null &&
               exclude == null))
            {
                Console.WriteLine("'Regex' was specified, all other options will be ignored.");
            }
            else if (includeOnly != null && !(
               includeAll == null &&
               include == null &&
               exclude == null))
            {
                Console.WriteLine("Include Only was specified, all other include/exclude rules may be ineffective.");
            }

            if (minLength.GetValueOrDefault(0) < 1 || minLength.GetValueOrDefault(0) > maxLength.GetValueOrDefault(0))
            {
                throw new ArgumentException("Minimum length cannot be greater than maximum length, and greater than zero");
            }

            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Creating dictionary from: {file}");
            var dictionary = new WordDictionary(file, minLength!.Value, maxLength!.Value);
            stopwatch.WritelineElapsedAndRestart("Dictionary created in");

            Console.WriteLine($"Loading words from size {minLength!.Value} to {maxLength!.Value}");
            dictionary.LoadWords();
            stopwatch.WritelineElapsedAndRestart("Dictionary loaded in");

#if DEBUG_CHUNK
            Console.WriteLine($"Loaded {dictionary.GetAvailableLengths().Count()} word lengths.");
            foreach (var length in dictionary.GetAvailableLengths().OrderBy(l => l))
            {
                var items = 1 + (80 / (length + 4));
                var words = dictionary.GetWordsByLength(length);
                //Console.WriteLine($" {length:D02}: Chunks {words.Chunk(words.Count/items).Count()} ChunkSize {words.Chunk(words.Count / items).FirstOrDefault()?.Length}");


                Console.Write($" {length,-3}: {words.Chunk(Math.Max(1, words.Count / items))?.Select(chk => chk.FirstOrDefault("")).Take(items).Detailed()}");


                //Console.Write($" {length:D02}: {(words.Chunk(items).Skip(Random.Shared.Next(0, items)).Take(1)).FirstOrDefault().Detailed()}");

                if (items < words.Count)
                {
                    Console.Write($", (more {words.Count - items})");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
            //int count = 0;
            //bool printDetails = true;
            //foreach (var length in dictionary.GetAvailableLengths().OrderBy(l => l))
            //{
            //    count += dictionary.GetWordsByLength(length).Count;
            //    if (count > 50)
            //    {
            //        printDetails = false;
            //        break;
            //    }
            //}
            //if (printDetails)
            //{
            //    foreach (var length in dictionary.GetAvailableLengths().OrderBy(l => l))
            //    {
            //        var words = dictionary.GetWordsByLength(length);
            //        Console.WriteLine(words.Detailed());
            //    }
            //}
#endif

            List<string> matches;

            if (regex != null)
            {
                Console.WriteLine($"Searching with regex: [{regex}]");
                matches = dictionary.SearchWithRegex(regex);
            }
            else
            {
                matches = dictionary.Search(
                    includeAll,
                    includeOnly,
                    include,
                    exclude,
                    includeAllOrdered);
            }

            stopwatch.WritelineElapsedAndRestart("Searched in");

            var columnWidth = matches.Max(s => s.Length) + 4; // spaces for padding

            Console.WriteLine($"\nFound {matches.Count} matching words:");
            //Console.WriteLine("----- OLD");

            //for (int i = 0; i < matches.Count; i++)
            //{
            //    Console.Write(matches[i]);
            //    if (i % 4 == 3)
            //    {
            //        Console.WriteLine();
            //        continue;
            //    }
            //    Console.Write(_spaces[..(columnWidth - matches[i].Length)]);

            //}
            //Console.WriteLine();
            //Console.WriteLine("----- NEW");
            //Sort by length then alphabetically
            if (resultSortByLength)
            {
                matches = [.. matches
                .OrderBy(s => s.Length)
                .ThenBy(s => s, StringComparer.CurrentCulture)];
            }
            else
            {
                matches = [.. matches
                .OrderBy(s => s, StringComparer.CurrentCulture)];
            }

            stopwatch.WritelineElapsedAndRestart("Sorted in");

                if(resultSortByLength)
            {
                for(int len = matches.FirstOrDefault("").Length; len <= matches.LastOrDefault("").Length; len++)
                {
                    var countByLength = matches.Count(s => s.Length == len);
                    if(countByLength > 0)
                    {
                        Console.Write($"Length {len:D2}: {countByLength} word");
                        if(countByLength > 1)
                        {
                            Console.Write("(s)");
                        }
                        Console.WriteLine();

                        var wordsByLength = matches.Where(s => s.Length == len);
                        for(int chunkStart = 0; chunkStart < countByLength; chunkStart += PRINTING_COLUMNS)
                        {
                            Console.Write("  ");
                            var wordsLine = wordsByLength.Skip(chunkStart).Take(PRINTING_COLUMNS);
                            Console.WriteLine(wordsLine.Detailed(_spaces[..(columnWidth - len)]));
                        }
                    }
                }
            } else
            {
                List<string> starting = [.. matches.Select(s => s[..2]).DistinctBy(s => s)];
                foreach(var start in starting.OrderBy(s => s, StringComparer.CurrentCulture))
                {
                    var startGroup = matches.Where(s => s.StartsWith(start, StringComparison.CurrentCultureIgnoreCase)).ToList();
                    Console.WriteLine($"{start.ToUpperInvariant()}: {startGroup.Count}");
                    Console.WriteLine(startGroup.Detailed());
                }
            }

            Console.WriteLine();
        });

        var ret = rootCommand.Parse(args).Invoke();
        Console.WriteLine($"Command return code: {ret}.\n\tPress Enter to exit...");
        Console.ReadLine();
        return ret;
    }
}
