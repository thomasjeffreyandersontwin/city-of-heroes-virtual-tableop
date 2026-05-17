// csc /out:ExtractArmageddons.exe /reference:Newtonsoft.Json.dll ExtractArmageddons.cs
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

class Program
{
    static void Main()
    {
        string src = @"C:\hero-desktop\City Of Heroes\data\CrowdRepo.data";
        string dst = @"C:\hero-desktop\City Of Heroes\data\CrowdRepo.data";

        Console.WriteLine("Reading ...");
        string raw = File.ReadAllText(src);

        // Extract full JSON block for each character by $id
        string[] charIds = new string[] { "541", "538", "1430" };
        string[] charBlocks = new string[charIds.Length];

        for (int c = 0; c < charIds.Length; c++)
        {
            string marker = "\"$id\": \"" + charIds[c] + "\"";
            int markerPos = raw.IndexOf(marker);
            if (markerPos < 0) { Console.WriteLine("NOT FOUND: " + charIds[c]); return; }
            // walk back to opening {
            int start = markerPos;
            while (start > 0 && raw[start] != '{') start--;
            // brace-count forward
            int depth = 0, end = start;
            for (int i = start; i < raw.Length; i++)
            {
                if (raw[i] == '{') depth++;
                else if (raw[i] == '}') { depth--; if (depth == 0) { end = i; break; } }
            }
            string block = raw.Substring(start, end - start + 1);
            Console.WriteLine("Extracted $id=" + charIds[c] + " size=" + block.Length);

            // Collect all $ids defined WITHIN this block
            var localIds = new HashSet<string>();
            foreach (Match m in Regex.Matches(block, "\"\\$id\":\\s*\"(\\d+)\""))
                localIds.Add(m.Groups[1].Value);
            Console.WriteLine("  Local $ids: " + localIds.Count);

            // Replace any $ref that points OUTSIDE this block with null
            block = Regex.Replace(block,
                "\\{\\s*\"\\$ref\":\\s*\"(\\d+)\"\\s*\\}",
                delegate(Match m) {
                    string refId = m.Groups[1].Value;
                    if (localIds.Contains(refId)) return m.Value; // internal - keep
                    return "null";
                });

            charBlocks[c] = block;
        }

        // Build new CrowdRepo.data: one Armageddons crowd with 3 inline members
        var sb = new StringBuilder();
        sb.AppendLine("[");
        sb.AppendLine("  {");
        sb.AppendLine("    \"$id\": \"1\",");
        sb.AppendLine("    \"$type\": \"Module.HeroVirtualTabletop.Crowds.CrowdModel, Module.HeroVirtualTabletop\",");
        sb.AppendLine("    \"IsGangMode\": true,");
        sb.AppendLine("    \"Name\": \"Armageddons\",");
        sb.AppendLine("    \"Order\": 0,");
        sb.AppendLine("    \"CrowdMemberCollection\": [");
        for (int c = 0; c < charBlocks.Length; c++)
        {
            sb.Append(charBlocks[c]);
            if (c < charBlocks.Length - 1) sb.AppendLine(",");
            else sb.AppendLine();
        }
        sb.AppendLine("    ],");
        sb.AppendLine("    \"SavedPositions\": {}");
        sb.AppendLine("  }");
        sb.AppendLine("]");

        string output = sb.ToString();
        File.WriteAllText(dst, output);
        long kb = new FileInfo(dst).Length / 1024;
        Console.WriteLine("Done: " + dst + " (" + kb + " KB)");
    }
}
