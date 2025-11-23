using System.CommandLine;

namespace WordFinder;

class Program
{
    //--includeAll "gce" --include "st" --exclude "jzx" --min-length 8 --max-length 16 --include-ordered true
    const string _spaces = "                                                                                                                                                                         ";
    static int Main(string[] args)
    {
        var rootCommand = new RootCommand("Word Finder - A tool to search for words based on various criteria");
        rootCommand.Aliases.Add("word-finder");
        

        var fileOption = new Option<string>("--file", ["-p"])
        {
            Description = "Path to the words file (text, one word per line)",
            Required = true,
            HelpName = "file-path",
            //DefaultValueFactory = parseResult => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "words_alpha.txt")
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

        // "abefkst": Valids: beetle, test, sorry, fly. Invalids: clock, dull
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

        rootCommand.Options.Add(fileOption);
        rootCommand.Options.Add(regexOption);
        rootCommand.Options.Add(includeAllOption);
        rootCommand.Options.Add(includeOnlyOption);
        rootCommand.Options.Add(includeOption);
        rootCommand.Options.Add(excludeOption);
        rootCommand.Options.Add(minLengthOption);
        rootCommand.Options.Add(maxLengthOption);
        rootCommand.Options.Add(includeAllOrderedOption);

    
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
                Console.WriteLine("Regex was specified, all other options are ineffective.");
            }
            else if (includeOnly != null && !(
               includeAll == null &&
               include == null &&
               exclude == null))
            {
                Console.WriteLine("Include Only was specified, all other include/exclude are ineffective.");
            }

            if (minLength.GetValueOrDefault() > maxLength.GetValueOrDefault())
            {
                throw new ArgumentException("Minimum length cannot be greater than maximum length");
            }

            Console.WriteLine($"Loading words from: {file}");
            var dictionary = new WordDictionary(file);
            dictionary.LoadWords();

#if DEBUG
            Console.WriteLine($"Loaded {dictionary.GetAvailableLengths().Count()} word lengths.");
            foreach (var length in dictionary.GetAvailableLengths().OrderBy(l => l))
            {
                var items = Math.Max(1, 120 / Math.Max(1, length));
                var words = dictionary.GetWordsByLength(length);
                Console.Write($"{length:D02}: {words.Take(items).Detailed()}");
                if (items < words.Count)
                {
                    Console.Write($",..more {words.Count - items} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("---------------------------------------------------");
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


            Console.WriteLine($"\nFound {matches.Count} matching words:");
            matches.Sort();

            var columnWidth = (maxLength ?? matches.Max(s => s.Length)) + 2; // Add 2 spaces for padding

            for (int i = 0; i < matches.Count; i++)
            {
                Console.Write(matches[i]);
                if ((matches.Count - i) % 4 == 0)
                {
                    Console.WriteLine();
                    continue;
                }
                Console.Write(_spaces[..(columnWidth - matches[i].Length)]);

            }
            Console.WriteLine();

        });

        var ret = rootCommand.Parse(args).Invoke();
        Console.WriteLine($"Command returned {ret}. Press Enter to exit...");
        Console.ReadLine();
        return ret;
    }
}
