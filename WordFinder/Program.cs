using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace WordFinder;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Word Finder - A tool to search for words based on various criteria");

        var fileOption = new Option<string>(
            name: "--file",
            description: "Path to the words file",
            getDefaultValue: () => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "words_alpha.txt"));

        var includeOption = new Option<string?>(
            name: "--include",
            description: "Letters that must be included in the word");

        var excludeOption = new Option<string?>(
            name: "--exclude",
            description: "Letters that must be excluded from the word");

        var minLengthOption = new Option<int?>(
            name: "--min-length",
            description: "Minimum word length");

        var maxLengthOption = new Option<int?>(
            name: "--max-length",
            description: "Maximum word length");

        var includeOrderedOption = new Option<bool>(
            name: "--include-ordered",
            description: "Include letters in order");

        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(includeOption);
        rootCommand.AddOption(excludeOption);
        rootCommand.AddOption(minLengthOption);
        rootCommand.AddOption(maxLengthOption);
        rootCommand.AddOption(includeOrderedOption);


        rootCommand.SetHandler(context =>
        {
            var file = context.ParseResult.GetValueForOption(fileOption);
            var include = context.ParseResult.GetValueForOption(includeOption);
            var exclude = context.ParseResult.GetValueForOption(excludeOption);
            var minLength = context.ParseResult.GetValueForOption(minLengthOption);
            var maxLength = context.ParseResult.GetValueForOption(maxLengthOption);
            var includeOrdered = context.ParseResult.GetValueForOption(includeOrderedOption);

            if (!File.Exists(file))
            {
                Console.WriteLine($"Error: Word file not found at: {file}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine($"Loading words from: {file}");
            var dictionary = new WordDictionary(file);
            dictionary.LoadWords();

            var matches = dictionary.Search(
                include,
                exclude,
                minLength?.ToString(),
                maxLength?.ToString(),
                includeOrdered);

            Console.WriteLine($"\nFound {matches.Count} matching words:");
            matches.Sort();

            var columnWidth = (maxLength ?? 0) + 2; // Add 2 spaces for padding

            for (int i = 0; i < matches.Count; i++)
            {
                Console.Write(matches[i]);
                if ((matches.Count - i) % 4 == 0)
                {
                    Console.WriteLine();
                    continue;
                }
                Console.Write("             "[..(columnWidth - matches[i].Length)]);

            }
            Console.WriteLine();
        });

        return await rootCommand.InvokeAsync(args);
    }
}
