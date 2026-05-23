// Standalone HVT CrowdRepo.data migrator.
// Deps: only Newtonsoft.Json.dll (already in the COH folder).
// Reads the monolithic file, writes crowds/*.json and characters/*.json.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Migrator
{
    static StreamWriter logFile;

    static void Log(string msg)
    {
        string line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg;
        Console.WriteLine(line);
        logFile.WriteLine(line);
        logFile.Flush();
    }

    static int Main(string[] args)
    {
        string cohDir   = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\city-of-heroes"));
        string dataDir  = Path.Combine(cohDir, "data");
        string dataFile = Path.Combine(dataDir, "CrowdRepo.data");
        string crowdsDir = Path.Combine(dataDir, "crowds");
        string charsDir  = Path.Combine(dataDir, "characters");
        string marker    = Path.Combine(dataDir, ".crowd_migrated");
        string logPath   = Path.Combine(cohDir, "hvt_migrate.log");

        using (logFile = new StreamWriter(logPath, false, Encoding.UTF8))
        {
            Log("=== HVT Migration v2 (standalone DOM parser) ===");
            Log("COH dir  : " + cohDir);
            Log("Data file: " + dataFile);

            if (!File.Exists(dataFile)) { Log("ERROR: data file not found!"); return 1; }
            if (File.Exists(marker) && Directory.Exists(crowdsDir))
            {
                Log("Already migrated (marker + crowds/ exist). Nothing to do.");
                return 0;
            }

            // ----------------------------------------------------------------
            // 1. Parse JSON with a DOM reader — progress every 10 MB
            // ----------------------------------------------------------------
            Log("Step 1: Parsing JSON (may take a few minutes for 106 MB)...");
            long fileLen = new FileInfo(dataFile).Length;
            Log("  File size: " + (fileLen / 1024 / 1024) + " MB");

            JArray root;
            using (FileStream fs = File.OpenRead(dataFile))
            using (StreamReader sr = new StreamReader(fs))
            {
                // Wrap reader to track progress
                var trackingReader = new TrackingTextReader(sr, fs, fileLen);
                var jsonReader = new JsonTextReader(trackingReader);
                root = JArray.Load(jsonReader);
            }
            Log("  Parsed OK. Top-level items: " + root.Count);

            // ----------------------------------------------------------------
            // 2. Build $id → JObject map (one pass over the DOM)
            // ----------------------------------------------------------------
            Log("Step 2: Building ref map...");
            var refMap = new Dictionary<string, JObject>(StringComparer.Ordinal);
            BuildRefMap(root, refMap);
            Log("  Ref map entries: " + refMap.Count);

            // ----------------------------------------------------------------
            // 3. Create output directories
            // ----------------------------------------------------------------
            Directory.CreateDirectory(crowdsDir);
            Directory.CreateDirectory(charsDir);
            Log("Step 3: Output dirs ready.");

            // ----------------------------------------------------------------
            // 4. Find "All Characters" crowd to get all full character objects
            // ----------------------------------------------------------------
            Log("Step 4: Collecting characters from All Characters crowd...");
            var allCharacters = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            // Also track by $id to avoid double-writing refs
            var savedIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (JObject topItem in root)
            {
                string name = GetString(topItem, "Name");
                if (string.Equals(name, "All Characters", StringComparison.OrdinalIgnoreCase))
                {
                    JArray members = topItem["CrowdMemberCollection"] as JArray;
                    if (members != null)
                    {
                        foreach (JToken mt in members)
                        {
                            JObject member = Resolve(mt, refMap);
                            if (member == null) continue;
                            if (IsCrowd(member)) continue; // nested crowd, skip
                            string charName = GetString(member, "Name");
                            if (string.IsNullOrEmpty(charName)) continue;
                            if (!allCharacters.ContainsKey(charName))
                                allCharacters[charName] = member;
                        }
                    }
                    Log("  Found " + allCharacters.Count + " characters in All Characters crowd.");
                    break;
                }
            }

            // Fall back: collect from ALL crowds if "All Characters" not found
            if (allCharacters.Count == 0)
            {
                Log("  'All Characters' not found, scanning all crowds...");
                CollectAllCharacters(root, refMap, allCharacters);
                Log("  Found " + allCharacters.Count + " characters total.");
            }

            // ----------------------------------------------------------------
            // 5. Save character files
            // ----------------------------------------------------------------
            Log("Step 5: Saving " + allCharacters.Count + " character files...");
            int charCount = 0;
            foreach (var kv in allCharacters)
            {
                string safeName = SanitizeFileName(kv.Key);
                string charPath = Path.Combine(charsDir, safeName + ".json");
                File.WriteAllText(charPath, kv.Value.ToString(Formatting.Indented));
                charCount++;
                if (charCount % 100 == 0)
                    Log("  Saved " + charCount + " / " + allCharacters.Count + " characters...");
            }
            Log("  Done: " + charCount + " character files written.");

            // ----------------------------------------------------------------
            // 6. Save crowd shell files
            // ----------------------------------------------------------------
            Log("Step 6: Saving crowd shell files...");
            int crowdCount = 0;
            foreach (JObject topItem in root)
            {
                if (!IsCrowd(topItem)) continue;
                SaveCrowdShell(topItem, refMap, crowdsDir);
                crowdCount++;
                Log("  [" + crowdCount + "] Crowd: " + GetString(topItem, "Name"));
            }
            Log("  Done: " + crowdCount + " crowd files written.");

            // ----------------------------------------------------------------
            // 7. Write marker and backup
            // ----------------------------------------------------------------
            File.WriteAllText(marker, DateTime.UtcNow.ToString("o"));
            Log("Step 7: Marker written.");

            string backup = dataFile + ".pre-split";
            if (!File.Exists(backup))
            {
                Log("  Backing up original data file...");
                File.Copy(dataFile, backup, false);
                Log("  Backup: " + backup);
            }

            Log("=== MIGRATION COMPLETE ===");
            Log("Crowds   : " + crowdCount + " files in " + crowdsDir);
            Log("Characters: " + charCount + " files in " + charsDir);
            return 0;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    static void BuildRefMap(JToken token, Dictionary<string, JObject> refMap)
    {
        if (token is JObject)
        {
            JObject obj = (JObject)token;
            JToken id;
            if (obj.TryGetValue("$id", out id))
                refMap[(string)id] = obj;
            foreach (var prop in obj.Properties())
                BuildRefMap(prop.Value, refMap);
        }
        else if (token is JArray)
        {
            JArray arr = (JArray)token;
            foreach (JToken child in arr)
                BuildRefMap(child, refMap);
        }
    }

    static JObject Resolve(JToken token, Dictionary<string, JObject> refMap)
    {
        if (token is JObject)
        {
            JObject obj = (JObject)token;
            JToken refVal;
            if (obj.TryGetValue("$ref", out refVal))
            {
                JObject resolved;
                if (refMap.TryGetValue((string)refVal, out resolved))
                    return resolved;
                return null;
            }
            return obj;
        }
        return null;
    }

    static bool IsCrowd(JObject obj)
    {
        string t = GetString(obj, "$type");
        return t != null && t.Contains("CrowdModel");
    }

    static string GetString(JObject obj, string key)
    {
        JToken v;
        if (obj.TryGetValue(key, out v) && v.Type == JTokenType.String)
            return (string)v;
        return null;
    }

    static void CollectAllCharacters(JArray root, Dictionary<string, JObject> refMap, Dictionary<string, JObject> result)
    {
        foreach (JObject topItem in root)
        {
            JArray members = topItem["CrowdMemberCollection"] as JArray;
            if (members == null) continue;
            foreach (JToken mt in members)
            {
                JObject member = Resolve(mt, refMap);
                if (member == null || IsCrowd(member)) continue;
                string name = GetString(member, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                if (!result.ContainsKey(name))
                    result[name] = member;
            }
        }
    }

    static void SaveCrowdShell(JObject crowd, Dictionary<string, JObject> refMap, string crowdsDir)
    {
        string name  = GetString(crowd, "Name") ?? "Unknown";
        int order    = 0;
        JToken ov;
        if (crowd.TryGetValue("Order", out ov) && ov.Type == JTokenType.Integer)
            order = (int)ov;

        var memberRefs = new List<object>();
        JArray members = crowd["CrowdMemberCollection"] as JArray;
        if (members != null)
        {
            foreach (JToken mt in members)
            {
                JObject member = Resolve(mt, refMap);
                if (member == null) continue;
                memberRefs.Add(new { Name = GetString(member, "Name"), IsCrowd = IsCrowd(member) });
            }
        }

        var shell = new { Name = name, Order = order, Members = memberRefs };
        string safeName = SanitizeFileName(name);
        File.WriteAllText(Path.Combine(crowdsDir, safeName + ".json"),
            JsonConvert.SerializeObject(shell, Formatting.Indented));
    }

    static string SanitizeFileName(string name)
    {
        if (name == null) return "_";
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}

// ---------------------------------------------------------------------------
// Progress-tracking wrapper around a TextReader
// ---------------------------------------------------------------------------
class TrackingTextReader : TextReader
{
    readonly StreamReader inner;
    readonly FileStream stream;
    readonly long totalBytes;
    int lastReportedPct = -1;
    DateTime lastReport = DateTime.MinValue;

    public TrackingTextReader(StreamReader inner, FileStream stream, long totalBytes)
    {
        this.inner = inner;
        this.stream = stream;
        this.totalBytes = totalBytes;
    }

    void MaybeReport()
    {
        if (totalBytes <= 0) return;
        if ((DateTime.Now - lastReport).TotalSeconds < 10) return;
        long pos = stream.Position;
        int pct = (int)(pos * 100L / totalBytes);
        if (pct != lastReportedPct)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + "  Parsing... " + pct + "% (" + (pos / 1024 / 1024) + " MB / " + (totalBytes / 1024 / 1024) + " MB)");
            lastReportedPct = pct;
            lastReport = DateTime.Now;
        }
    }

    public override int Read()                         { MaybeReport(); return inner.Read(); }
    public override int Read(char[] buf, int idx, int count) { MaybeReport(); return inner.Read(buf, idx, count); }
    public override int Peek()                         { return inner.Peek(); }
    public override string ReadLine()                  { MaybeReport(); return inner.ReadLine(); }
    public override string ReadToEnd()                 { MaybeReport(); return inner.ReadToEnd(); }
    protected override void Dispose(bool disposing)   { if (disposing) inner.Dispose(); base.Dispose(disposing); }
}
