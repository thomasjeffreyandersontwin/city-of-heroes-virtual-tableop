using System;
using System.IO;
using System.Reflection;

class Migrate
{
    static void Log(string msg)
    {
        string line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg;
        Console.WriteLine(line);
        File.AppendAllText("migrate_log.txt", line + "\r\n");
    }

    static int Main(string[] args)
    {
        File.WriteAllText("migrate_log.txt", "");
        string coh = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\city-of-heroes"));
        string binDir = coh;

        Log("=== HVT Migration ===");
        Log("COH dir: " + coh);

        // Set up assembly resolver so all the HVT DLLs load from the COH folder
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
        {
            string name = new AssemblyName(e.Name).Name + ".dll";
            string path = Path.Combine(binDir, name);
            if (File.Exists(path))
            {
                Log("  [resolve] " + name);
                return Assembly.LoadFrom(path);
            }
            return null;
        };

        try
        {
            Log("Loading Module.HeroVirtualTabletop.dll...");
            Assembly hvt = Assembly.LoadFrom(Path.Combine(binDir, "Module.HeroVirtualTabletop.dll"));

            Log("Creating CrowdRepository...");
            Type repoType = hvt.GetType("Module.HeroVirtualTabletop.Crowds.CrowdRepository");
            if (repoType == null) { Log("ERROR: CrowdRepository type not found!"); return 1; }

            // Use the data-dir constructor
            string dataDir = Path.Combine(coh, "data");
            object repo = Activator.CreateInstance(repoType, new object[] { dataDir });
            Log("CrowdRepository created. Data dir: " + dataDir);

            Log("Running MigrateToSplitFormat()... (this will take several minutes for the 106MB file)");
            MethodInfo migrate = repoType.GetMethod("MigrateToSplitFormat");
            if (migrate == null) { Log("ERROR: MigrateToSplitFormat not found!"); return 1; }

            migrate.Invoke(repo, null);

            Log("=== Migration COMPLETE ===");
            Log("Check data/crowds/ and data/characters/ folders.");
            return 0;
        }
        catch (Exception ex)
        {
            Log("ERROR: " + ex);
            return 1;
        }
    }
}
