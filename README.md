# word-finder

A command-line tool to load a list of words and find possible matches based on various search criteria.

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run --project WordFinder -- [options]
```

Or after building:

```bash
./WordFinder/bin/Debug/net10.0/WordFinder [options]
```






> WordFinder --includeOnly [-only]
## Usage for "Spelling Bee", a mandatory letter (o) with only a few letters allowed (ahmnpt) phono, phantom, not homer
```bash
#"Spelling Bee", a mandatory letter (o) with only a few letters allowed (ahmnpt) phono, phantom, not homer
WordFinder -p dictionary.txt --only "ahomnpt" -all "o" -min 4


```
