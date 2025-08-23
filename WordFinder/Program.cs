using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace WordFinder;

class Program
{
    //--includeAll "gce" --include "st" --exclude "jzx" --min-length 8 --max-length 16 --include-ordered true
    const string _spaces = "                                                                                                                                                                         ";
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Word Finder - A tool to search for words based on various criteria");

        var fileOption = new Option<string>(
            name: "--file",
            description: "Path to the words file",
            getDefaultValue: () => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "words_alpha.txt"));

        var regexOption = new Option<string?>(
            name: "--regex",
            description: "Regular expression for word matching");

        var includeAllOption = new Option<string?>(
            name: "--includeAll",
            description: "Letters that must be all present in the word");

        var includeOption = new Option<string?>(
            name: "--include",
            description: "Letters that must appear at least one in the word");

        var excludeOption = new Option<string?>(
            name: "--exclude",
            description: "Letters that cannot exist in the word");

        var minLengthOption = new Option<int?>(
            name: "--min-length",
            description: "Minimum word length");

        var maxLengthOption = new Option<int?>(
            name: "--max-length",
            description: "Maximum word length");

        var includeAllOrderedOption = new Option<bool>(
            name: "--ordered",
            description: "IncludeAll letters must respect order of appearance");

        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(regexOption);
        rootCommand.AddOption(includeAllOption);
        rootCommand.AddOption(includeOption);
        rootCommand.AddOption(excludeOption);
        rootCommand.AddOption(minLengthOption);
        rootCommand.AddOption(maxLengthOption);
        rootCommand.AddOption(includeAllOrderedOption);


        rootCommand.SetHandler(context =>
        {
            var file = context.ParseResult.GetValueForOption(fileOption);
            var regex = context.ParseResult.GetValueForOption(regexOption);
            var includeAll = context.ParseResult.GetValueForOption(includeAllOption);
            var include = context.ParseResult.GetValueForOption(includeOption);
            var exclude = context.ParseResult.GetValueForOption(excludeOption);
            var minLength = context.ParseResult.GetValueForOption(minLengthOption);
            var maxLength = context.ParseResult.GetValueForOption(maxLengthOption);
            var includeAllOrdered = context.ParseResult.GetValueForOption(includeAllOrderedOption);

            if (!File.Exists(file))
            {
                Console.WriteLine($"Error: Word file not found at: {file}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine($"Loading words from: {file}");
            var dictionary = new WordDictionary(file);
            dictionary.LoadWords();

            List<string> matches;

            if (regex != null)
            {
                Console.WriteLine($"Searching with regex: [{regex}]");
                matches = dictionary.SearchWithRegex(regex, minLength?.ToString(), maxLength?.ToString());
            }
            else
            {
                matches = dictionary.Search(
                    includeAll,
                    include,
                    exclude,
                    minLength?.ToString(),
                    maxLength?.ToString(),
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

        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }
}
