using System.Diagnostics;
using System.Text;
using System.Text.Json;

// string[] words = JsonSerializer.Deserialize<string[]>(File.ReadAllText("words.txt"))!;
//
// File.WriteAllLines("wordslist.txt", words);
//
// return 0;

// string[] words2 = File.ReadAllLines("wordslist.txt");
// File.WriteAllText("words.txt", JsonSerializer.Serialize(words2));
// return 0;

// Load words.txt
string[] words = JsonSerializer.Deserialize<string[]>(File.ReadAllText("words.txt"))!;

Console.Write("Enter pull URL: ");
string url = Console.ReadLine() ?? "";

// Run git clone
string[] dirs = Directory.GetDirectories(".");
ProcessStartInfo startInfo = new() {
    FileName = "git",
    Arguments = $"clone {url}",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    CreateNoWindow = true
};
using Process process = Process.Start(startInfo)!;
process.WaitForExit();

// Get the name of the new directory
string[] newDirs = Directory.GetDirectories(".");
string newDir = newDirs.Except(dirs).Single();

Console.WriteLine($"Cloned into {newDir} successfully");

// Recursively search files for spelling mistakes except files in the .git directory
string[] files = Directory.GetFiles(newDir, "*.*", SearchOption.AllDirectories)
    .Where(f => !f.Contains(".git")).ToArray();

// Filter out files that aren't text files
files = files.Where(f => {
    string ext = Path.GetExtension(f);
    return ext is ".txt" or ".md" or ".cs" or ".json" or ".html";
}).ToArray();

List<string> incorrectWords = new();

foreach (string file in files) {
    string text = File.ReadAllText(file);
    
    // Generate a list of words in the file split by spaces . , ; : ! ? " ' ( ) [ ] { } < > / \ | ` ~ @ # $ % ^ & * - _ + = \r \n
    string[] fileWords = text.Split(' ', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '|', '`', '~', '@', '#', '$', '%', '^', '&', '*', '-', '_', '+', '=', '\r', '\n')
        .Select(w => w.Trim('\n', ' ', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '|', '`', '~', '@', '#', '$', '%', '^', '&', '*', '-', '=', '+', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'))
        .Where(w => w.Length > 0).ToArray();
    
    // Separate words in camelCase EG: camelCase -> camel Case
    List<string> camelCaseWords = new();
    
    foreach (string word in fileWords) {

        if (word.Length < 4) {
            continue;
        }
        
        // If the word is camelCase
        if (word.Any(char.IsUpper) && !word.All(char.IsUpper)) {
            StringBuilder sb = new();
            
            // Loop through each character in the word
            for (int i = 0; i < word.Length; i++) {
                char c = word[i];
                
                // If the character is uppercase and it's not the first character
                if (char.IsUpper(c) && i != 0) {
                    // Add a space before the character
                    sb.Append(' ');
                }
                
                // Add the character to the string builder
                sb.Append(c);
            }
            
            // Add the new word to the list
            string[] wordsSplit = sb.ToString().Split(" ");
            camelCaseWords.AddRange(wordsSplit.Where(w => w.Length >= 4));
        } else camelCaseWords.Add(word);
    }

    fileWords = camelCaseWords.ToArray();

    // Make all words uppercase
    fileWords = fileWords.Select(w => w.ToUpper()).ToArray();


    // Remove all entries that are spaces or empty
    fileWords = fileWords.Where(w => w.Length > 0).ToArray();

    // Check if any of the words are not in the dictionary
    string[] badWords = fileWords.Except(words).ToArray();
    incorrectWords.AddRange(badWords);
    if (badWords.Length > 0) {
        Console.WriteLine($"Found spelling mistakes in {file}:");
        foreach (string badWord in badWords) {
            Console.WriteLine(badWord);
        }
    }
}

// Delete the directory
Console.WriteLine("Deleting directory...");
Directory.Delete(newDir, true);

Console.WriteLine("Done");

// Ask user whether each word is actually right
List<string> actuallyGoodWords = new();
foreach (string word in incorrectWords.Distinct()) {
    Console.Write($"\nIs {word} a correct word? (y/n): ");
    ConsoleKeyInfo input = Console.ReadKey();
    if (input.Key == ConsoleKey.Y) {
        Console.WriteLine("Adding to dictionary...");
        actuallyGoodWords.Add(word);
    }
}

Console.WriteLine("\n\ngood words:");
foreach (string word in actuallyGoodWords) {
    Console.WriteLine(word);
}

return 0;