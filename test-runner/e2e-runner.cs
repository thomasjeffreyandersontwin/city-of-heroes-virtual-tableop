// E2E test runner for FlaUI-based UI automation tests.
// Uses MTA apartment (not STA) so that COM/UIA3 calls are routed through
// the CLR's hidden MTA host rather than blocking on an STA thread without
// a message pump (which causes "UIA Timeout" with FlaUI.UIA3).
//
// Build:
//   cd city-of-heroes-virtual-tabletop\test-runner
//   "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /platform:anycpu /target:exe /out:e2e-runner.exe e2e-runner.cs /reference:Microsoft.VisualStudio.QualityTools.UnitTestFramework.dll
using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

class E2ETestRunner
{
    static int passed = 0;
    static int failed = 0;
    static List<string> failures = new List<string>();

    static int Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Console.Error.WriteLine("[BG-EXCEPTION] " + e.ExceptionObject);
        };

        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: e2e-runner.exe <testdll> [ClassName.MethodName]");
            Console.Error.WriteLine("       Omit method to run all tests; filter uses exact Class.Method match.");
            return 1;
        }
        string dllPath = Path.GetFullPath(args[0]);
        string testDir = Path.GetDirectoryName(dllPath);
        string targetTest = args.Length > 1 ? args[1] : null;

        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            try
            {
                string shortName = new AssemblyName(resolveArgs.Name).Name;
                string candidate = Path.Combine(testDir, shortName + ".dll");
                if (File.Exists(candidate))
                    return Assembly.UnsafeLoadFrom(candidate);
            }
            catch { }
            return null;
        };

        Assembly testAssembly = Assembly.LoadFrom(dllPath);
        RunAssemblyInitialize(testAssembly);

        var testClasses = testAssembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(TestClassAttribute), false).Length > 0)
            .ToList();

        foreach (var cls in testClasses)
        {
            RunClass(cls, targetTest);
        }

        Console.WriteLine();
        Console.WriteLine("Results: " + passed + " passed, " + failed + " failed.");
        if (failures.Count > 0)
        {
            Console.WriteLine("\nFailed tests:");
            foreach (var f in failures) Console.WriteLine("  - " + f);
        }

        Thread.Sleep(500);
        return failed > 0 ? 1 : 0;
    }

    static void RunAssemblyInitialize(Assembly testAssembly)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var type in testAssembly.GetTypes())
        {
            foreach (var method in type.GetMethods(flags))
            {
                if (method.GetCustomAttributes(typeof(AssemblyInitializeAttribute), false).Length == 0)
                    continue;
                if (method.GetParameters().Length != 1)
                    continue;
                method.Invoke(null, new object[] { null });
                Console.WriteLine("[ASSEMBLY] AssemblyInitialize: " + type.Name + "." + method.Name);
                return;
            }
        }
    }

    static void RunClass(Type cls, string targetTest)
    {
        var methods = cls.GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(TestMethodAttribute), false).Length > 0)
            .Where(m => string.IsNullOrEmpty(targetTest) ||
                        string.Equals(cls.Name + "." + m.Name, targetTest, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cls.Name, targetTest, StringComparison.OrdinalIgnoreCase) ||
                        cls.Name.IndexOf(targetTest, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        if (methods.Count == 0) return;

        Console.WriteLine("\n[CLASS] " + cls.Name + " (" + methods.Count + " tests)");

        foreach (var method in methods)
        {
            RunTest(cls, method);
        }
    }

    static void RunTest(Type cls, MethodInfo method)
    {
        // Run each test on an MTA thread (default for Thread when no STA is set).
        // FlaUI/UIA3 uses COM, and MTA allows the CLR to route COM calls through
        // the hidden host-STA or the process MTA — no explicit message pump needed.
        // Using a large stack (8 MB) to avoid stack exhaustion in deep call chains.
        const int StackSize = 8 * 1024 * 1024;
        Exception threadException = null;
        bool testPassed = false;
        string label = cls.Name + "." + method.Name;
        long elapsedMs = 0;

        var thread = new Thread(() =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            object instance = null;
            try
            {
                instance = Activator.CreateInstance(cls);
            }
            catch (Exception ex)
            {
                var inner = ex is TargetInvocationException ? ((TargetInvocationException)ex).InnerException ?? ex : ex;
                threadException = new Exception("[constructor] " + inner.GetType().Name + ": " + inner.Message);
                elapsedMs = sw.ElapsedMilliseconds;
                return;
            }

            var init = cls.GetMethods().FirstOrDefault(m => m.GetCustomAttributes(typeof(TestInitializeAttribute), false).Length > 0);
            try
            {
                if (init != null) init.Invoke(instance, null);
                method.Invoke(instance, null);
                testPassed = true;
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                threadException = new Exception(inner.GetType().Name + ": " + inner.Message);
            }
            finally
            {
                elapsedMs = sw.ElapsedMilliseconds;
            }

            var cleanup = cls.GetMethods().FirstOrDefault(m => m.GetCustomAttributes(typeof(TestCleanupAttribute), false).Length > 0);
            try { if (cleanup != null) cleanup.Invoke(instance, null); } catch {}
        }, StackSize);

        // STA is required for FlaUI/UIA3 COM calls. The .NET CLR provides an
        // automatic OLE message pump for STA threads waiting on COM operations,
        // which allows UIA cross-process callbacks to be dispatched correctly.
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        bool finished = thread.Join(120000); // 2-minute per-test timeout for UI tests
        if (!finished)
        {
            thread.Abort();
            thread.Join(2000);
            Console.WriteLine("  FAIL: " + label + " [TIMEOUT after 120s]");
            failures.Add(label + ": TIMEOUT");
            failed++;
            return;
        }

        if (testPassed)
        {
            Console.WriteLine("  PASS: " + label + " (" + elapsedMs + "ms)");
            passed++;
        }
        else
        {
            string reason = threadException != null ? threadException.Message : "Unknown failure";
            Console.WriteLine("  FAIL: " + label + " (" + elapsedMs + "ms)");
            Console.WriteLine("        " + reason);
            failures.Add(label + ": " + reason);
            failed++;
        }
    }
}
