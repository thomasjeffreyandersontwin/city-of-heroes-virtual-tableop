// AppDriver: FlaUI attachment + element finders for HeroVirtualDesktop.
// All learnings from the working PowerShell automation are baked in here.
//
// Key element map (from working automation):
//   Character Explorer panel  - always visible; no expander needed in current UI
//   Browse button             - AutomationId="btBrowse" in CharacterMenuControl; or HelpText="Browse Crowd Files..."
//   Crowd tree                - AutomationId="treeViewCrowd" in CharacterExplorerView
//   Crowd name in tree        - Edit child AutomationId="textBlockCrowd", read via ValuePattern
//   Member name in tree       - Edit child AutomationId="textBlockCharacter", read via ValuePattern
//   TreeItem.Name             - returns CLR type name, NOT the crowd name; always use textBlockCrowd/textBlockCharacter
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace CrowdManagement.E2ETests.Support
{
    public class AppDriver : IDisposable
    {
        private static readonly string AppExePath =
            @"C:\hero-desktop\city-of-heroes-virtual-tabletop\HerovirtualTableTop\HeroVirtualTabletop.WPF\Shell\HeroVirtualTableTop.Shell\bin\Debug\HeroVirtualDesktop.exe";

        private static readonly string ActiveCrowdsJson =
            @"C:\hero-desktop\city-of-heroes-virtual-tabletop\data\active-crowds.json";

        private static readonly string CrowdLoadErrorLog =
            @"C:\hero-desktop\city-of-heroes-virtual-tabletop\data\crowd-load-error.log";

        public static string ActiveCrowdsJsonPath
        {
            get { return ActiveCrowdsJson; }
        }

        private readonly UIA3Automation _automation;
        private Application _app;
        private Window _mainWindow;

        public AppDriver()
        {
            _automation = new UIA3Automation();
        }

        // ---------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------

        // Number of top-level crowds expected from the files passed to LaunchWithCrowdFiles.
        // Used by WaitForCrowdsToLoad to know when to stop polling.
        private int _expectedTopLevelCrowds;

        // expectedTopLevelCrowds overrides the default (crowdFilePaths.Length) for cases
        // where some paths are intentionally missing/malformed and won't produce a crowd.
        public void LaunchWithCrowdFiles(string[] crowdFilePaths, int expectedTopLevelCrowds)
        {
            _expectedTopLevelCrowds = expectedTopLevelCrowds;
            LaunchWithCrowdFilesCore(crowdFilePaths);
        }

        public void LaunchWithCrowdFiles(string[] crowdFilePaths)
        {
            // One top-level crowd per file is the convention for E2E test data files.
            _expectedTopLevelCrowds = crowdFilePaths.Length;
            LaunchWithCrowdFilesCore(crowdFilePaths);
        }

        private void LaunchWithCrowdFilesCore(string[] crowdFilePaths)
        {
            // Kill first so the previous instance releases its file-lock on active-crowds.json
            // before we try to write the new contents.
            KillExistingInstance();
            WriteActiveCrowdsJson(crowdFilePaths);

            string appDir = Path.GetDirectoryName(AppExePath);
            var psi = new System.Diagnostics.ProcessStartInfo(AppExePath)
            {
                // UseShellExecute=true: the app launches via ShellExecute and does NOT inherit
                // the runner's file handles (stdout/stderr). This prevents the WPF process from
                // blocking when the runner's pipe or file fills up, and avoids Win32Exception
                // "Access is denied" from inappropriate handle inheritance.
                UseShellExecute = true,
                WorkingDirectory = appDir
            };

            // Retry Process.Start — antivirus may briefly lock the exe after a kill.
            System.Diagnostics.Process heroProc = null;
            Exception lastStartEx = null;
            for (int startAttempt = 0; startAttempt < 5; startAttempt++)
            {
                try
                {
                    heroProc = System.Diagnostics.Process.Start(psi);
                    if (heroProc != null) break;
                }
                catch (Exception ex)
                {
                    lastStartEx = ex;
                    Console.WriteLine("[AppDriver] Process.Start attempt " + (startAttempt + 1) + " failed: " + ex.Message + " — retrying in 2s...");
                    Thread.Sleep(2000);
                }
            }
            if (heroProc == null)
                throw new InvalidOperationException("Could not start HeroVirtualDesktop after 5 attempts: " + (lastStartEx != null ? lastStartEx.Message : "null returned"));

            Console.WriteLine("[AppDriver] Process started PID=" + heroProc.Id + ". Polling MainWindowHandle (30s)...");
            IntPtr hwnd = IntPtr.Zero;
            var hwndDeadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < hwndDeadline)
            {
                try
                {
                    heroProc.Refresh();
                    if (heroProc.HasExited)
                        throw new InvalidOperationException("HeroVirtualDesktop exited unexpectedly (PID=" + heroProc.Id + ")");
                    hwnd = heroProc.MainWindowHandle;
                    if (hwnd != IntPtr.Zero) break;
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[AppDriver] Polling hwnd: " + ex.GetType().Name + ": " + ex.Message);
                }
                Thread.Sleep(500);
            }
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("MainWindowHandle never set within 30 s (PID=" + heroProc.Id + ")");

            // Retry FromHandle: UIA needs the app's accessibility provider to be ready.
            // The first call may fail with TimeoutException if the provider isn't registered yet.
            Console.WriteLine("[AppDriver] Got HWND=" + hwnd + ". Connecting UIA (up to 30s retry)...");
            Exception lastUiaEx = null;
            var uiaDeadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < uiaDeadline)
            {
                try
                {
                    _mainWindow = _automation.FromHandle(hwnd).AsWindow();
                    lastUiaEx = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastUiaEx = ex;
                    Console.WriteLine("[AppDriver] FromHandle attempt failed: " + ex.Message + " — retrying...");
                    Thread.Sleep(1000);
                }
            }
            if (lastUiaEx != null)
                throw new InvalidOperationException("UIA connection failed after 30s: " + lastUiaEx.Message);

            // Give Prism 3s to start loading its regions before we search for UI elements.
            // Without this delay, tests that connect UIA immediately (no UIA-timeout retry)
            // find an empty shell window with no CharacterCrowdMainView region yet loaded.
            Console.WriteLine("[AppDriver] Got main window OK. Waiting 3s for Prism regions...");
            Thread.Sleep(3000);
            Console.WriteLine("[AppDriver] EnsureCharacterExplorerExpanded...");
            EnsureCharacterExplorerExpandedWithRetry();
            Console.WriteLine("[AppDriver] WaitForCrowdsToLoad...");
            WaitForCrowdsToLoad();
            Console.WriteLine("[AppDriver] LaunchWithCrowdFiles done.");
        }

        public void AttachToRunning()
        {
            var proc = System.Diagnostics.Process
                .GetProcessesByName("HeroVirtualDesktop")
                .FirstOrDefault();
            if (proc == null)
                throw new InvalidOperationException("HeroVirtualDesktop is not running.");
            _app = Application.Attach(proc);
            _mainWindow = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(20));
            EnsureCharacterExplorerExpandedWithRetry();
        }

        public void Dispose()
        {
            _automation.Dispose();
        }

        public void Close()
        {
            KillExistingInstance();
            _automation.Dispose();
        }

        public static void KillApp()
        {
            KillExistingInstance();
        }

        // ---------------------------------------------------------------
        // Setup helpers
        // ---------------------------------------------------------------

        public static void WriteActiveCrowdsJson(string[] paths)
        {
            var entries = paths.Select(p => string.Format("\"{0}\"", p.Replace("\\", "/"))).ToArray();
            File.WriteAllText(ActiveCrowdsJson, "[" + string.Join(",", entries) + "]");
        }

        public static void ClearActiveCrowdsJson()
        {
            File.WriteAllText(ActiveCrowdsJson, "[]");
        }

        public static void DeleteCrowdLoadErrorLog()
        {
            if (File.Exists(CrowdLoadErrorLog)) File.Delete(CrowdLoadErrorLog);
        }

        public static string ReadCrowdLoadErrorLog()
        {
            return File.Exists(CrowdLoadErrorLog) ? File.ReadAllText(CrowdLoadErrorLog) : null;
        }

        // ---------------------------------------------------------------
        // Element finders
        // ---------------------------------------------------------------

        public void ClickSaveButton()
        {
            var cf = _mainWindow.ConditionFactory;
            var btn = _mainWindow.FindFirstDescendant(cf.ByAutomationId("btSave"));
            if (btn == null)
                btn = _mainWindow.FindFirstDescendant(cf.ByHelpText("Save (Ctrl+S)"));
            if (btn != null)
            {
                InvokeSafely(btn);
                Thread.Sleep(1500);
            }
        }

        // Renames a top-level crowd via the automation hook in CharacterExplorerView.xaml.
        // Uses UIA ValuePattern + InvokePattern only — no mouse or keyboard injection — so
        // the method works even when the runner process does not have UIAccess rights.
        public void InlineRenameCrowd(string currentName, string newName)
        {
            // Find the automation rename input TextBox and set "CurrentName|NewName".
            var cf = _mainWindow.ConditionFactory;
            var renameInput = _mainWindow.FindFirstDescendant(
                cf.ByAutomationId("automationRenameInput"));
            if (renameInput == null)
            {
                Console.WriteLine("[AppDriver] InlineRenameCrowd: automationRenameInput not found");
                return;
            }

            var vp = renameInput.Patterns.Value.PatternOrDefault;
            if (vp == null)
            {
                Console.WriteLine("[AppDriver] InlineRenameCrowd: no ValuePattern on automationRenameInput");
                return;
            }

            try { vp.SetValue(currentName + "|" + newName); }
            catch (Exception ex)
            {
                Console.WriteLine("[AppDriver] InlineRenameCrowd: SetValue failed: " + ex.Message);
                return;
            }
            Thread.Sleep(200);

            // Invoke the automation rename button to trigger AutomationRenameCommand.
            var renameBtn = _mainWindow.FindFirstDescendant(
                cf.ByAutomationId("automationRenameBtn"));
            if (renameBtn != null)
                InvokeSafely(renameBtn);
            else
                Console.WriteLine("[AppDriver] InlineRenameCrowd: automationRenameBtn not found");

            Thread.Sleep(600);
        }

        public AutomationElement FindBrowseButton()
        {
            var cf = _mainWindow.ConditionFactory;
            var btn = _mainWindow.FindFirstDescendant(cf.ByAutomationId("btBrowse"));
            if (btn == null)
                btn = _mainWindow.FindFirstDescendant(cf.ByHelpText("Browse Crowd Files..."));
            return btn;
        }

        public AutomationElement FindCrowdTree()
        {
            try
            {
                return _mainWindow.FindFirstDescendant(
                    _mainWindow.ConditionFactory.ByAutomationId("treeViewCrowd"));
            }
            catch
            {
                return null;
            }
        }

        // Returns top-level crowd names from the tree (reads textBlockCrowd via ValuePattern).
        // Excludes the always-present "System Characters" infrastructure crowd (Order=-1)
        // so that ThenCrowdTreeIsEmpty() passes when no user crowds are loaded.
        public List<string> GetTopLevelCrowdNames()
        {
            var tree = FindCrowdTree();
            if (tree == null) return new List<string>();
            var cf = tree.ConditionFactory;
            AutomationElement[] topItems = null;
            try
            {
                topItems = tree.FindAllChildren(cf.ByControlType(ControlType.TreeItem));
            }
            catch
            {
                return new List<string>();
            }
            var names = new List<string>();
            foreach (var item in topItems)
            {
                string name = ReadCrowdName(item);
                if (name != null && name != "System Characters")
                    names.Add(name);
            }
            return names;
        }

        // Returns child crowd/member names under a named top-level crowd
        public List<string> GetChildNamesUnder(string topLevelCrowdName)
        {
            var tree = FindCrowdTree();
            if (tree == null) return new List<string>();
            var parent = FindTopLevelCrowdItem(tree, topLevelCrowdName);
            if (parent == null) return new List<string>();
            ExpandTreeItem(parent);
            Thread.Sleep(600);
            return ReadChildNames(parent);
        }

        // ---------------------------------------------------------------
        // Private element helpers
        // ---------------------------------------------------------------

        private void WaitForCrowdsToLoad()
        {
            if (_expectedTopLevelCrowds == 0)
            {
                // Empty-list case: just let the dispatcher settle.
                Thread.Sleep(2000);
                return;
            }

            // Phase A: wait up to 90s for the tree itself to appear (Prism loads async).
            // After the ClassInitialize warm-up this normally completes in < 5s.
            // On cold first-run it can take much longer; 90s fits in the 120s test budget.
            bool treeFound = false;
            var treeDeadline = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < treeDeadline)
            {
                var t = FindCrowdTreeDirect();
                if (t != null) { treeFound = true; break; }
                Thread.Sleep(500);
            }

            if (!treeFound)
            {
                // Tree never appeared — let assertions produce the failure message.
                Console.WriteLine("[AppDriver] WaitForCrowdsToLoad: tree not found after 90s — continuing");
                return;
            }

            // Phase B: poll until crowd count is stable and >= expected. Cap at 12s.
            int prevCount = -1;
            int stableRounds = 0;
            var pollDeadline = DateTime.UtcNow.AddSeconds(12);
            while (DateTime.UtcNow < pollDeadline)
            {
                int count = GetTopLevelTreeItemCountSafe();
                Console.WriteLine("[AppDriver] WaitForCrowdsToLoad: count=" + count + " expected=" + _expectedTopLevelCrowds);
                if (count >= _expectedTopLevelCrowds)
                {
                    if (count == prevCount)
                    {
                        stableRounds++;
                        if (stableRounds >= 2)
                        {
                            Thread.Sleep(500);
                            return;
                        }
                    }
                    else
                    {
                        stableRounds = 0;
                        prevCount = count;
                    }
                }
                else
                {
                    stableRounds = 0;
                    prevCount = count;
                }
                Thread.Sleep(1000);
            }
            // Timeout — return anyway and let assertions decide.
        }

        // Returns the count of top-level TreeItem children of the crowd tree,
        // or -1 if the tree cannot be accessed.  Uses a background thread with
        // a short timeout so a hung UIA call does not block the test indefinitely.
        private int GetTopLevelTreeItemCountSafe()
        {
            int result = -1;
            string diagnostic = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var tree = FindCrowdTree();
                    if (tree == null) { result = 0; diagnostic = "tree=null"; return; }
                    var items = tree.FindAllChildren(
                        tree.ConditionFactory.ByControlType(ControlType.TreeItem));
                    result = items == null ? 0 : items.Length;
                    // Extra: also count ALL children regardless of type
                    AutomationElement[] allKids = null;
                    try { allKids = tree.FindAllChildren(); } catch { }
                    diagnostic = string.Format("tree=found items={0} allKids={1}",
                        result, allKids == null ? "?" : allKids.Length.ToString());
                }
                catch (Exception ex)
                {
                    diagnostic = "ex:" + ex.GetType().Name + ":" + ex.Message;
                    result = -1;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            bool done = thread.Join(5000);
            if (!done)
            {
                try { thread.Abort(); } catch { }
                thread.Join(1000);
                Console.WriteLine("[AppDriver] GetTopLevelTreeItemCountSafe: thread timed out");
                return -1;
            }
            if (diagnostic != null)
                Console.WriteLine("[AppDriver] GetTopLevelTreeItemCountSafe: " + diagnostic);
            return result;
        }

        // Quick check: if the tree is already accessible, we are done.
        // CharacterExplorerViewModel sets IsCharacterExplorerExpanded=true on startup, so the
        // tree should always be visible once Prism loads CharacterCrowdMainView.
        // WaitForCrowdsToLoad Phase-A handles the longer wait; this just covers the instant case.
        private void EnsureCharacterExplorerExpandedWithRetry()
        {
            var tree = FindCrowdTreeDirect();
            if (tree != null)
            {
                Console.WriteLine("[AppDriver] EnsureCharacterExplorerExpanded: tree already visible");
                return;
            }

            // Quick 2s attempt to find and toggle the expander button in case the
            // ViewModel initial state isn't yet reflected in the UIA tree.
            AutomationElement btn = null;
            var btnDeadline = DateTime.UtcNow.AddSeconds(2);
            while (btn == null && DateTime.UtcNow < btnDeadline)
            {
                try
                {
                    btn = _mainWindow.FindFirstDescendant(
                        _mainWindow.ConditionFactory
                            .ByAutomationId("ExpanderButton")
                            .And(_mainWindow.ConditionFactory.ByName("Character Explorer")));
                    if (btn == null)
                        btn = _mainWindow.FindFirstDescendant(
                            _mainWindow.ConditionFactory.ByName("Character Explorer")
                            .And(_mainWindow.ConditionFactory.ByControlType(ControlType.Button)));
                }
                catch { btn = null; }
                if (btn == null) Thread.Sleep(200);
            }

            if (btn != null)
            {
                try
                {
                    var toggle = btn.Patterns.Toggle.PatternOrDefault;
                    if (toggle != null && toggle.ToggleState.Value == FlaUI.Core.Definitions.ToggleState.Off)
                        toggle.Toggle();
                    else
                    {
                        var invoke = btn.Patterns.Invoke.PatternOrDefault;
                        if (invoke != null) invoke.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[AppDriver] EnsureCharacterExplorerExpanded: toggle failed: " + ex.Message);
                }
            }
            // WaitForCrowdsToLoad Phase-A will wait up to 90s for the tree to appear.
            Console.WriteLine("[AppDriver] EnsureCharacterExplorerExpanded: tree not yet visible — WaitForCrowdsToLoad will wait");
        }

        // Finds the crowd tree WITHOUT triggering DumpAutomationIds diagnostics.
        private AutomationElement FindCrowdTreeDirect()
        {
            try
            {
                return _mainWindow.FindFirstDescendant(
                    _mainWindow.ConditionFactory.ByAutomationId("treeViewCrowd"));
            }
            catch { return null; }
        }

        // Invoke a button safely: prefer InvokePattern over mouse click to avoid UIPI issues.
        private static void InvokeSafely(AutomationElement element)
        {
            if (element == null) return;
            var invoke = element.Patterns.Invoke.PatternOrDefault;
            if (invoke != null)
                invoke.Invoke();
            else
                element.Click();
        }

        private static string ReadCrowdName(AutomationElement treeItem)
        {
            try
            {
                var cf = treeItem.ConditionFactory;
                var edit = treeItem.FindFirstDescendant(cf.ByAutomationId("textBlockCrowd"));
                if (edit == null) return null;
                var vp = edit.Patterns.Value.PatternOrDefault;
                return vp != null ? vp.Value.Value.Trim() : null;
            }
            catch
            {
                return null;
            }
        }

        private static string ReadMemberName(AutomationElement treeItem)
        {
            try
            {
                var cf = treeItem.ConditionFactory;
                var edit = treeItem.FindFirstDescendant(cf.ByAutomationId("textBlockCharacter"));
                if (edit == null) return ReadCrowdName(treeItem);
                var vp = edit.Patterns.Value.PatternOrDefault;
                return vp != null ? vp.Value.Value.Trim() : null;
            }
            catch
            {
                return null;
            }
        }

        private static AutomationElement FindTopLevelCrowdItem(AutomationElement tree, string name)
        {
            try
            {
                var cf = tree.ConditionFactory;
                var items = tree.FindAllChildren(cf.ByControlType(ControlType.TreeItem));
                foreach (var item in items)
                {
                    if (ReadCrowdName(item) == name) return item;
                }
            }
            catch { }
            return null;
        }

        private static void ExpandTreeItem(AutomationElement item)
        {
            try
            {
                var exp = item.Patterns.ExpandCollapse.PatternOrDefault;
                if (exp != null && exp.ExpandCollapseState.Value == ExpandCollapseState.Collapsed)
                    exp.Expand();
            }
            catch { }
        }

        private static List<string> ReadChildNames(AutomationElement parent)
        {
            var names = new List<string>();
            try
            {
                var cf = parent.ConditionFactory;
                var kids = parent.FindAllChildren(cf.ByControlType(ControlType.TreeItem));
                foreach (var kid in kids)
                {
                    string name = ReadMemberName(kid);
                    if (name != null) names.Add(name);
                }
            }
            catch { }
            return names;
        }

        private static void KillExistingInstance()
        {
            foreach (var p in System.Diagnostics.Process.GetProcessesByName("HeroVirtualDesktop"))
            {
                try { p.Kill(); } catch { /* already exited or access denied — ignore */ }
                try { p.WaitForExit(5000); } catch { }
            }

            // Active-poll until all instances are gone (max 10 s), then a short buffer.
            var killDeadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < killDeadline)
            {
                var remaining = System.Diagnostics.Process.GetProcessesByName("HeroVirtualDesktop");
                if (remaining.Length == 0) break;
                // Give any lingering instances another nudge.
                foreach (var r in remaining)
                    try { r.Kill(); } catch { }
                Thread.Sleep(500);
            }

            // Small buffer so the OS releases file handles and AV finishes scanning.
            Thread.Sleep(1500);
        }
    }
}
