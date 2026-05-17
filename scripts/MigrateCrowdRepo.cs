// MigrateCrowdRepo.cs  (C# 5 compatible)
// Compile:  csc /out:MigrateCrowdRepo.exe /r:Newtonsoft.Json.dll MigrateCrowdRepo.cs
// Run:      MigrateCrowdRepo.exe [GameDir]
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class MigrateCrowdRepo
{
    static string charDir;
    static readonly Dictionary<string, bool> savedChars = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, JObject> refMap  = new Dictionary<string, JObject>();
    static readonly char[] badChars = Path.GetInvalidFileNameChars();

    // ---- helpers -------------------------------------------------------

    static string GetStr(JObject obj, string key)
    {
        var t = obj[key];
        return t != null ? t.ToString() : "";
    }

    static int GetOrder(JObject obj)
    {
        var t = obj["Order"];
        return t != null ? (int)t : 0;
    }

    static string SafeFileName(string name)
    {
        var chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (Array.IndexOf(badChars, chars[i]) >= 0) chars[i] = '_';
        return new string(chars);
    }

    // Walk every JToken in the tree and register all objects that have "$id"
    static void BuildRefMap(JToken token)
    {
        if (token is JObject)
        {
            var obj = (JObject)token;
            var idToken = obj["$id"];
            if (idToken != null)
            {
                string id = idToken.ToString();
                if (!refMap.ContainsKey(id))
                    refMap[id] = obj;
            }
            foreach (var prop in obj.Properties())
                BuildRefMap(prop.Value);
        }
        else if (token is JArray)
        {
            foreach (var item in (JArray)token)
                BuildRefMap(item);
        }
    }

    // If obj is a $ref stub, return the full object from the map; else return obj
    static JObject Resolve(JObject obj)
    {
        var refToken = obj["$ref"];
        if (refToken == null) return obj;
        string id = refToken.ToString();
        JObject resolved;
        return refMap.TryGetValue(id, out resolved) ? resolved : obj;
    }

    // ---- processing ----------------------------------------------------

    static JObject MakeMemberEntry(string name, int order, bool isCrowd, JArray members)
    {
        var o = new JObject();
        o["Name"]    = name;
        o["Order"]   = order;
        o["IsCrowd"] = isCrowd;
        if (members != null) o["Members"] = members;
        return o;
    }

    static JObject ProcessMember(JObject raw, HashSet<string> seen)
    {
        JObject member = Resolve(raw);

        bool isCrowd = member["CrowdMemberCollection"] != null
                    || (member["$type"] != null && member["$type"].ToString().Contains("CrowdModel"));

        string name  = GetStr(member, "Name");
        int    order = GetOrder(member);

        if (string.IsNullOrEmpty(name)) return null;
        if (!seen.Add(name))            return null;

        if (isCrowd)
        {
            var nestedSeen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nestedMembers = new JArray();
            var coll = member["CrowdMemberCollection"] as JArray;
            if (coll != null)
            {
                foreach (JObject child in coll.OfType<JObject>())
                {
                    var entry = ProcessMember(child, nestedSeen);
                    if (entry != null) nestedMembers.Add(entry);
                }
            }
            return MakeMemberEntry(name, order, true, nestedMembers);
        }

        // Character — save its data file once
        if (!savedChars.ContainsKey(name))
        {
            var charCopy = (JObject)member.DeepClone();
            charCopy.Remove("RosterCrowd");
            charCopy.Remove("IsExpanded");
            charCopy.Remove("IsMatched");
            charCopy.Remove("Order");
            File.WriteAllText(
                Path.Combine(charDir, SafeFileName(name) + ".json"),
                charCopy.ToString(Formatting.Indented));
            savedChars[name] = true;
        }

        return MakeMemberEntry(name, order, false, null);
    }

    static JObject ProcessCrowd(JObject crowd)
    {
        string name  = GetStr(crowd, "Name");
        int    order = GetOrder(crowd);

        var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var members = new JArray();
        var coll    = crowd["CrowdMemberCollection"] as JArray;
        if (coll != null)
        {
            foreach (JObject member in coll.OfType<JObject>())
            {
                var entry = ProcessMember(member, seen);
                if (entry != null) members.Add(entry);
            }
        }

        var def = new JObject();
        def["Name"]    = name;
        def["Order"]   = order;
        def["Members"] = members;
        return def;
    }

    // ---- main ----------------------------------------------------------

    static void Main(string[] args)
    {
        string gameDir  = args.Length > 0 ? args[0] : @"C:\hero-desktop\City Of Heroes";
        string dataDir  = Path.Combine(gameDir, "data");
        string repoFile = Path.Combine(dataDir, "CrowdRepo.data");
        string bakFile  = Path.Combine(dataDir, "CrowdRepo.data.bak");
        charDir         = Path.Combine(dataDir, "characters");

        string sourceFile = File.Exists(bakFile) ? bakFile : repoFile;
        Console.WriteLine("Source: " + sourceFile);

        if (!File.Exists(sourceFile))
        {
            Console.Error.WriteLine("ERROR: source file not found");
            Environment.Exit(1);
        }

        string peek = File.ReadAllText(sourceFile);
        if (!peek.Contains("CrowdMemberCollection"))
        {
            Console.Error.WriteLine("Source is already in lean format. Aborting.");
            Environment.Exit(1);
        }

        Console.WriteLine("Parsing...");
        // Load raw — no PreserveReferences, no TypeNameHandling
        // $ref stubs remain as {"$ref":"N"} objects; we resolve manually
        JArray oldData = JArray.Parse(peek);
        Console.WriteLine("Parsed " + oldData.Count + " top-level crowds");

        Console.WriteLine("Building reference map...");
        BuildRefMap(oldData);
        Console.WriteLine("Reference map: " + refMap.Count + " entries");

        Directory.CreateDirectory(charDir);

        var leanCrowds = new JArray();
        foreach (JObject crowd in oldData.OfType<JObject>())
        {
            var lean = ProcessCrowd(crowd);
            leanCrowds.Add(lean);
            Console.WriteLine("  Crowd: " + lean["Name"] + "  (" + ((JArray)lean["Members"]).Count + " members)");
        }

        Console.WriteLine("\nCharacters extracted: " + savedChars.Count);

        if (sourceFile == repoFile && !File.Exists(bakFile))
        {
            File.Copy(repoFile, bakFile);
            Console.WriteLine("Backup created: " + bakFile);
        }

        File.WriteAllText(repoFile, leanCrowds.ToString(Formatting.Indented));
        Console.WriteLine("Written: " + repoFile);
        Console.WriteLine("Done.");
    }
}
