// csc /out:ExtractAllChars.exe ExtractAllChars.cs
// Extracts only the "All Characters" crowd from CrowdRepo.data
using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string src = @"C:\hero-desktop\City Of Heroes\data\CrowdRepo.data.bak";
        string dst = @"C:\hero-desktop\City Of Heroes\data\CrowdRepo.data";

        Console.WriteLine("Reading...");
        string raw = File.ReadAllText(src);

        // Find the opening [ of the root array
        int arrayStart = raw.IndexOf('[');
        // Find first { — start of "All Characters" crowd
        int crowdStart = raw.IndexOf('{', arrayStart);

        // Brace-count to find the end of "All Characters"
        int depth = 0, crowdEnd = crowdStart;
        for (int i = crowdStart; i < raw.Length; i++)
        {
            if (raw[i] == '{') depth++;
            else if (raw[i] == '}') { depth--; if (depth == 0) { crowdEnd = i; break; } }
        }

        string allCharsCrowd = raw.Substring(crowdStart, crowdEnd - crowdStart + 1);
        Console.WriteLine("All Characters block: " + allCharsCrowd.Length + " chars");

        // Verify it really is All Characters
        if (!allCharsCrowd.Contains("\"All Characters\""))
        {
            Console.WriteLine("ERROR: first block is not All Characters!");
            return;
        }

        string result = "[\n" + allCharsCrowd + "\n]";
        File.WriteAllText(dst, result);
        Console.WriteLine("Written: " + dst + " (" + new FileInfo(dst).Length / 1024 + " KB)");
    }
}
