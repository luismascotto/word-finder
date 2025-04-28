using System;

namespace WordFinder;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            string wordFile;

            if (args.Length > 0)
            {
                wordFile = args[0];
                if (!File.Exists(wordFile))
                {
                    Console.WriteLine($"Error: File not found at: {wordFile}");
                    return;
                }
            }
            else
            {
                var resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources");
                Directory.CreateDirectory(resourcesPath);
                wordFile = Path.Combine(resourcesPath, "words_alpha.txt");
                if (!File.Exists(wordFile))
                {
                    Console.WriteLine($"Error: Default word file not found at: {wordFile}");
                    Console.WriteLine("Please provide the path to the words file as a command-line argument.");
                    return;
                }
            }

            Console.WriteLine($"Loading words from: {wordFile}");
            var dictionary = new WordDictionary(wordFile);
            dictionary.LoadWords();

            Console.WriteLine("\nAvailable word lengths:");
            foreach (var length in dictionary.GetAvailableLengths())
            {
                var wordCount = dictionary.GetWordsByLength(length).Count;
                Console.WriteLine($"Length {length}: {wordCount} words");
            }

            while (true)
            {
                Console.WriteLine("\nEnter a word length to see some examples (or press Enter to exit): ");
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    break;
                }

                if (!int.TryParse(input, out int length))
                {
                    Console.WriteLine("Please enter a valid number");
                    continue;
                }

                var words = dictionary.GetWordsByLength(length);
                if (words.Count <= 0)
                {
                    Console.WriteLine($"No words found with length {length}");
                    continue;
                }

                Console.WriteLine($"\nFirst 5 words of length {length}:");
                foreach (var word in words.Take(5))
                {
                    Console.WriteLine(word);
                }
                Console.WriteLine($"\nTotal words of length {length}: {words.Count}");


            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        //Ask for "minimum length", "maximum length", "containing letters" and "not containing letters"

        Console.WriteLine("\nEnter the minimum length: ");
        var inputLength = Console.ReadLine();
        if (string.IsNullOrEmpty(inputLength))
        {
            Console.WriteLine("No length provided");
        }

        Console.WriteLine("\nEnter the maximum length: ");
        var inputMaxLength = Console.ReadLine();
        if (string.IsNullOrEmpty(inputMaxLength))
        {
            Console.WriteLine("No maximum length provided");
        }

        Console.WriteLine("\nEnter the letters/words you want to find comma separated: ");
        var inputInclude = Console.ReadLine();
        if (string.IsNullOrEmpty(inputInclude))
        {
            Console.WriteLine("No letters provided");
        }

        Console.WriteLine("\nEnter the letters/words you want to exclude comma separated: ");
        var inputExclude = Console.ReadLine();
        if (string.IsNullOrEmpty(inputExclude))
        {
            Console.WriteLine("No letters provided");
        }
        
        //TODO: Implement search
    }
}
