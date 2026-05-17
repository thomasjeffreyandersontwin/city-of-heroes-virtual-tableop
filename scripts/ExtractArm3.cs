// Uses the real app DLL to deserialize, then re-serializes just Armageddons
// csc /out:ExtractArm3.exe /reference:Newtonsoft.Json.dll /reference:Module.HeroVirtualTabletop.dll /reference:Module.Shared.dll ExtractArm3.cs
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Module.HeroVirtualTabletop.Crowds;

class Program
{
    static void Main()
    {
        string src = @"C:\hero-desktop\City Of Heroes\data\CrowdRepo.data.bak";
        string dst = @"C:\hero-desktop\City Of Heroes\data\CrowdRepo.data";

        Console.WriteLine("Deserializing full file ...");
        List<CrowdModel> all = Deserialize(src);
        if (all == null) { Console.WriteLine("ERROR: null result"); return; }
        Console.WriteLine("Crowds loaded: " + all.Count);

        // Find Armageddons
        CrowdModel arm = null;
        foreach (CrowdModel c in all)
        {
            Console.WriteLine("  " + c.Name);
            if (string.Equals(c.Name, "Armageddons", StringComparison.OrdinalIgnoreCase))
                arm = c;
        }
        if (arm == null) { Console.WriteLine("ERROR: Armageddons not found"); return; }

        Console.WriteLine("Members: " + arm.CrowdMemberCollection.Count);
        foreach (ICrowdMemberModel m in arm.CrowdMemberCollection)
            Console.WriteLine("  " + m.Name);

        // Null out RosterCrowd on each member to break circular serialization
        foreach (ICrowdMemberModel m in arm.CrowdMemberCollection)
        {
            CrowdMemberModel cm = m as CrowdMemberModel;
            if (cm != null) cm.RosterCrowd = null;
        }

        // Serialize just Armageddons
        var output = new List<CrowdModel> { arm };
        Serialize(dst, output);

        long kb = new FileInfo(dst).Length / 1024;
        Console.WriteLine("Written: " + dst + " (" + kb + " KB)");
    }

    static List<CrowdModel> Deserialize(string path)
    {
        var serializer = new JsonSerializer();
        serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
        serializer.ReferenceLoopHandling      = ReferenceLoopHandling.Ignore;
        serializer.Formatting                 = Formatting.Indented;
        serializer.TypeNameHandling           = TypeNameHandling.Objects;
        using (var sr = new StreamReader(path))
        using (var reader = new JsonTextReader(sr))
            return serializer.Deserialize<List<CrowdModel>>(reader);
    }

    static void Serialize(string path, List<CrowdModel> obj)
    {
        var serializer = new JsonSerializer();
        serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
        serializer.ReferenceLoopHandling      = ReferenceLoopHandling.Ignore;
        serializer.Formatting                 = Formatting.Indented;
        serializer.TypeNameHandling           = TypeNameHandling.Objects;
        using (var sw = new StreamWriter(path))
        using (var writer = new JsonTextWriter(sw))
            serializer.Serialize(writer, obj);
    }
}
