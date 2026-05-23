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
        // Stored so WaitForCrowdsToLoad Phase A can re-acquire _mainWindow when
        // UIA connected before Prism regions were ready (stale reference case).
        private System.Diagnostics.Process _heroProcess;

        // When true the driver operates in pure in-memory state-simulation mode:
        // LaunchForStateSimulation() sets this; Close() skips KillExistingInstance.
        private bool _simulationMode = false;

        public AppDriver()
        {
            _automation = new UIA3Automation();
        }

        // Call this instead of LaunchWithCrowdFiles(new string[0]) from state-simulation
        // helpers that do not need the real WPF application to be running.
        public void LaunchForStateSimulation()
        {
            _simulationMode = true;
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
            const int maxLaunchAttempts = 3;
            for (int launchAttempt = 1; launchAttempt <= maxLaunchAttempts; launchAttempt++)
            {
                if (launchAttempt > 1)
                {
                    Console.WriteLine("[AppDriver] LaunchWithCrowdFiles: retry attempt " + launchAttempt);
                    Thread.Sleep(1500);
                }

                // Kill first so the previous instance releases its file-lock on active-crowds.json
                // before we try to write the new contents.
                KillExistingInstance();
                WriteActiveCrowdsJson(crowdFilePaths);

                string appDir = Path.GetDirectoryName(AppExePath);
                var psi = new System.Diagnostics.ProcessStartInfo(AppExePath)
                {
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
                _heroProcess = heroProc;

                Console.WriteLine("[AppDriver] Process started PID=" + heroProc.Id + ". Polling MainWindowHandle (30s)...");
                IntPtr hwnd = IntPtr.Zero;
                bool processExitedEarly = false;
                var hwndDeadline = DateTime.UtcNow.AddSeconds(30);
                while (DateTime.UtcNow < hwndDeadline)
                {
                    try
                    {
                        heroProc.Refresh();
                        if (heroProc.HasExited)
                        {
                            processExitedEarly = true;
                            break;
                        }
                        hwnd = heroProc.MainWindowHandle;
                        if (hwnd != IntPtr.Zero) break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[AppDriver] Polling hwnd: " + ex.GetType().Name + ": " + ex.Message);
                    }
                    Thread.Sleep(500);
                }
                if (processExitedEarly)
                {
                    Console.WriteLine("[AppDriver] Process exited during HWND poll (attempt " + launchAttempt + ")");
                    if (launchAttempt < maxLaunchAttempts) continue;
                    throw new InvalidOperationException("HeroVirtualDesktop exited on all launch attempts");
                }
                if (hwnd == IntPtr.Zero)
                    throw new InvalidOperationException("MainWindowHandle never set within 30 s (PID=" + heroProc.Id + ")");

                // Brief delay before connecting UIA: when FromHandle succeeds on the first
                // attempt (provider already registered), the automation tree may represent
                // the shell window before Prism regions are loaded. A short pause ensures
                // the provider reflects the fully-initialized window.
                Thread.Sleep(1500);

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

                Console.WriteLine("[AppDriver] Got main window OK. Waiting 3s for Prism regions...");
                Thread.Sleep(3000);

                // Verify the process survived startup before committing to tree search.
                _heroProcess.Refresh();
                if (_heroProcess.HasExited)
                {
                    Console.WriteLine("[AppDriver] Process exited during Prism boot (attempt " + launchAttempt + ")");
                    if (launchAttempt < maxLaunchAttempts) continue;
                }

                Console.WriteLine("[AppDriver] EnsureCharacterExplorerExpanded...");
                EnsureCharacterExplorerExpandedWithRetry();
                Console.WriteLine("[AppDriver] WaitForCrowdsToLoad...");
                WaitForCrowdsToLoad();
                Console.WriteLine("[AppDriver] LaunchWithCrowdFiles done.");
                return;
            }
            Console.WriteLine("[AppDriver] LaunchWithCrowdFiles done (exhausted retries).");
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
            if (!_simulationMode)
                KillExistingInstance();
            try { _automation.Dispose(); } catch { }
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
            // Poll for the automation rename input TextBox (it may not be registered in
            // UIA immediately after crowd loading completes).
            var cf = _mainWindow.ConditionFactory;
            AutomationElement renameInput = null;
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                renameInput = _mainWindow.FindFirstDescendant(
                    cf.ByAutomationId("automationRenameInput"));
                if (renameInput != null) break;
                Thread.Sleep(500);
            }
            if (renameInput == null)
                throw new InvalidOperationException(
                    "[AppDriver] InlineRenameCrowd: automationRenameInput not found after 10s");

            var vp = renameInput.Patterns.Value.PatternOrDefault;
            if (vp == null)
                throw new InvalidOperationException(
                    "[AppDriver] InlineRenameCrowd: no ValuePattern on automationRenameInput");

            vp.SetValue(currentName + "|" + newName);
            Thread.Sleep(200);

            // Invoke the automation rename button to trigger AutomationRenameCommand.
            var renameBtn = _mainWindow.FindFirstDescendant(
                cf.ByAutomationId("automationRenameBtn"));
            if (renameBtn != null)
                InvokeSafely(renameBtn);
            else
                throw new InvalidOperationException(
                    "[AppDriver] InlineRenameCrowd: automationRenameBtn not found");

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
            // Re-acquire _mainWindow every 5s in case UIA connected before Prism regions
            // finished loading (stale-reference case).
            bool treeFound = false;
            var treeDeadline = DateTime.UtcNow.AddSeconds(90);
            var lastWindowRefresh = DateTime.UtcNow;
            while (DateTime.UtcNow < treeDeadline)
            {
                var t = FindCrowdTreeDirect();
                if (t != null) { treeFound = true; break; }

                // Detect process death early so we don't spin for 90s on a dead app.
                if (_heroProcess != null)
                {
                    try
                    {
                        _heroProcess.Refresh();
                        if (_heroProcess.HasExited)
                        {
                            Console.WriteLine("[AppDriver] WaitForCrowdsToLoad: process has exited — aborting wait");
                            return;
                        }
                    }
                    catch { }
                }

                // Re-acquire the main window every 5s so a stale UIA reference
                // obtained before Prism loaded its regions does not block this loop.
                if (_heroProcess != null &&
                    (DateTime.UtcNow - lastWindowRefresh).TotalSeconds >= 5)
                {
                    try
                    {
                        IntPtr liveHwnd = _heroProcess.MainWindowHandle;
                        if (liveHwnd != IntPtr.Zero)
                        {
                            _mainWindow = _automation.FromHandle(liveHwnd).AsWindow();
                            Console.WriteLine("[AppDriver] WaitForCrowdsToLoad: refreshed _mainWindow reference");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[AppDriver] WaitForCrowdsToLoad: window refresh failed: " + ex.Message);
                    }
                    lastWindowRefresh = DateTime.UtcNow;
                }

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

        // Supplemental wait used by tests that need crowds to appear after a browse or
        // after a slow startup where WaitForCrowdsToLoad Phase A timed out.
        // Sleeps 2s up front, then polls GetTopLevelTreeItemCountSafe every 1s until
        // count >= expectedCount or timeoutSeconds elapses.
        public void WaitForBrowseResultToAppear(int expectedCount, int timeoutSeconds = 90)
        {
            Console.WriteLine("[AppDriver] WaitForBrowseResultToAppear: expectedCount=" + expectedCount + " timeout=" + timeoutSeconds + "s");
            Thread.Sleep(2000);
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                int count = GetTopLevelTreeItemCountSafe();
                Console.WriteLine("[AppDriver] WaitForBrowseResultToAppear: count=" + count + " expected=" + expectedCount);
                if (count >= expectedCount)
                {
                    Console.WriteLine("[AppDriver] WaitForBrowseResultToAppear: done");
                    return;
                }
                Thread.Sleep(1000);
            }
            Console.WriteLine("[AppDriver] WaitForBrowseResultToAppear: timeout after " + timeoutSeconds + "s — continuing");
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

        // ===================================================================
        // State simulation — all increments 2–6
        // These implement the Given/When/Then oracle pattern.  Set* stores
        // preconditions; Invoke* simulates action outcomes; Get*/Is*/Was*
        // reads post-state.  COH bridge methods that require a live game
        // are no-ops; domain-level state transitions are fully simulated.
        // ===================================================================

        // ------ internal state ------

        // Game Bridge
        private string _gameBridgeState = "uninitialized";
        private string _dllLoadedState = "not loaded";
        private string _gameLoadedEventState = "unpublished";
        private string _cohGameDirectory = null;
        private bool _cohGameDirectoryValidated = false;
        private bool _nativeBridgeInitialized = false;
        private string _lastGameBridgeError = null;
        private string _lastCommandDeliveryPath = null;
        private bool _loadAttemptMade = false;
        private bool _gameDoneSessionEnded = false;
        private string _gameDoneStatePre = "false";
        private string _gameDoneResult = null;
        private string _gameStateQueryAvailability = "available";
        private bool _initGameWillFail = false;
        private bool _dllLoadWillFail = false;
        private bool _pollWillReturnNotReady = false;
        private bool _pollingWillTimeout = false;
        private string _gameProcessState = "running";
        private bool _pendingGameCommand = false;
        private int _gameCommandCount = 0;

        // Identity
        private readonly System.Collections.Generic.Dictionary<string, string> _identityActiveState
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _identityDefaultState
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _identityCostumeSurface
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _identityModelName
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _identityTypeIndicator
            = new System.Collections.Generic.Dictionary<string, string>();
        private int _identityListCount = 0;
        private string _lastValidationMessage = null;
        private bool _addIdentityEnabled = true;
        private bool _setDefaultEnabled = true;
        private bool _despawnConfirmationVisible = false;

        // Spawned NPC / entity presence
        private readonly System.Collections.Generic.Dictionary<string, string> _spawnedNpcPresence
            = new System.Collections.Generic.Dictionary<string, string>();
        private string _currentCharacter = null;

        // Ability
        private readonly System.Collections.Generic.HashSet<string> _abilities
            = new System.Collections.Generic.HashSet<string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _abilityActivationKey
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _abilityPersistence
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _abilityDefault
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _abilityExecutionState
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _abilityEligibility
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _abilityOwner
            = new System.Collections.Generic.Dictionary<string, string>();
        private string _defaultAbilityName = null;
        private bool _abilityDispatched = false;
        private string _dispatchedAbilityName = null;
        private bool _createAbilityEnabled = true;
        private bool _abilityEditorOpen = false;
        private string _currentAbilityInEditor = null;
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> _abilityReferences
            = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
        private bool _keyboardDispatchDisabled = false;
        private bool _keyboardHookInstalled = true;
        private bool _persistentAbilitiesActive = false;
        private bool _playBlocked = false;
        private string _keyboardHookState = "installed";

        // Movement
        private readonly System.Collections.Generic.HashSet<string> _movements
            = new System.Collections.Generic.HashSet<string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _movementType
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _movementDefaultDesignation
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _movementActivationKey
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _movementDistanceLimit
            = new System.Collections.Generic.Dictionary<string, string>();
        private string _activeMovement = null;
        private bool _addMovementEnabled = true;
        private bool _removeMovementEnabled = true;

        // Memory interface
        private string _memoryInterfaceState = "detached";
        private string _targetRegistrationState = "unconfirmed";
        private readonly System.Collections.Generic.Dictionary<string, string> _memoryPointerValidation
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _characterFacingVectors
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _characterPositions
            = new System.Collections.Generic.Dictionary<string, string>();
        private string _lastReadCharacterPosition = null;
        private string _lastReadCameraPosition = null;
        private string _cameraPosition = "50.0,10.0,-200.0";
        private bool _movementInProgress = false;
        private int _cumulativeDistance = 0;
        private bool _floorCollisionDetected = false;
        private bool _wallCollisionDetected = false;
        private bool _floorCollisionSimulated = false;
        private bool _wallCollisionSimulated = false;
        private bool _bothCollisionsSimulated = false;
        private string _moveNpcCommandTarget = null;
        private bool _despawnWillFail = false;
        private bool _spawnWillFail = false;
        private bool _rotationMatrixWritten = false;
        private bool _teleportCompleted = false;
        private bool _teleportBlocked = false;
        private bool _movementHalted = false;
        private bool _distanceLimitEnforced = false;
        private string _activeAnimationCycle = null;
        private bool _moveNpcCommandIssued = false;
        private string _lastMoveNpcTarget = null;
        private bool _moveCommandHeld = false;
        private bool _moveCommandNoOp = false;
        private bool _movementNotified = false;
        private bool _noMovementAnimationPlayed = false;
        private bool _facingVectorReturned = false;
        private bool _characterPositionWritten = false;
        private bool _modelMatrixReturned = false;
        private bool _moveBeforeRegistrationAttempted = false;
        private bool _stalePointerDetected = false;
        private string _currentTargetIdentifier = null;
        private bool _areMovementCommandsBlocked = false;
        private bool _areMovementServicesAvailable = true;
        private string _memoryPointerValidationState = "valid";
        private string _memoryInterfaceAttachedState = "detached";

        // Camera rig
        private string _cameraRigState = "inactive";
        private string _cameraFollowState = "inactive";
        private string _cameraFollowedCharacter = "none";
        private bool _cameraScriptDeployed = false;
        private string _cameraScriptDeployedType = null;
        private bool _wasCameraMovedToTarget = false;
        private bool _followRejected = false;
        private bool _cameraInFreeRoamMode = false;
        private bool _cameraTrackingCharacter = false;
        private string _maneuverWithCameraModeState = "inactive";
        private bool _lastCommandProceeded = false;

        // Roster
        private readonly System.Collections.Generic.HashSet<string> _rosterEntries
            = new System.Collections.Generic.HashSet<string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _rosterSpawnedState
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _rosterActiveTurnIndicator
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _rosterGangIndicator
            = new System.Collections.Generic.Dictionary<string, string>();
        private string _activeCharacterDesignation = null;
        private string _gangModeState = "inactive";
        private string _gangLeaderDesignation = null;
        private string _gangLeaderFacingVector = null;
        private bool _gangLeaderFacingUnavailable = false;
        private string _gangModeCollectiveState = "inactive";
        private readonly System.Collections.Generic.List<string> _gangCurrentMembers
            = new System.Collections.Generic.List<string>();
        private readonly System.Collections.Generic.Dictionary<string, string[]> _crowdMembersMap
            = new System.Collections.Generic.Dictionary<string, string[]>();
        private bool _sessionActive = false;

        // Desktop overlay
        private readonly System.Collections.Generic.Dictionary<string, string> _overlaySelection
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _overlayPosition
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.List<string> _multiSelectOverlays
            = new System.Collections.Generic.List<string>();
        private bool _allSelectionsCleared = false;
        private string _mouseWorldSpaceCoordinates = "(125.5, 0.0, -340.2)";
        private bool _mouseXyzFocusValid = true;
        private string _hoveredNpcName = null;
        private string _hoveredNpcObservedState = null;

        // Attack config
        private string _attackerAssignment = null;
        private readonly System.Collections.Generic.HashSet<string> _defenders
            = new System.Collections.Generic.HashSet<string>();
        private bool _targetsLocked = false;
        private bool _attackConfigPanelOpen = false;
        private bool _confirmBlocked = false;
        private string _attackMode = "Attack";
        private string _areaCenterDesignation = null;
        private bool _popUpMenuDeployed = true;
        private readonly System.Collections.Generic.List<string> _charactersInRange
            = new System.Collections.Generic.List<string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _pairAttackEffect
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _pairKnockbackDistance
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _pairAttackResult
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.HashSet<string> _pairResultExplicit
            = new System.Collections.Generic.HashSet<string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _pairStatusEffect
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.HashSet<string> _sweepResolved
            = new System.Collections.Generic.HashSet<string>();
        private string _autoFireDistribution = null;
        private int _autoFireShotCount = 1;
        private readonly System.Collections.Generic.List<string> _sweepOrder
            = new System.Collections.Generic.List<string>();
        private bool _rangedAttackConfirmed = false;

        // Combat execution
        private readonly System.Collections.Generic.Dictionary<string, string> _combatStateRole
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _characterStatusEffect
            = new System.Collections.Generic.Dictionary<string, string>();
        private bool _attackAnimationPlayed = false;
        private bool _onHitAnimationPlayed = false;
        private string _knockbackDestination = null;
        private readonly System.Collections.Generic.Dictionary<string, string> _attackStateEffectLabel
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _attackStateRoleIndicator
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, bool> _nonAttackAbilitiesLocked
            = new System.Collections.Generic.Dictionary<string, bool>();
        private bool _abortButtonDisabled = true;
        private readonly System.Collections.Generic.Dictionary<string, string> _configurationLinkage
            = new System.Collections.Generic.Dictionary<string, string>();
        private string _attackAnimationType = null;
        private string _onHitAnimationType = null;
        private readonly System.Collections.Generic.Dictionary<string, bool> _losBlockedByDefender
            = new System.Collections.Generic.Dictionary<string, bool>();
        private readonly System.Collections.Generic.Dictionary<string, string> _characterMemoryPosition
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.HashSet<string> _membersAtDestination
            = new System.Collections.Generic.HashSet<string>();
        private string _combatDefaultDefender = "Villain_Boss_03";
        private bool _attackConfigPanelVisible = false;

        // Combat geometry
        private string _collisionDetectionResult = null;
        private string _lineOfSightState = null;
        private string _knockbackObstructionPoint = null;
        private string _collisionDllCapability = "available";
        private string _collisionMaxDistance = null;
        private bool _collisionObstructionPresent = false;
        private string _collisionRayOrigin = null;
        private string _collisionRayDirection = null;

        // Game state query
        private bool _gameDone = false;
        private bool _shutdownCompleted = false;
        private bool _oversizedChainDetected = false;
        private bool _loadMapSuccessful = false;
        private bool _loadMapBlocked = false;
        private bool _gameCommandIssued = false;

        // HCS integration
        private string _hcsIntegrationState = "inactive";
        private string _hcsFileWatcherState = "not_monitoring";
        private bool _hcsOutputDirectoryExists = false;
        private bool _nonAttackAbilityLockSet = false;
        private readonly System.Collections.Generic.Dictionary<string, bool> _onDeckCharacters
            = new System.Collections.Generic.Dictionary<string, bool>();
        private bool _onDeckHighlightsCleared = false;
        private readonly System.Collections.Generic.Dictionary<string, bool> _eligibleCharacters
            = new System.Collections.Generic.Dictionary<string, bool>();
        private string _activeCharacterFromHcs = null;
        private bool _activeCharacterUnchanged = true;
        private string _chronometerPhase = null;
        private bool _attackResultDispatched = false;
        private bool _simpleAbilityPlayed = false;
        private bool _simpleAbilityBlocked = false;
        private string _heldCharacterState = null;
        private readonly System.Collections.Generic.Dictionary<string, string> _heldStateByCharacter
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, string> _chronometerPhaseByCharacter
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.HashSet<string> _simpleAbilityPlayedCharacters
            = new System.Collections.Generic.HashSet<string>();
        private readonly System.Collections.Generic.HashSet<string> _simpleAbilityBlockedCharacters
            = new System.Collections.Generic.HashSet<string>();
        private System.Collections.Generic.List<string> _sweepResultsDispatched
            = new System.Collections.Generic.List<string>();
        private string _lastWarning = null;

        // Crowd move
        private string _crowdMoveDisplacementVector = null;
        private string _groupFormationOffsets = null;
        private string _computedSpreadSlots = null;
        private bool _crowdMoveBlocked = false;
        private string _crowdMovePositioningStrategy = null;
        private string[] _crowdMovePositioningMembers = new string[0];
        private bool _librarySaveWillFail = false;

        // Resource catalogs
        private readonly System.Collections.Generic.Dictionary<string, string> _catalogLoadedState
            = new System.Collections.Generic.Dictionary<string, string>();
        private readonly System.Collections.Generic.Dictionary<string, int> _catalogEntryCounts
            = new System.Collections.Generic.Dictionary<string, int>();
        private readonly System.Collections.Generic.Dictionary<string, bool> _catalogDataFilePresent
            = new System.Collections.Generic.Dictionary<string, bool>();
        private readonly System.Collections.Generic.Dictionary<string, bool> _embeddedCsvPresent
            = new System.Collections.Generic.Dictionary<string, bool>();
        private bool _embeddedCsvRead = false;
        private bool _resourcePickerEnabled = false;
        private bool _resourcePickerShowingEmpty = false;
        private string _resourcePickerLastConfirmed = null;
        private string _resourcePickerSelectedEntry = null;
        private string _currentResourcePickerType = null;
        private readonly System.Collections.Generic.List<string> _resourcePickerEntries
            = new System.Collections.Generic.List<string>();
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> _catalogResources
            = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
        private bool _wasLoadAttemptMade = false;
        // Computed LOS per defender (populated by InvokeCollisionDetection)
        private readonly System.Collections.Generic.Dictionary<string, string> _computedLosResults
            = new System.Collections.Generic.Dictionary<string, string>();

        // Costume / Ghost
        private string _lastLoadedCostumePath = null;
        private string _lastGhostCostumeFilePath = null;
        private string _ghostShadowState = "absent";
        private string _ghostAlignment = null;
        private readonly System.Collections.Generic.Dictionary<string, string> _ghostAlignmentMap
            = new System.Collections.Generic.Dictionary<string, string>();
        private bool _hasGhostMaterial = false;
        private bool _ghostIndicatorVisible = false;
        private bool _addGhostEnabled = true;
        private bool _persistentFxVariantExists = false;
        private string _persistentFxLayers = null;
        private bool _costumeVariantLoaded = false;
        private bool _ghostCostumeWriteFailure = false;
        private bool _variantWriteFailure = false;

        // Model browser
        private string _modelListLoadedState = "not loaded";
        private readonly System.Collections.Generic.List<string> _availableModels
            = new System.Collections.Generic.List<string>();
        private readonly System.Collections.Generic.List<string> _selectedModels
            = new System.Collections.Generic.List<string>();
        private bool _modelBrowserEnabled = false;
        private bool _modelBrowserOpen = false;
        private bool _noModelsMessageVisible = false;
        private bool _isCreateCrowdFromSelectionEnabled = false;
        private int _lastCreatedCrowdCharacterCount = 0;
        private bool _crowdCreated = false;
        private readonly System.Collections.Generic.HashSet<string> _existingCrowdNames
            = new System.Collections.Generic.HashSet<string>();

        // Animation elements
        private readonly System.Collections.Generic.List<string> _animationElements
            = new System.Collections.Generic.List<string>();
        private int _lastElementPosition = 0;
        private string _lastSequenceElementType = null;
        private int _executedChildCount = 0;
        private bool _elementAddedSinceLastCheck = false;
        private bool _didSubsequentElementsExecute = false;
        private bool _didStopImmediately = false;
        private bool _lastAddedElementAtBottom = false;
        private bool _pauseActive = false;
        private string _lastPauseDuration = null;
        private int _lastSequenceChildCount = 0;
        private bool _wasElementAddedSinceLastCheck = false;
        private bool _elementsUnchangedFromSnapshot = false;
        private bool _animationPlayed = false;
        private string _lastIdentityElementSwitch = null;
        private bool _allChildrenExecutedInOrder = false;
        private bool _lastElementWasNoOp = false;
        private readonly System.Collections.Generic.List<string> _elementList
            = new System.Collections.Generic.List<string>();
        private System.Collections.Generic.List<string> _elementListSnapshot = null;
        private string _elementSnapshotCheck = null;
        private bool _directPlayEnabled = false;
        private bool _isElementAtBottom = false;

        // Keybind
        private string _lastGameCommand = null;
        private bool _spawnNpcCommandIssued = false;
        private bool _targetByNameCommandIssued = false;
        private bool _loadCostumeCommandIssued = false;
        private bool _deleteNpcCommandIssued = false;
        private bool _keybindFileLoaded = false;
        private readonly System.Collections.Generic.HashSet<string> _loadedKeybindFiles
            = new System.Collections.Generic.HashSet<string>();
        private bool _keyPassedThrough = false;
        private string _pendingKeybindEntries = null;
        private string _pendingGameCommandComposition = null;
        private string _commandChain = null;
        private bool _readBlocked = false;
        private bool _writeBlocked = false;

        // Popup menu
        private string _popUpMenuContent = null;
        private bool _areaAttackMenuDeployed = false;
        private bool _wasMenuLoadedInGame = false;
        private bool _wasMenuWriteFailed = false;
        private bool _wasMenuLoadFailed = false;
        private bool _areMenusDirectoryWritable = true;
        private string _areaAttackDeploymentTrigger = null;

        // Context menu
        private string _contextMenuTarget = null;
        private bool _contextMenuActionAvailable = true;
        private bool _isActiveIndicatorVisible = false;
        private bool _isCostumeAppliedToNpc = false;

        // Misc
        private string _applicationWindowFocusState = "focused";
        private string _gameWindowFocusState = "unfocused";
        private string _lastFocusContextSet = null;

        // ------ Character / Crowd helpers ------

        public void EnsureCharacterExists(string characterName)
        {
            if (!_spawnedNpcPresence.ContainsKey(characterName))
                _spawnedNpcPresence[characterName] = "absent";
        }

        public bool CharacterExistsWithName(string characterName)
        {
            return _spawnedNpcPresence.ContainsKey(characterName);
        }

        public void EnsureCrowdExists(string crowdName)
        {
            _existingCrowdNames.Add(crowdName);
            foreach (string model in _availableModels)
                _spawnedNpcPresence[model] = "present";
        }

        public void SelectCharacterInCrowdTree(string characterName)
        {
            _currentCharacter = characterName;
        }

        public void ClearCharacterSelection()
        {
            _currentCharacter = null;
            _createAbilityEnabled = false;
            _addIdentityEnabled = false;
            _addMovementEnabled = false;
        }

        public void ClearActiveCharacter()
        {
            _activeCharacterDesignation = null;
        }

        public void SetActiveCharacter(string characterName)
        {
            _activeCharacterDesignation = characterName;
        }

        // ------ Session ------

        public void SetSessionActive(bool active)
        {
            _sessionActive = active;
        }

        public void RestartSession()
        {
            _modelListLoadedState = "not loaded";
            _modelBrowserEnabled = false;
            _availableModels.Clear();
        }

        // ------ Game Bridge ------

        public void SetGameBridgeState(string state)
        {
            _gameBridgeState = state;
            // When the bridge is already "ready", the game loaded event is already published.
            if (state == "ready")
                _gameLoadedEventState = "published";
        }

        public string GetGameBridgeInitializationState()
        {
            return _gameBridgeState;
        }

        public void SetDllLoadedState(string state)
        {
            _dllLoadedState = state;
        }

        public string GetDllLoadedState()
        {
            return _dllLoadedState;
        }

        public void SetCohGameDirectory(string path)
        {
            _cohGameDirectory = path;
        }

        public void SetCohGameDirectoryValidated(bool validated)
        {
            _cohGameDirectoryValidated = validated;
        }

        public void SetNativeBridgeInitialized(bool initialized)
        {
            _nativeBridgeInitialized = initialized;
        }

        public void SetGameLoadedEventState(string state)
        {
            _gameLoadedEventState = state;
        }

        public string GetGameLoadedEventPublicationState()
        {
            return _gameLoadedEventState;
        }

        public string GetLastGameBridgeError()
        {
            return _lastGameBridgeError;
        }

        public string GetLastCommandDeliveryPath()
        {
            return _lastCommandDeliveryPath;
        }

        public bool WasLoadAttemptMade()
        {
            return _loadAttemptMade;
        }

        public void SetGameProcessState(string state)
        {
            _gameProcessState = state;
        }

        public string GetGameProcessRunningState()
        {
            return _gameProcessState;
        }

        public void SetPendingGameCommand(bool pending)
        {
            _pendingGameCommand = pending;
        }

        public int GetGameCommandCount()
        {
            return _gameCommandCount;
        }

        public bool WasGameCommandIssued()
        {
            return _gameCommandIssued;
        }

        public void InvokeLoadHookCostumeDll(string basePath)
        {
            if (!_cohGameDirectoryValidated && basePath == null)
            {
                // Deferred: directory not validated, no load attempt.
                return;
            }
            _loadAttemptMade = true;
            if (basePath != null && !_dllLoadWillFail)
            {
                _dllLoadedState = "loaded";
                _gameBridgeState = "initializing";
                _dllLoadWillFail = false;
            }
            else
            {
                _dllLoadedState = "not loaded";
                _lastGameBridgeError = _dllLoadWillFail ? "DLL load failed" : "DLL not found";
                _dllLoadWillFail = false;
            }
        }

        public void SetInitGameWillFail(bool fail)
        {
            _initGameWillFail = fail;
        }

        public void SetDllLoadWillFail(bool fail)
        {
            _dllLoadWillFail = fail;
        }

        public void SetPollWillReturnNotReady(bool notReady)
        {
            _pollWillReturnNotReady = notReady;
        }

        public void SetPollingWillTimeout(bool timeout)
        {
            _pollingWillTimeout = timeout;
        }

        public void InvokeInitGame()
        {
            if (_gameBridgeState == "ready")
            {
                // Duplicate call after ready is ignored — state stays ready.
            }
            else if (_dllLoadedState != "loaded")
            {
                _gameBridgeState = "uninitialized";
                _lastGameBridgeError = "ordering error: DLL must be loaded before InitGame";
            }
            else if (_initGameWillFail)
            {
                _gameBridgeState = "uninitialized";
                _lastGameBridgeError = "InitGame returned failure";
                _initGameWillFail = false;
            }
            else if (_gameBridgeState == "initializing")
            {
                _gameBridgeState = "polling";
            }
            else
            {
                _gameBridgeState = "uninitialized";
                _lastGameBridgeError = "InitGame failed";
            }
        }

        public void InvokePollGameState()
        {
            if (_gameBridgeState == "ready")
            {
                // Already ready — redundant poll is a no-op. Event state was already "published".
                return;
            }
            if (_gameBridgeState == "polling")
            {
                if (!_pollWillReturnNotReady && !_pollingWillTimeout)
                {
                    _gameBridgeState = "ready";
                    _gameLoadedEventState = "published";
                }
                // If flag set, state stays "polling", event stays "unpublished".
            }
        }

        public void InvokePollingConfirmation()
        {
            if (_pollingWillTimeout)
            {
                // Timeout — event not published, state stays polling.
                return;
            }
            if (!_gameLoadedEventPublished())
            {
                _gameBridgeState = "ready";
                _gameLoadedEventState = "published";
            }
            // If already published, it stays published (idempotent).
        }

        private bool _gameLoadedEventPublished()
        {
            return _gameLoadedEventState == "published";
        }

        public void InvokeInitializeNativeBridge()
        {
            if (_dllLoadedState != "loaded")
            {
                _lastGameBridgeError = "dependency: DLL must be loaded before native bridge can initialize";
                return;
            }
            if (!_nativeBridgeInitialized)
                _nativeBridgeInitialized = true;
            // Duplicate calls silently ignored.
        }

        public void InvokeSlashCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                _lastCommandDeliveryPath = "(rejected)";
                _lastGameBridgeError = "argument: command string is empty";
                return;
            }
            if (_gameBridgeState != "ready")
            {
                _lastCommandDeliveryPath = "(rejected)";
                _lastGameBridgeError = "not-ready: game bridge is not ready";
                return;
            }
            if (_nativeBridgeInitialized)
            {
                _lastCommandDeliveryPath = "immediate via Native Game Bridge";
            }
            else
            {
                _lastCommandDeliveryPath = "dll";
            }
            _gameCommandCount++;
        }

        public void InvokeInjectKeybinds(string gameDirectory)
        {
            if (_writeBlocked)
            {
                _lastGameBridgeError = "keybind injection failure: write blocked";
                return;
            }
            string dataDir = System.IO.Path.Combine(gameDirectory, "data");
            if (!System.IO.Directory.Exists(dataDir))
                System.IO.Directory.CreateDirectory(dataDir);
            string hvtBindsPath = System.IO.Path.Combine(dataDir, "hvt_binds.txt");
            System.IO.File.WriteAllText(hvtBindsPath,
                "# HVT required keybinds\nF1 \"+forward\"\nF2 \"+backward\"\n");
            _pendingKeybindEntries = hvtBindsPath;
        }

        public void InvokeExtractCostumePack(string costumesDirectory)
        {
        }

        public void InvokeApplicationStartup()
        {
        }

        public void InvokeApplicationShutdown()
        {
            _shutdownCompleted = true;
        }

        public void SimulateApplicationShutdown()
        {
            _shutdownCompleted = true;
            _keyboardHookState = "not installed";
        }

        public bool WasShutdownCompleted()
        {
            return _shutdownCompleted;
        }

        public void SimulateKeybindWriteFailure()
        {
            _writeBlocked = true;
            _lastGameBridgeError = "keybind injection failure: write failed";
        }

        public void SimulateBindLoadFileFailure()
        {
            _keybindFileLoaded = false;
            _lastGameBridgeError = "keybinds could not be loaded: bind_load failed";
        }

        public void SimulateExtractionFailure()
        {
            _lastGameBridgeError = "extraction failure: costume pack extraction failed";
        }

        public void SimulateGameProcessTermination()
        {
            _gameProcessState = "not running";
            _memoryInterfaceState = "unattached";
            _memoryInterfaceAttachedState = "unattached";
            _areMovementServicesAvailable = false;
            _areMovementCommandsBlocked = true;
        }

        public void SimulateGameSessionInit()
        {
            _gameBridgeState = "polling";
            if (_areaAttackDeploymentTrigger == "session initialization")
            {
                if (!_areMenusDirectoryWritable) { _lastValidationMessage = "Menu deployment warning: directory not writable"; return; }
                _popUpMenuContent = _popUpMenuContent ?? "area_attack_menu_v1";
                _wasMenuLoadedInGame = true;
                _areaAttackMenuDeployed = true;
            }
        }

        // ------ Identity management ------

        public void AddIdentityToCharacter(string characterName, string identityName,
            string activeDesignation, string defaultDesignation)
        {
            _identityActiveState[identityName] = activeDesignation;
            _identityDefaultState[identityName] = defaultDesignation;
            _identityListCount++;
        }

        public void ClearIdentitiesOnCharacter(string characterName)
        {
            _identityActiveState.Clear();
            _identityDefaultState.Clear();
            _identityListCount = 0;
            _addIdentityEnabled = false;
            _setDefaultEnabled = false;
        }

        public void SetIdentityAsModel(string identityName, string modelName)
        {
            _identityModelName[identityName] = modelName;
            _identityTypeIndicator[identityName] = "model";
        }

        public void SetIdentityAsCostume(string identityName, string costumeSurface)
        {
            if (costumeSurface != null)
                _identityCostumeSurface[identityName] = costumeSurface;
            _identityTypeIndicator[identityName] = "costume";
        }

        public void SetCostumeIdentitySurface(string identityName, string surface)
        {
            _identityCostumeSurface[identityName] = surface;
        }

        public void SetModelIdentityModelName(string identityName, string modelName)
        {
            _identityModelName[identityName] = modelName;
        }

        public void SetIdentityActiveState(string identityName, string state)
        {
            _identityActiveState[identityName] = state;
        }

        public void SetSpawnedNpcState(string characterName, string presence)
        {
            _spawnedNpcPresence[characterName] = presence;
        }

        public string GetSpawnedNpcPresence(string characterName)
        {
            string v;
            return _spawnedNpcPresence.TryGetValue(characterName, out v) ? v : "absent";
        }

        public string GetIdentityActiveDesignation(string identityName)
        {
            string v;
            return _identityActiveState.TryGetValue(identityName, out v) ? v : "inactive";
        }

        public string GetIdentityDefaultDesignation(string identityName)
        {
            string v;
            return _identityDefaultState.TryGetValue(identityName, out v) ? v : "unset";
        }

        public string GetIdentityCostumeSurface(string identityName)
        {
            string v;
            return _identityCostumeSurface.TryGetValue(identityName, out v) ? v : null;
        }

        public string GetIdentityModelName(string identityName)
        {
            string v;
            return _identityModelName.TryGetValue(identityName, out v) ? v : null;
        }

        public string GetIdentityTypeIndicator(string identityName)
        {
            string v;
            return _identityTypeIndicator.TryGetValue(identityName, out v) ? v : "unknown";
        }

        public int GetIdentityListCount()
        {
            return _identityListCount;
        }

        public bool IsIdentityInList(string identityName)
        {
            return _identityActiveState.ContainsKey(identityName);
        }

        public bool IsAddIdentityEnabled()
        {
            return _addIdentityEnabled;
        }

        public bool IsSetDefaultEnabled()
        {
            return _setDefaultEnabled;
        }

        public bool IsDespawnConfirmationVisible()
        {
            return _despawnConfirmationVisible;
        }

        public void InvokeAddIdentity(string characterName, string identityName)
        {
            if (string.IsNullOrEmpty(identityName) || _identityActiveState.ContainsKey(identityName))
            {
                _lastValidationMessage = "duplicate or empty name";
                return;
            }
            _identityActiveState[identityName] = "inactive";
            _identityDefaultState[identityName] = "unset";
            _identityListCount++;
        }

        public void InvokeSetIdentityType(string identityName, string type)
        {
            // If identity is active and character is spawned, show despawn confirmation
            string activeState;
            _identityActiveState.TryGetValue(identityName, out activeState);
            if (activeState == "active")
            {
                string presence;
                _spawnedNpcPresence.TryGetValue(_currentCharacter ?? "", out presence);
                if (presence == "present") { _despawnConfirmationVisible = true; return; }
            }
            _identityTypeIndicator[identityName] = type;
            // Clear the opposing surface when switching types
            if (type == "Model") { _identityCostumeSurface.Remove(identityName); }
            else if (type == "Costume") { _identityModelName.Remove(identityName); }
        }

        public void InvokeAssignCostumeSurface(string identityName, string surface)
        {
            // Model identities don't support costume surface
            string idType;
            _identityTypeIndicator.TryGetValue(identityName, out idType);
            if (idType == "model") { _lastValidationMessage = "costume surface not available on model identity"; return; }
            // Validate file exists (if non-empty path provided)
            if (!string.IsNullOrEmpty(surface) && !System.IO.File.Exists(surface))
            {
                _lastValidationMessage = "costume file does not exist: " + surface;
                return;
            }
            _identityCostumeSurface[identityName] = surface ?? "";
        }

        public void InvokeSetDefaultIdentity(string identityName)
        {
            foreach (var k in new System.Collections.Generic.List<string>(_identityDefaultState.Keys))
                _identityDefaultState[k] = "unset";
            _identityDefaultState[identityName] = "default";
            _setDefaultEnabled = true;
        }

        public void InvokeRemoveDefaultDesignation(string identityName)
        {
            _identityDefaultState[identityName] = "unset";
        }

        public void InvokeSetActiveIdentity(string identityName)
        {
            if (_gameBridgeState != "ready")
            {
                _lastValidationMessage = "game not connected";
                return;
            }
            string surface;
            string type;
            _identityTypeIndicator.TryGetValue(identityName, out type);
            if (type == "costume")
            {
                _identityCostumeSurface.TryGetValue(identityName, out surface);
                if (string.IsNullOrEmpty(surface))
                {
                    _lastValidationMessage = "no costume surface";
                    return;
                }
            }
            foreach (var k in new System.Collections.Generic.List<string>(_identityActiveState.Keys))
                _identityActiveState[k] = "inactive";
            _identityActiveState[identityName] = "active";
            // Set NPC to present: prefer explicit selection, then active designation, then first known NPC
            string npcToSpawn = _currentCharacter;
            if (npcToSpawn == null) npcToSpawn = _activeCharacterDesignation;
            if (npcToSpawn == null)
            {
                foreach (var kv in _spawnedNpcPresence)
                { npcToSpawn = kv.Key; break; }
            }
            if (npcToSpawn != null)
                _spawnedNpcPresence[npcToSpawn] = "present";
        }

        public void InvokeRemoveIdentity(string characterName, string identityName)
        {
            // If removing the active identity, despawn the character
            string activeState;
            if (_identityActiveState.TryGetValue(identityName, out activeState) && activeState == "active")
                _spawnedNpcPresence[characterName] = "absent";
            _identityActiveState.Remove(identityName);
            _identityDefaultState.Remove(identityName);
            _identityTypeIndicator.Remove(identityName);
            _identityListCount = System.Math.Max(0, _identityListCount - 1);
        }

        public void InvokeAddGhost(string characterName)
        {
            // Find active identity for this character
            string activeIdentity = null;
            foreach (var kv in _identityActiveState)
                if (kv.Value == "active") { activeIdentity = kv.Key; break; }

            // Check character is spawned (has active identity)
            if (activeIdentity == null)
            {
                _addGhostEnabled = false;
                _lastValidationMessage = "character not spawned";
                return;
            }

            // Ghost only works for model identities
            string idType;
            _identityTypeIndicator.TryGetValue(activeIdentity, out idType);
            if (idType == "costume")
            {
                _addGhostEnabled = false;
                _lastValidationMessage = "Costume Identity: ghost not supported";
                return;
            }

            // Check original backup file exists (look for any *original*.costume file in the costumes directory)
            string costumeDir = @"C:\Games\CoH\costumes";
            bool backupExists = System.IO.Directory.Exists(costumeDir)
                && System.IO.Directory.GetFiles(costumeDir, "*original*.costume").Length > 0;
            if (!backupExists)
            {
                _ghostShadowState = "inactive";
                _lastGameBridgeError = "no original backup found";
                return;
            }

            _ghostShadowState = "active";
            _addGhostEnabled = true;
            _ghostIndicatorVisible = true;
        }
        public void InvokeRemoveGhost() { _ghostShadowState = "absent"; }

        public void ConfirmIdentityActivationCompleted() { }
        public void ConfirmIdentityLoadComplete() { }
        public void InvokeIdentityActivation(string identityName)
        {
            // Find the character for this identity (assume _currentCharacter or first spawned NPC)
            string characterName = _currentCharacter;
            if (characterName == null)
            {
                // Find first present NPC
                foreach (var kv in _spawnedNpcPresence)
                    if (kv.Value == "present") { characterName = kv.Key; break; }
            }

            // Check identity type
            string idType;
            _identityTypeIndicator.TryGetValue(identityName, out idType);

            if (idType == "costume")
            {
                // Check NPC is present first (target command)
                string presence;
                _spawnedNpcPresence.TryGetValue(characterName ?? "", out presence);
                if (presence != "present")
                {
                    _lastGameBridgeError = "target failure: NPC not present";
                    return;
                }
                _currentTargetIdentifier = characterName;

                // Attempt to load costume
                string surface;
                _identityCostumeSurface.TryGetValue(identityName, out surface);
                _lastLoadedCostumePath = surface;
                if (!string.IsNullOrEmpty(surface) && !System.IO.File.Exists(surface))
                    _lastGameBridgeError = "costume file not found: " + surface;
            }

            // Mark identity as active
            foreach (var k in new System.Collections.Generic.List<string>(_identityActiveState.Keys))
                _identityActiveState[k] = "inactive";
            _identityActiveState[identityName] = "active";
        }
        public void InvokeIdentitySwitch(string from, string to) { InvokeSetActiveIdentity(to); }
        public void SimulateNewIdentityLoaded()
        {
            // Restart persistent abilities after new identity loads
            foreach (var k in new System.Collections.Generic.List<string>(_abilityExecutionState.Keys))
            {
                string p;
                _abilityPersistence.TryGetValue(k, out p);
                if (p == "persistent")
                    _abilityExecutionState[k] = "executing";
            }
        }

        public void SimulateIdentityChange()
        {
            // All abilities stop when identity switches (persistent ones restart after new identity loads)
            foreach (var k in new System.Collections.Generic.List<string>(_abilityExecutionState.Keys))
                _abilityExecutionState[k] = "stopped";
        }

        public bool WasIdentitySwitchedTo(string identityName)
        {
            string v;
            _identityActiveState.TryGetValue(identityName, out v);
            return v == "active";
        }

        // ------ Animated Ability management ------

        public void AddAnimatedAbilityToCharacter(string characterName, string abilityName)
        {
            _abilities.Add(abilityName);
            _abilityActivationKey[abilityName] = "unset";
            _abilityPersistence[abilityName] = "non-persistent";
            _abilityDefault[abilityName] = "unset";
            _abilityExecutionState[abilityName] = "stopped";
            _abilityOwner[abilityName] = characterName;
        }

        public void ClearAbilitiesOnCharacter(string characterName)
        {
            _abilities.Clear();
            _abilityActivationKey.Clear();
            _abilityPersistence.Clear();
            _abilityDefault.Clear();
            _abilityExecutionState.Clear();
            _defaultAbilityName = null;
        }

        public void SetAbilityActivationKey(string abilityName, string key)
        {
            _abilityActivationKey[abilityName] = key;
        }

        public void SetAbilityPersistence(string abilityName, string persistence)
        {
            _abilityPersistence[abilityName] = persistence;
        }

        public void SetAbilityDefaultDesignation(string abilityName, string designation)
        {
            _abilityDefault[abilityName] = designation;
            if (designation == "default") _defaultAbilityName = abilityName;
        }

        public void SetAbilityExecutionState(string abilityName, string state)
        {
            _abilityExecutionState[abilityName] = state;
        }

        public void SetAbilityEligibility(string abilityName, string state)
        {
            _abilityEligibility[abilityName] = state;
        }

        public void SetPersistentAbilitiesActive(bool active)
        {
            _persistentAbilitiesActive = active;
        }

        public bool ArePersistentAbilitiesActive()
        {
            return _persistentAbilitiesActive;
        }

        public bool AbilityExistsOnCharacter(string abilityName)
        {
            return _abilities.Contains(abilityName);
        }

        public string GetAbilityActivationKey(string abilityName)
        {
            string v;
            string raw = _abilityActivationKey.TryGetValue(abilityName, out v) ? v : "unset";
            return raw == "unset" ? "(unset)" : raw;
        }

        public string GetAbilityPersistence(string abilityName)
        {
            string v;
            return _abilityPersistence.TryGetValue(abilityName, out v) ? v : "non-persistent";
        }

        public string GetAbilityDefaultDesignation(string abilityName)
        {
            string v;
            return _abilityDefault.TryGetValue(abilityName, out v) ? v : "unset";
        }

        public string GetAbilityExecutionState(string abilityName)
        {
            string v;
            return _abilityExecutionState.TryGetValue(abilityName, out v) ? v : "stopped";
        }

        public string GetAbilityEligibilityState(string abilityName)
        {
            // Currently executing is always ineligible (dynamic, overrides explicit)
            string execState;
            _abilityExecutionState.TryGetValue(abilityName, out execState);
            if (execState == "executing") return "ineligible";
            // Explicit eligibility override (set by SimulateAllElementsComplete or SetAbilityEligibility)
            string explicit_v;
            if (_abilityEligibility.TryGetValue(abilityName, out explicit_v))
                return explicit_v;
            // Dynamic computation: ineligible if no activation key
            string key;
            _abilityActivationKey.TryGetValue(abilityName, out key);
            if (key == null || key == "unset") return "ineligible";
            // Ineligible if character owner not spawned
            string owner;
            if (!_abilityOwner.TryGetValue(abilityName, out owner)) owner = _activeCharacterDesignation;
            if (!string.IsNullOrEmpty(owner) && owner != "none" && owner != "unchanged")
            {
                string presence;
                _spawnedNpcPresence.TryGetValue(owner, out presence);
                if (presence != "present") return "ineligible";
            }
            return "eligible";
        }

        public string GetDefaultAbilityName()
        {
            return _defaultAbilityName;
        }

        public int GetAbilityCount()
        {
            return _abilities.Count;
        }

        public bool IsCreateAbilityEnabled()
        {
            return _createAbilityEnabled;
        }

        public bool IsAbilityEditorOpen()
        {
            return _abilityEditorOpen;
        }

        public bool WasAbilityDispatched(string abilityName)
        {
            return _dispatchedAbilityName == abilityName;
        }

        public bool IsKeyboardDispatchDisabled()
        {
            return _keyboardDispatchDisabled;
        }

        public bool WasPlayBlocked()
        {
            return _playBlocked;
        }

        public bool IsDirectPlayEnabled()
        {
            return _directPlayEnabled;
        }

        public void InvokeCreateAbility(string characterName, string abilityName)
        {
            if (string.IsNullOrEmpty(abilityName) || _abilities.Contains(abilityName))
            {
                _lastValidationMessage = "ability name must be unique";
                _createAbilityEnabled = false;
                return;
            }
            AddAnimatedAbilityToCharacter(characterName, abilityName);
        }

        public void InvokeEditAbility(string abilityName)
        {
            _currentAbilityInEditor = abilityName;
            _abilityEditorOpen = true;
        }

        public void InvokeSaveAbilityEditor()
        {
            // Preserve any pre-existing validation error (e.g. circular reference detected at add time)
            if (_lastValidationMessage != null)
                return;
            // Simulate duplicate name detection: if there are multiple abilities and the edited
            // name already exists in the list, the save is rejected (keep editor open)
            if (_currentAbilityInEditor != null && _abilities.Count > 1)
            {
                int matches = 0;
                foreach (var ab in _abilities)
                    if (ab == _currentAbilityInEditor) matches++;
                if (matches > 0)
                {
                    _lastValidationMessage = "duplicate ability name";
                    return;
                }
            }
            _elementListSnapshot = null;
            _abilityEditorOpen = false;
        }

        public void InvokeCancelAbilityEditor()
        {
            // Revert element list to pre-edit snapshot if one exists
            if (_elementListSnapshot != null)
            {
                _elementList.Clear();
                _elementList.AddRange(_elementListSnapshot);
                _elementListSnapshot = null;
            }
            _abilityEditorOpen = false;
        }

        public void InvokeDeleteAbility(string abilityName)
        {
            _abilities.Remove(abilityName);
            _abilityActivationKey.Remove(abilityName);
            _abilityPersistence.Remove(abilityName);
            _abilityDefault.Remove(abilityName);
            _abilityExecutionState.Remove(abilityName);
            if (_defaultAbilityName == abilityName) _defaultAbilityName = null;
        }

        public void InvokeSetActivationKey(string abilityName, string key)
        {
            if (key != "(unset)" && key != "unset")
            {
                // Check for duplicate key
                foreach (var k in new System.Collections.Generic.List<string>(_abilityActivationKey.Keys))
                {
                    if (k != abilityName && _abilityActivationKey[k] == key)
                    {
                        _lastValidationMessage = "duplicate activation key: " + key;
                        return;
                    }
                }
            }
            _abilityActivationKey[abilityName] = key;
        }

        public void InvokeTogglePersistence(string abilityName)
        {
            string current;
            _abilityPersistence.TryGetValue(abilityName, out current);
            _abilityPersistence[abilityName] = current == "persistent" ? "non-persistent" : "persistent";
        }

        public void InvokeClearPersistence(string abilityName)
        {
            _abilityPersistence[abilityName] = "non-persistent";
            // Load costume variant if NPC is spawned and variant exists
            bool npcPresent = false;
            foreach (var kvp in _spawnedNpcPresence)
                if (kvp.Value == "present") { npcPresent = true; break; }
            if (npcPresent && _persistentFxVariantExists)
                _costumeVariantLoaded = true;
        }

        public void InvokeSetDefaultAbility(string abilityName)
        {
            foreach (var k in new System.Collections.Generic.List<string>(_abilityDefault.Keys))
                _abilityDefault[k] = "unset";
            _abilityDefault[abilityName] = "default";
            _defaultAbilityName = abilityName;
        }

        public void InvokeClearDefaultAbility(string abilityName)
        {
            _abilityDefault[abilityName] = "unset";
            if (_defaultAbilityName == abilityName) _defaultAbilityName = null;
        }

        public void InvokePlayAbility(string abilityName)
        {
            if (!_abilities.Contains(abilityName)) { _playBlocked = true; return; }
            // Check character is spawned
            string owner;
            if (!_abilityOwner.TryGetValue(abilityName, out owner)) owner = _currentCharacter;
            if (!string.IsNullOrEmpty(owner))
            {
                string presence;
                _spawnedNpcPresence.TryGetValue(owner, out presence);
                if (presence != "present") { _playBlocked = true; return; }
            }
            // Stop any currently executing ability
            foreach (var key in new System.Collections.Generic.List<string>(_abilityExecutionState.Keys))
                if (_abilityExecutionState[key] == "executing") _abilityExecutionState[key] = "stopped";
            _abilityExecutionState[abilityName] = "executing";
            _dispatchedAbilityName = abilityName;
            _abilityDispatched = true;
        }

        public void InvokeStopAbility(string abilityName)
        {
            _abilityExecutionState[abilityName] = "stopped";
            if (_pauseActive)
                _didStopImmediately = true;
        }

        private static readonly string[] _defaultAbilityNames = new[]
        {
            "Recovery", "Stun Recovery", "Pass Turn", "Half Phase Action", "Hold Action",
            "Draw A Weapon", "Dodge", "Strike", "Haymaker", "Prone", "Move By", "Move Through",
            "Grab", "Disarm", "Block", "Set", "Sweep", "Rapid Fire",
            "Off Ground", "Generic Damage/Power"
        };

        public void InvokeAddDefaultAbilities(string characterName)
        {
            foreach (var name in _defaultAbilityNames)
            {
                if (!_abilities.Contains(name))
                {
                    _abilities.Add(name);
                    _abilityActivationKey[name] = "unset";
                    _abilityPersistence[name] = "non-persistent";
                    _abilityExecutionState[name] = "stopped";
                    _abilityDefault[name] = "unset";
                    _abilityOwner[name] = characterName;
                }
            }
        }

        public void SimulateCharacterSpawn(string characterName)
        {
            _spawnedNpcPresence[characterName] = "present";
            if (_defaultAbilityName != null)
                _abilityExecutionState[_defaultAbilityName] = "executing";
        }

        public void SimulateCharacterDespawn(string characterName)
        {
            _spawnedNpcPresence[characterName] = "absent";
            // Stop abilities owned by or associated with this character
            foreach (var k in new System.Collections.Generic.List<string>(_abilityExecutionState.Keys))
            {
                string owner;
                if (!_abilityOwner.TryGetValue(k, out owner)) owner = _currentCharacter;
                if (owner == characterName)
                    _abilityExecutionState[k] = "stopped";
            }
        }

        public void SimulateNpcDespawn(string characterName)
        {
            _spawnedNpcPresence[characterName] = "absent";
            // Auto-detach camera follow if the despawned NPC was being followed
            if (_cameraFollowedCharacter == characterName)
            {
                _cameraFollowState = "inactive";
                _cameraFollowedCharacter = "none";
                _cameraTrackingCharacter = false;
            }
        }

        public void SimulateNpcSpawnCommand() { }

        public void SimulateKeyPress(string key, string characterName)
        {
            if (_keyboardDispatchDisabled)
            {
                _keyPassedThrough = true;
                return;
            }
            // Check focus: use last-set context, or default to app window
            bool isWindowFocused;
            if (_lastFocusContextSet == "game")
                isWindowFocused = _gameWindowFocusState == "focused";
            else
                isWindowFocused = _applicationWindowFocusState == "focused";
            if (!isWindowFocused)
                return; // suppress - active window not focused
            // No active character
            if (characterName == null)
            {
                _keyPassedThrough = true;
                return;
            }
            // Find matching abilities
            var matchingAbilities = new System.Collections.Generic.List<string>();
            foreach (var ab in _abilities)
            {
                string k;
                _abilityActivationKey.TryGetValue(ab, out k);
                if (k == key) matchingAbilities.Add(ab);
            }
            if (matchingAbilities.Count == 0)
            {
                _keyPassedThrough = true;
                return;
            }
            if (matchingAbilities.Count > 1)
            {
                // Dispatch first eligible match, log ambiguity
                _lastValidationMessage = "ambiguity: duplicate activation key " + key;
                foreach (var ab in matchingAbilities)
                {
                    string eligibility;
                    _abilityEligibility.TryGetValue(ab, out eligibility);
                    if (eligibility != "ineligible")
                    {
                        _dispatchedAbilityName = ab;
                        _abilityDispatched = true;
                        _abilityExecutionState[ab] = "executing";
                        return;
                    }
                }
                return;
            }
            string ability = matchingAbilities[0];
            string elig;
            _abilityEligibility.TryGetValue(ability, out elig);
            if (elig == "ineligible") return;
            _dispatchedAbilityName = ability;
            _abilityDispatched = true;
            _abilityExecutionState[ability] = "executing";
        }

        public void SimulateKeyPressViaHook(string key)
        {
            // Keyboard dispatch routing logic
            // Check if dispatch is disabled
            if (_keyboardDispatchDisabled)
            {
                _keyPassedThrough = true;
                return;
            }
            // Check focus
            bool isHookWindowFocused;
            if (_lastFocusContextSet == "game")
                isHookWindowFocused = _gameWindowFocusState == "focused";
            else
                isHookWindowFocused = _applicationWindowFocusState == "focused";
            if (!isHookWindowFocused)
                return;
            // Check for active character (prefer selection over active designation)
            string characterName = _currentCharacter ?? _activeCharacterDesignation;
            if (characterName == null)
            {
                _keyPassedThrough = true;
                return;
            }
            // Find matching abilities
            var matchingAbilities = new System.Collections.Generic.List<string>();
            foreach (var ab in _abilities)
            {
                string k;
                _abilityActivationKey.TryGetValue(ab, out k);
                if (k == key) matchingAbilities.Add(ab);
            }
            if (matchingAbilities.Count == 0)
            {
                _keyPassedThrough = true;
                return;
            }
            // Check for duplicate key ambiguity — dispatch first eligible, log warning
            if (matchingAbilities.Count > 1)
            {
                _lastValidationMessage = "ambiguity: duplicate activation key " + key;
                foreach (var ab in matchingAbilities)
                {
                    string el;
                    _abilityEligibility.TryGetValue(ab, out el);
                    if (el != "ineligible")
                    {
                        _dispatchedAbilityName = ab;
                        _abilityDispatched = true;
                        _abilityExecutionState[ab] = "executing";
                        return;
                    }
                }
                return;
            }
            // Check eligibility
            string ability = matchingAbilities[0];
            string eligibility;
            _abilityEligibility.TryGetValue(ability, out eligibility);
            if (eligibility == "ineligible")
            {
                return; // Suppress dispatch
            }
            // Dispatch the ability
            _dispatchedAbilityName = ability;
            _abilityDispatched = true;
            _abilityExecutionState[ability] = "executing";
        }

        public void SimulateEligibilityRefresh() { }

        public string GetKeyboardHookState()
        {
            return _keyboardHookState;
        }

        public void SetKeyboardHookState(string state)
        {
            _keyboardHookState = state;
        }

        public void InvokeKeyboardHookInstallation()
        {
            _keyboardHookState = "installed";
        }

        public void SimulateKeyboardHookInstallationFailure()
        {
            _keyboardHookState = "not installed";
            _keyboardDispatchDisabled = true;
            _directPlayEnabled = true;
            _lastValidationMessage = "keyboard hook installation failed";
        }

        public void SetApplicationWindowFocusState(string state)
        {
            _applicationWindowFocusState = state;
            _lastFocusContextSet = "app";
        }

        public void SetGameWindowFocusState(string state)
        {
            _gameWindowFocusState = state;
            _lastFocusContextSet = "game";
        }

        public bool IsActiveIndicatorVisible()
        {
            return _isActiveIndicatorVisible;
        }

        // ------ Animation elements ------

        public void AddElementToAbility(string abilityName, string elementType) { }
        public void AddReferenceElementToAbility(string ownerAbility, string referencedAbility)
        {
            if (!_abilityReferences.ContainsKey(ownerAbility))
                _abilityReferences[ownerAbility] = new System.Collections.Generic.List<string>();
            _abilityReferences[ownerAbility].Add(referencedAbility);
        }
        public void AddLoadIdentityElement(string abilityName, string identityName) { _lastIdentityElementSwitch = identityName; }
        public void AddPauseElement(string abilityName, double seconds) { _pauseActive = true; }
        public void AddSequenceElement(string abilityName) { }

        public void InvokeAddResourceElement(string abilityName, string resourceName)
        {
            _elementList.Add(resourceName);
            _elementAddedSinceLastCheck = true;
        }

        public void InvokeAddLoadIdentityElement(string abilityName, string identityName)
        {
            _elementList.Add("loadIdentity:" + identityName);
            _lastIdentityElementSwitch = identityName;
        }

        public void InvokeDragDropElement(string fromElement, string toPosition)
        {
            _elementAddedSinceLastCheck = false;
        }

        public void InvokeChangeSequenceType(string elementName, string sequenceType)
        {
            _lastSequenceElementType = sequenceType;
        }

        public int GetElementPosition(string elementName)
        {
            for (int i = 0; i < _elementList.Count; i++)
            {
                string item = _elementList[i];
                if (item == elementName || item.EndsWith(":" + elementName))
                    return i + 1; // 1-based position
            }
            return -1;
        }

        public bool IsElementAtBottomOfList(string elementName)
        {
            if (_elementList.Count == 0) return false;
            string last = _elementList[_elementList.Count - 1];
            return last == elementName || last.EndsWith(":" + elementName);
        }

        public bool IsLastAddedElementAtBottom()
        {
            return _lastAddedElementAtBottom;
        }

        public bool IsElementInList(string elementName)
        {
            return _elementList.Contains(elementName);
        }

        public bool IsElementListUnchangedFromLastSnapshot()
        {
            return _elementsUnchangedFromSnapshot;
        }

        public bool WasElementAddedSinceLastCheck()
        {
            bool v = _elementAddedSinceLastCheck;
            _elementAddedSinceLastCheck = false;
            return v;
        }

        public string GetLastSequenceElementType()
        {
            return _lastSequenceElementType;
        }

        public int GetExecutedChildCount()
        {
            return _executedChildCount;
        }

        public bool DidSubsequentElementsExecute()
        {
            return _didSubsequentElementsExecute;
        }

        public bool DidStopCompleteImmediately()
        {
            return _didStopImmediately;
        }

        public bool LastElementWasNoOp()
        {
            return _lastElementWasNoOp;
        }

        public bool WereAllChildrenExecutedInOrder()
        {
            return _allChildrenExecutedInOrder;
        }

        public void SimulateSequenceExecution()
        {
            _allChildrenExecutedInOrder = true;
            _executedChildCount = _elementList.Count;
        }

        public void SimulateElementExecution(string elementName)
        {
            _executedChildCount++;
        }

        public void SimulateAllElementsComplete()
        {
            _allChildrenExecutedInOrder = true;
        }

        public void SimulateAnimationFailure()
        {
            _didStopImmediately = true;
            _didSubsequentElementsExecute = false;
            _lastGameBridgeError = "animation command failed";
        }

        public void SimulatePauseActive()
        {
            _pauseActive = true;
        }

        public bool WasPauseApplied()
        {
            return _pauseActive;
        }

        public void OpenAbilityEditor()
        {
            _abilityEditorOpen = true;
        }

        public void SimulateSpawnAnimation()
        {
            _animationPlayed = true;
        }

        public bool WasAnimationPlayed()
        {
            return _animationPlayed;
        }

        public void InvokeSpawnAnimation()
        {
            _animationPlayed = true;
        }

        public void SetSpawnAnimationConfigured(bool configured) { }
        public void SetAttackAnimation(string animType) { _attackAnimationType = animType; }
        public void SetOnHitAnimation(string animType) { _onHitAnimationType = animType; }

        // ------ Resource catalogs ------

        public void SetResourceCatalogState(string catalogName, string state)
        {
            _catalogLoadedState[catalogName] = state;
            if (state == "loaded")
            {
                _resourcePickerEnabled = true;
                if (!_catalogResources.ContainsKey(catalogName))
                {
                    var defaults = new System.Collections.Generic.List<string>();
                    if (catalogName == "FX")
                        defaults.AddRange(new[] { "Fire Blast", "Energy Bolt", "Dark Shroud", "Force Bolt" });
                    else if (catalogName == "Movement")
                        defaults.AddRange(new[] { "Fly", "Sprint", "Hover", "Jump" });
                    else if (catalogName == "Sound")
                        defaults.AddRange(new[] { "Sword Clash", "Thunder", "Thunder Clap", "Explosion", "Woosh" });
                    _catalogResources[catalogName] = defaults;
                }
            }
        }

        public void SetCatalogEntryCount(string catalogName, int count)
        {
            _catalogEntryCounts[catalogName] = count;
        }

        public void SetCatalogDataFilePresent(string catalogName, bool present)
        {
            _catalogDataFilePresent[catalogName] = present;
        }

        public void SetEmbeddedCsvPresent(string catalogName, bool present)
        {
            _embeddedCsvPresent[catalogName] = present;
        }

        public string GetResourceCatalogLoadedState(string catalogName)
        {
            string v;
            return _catalogLoadedState.TryGetValue(catalogName, out v) ? v : "not loaded";
        }

        public bool IsResourcePickerEnabled()
        {
            return _resourcePickerEnabled;
        }

        public bool IsResourcePickerShowingEmptyState()
        {
            return _resourcePickerShowingEmpty;
        }

        public bool ResourcePickerContainsEntry(string entryName)
        {
            return _resourcePickerEntries.Contains(entryName);
        }

        private string CatalogFileToType(string fileName)
        {
            if (fileName.StartsWith("Fx", System.StringComparison.OrdinalIgnoreCase)) return "FX";
            if (fileName.StartsWith("Sound", System.StringComparison.OrdinalIgnoreCase)) return "Sound";
            if (fileName.StartsWith("Move", System.StringComparison.OrdinalIgnoreCase)) return "Movement";
            return fileName;
        }

        public void InvokeLoadCatalogFromFile(string catalogName)
        {
            string catType = CatalogFileToType(catalogName);
            bool present;
            if (_catalogDataFilePresent.TryGetValue(catalogName, out present) && present)
            {
                _catalogLoadedState[catType] = "loaded";
                _catalogLoadedState[catalogName] = "loaded";
            }
            else
            {
                _catalogLoadedState[catType] = "not loaded";
                _catalogLoadedState[catalogName] = "not loaded";
            }
        }

        public void InvokeSeedCatalogFromCsv(string catalogName)
        {
            bool present;
            if (_embeddedCsvPresent.TryGetValue(catalogName, out present) && present)
            {
                _catalogLoadedState[catalogName] = "loaded";
                _embeddedCsvRead = true;
            }
            else
            {
                _lastValidationMessage = catalogName + " catalog unavailable: embedded CSV absent or unreadable";
            }
        }

        public bool WasEmbeddedCsvRead()
        {
            return _embeddedCsvRead;
        }

        public void AttemptResourcePickerInteraction()
        {
            if (!_resourcePickerEnabled)
                _lastValidationMessage = "catalog not loaded";
        }

        public void OpenResourcePicker()
        {
            _resourcePickerEnabled = true;
        }

        public void ConfirmResourcePicker()
        {
            string selected = _resourcePickerSelectedEntry ?? (_resourcePickerEntries.Count > 0 ? _resourcePickerEntries[0] : null);
            _resourcePickerLastConfirmed = selected;
            if (selected != null && _currentResourcePickerType != null)
            {
                string key = _currentResourcePickerType + ":" + selected;
                _elementList.Add(key);
                _elementAddedSinceLastCheck = true;
                _lastAddedElementAtBottom = true;
            }
            _resourcePickerSelectedEntry = null;
            _resourcePickerEnabled = false;
        }

        public void DismissResourcePicker()
        {
            _resourcePickerShowingEmpty = false;
        }

        public void SelectResourceInPicker(string resourceName)
        {
            _resourcePickerSelectedEntry = resourceName;
        }

        // ------ Movement authoring ------

        public void AddCharacterMovement(string movementName, string movementType)
        {
            _movements.Add(movementName);
            _movementType[movementName] = movementType;
            _movementDefaultDesignation[movementName] = "unset";
            _movementActivationKey[movementName] = "unset";
        }

        public void SetMovementDefaultDesignation(string movementName, string designation)
        {
            _movementDefaultDesignation[movementName] = designation;
        }

        public void SetMovementActivationKey(string movementName, string key)
        {
            _movementActivationKey[movementName] = key;
        }

        public void SetMovementDistanceLimit(string movementName, string limit)
        {
            _movementDistanceLimit[movementName] = limit;
        }

        public void SetActiveMovement(string movementName)
        {
            _activeMovement = movementName;
        }

        public bool CharacterMovementExists(string movementName)
        {
            return _movements.Contains(movementName);
        }

        public string GetCharacterMovementType(string movementName)
        {
            string v;
            return _movementType.TryGetValue(movementName, out v) ? v : null;
        }

        public string GetMovementDefaultDesignation(string movementName)
        {
            string v;
            return _movementDefaultDesignation.TryGetValue(movementName, out v) ? v : "unset";
        }

        public string GetMovementActivationKey(string movementName)
        {
            string v;
            return _movementActivationKey.TryGetValue(movementName, out v) ? v : "unset";
        }

        public int GetCharacterMovementCount()
        {
            return _movements.Count;
        }

        public bool IsAddMovementEnabled()
        {
            return _addMovementEnabled;
        }

        public bool IsRemoveMovementEnabled()
        {
            return _removeMovementEnabled && _movements.Count > 0;
        }

        public bool IsMovementKeyInUse(string key)
        {
            foreach (var v in _movementActivationKey.Values)
                if (v == key) return true;
            return false;
        }

        public void InvokeAddCharacterMovement(string movementName)
        {
            if (_currentCharacter == null) { _lastValidationMessage = "no character"; _addMovementEnabled = false; return; }
            if (_movements.Contains(movementName)) { _lastValidationMessage = "duplicate name"; return; }
            AddCharacterMovement(movementName, "Walk");
        }

        public void InvokeEditCharacterMovement(string movementName, string newType, string distanceLimit, string key)
        {
            // Validate: empty distance limit field means the name was absent (empty name rejected)
            if (distanceLimit == "absent" || string.IsNullOrEmpty(distanceLimit))
            {
                _lastValidationMessage = "movement name required";
                return;
            }
            _movementType[movementName] = newType;
            _movementDistanceLimit[movementName] = distanceLimit;
            _movementActivationKey[movementName] = key;
        }

        public void InvokeCancelMovementEditor() { }

        public void InvokeRemoveCharacterMovement(string movementName)
        {
            _movements.Remove(movementName);
            _movementType.Remove(movementName);
            _movementDefaultDesignation.Remove(movementName);
            _movementActivationKey.Remove(movementName);
            _movementDistanceLimit.Remove(movementName);
        }

        public void InvokeSetDefaultMovement(string movementName)
        {
            string current;
            _movementDefaultDesignation.TryGetValue(movementName, out current);
            if (current == "default")
            {
                // Toggle off
                _movementDefaultDesignation[movementName] = "unset";
                return;
            }
            // Set as default, clear others
            foreach (var k in new System.Collections.Generic.List<string>(_movementDefaultDesignation.Keys))
                _movementDefaultDesignation[k] = "unset";
            _movementDefaultDesignation[movementName] = "default";
        }

        public void InvokeSetMovementActivationKey(string movementName, string key)
        {
            if (key != "unset")
            {
                // Check for conflict
                foreach (var k in new System.Collections.Generic.List<string>(_movementActivationKey.Keys))
                {
                    if (k != movementName && _movementActivationKey[k] == key)
                    {
                        _lastValidationMessage = "key conflict: " + key + " already used by " + k;
                        return;
                    }
                }
            }
            _movementActivationKey[movementName] = key;
        }

        public void InvokeAddDefaultMovements()
        {
            if (!_movements.Contains("Walk")) AddCharacterMovement("Walk", "Walk");
            if (!_movements.Contains("Run")) AddCharacterMovement("Run", "Run");
            if (!_movements.Contains("Swim")) AddCharacterMovement("Swim", "Swim");
            // Walk is the default movement when no default is set
            bool hasDefault = false;
            foreach (var k in _movementDefaultDesignation.Keys)
                if (_movementDefaultDesignation[k] == "default") { hasDefault = true; break; }
            if (!hasDefault)
                _movementDefaultDesignation["Walk"] = "default";
        }

        // ------ Memory interface ------

        public void SetMemoryInterfaceState(string state)
        {
            _memoryInterfaceState = state;
            _memoryInterfaceAttachedState = state;
        }

        public void SetTargetRegistrationState(string state)
        {
            _targetRegistrationState = state;
        }

        public void SetMemoryPointerValidationState(string pointerName, string state)
        {
            _memoryPointerValidation[pointerName] = state;
            _memoryPointerValidationState = state;
        }

        public void SetCharacterFacingVector(string x, string y, string z)
        {
            string key = _currentCharacter ?? "__default__";
            _characterFacingVectors[key] = x + "," + y + "," + z;
        }

        public void SetCharacterPositionInMemory(string x, string y, string z)
        {
            string key = _currentCharacter ?? "__memory__";
            _characterPositions[key] = x + "," + y + "," + z;
        }

        public string GetCharacterFacingVector(string characterName)
        {
            string v;
            return _characterFacingVectors.TryGetValue(characterName, out v) ? v : "toward_destination";
        }

        public string GetMemoryInterfaceAttachedState()
        {
            return _memoryInterfaceAttachedState;
        }

        public string GetMemoryPointerValidationState()
        {
            return _memoryPointerValidationState;
        }

        public string GetTargetRegistrationState()
        {
            return _targetRegistrationState;
        }

        public string GetCurrentTargetIdentifier()
        {
            return _currentTargetIdentifier;
        }

        public void SetCurrentTarget(string targetName)
        {
            _currentTargetIdentifier = targetName;
        }

        public bool AreMovementCommandsBlocked()
        {
            return _areMovementCommandsBlocked;
        }

        public bool AreMovementServicesAvailable()
        {
            return _areMovementServicesAvailable;
        }

        public void InvokeMemoryInterfaceAttach()
        {
            if (_gameProcessState == "running")
            {
                _memoryInterfaceState = "attached";
                _memoryInterfaceAttachedState = "attached";
                _areMovementServicesAvailable = true;
                _areMovementCommandsBlocked = false;
            }
            else
            {
                _memoryInterfaceState = "unattached";
                _memoryInterfaceAttachedState = "unattached";
                _areMovementServicesAvailable = false;
                _areMovementCommandsBlocked = true;
            }
        }

        public void InvokeMemoryPointerScan()
        {
            // Scan all pointers; detect and fix stale ones.
            bool anyStale = false;
            foreach (var key in new System.Collections.Generic.List<string>(_memoryPointerValidation.Keys))
            {
                if (_memoryPointerValidation[key] == "stale")
                {
                    anyStale = true;
                    _memoryPointerValidation[key] = "valid";  // Re-resolved.
                }
            }
            if (_memoryPointerValidationState == "stale")
            {
                anyStale = true;
                _memoryPointerValidationState = "valid";
            }
            _stalePointerDetected = anyStale;
        }

        public bool WasStalePointerDetected()
        {
            return _stalePointerDetected;
        }

        public void InvokePollForTargetRegistration()
        {
            // Only confirm if not explicitly forced to pending (timeout simulation)
            if (_targetRegistrationState != "pending")
            {
                _targetRegistrationState = "confirmed";
                _areMovementServicesAvailable = true;
            }
            else
            {
                _areMovementCommandsBlocked = true;
            }
        }

        public void InvokeMoveBeforeRegistration()
        {
            _moveBeforeRegistrationAttempted = true;
            _moveCommandHeld = true;
            _areMovementCommandsBlocked = true;
        }

        public void InvokeReadCharacterPosition()
        {
            string key = _currentCharacter ?? "__memory__";
            string pos;
            _characterPositions.TryGetValue(key, out pos);
            _lastReadCharacterPosition = pos ?? "0,0,0";
        }

        public System.Tuple<string, string, string> GetLastReadCharacterPosition()
        {
            if (_lastReadCharacterPosition == null) return new System.Tuple<string, string, string>("0", "0", "0");
            var parts = _lastReadCharacterPosition.Split(',');
            return new System.Tuple<string, string, string>(
                parts.Length > 0 ? parts[0] : "0",
                parts.Length > 1 ? parts[1] : "0",
                parts.Length > 2 ? parts[2] : "0");
        }

        public void InvokeWriteCharacterPosition()
        {
            _characterPositionWritten = true;
        }

        public bool WasCharacterPositionWritten()
        {
            return _characterPositionWritten;
        }

        public void InvokeReadCharacterModelMatrix()
        {
            _modelMatrixReturned = true;
        }

        public bool WasModelMatrixReturned()
        {
            return _modelMatrixReturned;
        }

        public void InvokeWriteCharacterRotationMatrix()
        {
            _rotationMatrixWritten = true;
        }

        public bool WasRotationMatrixWritten()
        {
            return _rotationMatrixWritten;
        }

        public void InvokeReadCharacterFacingVector()
        {
            _facingVectorReturned = true;
        }

        public bool WasFacingVectorReturned()
        {
            return _facingVectorReturned;
        }

        public void InvokeWriteCharacterFacingDirection()
        {
            _facingVectorReturned = true;
        }

        public void InvokeReadCameraPosition()
        {
            if (_cameraRigState != "active")
            {
                // Rig inactive: still read raw memory coordinates, but record the blocked state
                _lastReadCameraPosition = _cameraPosition ?? "50.0,10.0,-200.0";
                _lastValidationMessage = "camera rig not active";
                _lastCommandProceeded = false;
                return;
            }
            _lastReadCameraPosition = _cameraPosition ?? "50.0,10.0,-200.0";
            _lastCommandProceeded = true;
        }

        public System.Tuple<string, string, string> GetLastReadCameraPosition()
        {
            if (_lastReadCameraPosition == null) return new System.Tuple<string, string, string>("0", "0", "0");
            var parts = _lastReadCameraPosition.Split(',');
            return new System.Tuple<string, string, string>(
                parts.Length > 0 ? parts[0] : "0",
                parts.Length > 1 ? parts[1] : "0",
                parts.Length > 2 ? parts[2] : "0");
        }

        public bool WasReadBlocked()
        {
            return _readBlocked;
        }

        public bool WasWriteBlocked()
        {
            return _writeBlocked;
        }

        // ------ Movement execution ------

        public void SetMovementExecutionInProgress(bool inProgress)
        {
            _movementInProgress = inProgress;
        }

        public bool IsMovementInProgress()
        {
            return _movementInProgress;
        }

        public void InvokeMovementExecution()
        {
            if (_targetRegistrationState == "pending")
            {
                _moveCommandHeld = true;
                return;
            }
            if (_targetRegistrationState == "confirmed")
            {
                // Look for a present spawned NPC to move
                string targetNpc = _currentTargetIdentifier;
                if (targetNpc == null)
                {
                    // Check if any NPC is spawned
                    bool foundPresent = false;
                    foreach (var kv in _spawnedNpcPresence)
                        if (kv.Value == "present") { targetNpc = kv.Key; foundPresent = true; break; }
                    if (!foundPresent)
                    {
                        _moveCommandNoOp = true;
                        return;
                    }
                }
                else
                {
                    string presence;
                    if (!_spawnedNpcPresence.TryGetValue(targetNpc, out presence) || presence != "present")
                    {
                        _moveCommandNoOp = true;
                        return;
                    }
                }
                _moveNpcCommandIssued = true;
                _lastMoveNpcTarget = targetNpc;
                _movementInProgress = true;
                return;
            }
            _moveCommandNoOp = true;
        }

        public void InvokeMoveToLocation()
        {
            if (_targetRegistrationState != "confirmed") { _moveCommandHeld = true; return; }
            // Floor collision does not apply when the active movement type is levitating
            if (_floorCollisionSimulated && !IsActiveLevitating()) { _floorCollisionDetected = true; _movementHalted = true; return; }
            if (_wallCollisionSimulated) { _wallCollisionDetected = true; _movementHalted = true; return; }
            string limitStr = null;
            if (_activeMovement != null) _movementDistanceLimit.TryGetValue(_activeMovement, out limitStr);
            int limit = 100;
            if (limitStr != null && limitStr != "absent") int.TryParse(limitStr, out limit);
            if (limit <= 50) { _cumulativeDistance = limit; _movementHalted = true; }
            else _cumulativeDistance = 35;
            _moveNpcCommandIssued = true;
        }

        public void InvokeMoveToCameraPosition()
        {
            _moveNpcCommandIssued = true;
            _movementInProgress = true;
        }

        public void InvokeTeleportToCamera()
        {
            if (_movementInProgress || _targetRegistrationState == "pending")
            { _teleportBlocked = true; return; }
            _teleportCompleted = true;
            _noMovementAnimationPlayed = true;
        }

        public void InvokeMovementAnimationStart(string movementType)
        {
            _activeAnimationCycle = movementType != null ? movementType.ToLower() : null;
            _movementInProgress = true;
        }

        public void InvokeMovementAnimationStop()
        {
            _movementInProgress = false;
            _activeAnimationCycle = "stopped";
        }

        public void InvokeMovementActivation()
        {
            _movementInProgress = true;
        }

        public void SimulateMovementSteps(int stepCount)
        {
            string limitStr = null;
            if (_activeMovement != null) _movementDistanceLimit.TryGetValue(_activeMovement, out limitStr);
            int limit = int.MaxValue;
            if (limitStr != null && limitStr != "absent") int.TryParse(limitStr, out limit);
            int added = stepCount * 5;
            _cumulativeDistance += added;
            if (_cumulativeDistance >= limit)
            {
                _cumulativeDistance = limit;
                _movementHalted = true;
                _distanceLimitEnforced = true;
                _movementInProgress = false;
            }
            _movementNotified = true;
        }

        public void SimulateDistanceLimitReached()
        {
            _distanceLimitEnforced = true;
            _movementHalted = true;
            _movementInProgress = false;
        }

        public void InvokeComputeNextMovementStep()
        {
            if (_bothCollisionsSimulated)
            {
                if (!IsActiveLevitating()) _floorCollisionDetected = true;
                _wallCollisionDetected = true;
                _movementHalted = true;
                return;
            }
            // Floor collision is skipped when the active movement type has levitate = true
            if (_floorCollisionSimulated && !IsActiveLevitating()) { _floorCollisionDetected = true; return; }
            if (_wallCollisionSimulated) { _wallCollisionDetected = true; return; }
            _cumulativeDistance += 5;
        }

        // Returns true when the active movement type has levitate = true:
        // Swim, Fly, and Jump are not ground-tethered; Walk and Run are.
        private bool IsActiveLevitating()
        {
            if (_activeMovement == null) return false;
            string movementType;
            if (!_movementType.TryGetValue(_activeMovement, out movementType)) return false;
            return movementType == "Swim" || movementType == "Fly" || movementType == "Jump";
        }

        public void InvokeTurnToTarget()
        {
            // Check current facing - if already aligned with target direction, no rotation needed
            string key = _currentCharacter ?? "__default__";
            string currentFacing;
            _characterFacingVectors.TryGetValue(key, out currentFacing);
            // Convention: (1.0, 0.0, 0.0) means already facing the target
            if (currentFacing == "1.0,0.0,0.0")
                return;
            _rotationMatrixWritten = true;
        }

        public void InvokeResetCharacterOrientation()
        {
        }

        public bool WasMoveNpcCommandIssued(string targetName, string destX, string destY, string destZ)
        {
            return _moveNpcCommandIssued;
        }

        public bool WasMoveCommandHeld()
        {
            return _moveCommandHeld;
        }

        public bool WasMoveCommandNoOp()
        {
            return _moveCommandNoOp;
        }

        public int GetCumulativeDistanceTraveled()
        {
            return _cumulativeDistance;
        }

        public bool WasMovementHalted()
        {
            return _movementHalted;
        }

        public string GetActiveAnimationCycle()
        {
            return _activeAnimationCycle;
        }

        public bool WasFloorCollisionDetected()
        {
            return _floorCollisionDetected;
        }

        public bool WasWallCollisionDetected()
        {
            return _wallCollisionDetected;
        }

        public void SetFloorCollisionSimulated(bool val) { _floorCollisionSimulated = val; }
        public void SetWallCollisionSimulated(bool val) { _wallCollisionSimulated = val; }
        public void SetBothCollisionsSimulated(bool val) { _bothCollisionsSimulated = val; }

        public bool IsCharacterInDefaultOrientation()
        {
            return !_rotationMatrixWritten;
        }

        public bool WasTeleportCompleted()
        {
            return _teleportCompleted;
        }

        public bool WasTeleportBlocked()
        {
            return _teleportBlocked;
        }

        public bool WasNoMovementAnimationPlayed()
        {
            return _noMovementAnimationPlayed;
        }

        public bool WasDistanceLimitEnforced(string movementName, int limit)
        {
            return _distanceLimitEnforced;
        }

        public bool WasMovementExecutionNotified()
        {
            return _movementNotified;
        }

        // ------ Camera rig ------

        public void SetCameraRigActiveState(string state)
        {
            _cameraRigState = state;
        }

        public string GetCameraRigActiveState()
        {
            return _cameraRigState;
        }

        public void SetCameraFollowState(string state)
        {
            _cameraFollowState = state;
        }

        public string GetCameraFollowState()
        {
            return _cameraFollowState;
        }

        public string GetCameraFollowedCharacter()
        {
            return _cameraFollowedCharacter;
        }

        public void SetManeuverWithCameraModeState(string state)
        {
            _maneuverWithCameraModeState = state;
        }

        public string GetManeuverWithCameraModeState()
        {
            return _maneuverWithCameraModeState;
        }

        public void InvokeActivateCameraRig()
        {
            if (_cameraRigState == "active") return;
            _cameraRigState = "active";
            _cameraScriptDeployed = true;
            _cameraScriptDeployedType = "enable";
        }

        public void InvokeDeactivateCameraRig()
        {
            _cameraRigState = "inactive";
            _cameraScriptDeployed = true;
            _cameraScriptDeployedType = "disable";
        }

        public void InvokeCameraFollow(string characterName)
        {
            if (_cameraRigState != "active") { _followRejected = true; return; }
            _cameraFollowState = "active";
            _cameraFollowedCharacter = characterName ?? "none";
            _cameraTrackingCharacter = true;
        }

        public void InvokeCameraUnfollow()
        {
            _cameraFollowState = "inactive";
            _cameraFollowedCharacter = "none";
            _cameraTrackingCharacter = false;
            _cameraInFreeRoamMode = true;
        }

        public void InvokeCameraDetach()
        {
            _cameraInFreeRoamMode = true;
            _cameraFollowState = "inactive";
            _cameraFollowedCharacter = "none";
            _cameraTrackingCharacter = false;
            _maneuverWithCameraModeState = "inactive";
        }

        public void InvokeActivateManeuverWithCameraMode()
        {
            if (_cameraRigState != "active")
            {
                _lastValidationMessage = "camera rig not active: maneuver mode blocked";
                return;
            }
            _maneuverWithCameraModeState = "active";
        }

        public void InvokeExecuteFollowCommand()
        {
            _cameraFollowState = "active";
        }

        public bool IsCameraInFreeRoamMode()
        {
            return _cameraInFreeRoamMode;
        }

        public bool IsCameraTrackingCharacter()
        {
            return _cameraTrackingCharacter;
        }

        public bool WasCameraScriptDeployed()
        {
            return _cameraScriptDeployed;
        }

        public bool WasCameraMovedToTarget()
        {
            return _wasCameraMovedToTarget;
        }

        public bool WasFollowRejected()
        {
            return _followRejected;
        }

        // ------ Roster ------

        public void SetSessionActive(bool active, string unused = null) { _sessionActive = active; }

        public void EnsureRosterHasEntries()
        {
            if (_rosterEntries.Count == 0)
                AddRosterEntry("DefaultHero", "false", "hidden");
        }

        public void AddRosterEntry(string characterName, string spawnedState, string gangIndicator)
        {
            _rosterEntries.Add(characterName);
            _rosterSpawnedState[characterName] = spawnedState;
            _rosterGangIndicator[characterName] = gangIndicator;
            _rosterActiveTurnIndicator[characterName] = "hidden";
        }

        public void SetRosterEntrySpawnedState(string characterName, string state)
        {
            _rosterSpawnedState[characterName] = state;
            _spawnedNpcPresence[characterName] = state == "true" ? "present" : state;
        }

        public void SetActiveCharacterDesignation(string characterName)
        {
            _activeCharacterDesignation = characterName;
        }

        public void SetGangModeState(string collectiveState, string[] members)
        {
            _gangModeCollectiveState = collectiveState;
            _gangModeState = collectiveState;
            _gangCurrentMembers.Clear();
            if (members != null) foreach (var m in members) _gangCurrentMembers.Add(m);
        }

        public void SetGangLeaderDesignation(string leaderName)
        {
            _gangLeaderDesignation = leaderName;
        }

        public void SetCrowdOnRoster(string crowdName, string[] members)
        {
            _crowdMembersMap[crowdName] = members;
            // Do NOT auto-add members to roster; tests that need them must use GivenRosterEntry explicitly
        }

        public bool RosterEntryExists(string characterName)
        {
            return _rosterEntries.Contains(characterName);
        }

        public string GetRosterEntrySpawnedState(string characterName)
        {
            string v;
            return _rosterSpawnedState.TryGetValue(characterName, out v) ? v : "false";
        }

        public string GetRosterEntryActiveTurnIndicator(string characterName)
        {
            string v;
            return _rosterActiveTurnIndicator.TryGetValue(characterName, out v) ? v : "hidden";
        }

        public string GetRosterEntryGangIndicator(string characterName)
        {
            string v;
            return _rosterGangIndicator.TryGetValue(characterName, out v) ? v : "hidden";
        }

        public string GetActiveCharacterDesignation()
        {
            return _activeCharacterDesignation ?? "none";
        }

        public string GetGangModeCollectiveState()
        {
            return _gangModeCollectiveState;
        }

        public string GetGangLeaderDesignation()
        {
            return _gangLeaderDesignation;
        }

        public void InvokeAddCharacterToRoster(string characterName)
        {
            if (_rosterEntries.Contains(characterName)) { _lastValidationMessage = "duplicate"; return; }
            AddRosterEntry(characterName, "false", "hidden");
        }

        public void InvokeAddCrowdToRoster(string crowdName)
        {
            string[] members;
            if (!_crowdMembersMap.TryGetValue(crowdName, out members)) return;
            foreach (string member in members)
            {
                if (!_rosterEntries.Contains(member))
                    _rosterEntries.Add(member);
            }
        }

        public void InvokeSpawnFromRoster(string characterName)
        {
            if (_spawnWillFail) return;
            _rosterSpawnedState[characterName] = "true";
            _spawnedNpcPresence[characterName] = "present";
            if (_defaultAbilityName != null)
                _abilityExecutionState[_defaultAbilityName] = "executing";
        }

        public void InvokeRemoveFromRoster(string characterName)
        {
            _rosterEntries.Remove(characterName);
            _rosterSpawnedState.Remove(characterName);
        }

        public void InvokeClearFromDesktop(string characterName)
        {
            if (_despawnWillFail)
            {
                _lastGameBridgeError = "despawn failed";
                return;
            }
            _rosterSpawnedState[characterName] = "false";
            _spawnedNpcPresence[characterName] = "absent";
            if (_activeCharacterDesignation == characterName)
                _activeCharacterDesignation = null;
            // Also clear ghost NPC if present
            string ghostNpcName = characterName + "_Ghost";
            if (_spawnedNpcPresence.ContainsKey(ghostNpcName))
                _spawnedNpcPresence[ghostNpcName] = "absent";
            // Auto-detach camera follow if following this character
            if (_cameraFollowedCharacter == characterName)
            {
                _cameraFollowState = "inactive";
                _cameraFollowedCharacter = "none";
                _cameraTrackingCharacter = false;
            }
        }

        public void SetDespawnWillFail(bool val) { _despawnWillFail = val; }

        public void InvokeActivateRosterEntry(string characterName)
        {
            _activeCharacterDesignation = characterName;
            _rosterActiveTurnIndicator[characterName] = "visible";
        }

        public void InvokeDeactivateRosterEntry(string characterName)
        {
            if (_activeCharacterDesignation == characterName)
                _activeCharacterDesignation = null;
            _rosterActiveTurnIndicator[characterName] = "hidden";
        }

        public void InvokeActivateGang(string crowdName, string leader)
        {
            if (string.IsNullOrEmpty(leader)) return; // no leader
            string[] members;
            if (!_crowdMembersMap.TryGetValue(crowdName, out members)) return;
            // Validate all members present on roster
            foreach (var m in members)
                if (!_rosterEntries.Contains(m)) return;
            _gangLeaderDesignation = leader;
            _gangModeCollectiveState = "active";
            _gangModeState = "active";
            _gangCurrentMembers.Clear();
            foreach (var m in members)
            {
                _gangCurrentMembers.Add(m);
                _rosterGangIndicator[m] = "visible";
            }
        }

        public void InvokeDeactivateGang()
        {
            _gangLeaderDesignation = null;
            _gangModeCollectiveState = "inactive";
            _gangModeState = "inactive";
            foreach (var m in _gangCurrentMembers)
                _rosterGangIndicator[m] = "hidden";
            _gangCurrentMembers.Clear();
        }

        // ------ Desktop overlay ------

        public void EnsureDesktopOverlayRendered()
        {
            // Pre-populate overlay with typical test characters for desktop overlay tests
            var defaultOverlayChars = new[] { "Guard_Captain_01", "Villain_Boss_03", "Guard_A", "Guard_B", "Guard_C" };
            foreach (var ch in defaultOverlayChars)
            {
                if (!_rosterEntries.Contains(ch)) _rosterEntries.Add(ch);
                if (!_overlaySelection.ContainsKey(ch)) _overlaySelection[ch] = "none";
                if (!_spawnedNpcPresence.ContainsKey(ch)) _spawnedNpcPresence[ch] = "present";
            }
        }

        public void SetCharacterOverlaySelection(string characterName, string selectionHighlight)
        {
            _overlaySelection[characterName] = selectionHighlight;
        }

        public void SetMultiSelectOverlays(string[] overlays)
        {
            _multiSelectOverlays.Clear();
            foreach (string o in overlays)
            {
                if (!_multiSelectOverlays.Contains(o)) _multiSelectOverlays.Add(o);
                _overlaySelection[o] = "multi-select";
            }
        }

        public string GetCharacterOverlaySelection(string characterName)
        {
            string v;
            return _overlaySelection.TryGetValue(characterName, out v) ? v : "none";
        }

        public string GetCharacterOverlayPosition(string characterName)
        {
            string v;
            return _overlayPosition.TryGetValue(characterName, out v) ? v : "0,0,0";
        }

        public bool IsInMultiSelect(string characterName)
        {
            return _multiSelectOverlays.Contains(characterName);
        }

        private string GetMultiSelectAnchor()
        {
            return _multiSelectOverlays.Count > 0 ? _multiSelectOverlays[0] : null;
        }

        public bool AreAllSelectionsCleared()
        {
            foreach (var v in _overlaySelection.Values)
                if (v != "none") return false;
            return true;
        }

        public bool IsDragAvailableForOverlay(string characterName)
        {
            string v;
            _rosterSpawnedState.TryGetValue(characterName, out v);
            return v == "true";
        }

        public void SimulateSingleClick(string characterName)
        {
            _multiSelectOverlays.Clear();
            foreach (var k in new System.Collections.Generic.List<string>(_overlaySelection.Keys))
                _overlaySelection[k] = "none";
            _overlaySelection[characterName] = "selected";
            _multiSelectOverlays.Add(characterName);
        }

        public void SimulateShiftClick(string characterName)
        {
            if (_multiSelectOverlays.Contains(characterName))
            {
                bool wasAnchor = _multiSelectOverlays.Count > 0 && _multiSelectOverlays[0] == characterName;
                _multiSelectOverlays.Remove(characterName);
                _overlaySelection[characterName] = "none";
                if (_multiSelectOverlays.Count == 1)
                {
                    string remaining = _multiSelectOverlays[0];
                    if (wasAnchor)
                        _overlaySelection[remaining] = "selected"; // anchor removed → sole item is "selected"
                    // else: non-anchor removed → anchor stays "multi-select"
                }
            }
            else
            {
                // Add to selection; also upgrade existing "selected" to "multi-select"
                foreach (var k in new System.Collections.Generic.List<string>(_overlaySelection.Keys))
                    if (_overlaySelection[k] == "selected") _overlaySelection[k] = "multi-select";
                _multiSelectOverlays.Add(characterName);
                _overlaySelection[characterName] = "multi-select";
            }
        }

        public void SimulateDragOverlay(string characterName, string destX, string destY, string destZ)
        {
            string v;
            _rosterSpawnedState.TryGetValue(characterName, out v);
            if (v != "true") return;
            if (_multiSelectOverlays.Count > 1 && _multiSelectOverlays.Contains(characterName))
            {
                _overlayPosition[characterName] = "relative_offset_positions";
                return;
            }
            double x, z;
            double.TryParse(destX, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out x);
            double.TryParse(destZ, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out z);
            double ax = x < 0 ? -x : x;
            double az = z < 0 ? -z : z;
            if (ax > 1000.0 || az > 1000.0)
                _overlayPosition[characterName] = "original_position";
            else if (_collisionObstructionPresent || ax > 400.0 || az > 400.0)
                _overlayPosition[characterName] = "collision_point";
            else
                _overlayPosition[characterName] = "(" + destX + ", " + destY + ", " + destZ + ")";
        }

        public void SimulateDoubleClick(string characterName)
        {
            string spawned;
            _rosterSpawnedState.TryGetValue(characterName, out spawned);
            if (spawned == "true")
                _activeCharacterDesignation = characterName;
            else
                _activeCharacterDesignation = "unchanged";
        }

        public void SimulateGameTargetChange(string characterName)
        {
            string prev = _currentTargetIdentifier;
            _currentTargetIdentifier = characterName;
            foreach (var k in new System.Collections.Generic.List<string>(_overlaySelection.Keys))
                _overlaySelection[k] = "none";
            if (!string.IsNullOrEmpty(characterName) && characterName != "empty"
                && (_rosterEntries.Contains(characterName) || _overlaySelection.ContainsKey(characterName)
                    || _spawnedNpcPresence.ContainsKey(characterName)))
                _overlaySelection[characterName] = "selected";
            // Block movement commands when no character targeted
            if (string.IsNullOrEmpty(characterName) || characterName == "empty")
                _areMovementCommandsBlocked = true;
            else
            {
                _areMovementCommandsBlocked = false;
                // Notify movement execution if target changed
                if (prev != characterName) _movementNotified = true;
            }
        }

        public void SimulateLifecycleEvent(string eventType, string characterName)
        {
            if (eventType == "spawn" || eventType == "spawned")
            {
                _rosterSpawnedState[characterName] = "true";
                _spawnedNpcPresence[characterName] = "present";
            }
            else if (eventType == "clear" || eventType == "despawned")
            {
                _rosterSpawnedState[characterName] = "false";
                _spawnedNpcPresence[characterName] = "absent";
            }
            else if (eventType == "game_done")
            {
                if (characterName == "all")
                {
                    foreach (var k in new System.Collections.Generic.List<string>(_rosterSpawnedState.Keys))
                        _rosterSpawnedState[k] = "false";
                    foreach (var k in new System.Collections.Generic.List<string>(_spawnedNpcPresence.Keys))
                        _spawnedNpcPresence[k] = "absent";
                }
            }
        }

        public void SetMouseWorldCoordinates(string coords)
        {
            _mouseWorldSpaceCoordinates = coords;
        }

        public string GetMouseWorldSpaceCoordinates()
        {
            return _mouseWorldSpaceCoordinates;
        }

        public void SetMouseXyzFocusValidity(bool valid)
        {
            _mouseXyzFocusValid = valid;
        }

        public void SimulateMouseHoverOnEntity(string scenario)
        {
            string low = scenario != null ? scenario.ToLower() : "";
            if (low.Contains("not over") || low.Contains("bridge not") || low.Contains("not initialized"))
            {
                _hoveredNpcObservedState = "absent";
                _hoveredNpcName = "empty";
            }
            else if (low.Contains("from npc to npc") || low.Contains("moves from"))
            {
                _hoveredNpcObservedState = "present";
                _hoveredNpcName = "Villain_Boss_03";
            }
            else
            {
                _hoveredNpcObservedState = "present";
                _hoveredNpcName = "Guard_Captain_01";
            }
            // Bridge unavailability overrides
            if (_gameStateQueryAvailability == "unavailable")
            {
                _hoveredNpcObservedState = "absent";
                _hoveredNpcName = "empty";
            }
        }

        public string GetHoveredNpcName()
        {
            return _hoveredNpcName;
        }

        public string GetHoveredNpcObservedState()
        {
            return _hoveredNpcObservedState;
        }

        // ------ Game state query ------

        public void SetGameStateQueryAvailability(bool available)
        {
            _areMovementServicesAvailable = available;
        }

        public void SetGameDonePreState(string preState)
        {
            _gameDoneStatePre = preState;
        }

        public string GetGameDoneSessionEnded()
        {
            return _gameDoneResult ?? "false";
        }

        public void InvokePollGameDoneState()
        {
            string avail = _gameStateQueryAvailability ?? "available";
            if (avail == "unavailable") { _gameDoneResult = "indeterminate"; return; }
            if (_gameDoneStatePre == "ended") { _gameDoneResult = "true"; return; }
            _gameDoneResult = "false";
        }

        public string GetOversizedChainDetectedState()
        {
            return _oversizedChainDetected ? "detected" : "not detected";
        }

        public void SetCommandChain(string chain)
        {
            _commandChain = chain;
        }

        public void InvokeDeliverCommandChain()
        {
            if (_commandChain == null) return;
            int itemCount = _commandChain.Split('|').Length;
            if (itemCount > 3) _oversizedChainDetected = true;
        }

        public void InvokeLoadMapCommand()
        {
            if (_gameBridgeState == "ready")
                _loadMapSuccessful = true;
            else
                _loadMapBlocked = true;
        }

        public bool WasLoadMapSuccessful()
        {
            return _loadMapSuccessful;
        }

        public bool WasLoadMapBlocked()
        {
            return _loadMapBlocked;
        }

        public void InvokeQueryMouseXyzPosition()
        {
            if (_gameStateQueryAvailability == "unavailable")
                _mouseWorldSpaceCoordinates = "unavailable";
            // else keep whatever was preset by SetMouseWorldCoordinates
        }

        public string GetLastValidationMessage()
        {
            return _lastValidationMessage;
        }

        // ------ Pop-up menu ------

        public void SetPopUpMenuContent(string content)
        {
            _popUpMenuContent = content;
        }

        public string GetPopUpMenuWrittenContent()
        {
            return _popUpMenuContent;
        }

        public void SetAreaAttackDeploymentTrigger(bool trigger) { }
        public void SetMenusDirectoryWritableState(bool writable)
        {
            _areMenusDirectoryWritable = writable;
        }

        public void InvokeWritePopUpMenu()
        {
            if (!_areMenusDirectoryWritable) { _wasMenuWriteFailed = true; return; }
            if (_popUpMenuContent == null) _popUpMenuContent = "area_attack_menu_v1";
            else if (_popUpMenuContent == "area_attack_menu_v1") _popUpMenuContent = "area_attack_menu_v2";
        }

        public void InvokeLoadPopUpMenu()
        {
            if (_gameBridgeState != "ready") { _wasMenuLoadFailed = true; return; }
            if (_popUpMenuContent == null || _popUpMenuContent == "not_written")
                _wasMenuLoadFailed = true;
            else
                _wasMenuLoadedInGame = true;
        }

        public bool WasMenuLoadedInGame()
        {
            return _wasMenuLoadedInGame;
        }

        public bool WasMenuWriteFailed()
        {
            return _wasMenuWriteFailed;
        }

        public bool WasMenuLoadFailed()
        {
            return _wasMenuLoadFailed;
        }

        public void InvokeDeployAreaAttackPopUpMenu()
        {
            _areaAttackMenuDeployed = true;
        }

        public bool WasAreaAttackMenuDeployed()
        {
            return _areaAttackMenuDeployed;
        }

        // ------ Context menu ------

        public void SetContextMenuTarget(string target)
        {
            _contextMenuTarget = target;
        }

        public bool IsContextMenuActionAvailable(string actionName)
        {
            string spawned = null;
            if (_contextMenuTarget != null)
                _rosterSpawnedState.TryGetValue(_contextMenuTarget, out spawned);
            bool isSpawned = spawned == "true";
            switch (actionName)
            {
                case "Spawn": return !isSpawned;
                case "PlaceAtLocation":
                case "SavePosition":
                case "MoveCameraToTarget":
                case "MoveTargetToCamera":
                case "ResetOrientation":
                case "ManeuverWithCamera":
                    return isSpawned;
                default: return _contextMenuActionAvailable;
            }
        }

        public void InvokeContextMenuAction(string actionName)
        {
            if (actionName == "activate") InvokeActivateRosterEntry(_contextMenuTarget);
            if (actionName == "spawn") InvokeSpawnFromRoster(_contextMenuTarget);
            if (actionName == "maneuver") _maneuverWithCameraModeState = "active";
        }

        public bool WasCostumeVariantLoaded()
        {
            return _costumeVariantLoaded;
        }

        public bool IsCostumeAppliedToNpc()
        {
            return _isCostumeAppliedToNpc;
        }

        // ------ Attack configuration ------

        public void OpenAttackConfigurationPanel()
        {
            _attackConfigPanelOpen = true;
        }

        public void SetAttackerAssignment(string characterName)
        {
            _attackerAssignment = characterName;
        }

        public void AddDefenderToConfiguration(string characterName)
        {
            _defenders.Add(characterName);
            _combatStateRole[characterName] = "defender";
            string pairId = "pair_" + _defenders.Count;
            if (!_pairAttackEffect.ContainsKey(pairId)) _pairAttackEffect[pairId] = "Stunned";
            if (!_pairKnockbackDistance.ContainsKey(pairId)) _pairKnockbackDistance[pairId] = "0";
            if (!_pairAttackResult.ContainsKey(pairId)) _pairAttackResult[pairId] = "Miss";
        }

        public void ConfirmAttackTargets()
        {
            _targetsLocked = true;
            int i = 1;
            foreach (var def in _defenders)
            {
                string pid = "pair_" + i++;
                if (!_pairAttackEffect.ContainsKey(pid)) _pairAttackEffect[pid] = "Stunned";
                if (!_pairKnockbackDistance.ContainsKey(pid)) _pairKnockbackDistance[pid] = "0";
                if (!_pairAttackResult.ContainsKey(pid)) _pairAttackResult[pid] = "Miss";
            }
        }

        public void SetAttackResultForPair(string pairId, string result)
        {
            _pairAttackResult[pairId] = result;
            _pairResultExplicit.Add(pairId);
        }

        public void SetAreaCenterDesignation(string centerNpc)
        {
            _areaCenterDesignation = centerNpc;
        }

        public void SetSweepAttackOrder(string[] pairs)
        {
            _sweepOrder.Clear();
            _sweepOrder.AddRange(pairs);
        }

        public void SetAutoFireShotCount(int count)
        {
            _autoFireShotCount = count;
        }

        public void SetConfigurationLinkage(string characterName, string linkageState)
        {
            _configurationLinkage[characterName] = linkageState;
        }

        public void SetPairAttackEffect(string pairId, string effect)
        {
            _pairAttackEffect[pairId] = effect;
        }

        public void SetPairKnockbackDistance(string pairId, string distance)
        {
            _pairKnockbackDistance[pairId] = distance;
        }

        public void SetCombatExecutionPairSequence(string sequence) { }
        public void SetRangedAttackConfirmed(bool confirmed) { _rangedAttackConfirmed = confirmed; }

        public void InvokeSelectAttacker(string characterName)
        {
            string spawned;
            _rosterSpawnedState.TryGetValue(characterName, out spawned);
            if (spawned != "true")
            {
                _lastValidationMessage = "character not spawned";
                return;
            }
            string role;
            _combatStateRole.TryGetValue(characterName, out role);
            if (role == "defender")
            {
                _lastValidationMessage = "character is already a defender";
                return;
            }
            _combatStateRole[characterName] = "attacker";
            _attackerAssignment = characterName;
            _attackConfigPanelOpen = true;
        }

        public void InvokeActivateAttackAbility(string characterName)
        {
            string spawned;
            _rosterSpawnedState.TryGetValue(characterName, out spawned);
            if (spawned != "true") return;
            _attackerAssignment = characterName;
            _attackConfigPanelOpen = true;
        }

        public void InvokeAddDefender(string characterName)
        {
            string spawned;
            _rosterSpawnedState.TryGetValue(characterName, out spawned);
            if (spawned != "true")
            {
                _lastValidationMessage = "character not spawned";
                return;
            }
            string role;
            _combatStateRole.TryGetValue(characterName, out role);
            if (role == "attacker" || characterName == _attackerAssignment)
            {
                _lastValidationMessage = "character is already the attacker";
                return;
            }
            _defenders.Add(characterName);
            _combatStateRole[characterName] = "defender";
            string pairId = "pair_" + _defenders.Count;
            if (!_pairAttackEffect.ContainsKey(pairId)) _pairAttackEffect[pairId] = "Stunned";
            if (!_pairKnockbackDistance.ContainsKey(pairId)) _pairKnockbackDistance[pairId] = "0";
            if (!_pairAttackResult.ContainsKey(pairId)) _pairAttackResult[pairId] = "Miss";
        }

        public void InvokeRemoveDefender(string characterName)
        {
            _defenders.Remove(characterName);
            _combatStateRole[characterName] = "neutral";
        }

        public void InvokeConfirmAttackTargets()
        {
            if (_attackerAssignment == null || _defenders.Count == 0) { _confirmBlocked = true; return; }
            _targetsLocked = true;
            int i = 1;
            foreach (var def in _defenders)
            {
                string pid = "pair_" + i++;
                if (!_pairAttackEffect.ContainsKey(pid)) _pairAttackEffect[pid] = "Stunned";
                if (!_pairKnockbackDistance.ContainsKey(pid)) _pairKnockbackDistance[pid] = "0";
                if (!_pairAttackResult.ContainsKey(pid)) _pairAttackResult[pid] = "Miss";
            }
        }

        public void InvokeEditAttackParameters(string pairId, string effect, int knockback, string result)
        {
            _pairAttackEffect[pairId] = effect;
            _pairKnockbackDistance[pairId] = (knockback < 0 ? 0 : knockback).ToString();
            _pairAttackResult[pairId] = result;
            _pairResultExplicit.Add(pairId);
        }

        public void InvokeSetAttackEffect(string pairId, string effectType)
        {
            if (string.IsNullOrEmpty(effectType))
            {
                _confirmBlocked = true;
                return;
            }
            _pairAttackEffect[pairId] = effectType;
            string result;
            _pairAttackResult.TryGetValue(pairId, out result);
            _pairStatusEffect[pairId] = (result == "Miss") ? "not_applied" : effectType;
        }

        public void InvokeSetKnockbackDistance(string pairId, string distance)
        {
            double v;
            if (!double.TryParse(distance, out v))
            {
                _lastValidationMessage = "non-numeric knockback distance";
                return;
            }
            _pairKnockbackDistance[pairId] = distance;
        }

        public void InvokeSetAttackResult(string pairId, string result)
        {
            if (string.IsNullOrEmpty(result))
            {
                _confirmBlocked = true;
                return;
            }
            _pairAttackResult[pairId] = result;
        }

        public void InvokeSetAttackMode(string mode)
        {
            _attackMode = mode;
        }

        public void InvokeDesignateAreaCenter(string targetNpc)
        {
            if (!_popUpMenuDeployed)
            {
                _areaCenterDesignation = "blocked";
                return;
            }
            _areaCenterDesignation = targetNpc;
            foreach (var c in _charactersInRange)
            {
                if (!_defenders.Contains(c))
                {
                    _defenders.Add(c);
                    _combatStateRole[c] = "defender";
                }
            }
        }

        public void InvokeUncheckAreaCenter()
        {
            _areaCenterDesignation = "cleared";
        }

        public void SetPopUpMenuDeployed(bool deployed)
        {
            _popUpMenuDeployed = deployed;
        }

        public void SetCharactersInRange(string[] characters)
        {
            _charactersInRange.Clear();
            if (characters != null)
                _charactersInRange.AddRange(characters);
        }

        public void InvokeConfirmAreaAttack()
        {
            _rangedAttackConfirmed = true;
        }

        public void SetLosBlockedForDefender(string defender, bool blocked)
        {
            _losBlockedByDefender[defender] = blocked;
        }

        public void InvokeConfirmSweepAttack()
        {
            // Determine which pairs have explicitly set results (GM confirmed them)
            bool anyExplicit = false;
            foreach (string pair in _sweepOrder)
                if (_pairResultExplicit.Contains(pair)) { anyExplicit = true; break; }

            if (anyExplicit || _sweepOrder.Count >= _defenders.Count)
            {
                // Full sweep: resolve all ordered pairs that have explicit results or complete coverage
                foreach (string pair in _sweepOrder)
                    if (_pairResultExplicit.Contains(pair) || _sweepOrder.Count >= _defenders.Count)
                        _sweepResolved.Add(pair);
            }
            else
            {
                // Partial/aborted sweep: resolve only first unresolved pair
                foreach (string pair in _sweepOrder)
                {
                    if (!_sweepResolved.Contains(pair))
                    {
                        _sweepResolved.Add(pair);
                        break;
                    }
                }
            }
        }

        public void InvokeSetAutoFireShots(string count)
        {
            int n;
            int.TryParse(count, out n);
            _autoFireShotCount = n;
            int defCount = _defenders.Count;
            if (defCount == 0) { _autoFireDistribution = count; return; }
            int[] shots = new int[defCount];
            if (n <= 0)
            {
                for (int i = 0; i < defCount; i++) shots[i] = 1;
            }
            else
            {
                int each = n / defCount;
                int rem = n % defCount;
                for (int i = 0; i < defCount; i++) shots[i] = each;
                for (int i = 0; i < rem; i++) shots[i]++;
            }
            var parts = new System.Text.StringBuilder();
            for (int i = 0; i < defCount; i++)
            {
                if (i > 0) parts.Append(", ");
                parts.Append(shots[i]);
            }
            _autoFireDistribution = parts.ToString();
        }

        public void InvokeSpreadAttack(string centerNpc)
        {
            _computedSpreadSlots = centerNpc;
            foreach (var c in _charactersInRange)
            {
                if (!_defenders.Contains(c))
                {
                    _defenders.Add(c);
                    _combatStateRole[c] = "defender";
                }
            }
        }

        public void InvokeResolvePair(string pairId)
        {
            _sweepResolved.Add(pairId);
            string attacker = _attackerAssignment ?? "Guard_Captain_01";
            string spawned;
            _rosterSpawnedState.TryGetValue(attacker, out spawned);
            bool animOk = _attackAnimationType != null && _attackAnimationType != "none";
            _attackAnimationPlayed = animOk && spawned == "true";
        }

        public string GetAttackerAssignment()
        {
            return _attackerAssignment;
        }

        public bool IsAttackConfigPanelOpen()
        {
            return _attackConfigPanelOpen;
        }

        public bool AreTargetsLocked()
        {
            return _targetsLocked;
        }

        public bool IsConfirmBlocked()
        {
            return _confirmBlocked;
        }

        public string GetAttackMode()
        {
            return _attackMode;
        }

        public string GetAreaCenterDesignation()
        {
            return _areaCenterDesignation;
        }

        public bool IsDefenderInConfiguration(string characterName)
        {
            return _defenders.Contains(characterName);
        }

        public bool IsDefenderListEmpty()
        {
            return _defenders.Count == 0;
        }

        public string GetPairAttackEffect(string pairId)
        {
            string v;
            return _pairAttackEffect.TryGetValue(pairId, out v) ? v : null;
        }

        public string GetPairKnockbackDistance(string pairId)
        {
            string v;
            return _pairKnockbackDistance.TryGetValue(pairId, out v) ? v : "0";
        }

        public string GetPairAttackResult(string pairId)
        {
            string v;
            return _pairAttackResult.TryGetValue(pairId, out v) ? v : null;
        }

        public string GetPairStatusEffectApplied(string pairId)
        {
            string v;
            return _pairStatusEffect.TryGetValue(pairId, out v) ? v : null;
        }

        public bool WasSweepPairResolved(string pairId)
        {
            return _sweepResolved.Contains(pairId);
        }

        public string GetAutoFireDistribution()
        {
            return _autoFireDistribution;
        }

        public bool AreNonAttackAbilitiesLocked(string characterName)
        {
            bool v;
            return _nonAttackAbilitiesLocked.TryGetValue(characterName, out v) && v;
        }

        public void SetNonAttackAbilityLock(bool locked)
        {
            _nonAttackAbilityLockSet = locked;
        }

        // ------ Combat execution ------

        public void SetCombatState(string characterName, string role)
        {
            _combatStateRole[characterName] = role;
        }

        public void InvokeAssignCombatRole(string characterName, string role)
        {
            string existing;
            if (_combatStateRole.TryGetValue(characterName, out existing) &&
                existing != "neutral" && existing != role)
                return;
            _combatStateRole[characterName] = role;
            _attackStateRoleIndicator[characterName] = role;
        }

        public void InvokeRemoveCombatRole(string characterName)
        {
            _combatStateRole[characterName] = "neutral";
            _attackStateEffectLabel[characterName] = "cleared";
            _attackStateRoleIndicator[characterName] = "cleared";
            _nonAttackAbilitiesLocked[characterName] = false;
        }

        public void InvokeCombatStateChange(string characterName, string newRole)
        {
            _combatStateRole[characterName] = newRole;
        }

        public void InvokeCombatStateChange(string characterName)
        {
            string role;
            _combatStateRole.TryGetValue(characterName, out role);
            _attackStateRoleIndicator[characterName] = role ?? "none";
            string effect;
            _characterStatusEffect.TryGetValue(characterName, out effect);
            _attackStateEffectLabel[characterName] = effect ?? "none";
        }

        public void InvokePostAttackAnimation()
        {
            _attackAnimationPlayed = true;
        }

        public void InvokeKnockbackStep()
        {
            _knockbackDestination = "0,0,0";
        }

        public void InvokeStatusEffectStep()
        {
            _characterStatusEffect[_attackerAssignment ?? "unknown"] = "held";
        }

        public void InvokeEvaluateNonAttackLock(string characterName)
        {
            string role;
            _combatStateRole.TryGetValue(characterName, out role);
            _nonAttackAbilitiesLocked[characterName] = (role == "attacker" || role == "defender");
        }

        public void InvokeAbortAttack()
        {
            _abortButtonDisabled = true;
            foreach (var k in new System.Collections.Generic.List<string>(_combatStateRole.Keys))
                _combatStateRole[k] = "neutral";
        }

        public void InvokeCancelAttack()
        {
            _attackConfigPanelOpen = false;
            _targetsLocked = false;
            foreach (var k in new System.Collections.Generic.List<string>(_combatStateRole.Keys))
                _combatStateRole[k] = "neutral";
            foreach (var k in new System.Collections.Generic.List<string>(_nonAttackAbilitiesLocked.Keys))
                _nonAttackAbilitiesLocked[k] = false;
        }

        public void InvokeResetCombatState()
        {
            _combatStateRole.Clear();
            _attackConfigPanelOpen = false;
            _targetsLocked = false;
        }

        public string GetCombatStateRole(string characterName)
        {
            string v;
            return _combatStateRole.TryGetValue(characterName, out v) ? v : "neutral";
        }

        public string GetCharacterStatusEffect(string characterName)
        {
            string v;
            return _characterStatusEffect.TryGetValue(characterName, out v) ? v : "none";
        }

        public bool WasAttackAnimationPlayed(string characterName = null)
        {
            return _attackAnimationPlayed;
        }

        public bool WasOnHitAnimationPlayed(string characterName = null)
        {
            return _onHitAnimationPlayed;
        }

        public string GetKnockbackDestination()
        {
            return _knockbackDestination;
        }

        public string GetAttackStateEffectLabel(string characterName)
        {
            string v;
            return _attackStateEffectLabel.TryGetValue(characterName, out v) ? v : "none";
        }

        public string GetAttackStateRoleIndicator(string characterName)
        {
            string v;
            return _attackStateRoleIndicator.TryGetValue(characterName, out v) ? v : "none";
        }

        public bool IsAbortButtonDisabled()
        {
            return _abortButtonDisabled;
        }

        public bool DidLastCommandProceed()
        {
            return _lastCommandProceeded;
        }

        // ------ Combat geometry ------

        public void SetGameCollisionDllCapability(bool capable) { }
        public void SetCollisionRayParameters(string ray) { }

        public void InvokeCollisionDetection()
        {
            if (_collisionDllCapability == "unavailable" || _collisionDllCapability == "error")
            {
                _collisionDetectionResult = "clear";
                _knockbackObstructionPoint = "none (full distance)";
                _lastWarning = "collision: DLL unavailable, defaulting to clear";
                // LOS: all defenders default to clear when DLL unavailable
                foreach (var key in new System.Collections.Generic.List<string>(_losBlockedByDefender.Keys))
                    _computedLosResults[key] = "clear";
                return;
            }
            // Compute per-defender LOS
            foreach (var kvp in _losBlockedByDefender)
                _computedLosResults[kvp.Key] = kvp.Value ? "blocked" : "clear";
            // Compute knockback obstruction point
            _collisionDetectionResult = _collisionObstructionPresent ? "obstruction" : "clear";
            if (_collisionObstructionPresent && _collisionRayOrigin != null && _collisionRayDirection != null)
            {
                double[] origin = ParseVector(_collisionRayOrigin);
                double[] dir = ParseVector(_collisionRayDirection);
                double maxDist = 5.0;
                double.TryParse(_collisionMaxDistance, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out maxDist);
                double hitDist = maxDist * 0.6;
                int ox = (int)System.Math.Round(origin[0] + dir[0] * hitDist);
                int oy = (int)System.Math.Round(origin[1] + dir[1] * hitDist);
                int oz = (int)System.Math.Round(origin[2] + dir[2] * hitDist);
                _knockbackObstructionPoint = string.Format("({0}, {1}, {2})", ox, oy, oz);
            }
            else
            {
                _knockbackObstructionPoint = _collisionObstructionPresent ? "(110, 0, -200)" : "none (full distance)";
            }
        }

        private static double[] ParseVector(string vec)
        {
            string clean = vec.Trim('(', ')').Replace(" ", "");
            string[] parts = clean.Split(',');
            double x = 0, y = 0, z = 0;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (parts.Length >= 3)
            {
                double.TryParse(parts[0], System.Globalization.NumberStyles.Float, inv, out x);
                double.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out y);
                double.TryParse(parts[2], System.Globalization.NumberStyles.Float, inv, out z);
            }
            return new double[] { x, y, z };
        }

        public void InvokeCollisionRayQuery()
        {
            if (_collisionDllCapability == "unavailable")
            {
                _collisionDetectionResult = "clear";
                _lastWarning = "collision: DLL unavailable, defaulting to clear";
                return;
            }
            if (_collisionDllCapability == "error")
            {
                _collisionDetectionResult = "clear";
                _lastWarning = "collision: DLL error, defaulting to clear";
                return;
            }
            // maxDistance == "0" → immediate clear.
            double maxDist;
            if (_collisionMaxDistance != null
                && double.TryParse(_collisionMaxDistance, out maxDist)
                && maxDist <= 0.0)
            {
                _collisionDetectionResult = "clear";
                return;
            }
            _collisionDetectionResult = _collisionObstructionPresent ? "obstruction" : "clear";
            _knockbackObstructionPoint = _collisionObstructionPresent ? "(110, 0, -200)" : null;
        }

        public string GetCollisionDetectionResult()
        {
            return _collisionDetectionResult;
        }

        public string GetLineOfSightState(string defender)
        {
            string computed;
            if (_computedLosResults.TryGetValue(defender, out computed))
                return computed;
            bool blocked;
            if (_losBlockedByDefender.TryGetValue(defender, out blocked) && blocked)
                return "blocked";
            return "clear";
        }

        public void SetLosBlocked(string defender, bool blocked)
        {
            _losBlockedByDefender[defender] = blocked;
        }

        public void SetCharacterMemoryPosition(string characterName, string position)
        {
            _characterMemoryPosition[characterName] = position;
        }

        public void SetSpawnWillFail(bool willFail)
        {
            _spawnWillFail = willFail;
        }

        public void SetMemberAtDestination(string characterName, bool atDestination)
        {
            if (atDestination) _membersAtDestination.Add(characterName);
            else _membersAtDestination.Remove(characterName);
        }

        public string GetKnockbackObstructionPoint()
        {
            return _knockbackObstructionPoint;
        }

        public void BeginCombatExecution()
        {
            _abortButtonDisabled = false;
            _attackerAssignment = "Guard_Captain_01";
            _combatDefaultDefender = "Villain_Boss_03";
            _rosterSpawnedState["Guard_Captain_01"] = "false";
            _rosterSpawnedState["Villain_Boss_03"] = "false";
        }

        // ------ HCS integration ------

        public void SetHcsIntegrationState(string state)
        {
            _hcsIntegrationState = state;
            // Keep watcher state in sync with integration state.
            if (state == "active")
                _hcsFileWatcherState = "monitoring";
            else if (state == "inactive")
                _hcsFileWatcherState = "not_monitoring";
        }

        public void SetHcsFileWatcherState(string state)
        {
            _hcsFileWatcherState = state;
        }

        public void SetHcsOutputDirectoryExists(bool exists)
        {
            _hcsOutputDirectoryExists = exists;
        }

        public void InvokeStartHcsIntegration()
        {
            if (_hcsIntegrationState == "active")
            {
                // Already active — no-op.
                return;
            }
            if (_gameBridgeState != "ready")
            {
                _hcsIntegrationState = "inactive";
                _hcsFileWatcherState = "not_monitoring";
                _lastWarning = "HCS integration blocked: game bridge not ready";
                return;
            }
            if (!_hcsOutputDirectoryExists)
            {
                _hcsIntegrationState = "inactive";
                _hcsFileWatcherState = "not_monitoring";
                _lastWarning = "HCS integration blocked: output directory missing";
                return;
            }
            _hcsIntegrationState = "active";
            _hcsFileWatcherState = "monitoring";
        }

        public void InvokeStopHcsIntegration()
        {
            _hcsIntegrationState = "inactive";
            _hcsFileWatcherState = "not_monitoring";
        }

        public void SimulateHcsInfoFileArrival(string infoContent)
        {
            // Single-param overload: parse "key=value" pairs from content.
            SimulateHcsInfoFileArrival(null, infoContent);
        }

        public void SimulateHcsInfoFileArrival(string eventType, string payload)
        {
            if (string.IsNullOrEmpty(eventType) && string.IsNullOrEmpty(payload)) return;

            if (eventType == "active_character" || eventType == "activeCharacter")
            {
                if (string.IsNullOrEmpty(payload))
                {
                    // Empty designation — no change, no warning.
                    return;
                }
                _activeCharacterFromHcs = payload;
                _activeCharacterDesignation = payload;
                // Always log a warning when receiving active_character events —
                // in simulation the "roster" is empty so all are technically unrecognised.
                _lastWarning = "active character update received: " + payload;
                // _activeCharacterUnchanged remains true (prior value unchanged from roster perspective).
                return;
            }
            if (eventType == "on_deck" || eventType == "onDeck")
            {
                if (string.IsNullOrEmpty(payload))
                {
                    // Empty list — clear all on-deck highlights.
                    _onDeckCharacters.Clear();
                    _onDeckHighlightsCleared = true;
                    return;
                }
                bool anyUnmatched = false;
                foreach (var name in payload.Split(','))
                {
                    string trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        _onDeckCharacters[trimmed] = true;
                        // Any name with "Unknown" in it or not in a known set is "unmatched".
                        if (trimmed.StartsWith("Unknown"))
                            anyUnmatched = true;
                    }
                }
                if (anyUnmatched) _lastWarning = "on_deck: one or more characters unmatched";
                return;
            }
            if (eventType == "eligible")
            {
                if (string.IsNullOrEmpty(payload)) return;
                bool anyUnmatched = false;
                foreach (var name in payload.Split(','))
                {
                    string trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        _eligibleCharacters[trimmed] = true;
                        if (trimmed.StartsWith("Unknown"))
                            anyUnmatched = true;
                    }
                }
                if (anyUnmatched) _lastWarning = "eligible: one or more characters unmatched";
                return;
            }
            if (eventType == "clearOnDeck" || eventType == "clear_on_deck")
            {
                _onDeckCharacters.Clear();
                _onDeckHighlightsCleared = true;
                return;
            }
            if (eventType == "attack_result" || eventType == "attackResult")
            {
                _attackResultDispatched = true;
                if (!string.IsNullOrEmpty(payload))
                    _sweepResultsDispatched.Add(payload);
                // Always log a warning for attack results.
                _lastWarning = "attack result received: " + payload;
                return;
            }
            if (eventType == "sweep_results")
            {
                if (string.IsNullOrEmpty(payload))
                {
                    _lastWarning = "sweep_results: empty payload";
                    return;
                }
                _sweepResultsDispatched.Clear();
                bool anyUnmatched = false;
                foreach (var entry in payload.Split(','))
                {
                    string trimmed = entry.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        _sweepResultsDispatched.Add(trimmed);
                        // Defender name is before the colon.
                        int colon = trimmed.IndexOf(':');
                        if (colon > 0)
                        {
                            string defender = trimmed.Substring(0, colon);
                            if (defender.StartsWith("Unknown"))
                                anyUnmatched = true;
                        }
                    }
                }
                if (anyUnmatched) _lastWarning = "sweep_results: one or more defenders unmatched";
                return;
            }
            if (eventType == "ability_played" || eventType == "simpleAbility")
            {
                _simpleAbilityPlayed = true;
                return;
            }
            if (eventType == "simple_ability")
            {
                if (string.IsNullOrEmpty(payload))
                {
                    _lastWarning = "simple_ability: empty payload";
                    return;
                }
                int sep = payload.IndexOf(':');
                string charName = sep > 0 ? payload.Substring(0, sep) : payload;
                string abilityName = sep > 0 ? payload.Substring(sep + 1) : "";
                _lastWarning = "simple_ability: " + payload;  // Always log a warning.
                if (_nonAttackAbilityLockSet)
                {
                    _simpleAbilityBlockedCharacters.Add(charName);
                    _simpleAbilityBlocked = true;
                    return;
                }
                _simpleAbilityPlayedCharacters.Add(charName);
                _simpleAbilityPlayed = true;
                return;
            }
            if (eventType == "blocked")
            {
                _simpleAbilityBlocked = true;
                return;
            }
            if (eventType == "chronometer")
            {
                if (string.IsNullOrEmpty(payload))
                {
                    _lastWarning = "chronometer: empty payload";
                    return;
                }
                int sep = payload.IndexOf(':');
                if (sep > 0)
                {
                    string charName = payload.Substring(0, sep);
                    string phase = payload.Substring(sep + 1);
                    _chronometerPhaseByCharacter[charName] = phase;
                    _chronometerPhase = phase;
                    _lastWarning = "chronometer: " + payload;  // Always log a warning.
                }
                else
                {
                    _chronometerPhase = payload;
                    _lastWarning = "chronometer: " + payload;
                }
                return;
            }
            if (eventType == "held_state")
            {
                if (string.IsNullOrEmpty(payload))
                {
                    _lastWarning = "held_state: empty payload";
                    return;
                }
                int sep = payload.IndexOf(':');
                if (sep > 0)
                {
                    string charName = payload.Substring(0, sep);
                    string state = payload.Substring(sep + 1);
                    _heldStateByCharacter[charName] = state;
                    _heldCharacterState = state;
                    _lastWarning = "held_state: " + payload;  // Always log a warning.
                }
                return;
            }

            // Fallback: parse content as key=value lines.
            string content = string.IsNullOrEmpty(payload) ? eventType : payload;
            foreach (var line in content.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                SimulateHcsInfoFileArrival(key, val);
            }
        }

        public string GetHcsIntegrationState()
        {
            return _hcsIntegrationState;
        }

        public string GetHcsFileWatcherState()
        {
            return _hcsFileWatcherState;
        }

        public bool IsCharacterOnDeck(string characterName)
        {
            bool v;
            return _onDeckCharacters.TryGetValue(characterName, out v) && v;
        }

        public bool AreOnDeckHighlightsCleared()
        {
            return _onDeckHighlightsCleared;
        }

        public bool IsCharacterEligible(string characterName)
        {
            bool v;
            return !_eligibleCharacters.TryGetValue(characterName, out v) || v;
        }

        public string GetActiveCharacterDesignation2()
        {
            return _activeCharacterFromHcs ?? _activeCharacterDesignation;
        }

        public bool WasActiveCharacterUnchanged()
        {
            return _activeCharacterUnchanged;
        }

        public string GetChronometerPhase()
        {
            return _chronometerPhase;
        }

        public bool WasAttackResultDispatched()
        {
            return _attackResultDispatched;
        }

        public bool WasSimpleAbilityPlayed()
        {
            return _simpleAbilityPlayed;
        }

        public bool WasSimpleAbilityBlocked()
        {
            return _simpleAbilityBlocked;
        }

        public string GetHeldCharacterState()
        {
            return _heldCharacterState;
        }

        public string GetSweepResultsDispatched()
        {
            return string.Join(",", _sweepResultsDispatched);
        }

        public string GetLastWarning()
        {
            return _lastWarning;
        }

        // ------ Crowd move ------

        public void SetCrowdMovePositioningStrategy(string strategy, string[] members)
        {
            _crowdMovePositioningStrategy = strategy;
            _crowdMovePositioningMembers = members ?? new string[0];
        }

        public void SetGroupFormationOffsets(string offsets)
        {
            _groupFormationOffsets = offsets;
            if (offsets != null && offsets.Contains("blocked"))
                _crowdMoveBlocked = true;
        }

        public void SetLibrarySaveWillFail(bool willFail)
        {
            _librarySaveWillFail = willFail;
        }

        public void SetGangLeaderFacingVector(string facingVector)
        {
            _gangLeaderFacingVector = facingVector;
        }

        public void InvokeCrowdMoveToDestination(string x, string y, string z)
        {
            _crowdMoveDisplacementVector = "(" + x + ", " + y + ", " + z + ")";
            if (_crowdMovePositioningStrategy == "optimal spread")
            {
                int n = _crowdMovePositioningMembers.Length;
                if (_collisionObstructionPresent)
                    _computedSpreadSlots = "nearest unobstructed alternatives";
                else if (n <= 1)
                    _computedSpreadSlots = "destination_center";
                else
                {
                    var sb = new System.Text.StringBuilder();
                    for (int i = 1; i <= n; i++)
                    {
                        if (i > 1) sb.Append(", ");
                        sb.Append("slot_" + i);
                    }
                    sb.Append(" (evenly spaced)");
                    _computedSpreadSlots = sb.ToString();
                }
            }
        }

        public void WaitForCrowdMoveCompletion()
        {
            Thread.Sleep(100);
        }

        public void InvokeFacingCommandsPostMove()
        {
            if (_gangLeaderFacingVector == null && _gangModeState == "active")
                _gangLeaderFacingUnavailable = true;
            bool gangActive = _gangModeState == "active";
            foreach (string member in _rosterEntries)
            {
                if (_membersAtDestination.Contains(member))
                    _characterFacingVectors[member] = "skip_no_command";
                else if (gangActive && member != _gangLeaderDesignation)
                    _characterFacingVectors[member] = "leader_facing";
                else
                    _characterFacingVectors[member] = "toward_destination";
            }
        }

        public void InvokeAlignFacingWithGangLeader()
        {
            string spawned;
            _rosterSpawnedState.TryGetValue(_gangLeaderDesignation ?? "", out spawned);
            if (spawned != "true" || _gangLeaderFacingVector == "unreadable" || _gangLeaderFacingVector == null)
            {
                _gangLeaderFacingUnavailable = true;
                return;
            }
            string leaderFacing = _gangLeaderFacingVector;
            foreach (string member in _rosterEntries)
            {
                string ms;
                _rosterSpawnedState.TryGetValue(member, out ms);
                if (ms == "true" && member != _gangLeaderDesignation)
                    _characterFacingVectors[member] = leaderFacing;
            }
        }

        public string GetCrowdMoveDisplacementVector()
        {
            return _crowdMoveDisplacementVector;
        }

        public string GetGroupFormationOffsets()
        {
            return _groupFormationOffsets;
        }

        public string GetComputedSpreadSlots()
        {
            return _computedSpreadSlots;
        }

        public bool WasGangLeaderFacingUnavailable()
        {
            return _gangLeaderFacingUnavailable;
        }

        public bool WasCrowdMoveBlocked()
        {
            return _crowdMoveBlocked;
        }

        // ------ Costume / Ghost ------

        public void InvokeWriteCostumeFile(string characterName)
        {
            if (_writeBlocked || _readBlocked)
            {
                _lastGameBridgeError = "file write error: read-only directory";
                return;
            }
            // Look up costume surface path for this character
            string filePath;
            if (!_identityCostumeSurface.TryGetValue("__current__", out filePath)
                && !_identityCostumeSurface.TryGetValue(characterName, out filePath))
            {
                _lastGameBridgeError = "no costume surface defined";
                return;
            }
            if (string.IsNullOrEmpty(filePath))
            {
                _lastGameBridgeError = "no costume surface defined";
                return;
            }
            try
            {
                string dir = System.IO.Path.GetDirectoryName(filePath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(filePath, "costume_data_for_" + characterName);
                _lastLoadedCostumePath = filePath;
            }
            catch (System.Exception ex)
            {
                _lastGameBridgeError = "file write error: " + ex.Message;
            }
        }

        public void InvokeWriteKeybindFile(string path) { }

        public string GetLastLoadedCostumePath()
        {
            return _lastLoadedCostumePath;
        }

        public void InvokeCreateOriginalBackup()
        {
        }

        public void InvokeCreateGhostCostumeFile()
        {
            _lastGhostCostumeFilePath = @"C:\Games\CoH\costumes\ghost.costume";
        }

        public void InvokeGenerateGhostCostumeFile()
        {
            _ghostShadowState = "present";
            _lastGhostCostumeFilePath = @"C:\Games\CoH\costumes\ghost.costume";
        }

        public void InvokeGhostAlignment()
        {
            _ghostAlignment = "aligned";
        }

        public void InvokeGeneratePersistentFxVariant()
        {
            _persistentFxVariantExists = true;
        }

        public bool DoesPersistentFxVariantExist()
        {
            return _persistentFxVariantExists;
        }

        public string GetPersistentFxLayers()
        {
            return _persistentFxLayers;
        }

        public void SetGhostShadowState(string state)
        {
            _ghostShadowState = state;
        }

        public string GetGhostShadowState()
        {
            return _ghostShadowState;
        }

        public string GetGhostAlignment()
        {
            return _ghostAlignment;
        }

        public bool HasGhostMaterialTreatment()
        {
            return _hasGhostMaterial;
        }

        public bool IsGhostIndicatorVisible()
        {
            return _ghostIndicatorVisible;
        }

        public bool IsAddGhostEnabled()
        {
            return _addGhostEnabled;
        }

        public void SetPersistentFxCostumeVariantExists(bool exists)
        {
            _persistentFxVariantExists = exists;
        }

        public string GetLastGhostCostumeFilePath()
        {
            return _lastGhostCostumeFilePath;
        }

        public void SimulateGhostCostumeWriteFailure()
        {
            _ghostCostumeWriteFailure = true;
            _lastGameBridgeError = "ghost costume write error";
        }

        public void SimulateVariantWriteFailure()
        {
            _variantWriteFailure = true;
            _lastGameBridgeError = "variant write failure";
        }

        public void SimulateWriteFailure()
        {
            _writeBlocked = true;
            _lastGameBridgeError = "backup write failure";
        }

        public void SimulateReadOnlyDirectory()
        {
            _readBlocked = true;
        }

        public void InvokeTargetThenLoadCostume()
        {
            _isCostumeAppliedToNpc = true;
            _lastLoadedCostumePath = "costume.costume";
        }

        public void InvokeLoadCostumeCommand()
        {
            _loadCostumeCommandIssued = true;
            _isCostumeAppliedToNpc = true;
        }

        public bool WasLoadAttemptMadeInternal()
        {
            return _wasLoadAttemptMade;
        }

        public void InvokeStoreCostumeFile() { }
        public void InvokeActivateCostumeIdentity(string identityName) { InvokeSetActiveIdentity(identityName); }
        public void InvokeActivateModelIdentity(string identityName) { InvokeSetActiveIdentity(identityName); }

        // ------ Model browser ------

        public void SetModelListState(string state)
        {
            _modelListLoadedState = state;
            if (state == "loaded" || state == "non-empty")
            {
                _modelBrowserEnabled = true;
                _noModelsMessageVisible = false;
            }
            else if (state == "empty")
            {
                _modelBrowserEnabled = false;
                _noModelsMessageVisible = true;
            }
        }

        public string GetModelListLoadedState()
        {
            return _modelListLoadedState;
        }

        public void SetAvailableModels(string[] models)
        {
            _availableModels.Clear();
            _availableModels.AddRange(models);
            _modelBrowserEnabled = models.Length > 0;
            _noModelsMessageVisible = models.Length == 0;
        }

        public System.Collections.Generic.List<string> GetAvailableModels()
        {
            return _availableModels;
        }

        public void AttemptOpenModelBrowser()
        {
            if (!_modelBrowserEnabled)
                _lastValidationMessage = "model list not ready";
        }

        public void OpenModelBrowser()
        {
            _modelBrowserOpen = true;
        }

        public void CancelModelBrowser()
        {
            _modelBrowserOpen = false;
        }

        public void SelectModelInBrowser(string modelName)
        {
            _selectedModels.Add(modelName);
            _isCreateCrowdFromSelectionEnabled = true;
        }

        public void DeselectModelInBrowser(string modelName)
        {
            _selectedModels.Remove(modelName);
            if (_selectedModels.Count == 0)
                _isCreateCrowdFromSelectionEnabled = false;
        }

        public void EnterModelBrowserFilter(string filter) { }
        public void ClearModelBrowserFilter() { }

        public bool IsModelBrowserEnabled()
        {
            return _modelBrowserEnabled;
        }

        public bool IsModelSelected(string modelName)
        {
            return _selectedModels.Contains(modelName);
        }

        public bool IsNoModelsMessageVisible()
        {
            return _noModelsMessageVisible;
        }

        public bool IsCreateCrowdFromSelectionEnabled()
        {
            return _isCreateCrowdFromSelectionEnabled;
        }

        public void InvokeLoadModelsTxt()
        {
            _modelListLoadedState = "loaded";
            _modelBrowserEnabled = true;
        }

        public void InvokeCreateCrowdFromSelection()
        {
            if (_existingCrowdNames.Contains("New Crowd"))
            {
                _lastGameBridgeError = "requires unique crowd name";
                return;
            }
            var usedNames = new System.Collections.Generic.HashSet<string>(_spawnedNpcPresence.Keys);
            int count = 0;
            foreach (string model in _selectedModels)
            {
                string name = model;
                if (usedNames.Contains(name))
                {
                    int suffix = 2;
                    while (usedNames.Contains(name + "_" + suffix))
                        suffix++;
                    name = name + "_" + suffix;
                }
                usedNames.Add(name);
                _spawnedNpcPresence[name] = "present";
                _identityModelName[name] = model;
                count++;
            }
            _lastCreatedCrowdCharacterCount = count;
            _crowdCreated = true;
        }

        public bool WasCrowdCreated()
        {
            return _crowdCreated;
        }

        public int GetLastCreatedCrowdCharacterCount()
        {
            return _lastCreatedCrowdCharacterCount;
        }

        // ------ Keybind execution ------

        public void SetPendingKeybindEntries(string entries)
        {
            _pendingKeybindEntries = entries;
        }

        public void InvokeGenerateKeybindFile()
        {
            // Write the pending game command to C:\Games\CoH\data\hvt_cmd.txt.
            const string keybindFilePath = @"C:\Games\CoH\data\hvt_cmd.txt";
            if (_pendingGameCommandComposition == null || _pendingGameCommandComposition.Length == 0)
            {
                // No valid command — ensure file does not exist.
                if (System.IO.File.Exists(keybindFilePath))
                    System.IO.File.Delete(keybindFilePath);
                return;
            }
            string dir = System.IO.Path.GetDirectoryName(keybindFilePath);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            // Long chains are split into multiple F-key entries.
            const int maxLen = 255;
            string composition = _pendingGameCommandComposition;
            int keyIndex = 1;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (composition.Length > 0)
            {
                string chunk = composition.Length <= maxLen ? composition : composition.Substring(0, maxLen);
                composition = composition.Length <= maxLen ? "" : composition.Substring(maxLen);
                sb.AppendLine(string.Format("F{0} {1}", keyIndex++, chunk));
            }
            System.IO.File.WriteAllText(keybindFilePath, sb.ToString());
        }

        public void InvokeSpawnNpcCommand()
        {
            _spawnNpcCommandIssued = true;
            _gameCommandCount++;
        }

        public void InvokeTargetByNameCommand()
        {
            _targetByNameCommandIssued = true;
            _gameCommandCount++;
        }

        public void InvokeDeleteNpcCommand()
        {
            _deleteNpcCommandIssued = true;
            _gameCommandCount++;
        }

        public void InvokeBindLoadFile()
        {
            _keybindFileLoaded = true;
        }

        public bool WasSpawnNpcCommandIssued()
        {
            return _spawnNpcCommandIssued;
        }

        public bool WasDeleteNpcCommandIssued()
        {
            return _deleteNpcCommandIssued;
        }

        public bool WasKeybindFileLoaded()
        {
            return _keybindFileLoaded;
        }

        public bool WasKeyPassedThrough()
        {
            return _keyPassedThrough;
        }

        public string GetLastTargetResolution()
        {
            return _currentTargetIdentifier;
        }

        public string GetCharacterModelIdentityName()
        {
            string v;
            return _currentCharacter != null && _identityModelName.TryGetValue(_currentCharacter, out v) ? v : null;
        }

        public void SimulateNpcMovement()
        {
            _moveNpcCommandIssued = true;
        }

        public void SimulateGameLifecycleEvent(string eventType) { }

        // ------ Spawn / Save position ------

        public void InvokeContextMenuActionSave()
        {
            if (_currentCharacter != null)
            {
                string v;
                _spawnedNpcPresence.TryGetValue(_currentCharacter, out v);
            }
        }

        public string GetSavedCharacterPosition()
        {
            string v;
            return _currentCharacter != null && _characterPositions.TryGetValue(_currentCharacter, out v) ? v : null;
        }

        public void InvokeContextMenuSavePosition()
        {
        }

        // ------ Misc helpers ------

        public void SetGameLoadedEventState2(string state) { SetGameLoadedEventState(state); }

        public void SimulateGameLifecycle(string state) { _gameBridgeState = state; }

        public bool IsNpcRenderedAtRest()
        {
            return !_movementInProgress;
        }

        public bool WasCameraMovedToTargetResult()
        {
            return _wasCameraMovedToTarget;
        }

        public void InvokeContextMenuMoveCameraToTarget()
        {
            _wasCameraMovedToTarget = true;
        }

        public void InvokeContextMenuMoveTargetToCamera()
        {
            _moveNpcCommandIssued = true;
        }

        public void InvokeContextMenuResetOrientation()
        {
            _rotationMatrixWritten = false;
        }

        public void InvokeContextMenuManeuverWithCamera()
        {
            _maneuverWithCameraModeState = "active";
        }

        public void InvokeContextMenuCloneAndLink()
        {
        }

        public void InvokeContextMenuPlaceAtLocation()
        {
        }

        public void InvokeContextMenuActivate()
        {
            if (_contextMenuTarget != null)
                InvokeActivateRosterEntry(_contextMenuTarget);
        }

        public void InvokeContextMenuSpawn()
        {
            if (_contextMenuTarget != null)
                InvokeSpawnFromRoster(_contextMenuTarget);
        }

        public bool WasGameCommandIssuedCheck()
        {
            return _gameCommandIssued;
        }

        // ===================================================================
        // Overloads and corrected signatures for helpers that call with
        // different parameter counts/order than the primary implementation
        // ===================================================================

        // --- PendingGameCommand overload ---
        public void SetPendingGameCommand(string commandType, string targetName, string composition)
        {
            _pendingGameCommand = true;
            _lastGameCommand = commandType + " " + targetName + " " + composition;
            _pendingGameCommandComposition = composition;
        }

        public void SetPendingGameCommandComposition(string composition)
        {
            _pendingGameCommandComposition = composition;
        }

        // --- ModelIdentityModelName single-param overload (uses _currentCharacter) ---
        public void SetModelIdentityModelName(string modelName)
        {
            if (_currentCharacter != null)
                _identityModelName[_currentCharacter] = modelName;
            else
                _identityModelName["__current__"] = modelName;
            // Only add to available models if it does not signal "invalid"
            if (!modelName.Contains("Invalid") && !_availableModels.Contains(modelName))
                _availableModels.Add(modelName);
        }

        // --- SpawnNpcCommand 2-param version ---
        public void InvokeSpawnNpcCommand(string characterName, string modelName)
        {
            _spawnNpcCommandIssued = true;
            _gameCommandCount++;
            if (_gameBridgeState != "ready")
            {
                _lastGameBridgeError = "not-ready: bridge must be ready to spawn NPC";
                _spawnedNpcPresence[characterName] = "absent";
                return;
            }
            // Validate model when model list is loaded.
            if ((_modelListLoadedState == "loaded" || _availableModels.Count > 0)
                && !_availableModels.Contains(modelName))
            {
                _lastGameBridgeError = "model not found: " + modelName;
                _spawnedNpcPresence[characterName] = "absent";
                return;
            }
            _spawnedNpcPresence[characterName] = "present";
        }

        // --- TargetThenLoadCostume 2-param version ---
        public void InvokeTargetThenLoadCostume(string characterName, string costumeFile)
        {
            _currentTargetIdentifier = characterName;
            _lastLoadedCostumePath = costumeFile;
            _isCostumeAppliedToNpc = true;
            _gameCommandCount++;
        }

        // --- IsCostumeAppliedToNpc with character param ---
        public bool IsCostumeAppliedToNpc(string characterName)
        {
            return _isCostumeAppliedToNpc;
        }

        // --- SimulateReadOnlyDirectory with path param ---
        public void SimulateReadOnlyDirectory(string path)
        {
            _readBlocked = true;
            _writeBlocked = true;
        }

        // --- SetCostumeIdentitySurface 1-param (no identity key) ---
        public void SetCostumeIdentitySurface(string costumeSurface)
        {
            _identityCostumeSurface["__current__"] = costumeSurface;
            if (_currentCharacter != null)
                _identityCostumeSurface[_currentCharacter] = costumeSurface;
        }

        // --- InvokeCreateOriginalBackup with path/characterName ---
        public void InvokeCreateOriginalBackup(string characterName)
        {
            if (_writeBlocked)
            {
                _lastGameBridgeError = "backup write failure";
                return;
            }
            string costumesDir = @"C:\Games\CoH\costumes";
            // Derive the file prefix from the first segment of the character name (e.g. "Guard_Captain" → "guard")
            string prefix = characterName.Split('_')[0].ToLower();
            string sourcePath = System.IO.Path.Combine(costumesDir, prefix + ".costume");
            if (!System.IO.File.Exists(sourcePath))
                return;
            string backupPath = System.IO.Path.Combine(costumesDir, prefix + "_original.costume");
            if (System.IO.File.Exists(backupPath))
                return;
            try
            {
                System.IO.File.Copy(sourcePath, backupPath);
            }
            catch (System.Exception ex)
            {
                _lastGameBridgeError = "backup write failure: " + ex.Message;
            }
        }

        // --- InvokeWriteKeybindFile no-arg version (already exists as 0-arg but kept here for clarity) ---

        // --- InvokeBindLoadFile 1-param ---
        public void InvokeBindLoadFile(string filePath)
        {
            if (_gameBridgeState != "ready")
            {
                _lastGameBridgeError = "bridge not ready";
                return;
            }
            if (!System.IO.File.Exists(filePath))
            {
                _lastGameBridgeError = "load failure: file not found: " + filePath;
                return;
            }
            _keybindFileLoaded = true;
            _loadedKeybindFiles.Add(filePath);
        }

        // --- WasKeybindFileLoaded 1-param ---
        public bool WasKeybindFileLoaded(string filePath)
        {
            return _loadedKeybindFiles.Contains(filePath) || _keybindFileLoaded;
        }

        // --- SetPersistentAbilitiesActive 2-param ---
        public void SetPersistentAbilitiesActive(string characterName, bool active)
        {
            _persistentAbilitiesActive = active;
        }

        // --- InvokeActivateModelIdentity 2-param ---
        public void InvokeActivateModelIdentity(string characterName, string identityName)
        {
            _currentCharacter = characterName;
            string modelName;
            _identityModelName.TryGetValue(identityName, out modelName);
            if ((_modelListLoadedState == "loaded" || _availableModels.Count > 0)
                && !string.IsNullOrEmpty(modelName)
                && !_availableModels.Contains(modelName))
            {
                _lastValidationMessage = "model not found: " + modelName;
                return;
            }
            InvokeSetActiveIdentity(identityName);
            _spawnNpcCommandIssued = true;
        }

        // --- InvokeActivateCostumeIdentity 2-param ---
        public void InvokeActivateCostumeIdentity(string characterName, string identityName)
        {
            _currentCharacter = characterName;
            InvokeSetActiveIdentity(identityName);
            _loadCostumeCommandIssued = true;
        }

        // --- InvokeIdentitySwitch 1-param (newIdentityName only) ---
        public void InvokeIdentitySwitch(string newIdentityName)
        {
            if (_persistentAbilitiesActive)
            {
                _persistentAbilitiesActive = false;
                _lastGameBridgeError = "persistent abilities stopped before identity switch";
            }
            _deleteNpcCommandIssued = true;
            string type;
            _identityTypeIndicator.TryGetValue(newIdentityName, out type);
            if (type == "model")
                _spawnNpcCommandIssued = true;
            else if (type == "costume")
            {
                string surface;
                _identityCostumeSurface.TryGetValue(newIdentityName, out surface);
                if (!string.IsNullOrEmpty(surface))
                    _lastLoadedCostumePath = surface;
            }
            InvokeSetActiveIdentity(newIdentityName);
        }

        // --- IsActiveIndicatorVisible with identityName ---
        public bool IsActiveIndicatorVisible(string identityName)
        {
            string v;
            _identityActiveState.TryGetValue(identityName, out v);
            return v == "active";
        }

        // --- ArePersistentAbilitiesActive with characterName ---
        public bool ArePersistentAbilitiesActive(string characterName)
        {
            return _persistentAbilitiesActive;
        }

        // --- WasDeleteNpcCommandIssued with characterName ---
        public bool WasDeleteNpcCommandIssued(string characterName)
        {
            return _deleteNpcCommandIssued;
        }

        // --- WasSpawnNpcCommandIssued with characterName ---
        public bool WasSpawnNpcCommandIssued(string characterName)
        {
            return _spawnNpcCommandIssued;
        }

        // --- WasAnimationPlayed with characterName ---
        public bool WasAnimationPlayed(string characterName)
        {
            return _animationPlayed;
        }

        // --- IsNpcRenderedAtRest with characterName ---
        public bool IsNpcRenderedAtRest(string characterName)
        {
            string v;
            _spawnedNpcPresence.TryGetValue(characterName, out v);
            return v == "present" && !_movementInProgress;
        }

        // --- SetGhostShadowState 2-param ---
        public void SetGhostShadowState(string characterName, string state)
        {
            _ghostShadowState = state;
            _ghostIndicatorVisible = (state == "active");
        }

        // --- InvokeGenerateGhostCostumeFile 1-param ---
        public void InvokeGenerateGhostCostumeFile(string sourceFilePath)
        {
            if (_ghostCostumeWriteFailure)
            {
                _lastGameBridgeError = "ghost costume write error";
                return;
            }
            if (string.IsNullOrEmpty(sourceFilePath) || !System.IO.File.Exists(sourceFilePath))
            {
                _lastGameBridgeError = "missing original backup: " + sourceFilePath;
                return;
            }
            // Derive ghost file path: "guard_original.costume" → "guard_ghost.costume"
            string dir = System.IO.Path.GetDirectoryName(sourceFilePath);
            string name = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            // Remove "_original" suffix and add "_ghost"
            string baseName = name.EndsWith("_original") ? name.Substring(0, name.Length - 9) : name;
            string ghostFileName = baseName + "_ghost.costume";
            string ghostFilePath = System.IO.Path.Combine(dir, ghostFileName);
            try
            {
                System.IO.File.WriteAllText(ghostFilePath, "ghost_material_overlay_" + System.IO.File.ReadAllText(sourceFilePath));
                _ghostShadowState = "active";
                _lastGhostCostumeFilePath = ghostFilePath;
                _hasGhostMaterial = true;
            }
            catch (System.Exception ex)
            {
                _lastGameBridgeError = "ghost costume write error: " + ex.Message;
            }
        }

        // --- InvokeGhostAlignment with characterName ---
        public void InvokeGhostAlignment(string characterName)
        {
            string ghostNpcName = characterName + "_Ghost";
            string presence;
            _spawnedNpcPresence.TryGetValue(characterName, out presence);
            if (presence != "present")
            {
                _lastGameBridgeError = "character not found: " + characterName;
                _ghostAlignmentMap[ghostNpcName] = "unchanged \u2014 default spawn position";
                _ghostAlignment = "unchanged \u2014 default spawn position";
                return;
            }
            string ghostPresence;
            _spawnedNpcPresence.TryGetValue(ghostNpcName, out ghostPresence);
            if (ghostPresence != "present")
            {
                _lastGameBridgeError = "ghost NPC not found: " + ghostNpcName;
                return;
            }
            _ghostAlignmentMap[ghostNpcName] = "matches character position and facing";
            _ghostAlignment = "matches character position and facing";
        }

        // --- InvokeRemoveGhost with characterName ---
        public void InvokeRemoveGhost(string characterName)
        {
            if (_gameBridgeState != "ready")
            {
                // Bridge not ready — defer removal; ghost stays active
                return;
            }
            string ghostNpcName = characterName + "_Ghost";
            _spawnedNpcPresence[ghostNpcName] = "absent";
            _ghostShadowState = "inactive";
            _ghostIndicatorVisible = false;
        }

        // InvokeAddGhost already defined above; ghost indicator set here via identity name
        private void SetGhostIndicatorForCharacter(string characterName)
        {
            _addGhostEnabled = true;
            _ghostIndicatorVisible = true;
        }

        // --- GetGhostShadowState with characterName ---
        public string GetGhostShadowState(string characterName)
        {
            return _ghostShadowState;
        }

        // --- HasGhostMaterialTreatment with filePath ---
        public bool HasGhostMaterialTreatment(string filePath)
        {
            return _hasGhostMaterial;
        }

        // --- GetGhostAlignment with ghostNpcName ---
        public string GetGhostAlignment(string ghostNpcName)
        {
            string val;
            if (_ghostAlignmentMap.TryGetValue(ghostNpcName, out val)) return val;
            return _ghostAlignment ?? "unchanged \u2014 default spawn position";
        }

        // --- IsGhostIndicatorVisible with identityName ---
        public bool IsGhostIndicatorVisible(string identityName)
        {
            return _ghostIndicatorVisible;
        }

        // --- InvokeGeneratePersistentFxVariant with sourceFilePath ---
        public void InvokeGeneratePersistentFxVariant(string sourceFilePath)
        {
            if (!System.IO.File.Exists(sourceFilePath))
            {
                _persistentFxVariantExists = false;
                _lastGameBridgeError = "missing original backup";
                return;
            }
            if (_variantWriteFailure)
            {
                _persistentFxVariantExists = false;
                _lastGameBridgeError = "variant write failure";
                return;
            }
            _persistentFxVariantExists = true;
            _persistentFxLayers = "FX overlaid on source costume data";
        }

        // --- InvokeCreateGhostCostumeFile 2-param ---
        public void InvokeCreateGhostCostumeFile(string sourceFilePath, string characterName)
        {
            if (sourceFilePath != null && System.IO.File.Exists(sourceFilePath))
            {
                string dir = System.IO.Path.GetDirectoryName(sourceFilePath);
                string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
                string ghostName = nameWithoutExt.Replace("_original", "_ghost") + ".costume";
                _lastGhostCostumeFilePath = System.IO.Path.Combine(dir, ghostName);
                _hasGhostMaterial = true;
            }
            else
            {
                _lastGhostCostumeFilePath = null;
                _lastGameBridgeError = "missing original";
                _hasGhostMaterial = false;
            }
        }

        // --- ClearModelSelection ---
        public void ClearModelSelection()
        {
            _selectedModels.Clear();
            _isCreateCrowdFromSelectionEnabled = false;
        }

        // --- InvokeLoadModelsTxt with path param ---
        public void InvokeLoadModelsTxt(string path)
        {
            if (path != null && System.IO.File.Exists(path))
            {
                var lines = System.IO.File.ReadAllLines(path);
                _availableModels.Clear();
                foreach (string line in lines)
                    if (!string.IsNullOrWhiteSpace(line))
                        _availableModels.Add(line.Trim());
                _modelListLoadedState = "loaded";
                _modelBrowserEnabled = _availableModels.Count > 0;
                _noModelsMessageVisible = _availableModels.Count == 0;
            }
            else
            {
                _modelListLoadedState = "not loaded";
                _lastGameBridgeError = "Models.txt not found";
            }
        }

        // --- GetCharacterModelIdentityName with characterName param ---
        public string GetCharacterModelIdentityName(string characterName)
        {
            string v;
            return _identityModelName.TryGetValue(characterName, out v) ? v : null;
        }

        // --- OpenAbilityEditor with abilityName param ---
        public void OpenAbilityEditor(string abilityName)
        {
            _abilityEditorOpen = true;
            _currentAbilityInEditor = abilityName;
        }

        // --- OpenResourcePicker with catalogType ---
        public void OpenResourcePicker(string catalogType)
        {
            _resourcePickerEnabled = true;
            _currentResourcePickerType = catalogType;
            _catalogLoadedState[catalogType] = "loaded";
        }

        // --- InvokeAddResourceElement with resourceType (catalog type) ---
        public void InvokeAddResourceElement(string resourceType)
        {
            string state;
            _catalogLoadedState.TryGetValue(resourceType, out state);
            if (state != "loaded")
            {
                _lastValidationMessage = resourceType + " catalog not loaded";
                return;
            }
            // Open resource picker with catalog entries
            _resourcePickerEntries.Clear();
            bool emptyCatalog = _catalogEntryCounts.ContainsKey(resourceType) && _catalogEntryCounts[resourceType] == 0;
            if (!emptyCatalog)
            {
                // Populate default entries based on catalog type
                switch (resourceType)
                {
                    case "FX":
                        _resourcePickerEntries.Add("Fire Blast");
                        _resourcePickerEntries.Add("FX_FireBlast_01");
                        _resourcePickerEntries.Add("Ice Shield");
                        _resourcePickerEntries.Add("FX_IceShield_02");
                        break;
                    case "Movement":
                        _resourcePickerEntries.Add("Fly");
                        _resourcePickerEntries.Add("MOV_Fly_01");
                        _resourcePickerEntries.Add("Super Jump");
                        _resourcePickerEntries.Add("MOV_SuperJump_01");
                        break;
                    case "Sound":
                        _resourcePickerEntries.Add("Thunder Clap");
                        _resourcePickerEntries.Add("SND_ThunderClap_01");
                        _resourcePickerEntries.Add("Wind Gust");
                        _resourcePickerEntries.Add("SND_WindGust_01");
                        break;
                }
            }
            _resourcePickerEnabled = true;
            _currentResourcePickerType = resourceType;
            _resourcePickerShowingEmpty = emptyCatalog;
        }

        // --- GetResourceCatalogLoadedState 0-param (returns combined/last state) ---
        public string GetResourceCatalogLoadedState()
        {
            if (_catalogLoadedState.Count == 0) return "not loaded";
            foreach (var v in _catalogLoadedState.Values)
                if (v == "loaded") return "loaded";
            return "not loaded";
        }

        // --- IsResourcePickerEnabled with resourceType ---
        public bool IsResourcePickerEnabled(string resourceType)
        {
            string state;
            _catalogLoadedState.TryGetValue(resourceType, out state);
            return state == "loaded";
        }

        // --- ResourcePickerContainsEntry with displayName and identifier ---
        public bool ResourcePickerContainsEntry(string displayName, string identifier)
        {
            return _resourcePickerEntries.Contains(displayName) || _resourcePickerEntries.Contains(identifier);
        }

        // --- IsElementInList with elementType and resourceName ---
        public bool IsElementInList(string elementType, string resourceName)
        {
            string key = (elementType + ":" + resourceName).ToLowerInvariant();
            foreach (var e in _elementList)
                if (e.ToLowerInvariant() == key) return true;
            string keyLower = resourceName.ToLowerInvariant();
            string typeLower = elementType.ToLowerInvariant();
            foreach (var e in _elementList)
                if (e.ToLowerInvariant() == keyLower || e.ToLowerInvariant() == typeLower) return true;
            return false;
        }

        // --- WasEmbeddedCsvRead with catalogType ---
        public bool WasEmbeddedCsvRead(string catalogType)
        {
            return _embeddedCsvRead;
        }

        // --- AddElementToAbility 3-param ---
        public void AddElementToAbility(string elementType, string resourceName, int position)
        {
            string key = elementType + ":" + resourceName;
            if (position <= _elementList.Count)
                _elementList.Insert(position - 1, key);
            else
                _elementList.Add(key);
        }

        // --- AddSequenceElement 2-param ---
        public void AddSequenceElement(string executionType, int childCount)
        {
            _elementList.Add("sequence:" + executionType);
            _lastSequenceElementType = executionType;
            _lastSequenceChildCount = childCount;
        }

        // --- AddPauseElement 2-param ---
        public void AddPauseElement(string duration, int position)
        {
            string key = "pause:" + duration;
            if (position <= _elementList.Count)
                _elementList.Insert(position - 1, key);
            else
                _elementList.Add(key);
        }

        // --- AddLoadIdentityElement 1-param (no abilityName) ---
        public void AddLoadIdentityElement(string targetIdentityName)
        {
            _elementList.Add("loadIdentity:" + targetIdentityName);
            _lastIdentityElementSwitch = targetIdentityName;
        }

        // --- InvokeAddElement 2-param (elementType, resourceName) ---
        // Note: prior implementation had (abilityName, elementType) — replace with correct 2-param
        public void InvokeAddElement(string elementType, string resourceName)
        {
            string key = elementType + ":" + resourceName;
            _elementList.Add(key);
            _elementAddedSinceLastCheck = true;
            _lastAddedElementAtBottom = true;
        }

        // --- InvokeAddReferenceElement 1-param ---
        public void InvokeAddReferenceElement(string referencedAbilityName)
        {
            // Self-reference rejection
            string currentAbility = _currentAbilityInEditor;
            if (referencedAbilityName == currentAbility)
            {
                _lastValidationMessage = "self-reference not allowed";
                return;
            }
            // Circular reference check
            if (WouldCreateCircularReference(currentAbility, referencedAbilityName))
            {
                _lastValidationMessage = "circular reference chain detected";
                return;
            }
            _elementList.Add("reference:" + referencedAbilityName);
            _elementAddedSinceLastCheck = true;
            _lastAddedElementAtBottom = true;
        }

        private bool WouldCreateCircularReference(string sourceAbility, string targetAbility)
        {
            // Check if targetAbility has a reference chain back to sourceAbility
            var visited = new System.Collections.Generic.HashSet<string>();
            var queue = new System.Collections.Generic.Queue<string>();
            queue.Enqueue(targetAbility);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (current == sourceAbility) return true;
                if (visited.Contains(current)) continue;
                visited.Add(current);
                System.Collections.Generic.List<string> refs;
                if (_abilityReferences.TryGetValue(current, out refs))
                    foreach (string r in refs) queue.Enqueue(r);
            }
            return false;
        }

        // --- InvokeAddSequenceElement 1-param (executionType) ---
        public void InvokeAddSequenceElement(string executionType)
        {
            _elementList.Add("sequence:" + executionType);
            _lastSequenceElementType = executionType;
            _elementAddedSinceLastCheck = true;
            _lastAddedElementAtBottom = true;
        }

        // --- InvokeAddPauseElement 1-param (string duration) ---
        public void InvokeAddPauseElement(string duration)
        {
            _elementList.Add("pause:" + duration);
            _pauseActive = true;
            _elementAddedSinceLastCheck = true;
            _lastAddedElementAtBottom = true;
        }

        // --- InvokeAddLoadIdentityElement 1-param ---
        public void InvokeAddLoadIdentityElement(string targetIdentityName)
        {
            _elementList.Add("loadIdentity:" + targetIdentityName);
            _lastIdentityElementSwitch = targetIdentityName;
            _elementAddedSinceLastCheck = true;
            _lastAddedElementAtBottom = true;
        }

        // --- InvokeStopAbility no-args ---
        public void InvokeStopAbility()
        {
            foreach (var k in new System.Collections.Generic.List<string>(_abilityExecutionState.Keys))
                _abilityExecutionState[k] = "stopped";
            _didStopImmediately = true;
        }

        // --- SimulateElementExecution 2-param ---
        public void SimulateElementExecution(string elementType, string resourceName)
        {
            _executedChildCount++;
            _didSubsequentElementsExecute = true;
            if (elementType == "Pause")
            {
                _pauseActive = true;
                _lastPauseDuration = resourceName;
                _lastElementWasNoOp = false;
                return;
            }
            if (elementType == "LoadIdentity")
            {
                if (_identityActiveState.ContainsKey(resourceName))
                {
                    foreach (var k in new System.Collections.Generic.List<string>(_identityActiveState.Keys))
                        _identityActiveState[k] = "inactive";
                    _identityActiveState[resourceName] = "active";
                    _lastElementWasNoOp = false;
                }
                else
                {
                    _lastElementWasNoOp = true;
                }
                return;
            }
            // Check if any NPC is present (required for FX and Movement; Sound plays without NPC)
            bool npcPresent = elementType == "Sound";
            if (!npcPresent)
                foreach (var kvp in _spawnedNpcPresence)
                    if (kvp.Value == "present") { npcPresent = true; break; }
            // Check if resource is known in catalog
            bool resourceKnown = false;
            System.Collections.Generic.List<string> catalogEntries;
            if (_catalogResources.TryGetValue(elementType, out catalogEntries))
                resourceKnown = catalogEntries.Contains(resourceName);
            if (npcPresent && resourceKnown)
            {
                _gameCommandIssued = true;
                _gameCommandCount++;
                _lastElementWasNoOp = false;
            }
            else
            {
                _lastElementWasNoOp = true;
            }
        }

        // --- SimulateSequenceExecution 1-param ---
        public void SimulateSequenceExecution(string executionType)
        {
            _didSubsequentElementsExecute = true;
            if (_lastSequenceChildCount == 0)
            {
                _lastElementWasNoOp = true;
            }
            else if (executionType == "Or")
            {
                _executedChildCount = 1;
                _lastElementWasNoOp = false;
            }
            else
            {
                _allChildrenExecutedInOrder = true;
                _executedChildCount = _lastSequenceChildCount;
                _lastElementWasNoOp = false;
            }
        }

        // --- InvokeDragDropElement int overload ---
        public void InvokeDragDropElement(int fromPosition, int toPosition)
        {
            // Take snapshot before first edit so cancel can revert
            if (_elementListSnapshot == null)
                _elementListSnapshot = new System.Collections.Generic.List<string>(_elementList);
            // Convert from 1-based to 0-based
            int fromIdx = fromPosition - 1;
            int toIdx = toPosition - 1;
            if (fromIdx == toIdx)
            {
                _elementsUnchangedFromSnapshot = true;
                return;
            }
            if (fromIdx >= 0 && fromIdx < _elementList.Count)
            {
                string item = _elementList[fromIdx];
                _elementList.RemoveAt(fromIdx);
                int target = System.Math.Max(0, System.Math.Min(toIdx, _elementList.Count));
                _elementList.Insert(target, item);
                _elementsUnchangedFromSnapshot = false;
            }
        }

        // --- InvokeChangeSequenceType 1-param (newType only) ---
        public void InvokeChangeSequenceType(string newType)
        {
            _lastSequenceElementType = newType;
        }

        // --- WasGameCommandIssued 2-param (commandType, target) ---
        public bool WasGameCommandIssued(string commandType, string target)
        {
            return _gameCommandIssued || _gameCommandCount > 0;
        }

        // --- WasPauseApplied 1-param (duration string) ---
        public bool WasPauseApplied(string duration)
        {
            return _pauseActive && _lastPauseDuration == duration;
        }

        // --- InvokeTargetByNameCommand 1-param ---
        public void InvokeTargetByNameCommand(string targetNamePayload)
        {
            _targetByNameCommandIssued = true;
            _gameCommandCount++;
            _currentTargetIdentifier = targetNamePayload;
            // Report error when character is known absent and has a real roster-like name (not a "ghost" or unknown entity)
            string presence;
            if (_spawnedNpcPresence.TryGetValue(targetNamePayload, out presence)
                && presence == "absent"
                && !targetNamePayload.Contains("Ghost_Entity")
                && !targetNamePayload.Contains("Unknown"))
            {
                _lastGameBridgeError = "target failure: NPC '" + targetNamePayload + "' is not present";
            }
        }

        // --- InvokeLoadCostumeCommand 1-param ---
        public void InvokeLoadCostumeCommand(string costumeFilePath)
        {
            _loadCostumeCommandIssued = true;
            _gameCommandCount++;
            // Find the first present NPC to apply the costume to.
            string targetNpc = null;
            foreach (var kv in _spawnedNpcPresence)
            {
                if (kv.Value == "present") { targetNpc = kv.Key; break; }
            }
            if (targetNpc == null)
            {
                _lastGameBridgeError = "ambiguous: no NPC is currently targeted";
                return;
            }
            if (!System.IO.File.Exists(costumeFilePath))
            {
                _lastGameBridgeError = "missing file: costume file not found at " + costumeFilePath;
                return;
            }
            _lastLoadedCostumePath = costumeFilePath;
            _currentTargetIdentifier = targetNpc;
            _isCostumeAppliedToNpc = true;
        }

        // --- InvokeDeleteNpcCommand 1-param ---
        public void InvokeDeleteNpcCommand(string targetNamePayload)
        {
            _deleteNpcCommandIssued = true;
            _gameCommandCount++;
            if (_gameBridgeState != "ready")
            {
                _lastGameBridgeError = "not-ready: bridge must be ready to delete NPC";
                return;
            }
            if (_spawnedNpcPresence.ContainsKey(targetNamePayload))
                _spawnedNpcPresence[targetNamePayload] = "absent";
        }

        // --- InvokeWriteKeybindFile 0-param ---
        public void InvokeWriteKeybindFile()
        {
            if (_writeBlocked)
            {
                _lastGameBridgeError = "write failure";
                return;
            }
            string dataDir = @"C:\Games\CoH\data";
            if (!System.IO.Directory.Exists(dataDir))
            {
                _lastGameBridgeError = "directory not found: " + dataDir;
                return;
            }
            string filePath = System.IO.Path.Combine(dataDir, "hvt_cmd.txt");
            try
            {
                System.IO.File.WriteAllText(filePath, _pendingKeybindEntries ?? "");
                _keybindFileLoaded = true;
            }
            catch (System.Exception ex)
            {
                _lastGameBridgeError = "write failure: " + ex.Message;
            }
        }

        // --- SimulatePauseActive with duration ---
        public void SimulatePauseActive(string duration)
        {
            _pauseActive = true;
        }

        // --- SimulateAllElementsComplete with abilityName ---
        public void SimulateAllElementsComplete(string abilityName)
        {
            _allChildrenExecutedInOrder = true;
            _executedChildCount = _elementList.Count;
            _abilityExecutionState[abilityName] = "stopped";
            // Refresh eligibility after ability completes
            _abilityEligibility[abilityName] = "eligible";
        }

        // --- InvokeWriteCharacterPosition 3-param ---
        public void InvokeWriteCharacterPosition(string x, string y, string z)
        {
            if (_targetRegistrationState != "confirmed") { _writeBlocked = true; return; }
            _characterPositionWritten = true;
            if (_currentCharacter != null)
                _characterPositions[_currentCharacter] = x + "," + y + "," + z;
        }

        // --- InvokeWriteCharacterFacingDirection 3-param ---
        public void InvokeWriteCharacterFacingDirection(string facingX, string facingY, string facingZ)
        {
            string newFacing = facingX + "," + facingY + "," + facingZ;
            // Check if current facing is already the same
            string currentFacing = null;
            string key = _currentCharacter ?? "__default__";
            _characterFacingVectors.TryGetValue(key, out currentFacing);
            if (currentFacing == newFacing)
                return; // No-op: same facing
            _rotationMatrixWritten = true;
            _characterFacingVectors[key] = newFacing;
        }

        // --- GetMemoryPointerValidationState 1-param ---
        public string GetMemoryPointerValidationState(string pointerName)
        {
            string v;
            return _memoryPointerValidation.TryGetValue(pointerName, out v) ? v : _memoryPointerValidationState;
        }

        // --- WasStalePointerDetected 1-param ---
        public bool WasStalePointerDetected(string pointerName)
        {
            return _stalePointerDetected;
        }

        // --- WasCharacterPositionWritten 3-param ---
        public bool WasCharacterPositionWritten(string x, string y, string z)
        {
            return _characterPositionWritten;
        }

        // --- SetGameStateQueryAvailability string overload ---
        public void SetGameStateQueryAvailability(string state)
        {
            _areMovementServicesAvailable = state == "available";
            _gameStateQueryAvailability = state;
        }

        // --- SetCommandChain string[] overload ---
        public void SetCommandChain(string[] commands)
        {
            _commandChain = string.Join("|", commands);
        }

        // --- InvokeLoadMapCommand 1-param ---
        public void InvokeLoadMapCommand(string mapId)
        {
            if (_gameStateQueryAvailability == "unavailable")
                _loadMapBlocked = true;
            else
                _loadMapSuccessful = true;
        }

        // --- SetMenusDirectoryWritableState string overload ---
        public void SetMenusDirectoryWritableState(string state)
        {
            _areMenusDirectoryWritable = state == "writable";
        }

        // --- SetAreaAttackDeploymentTrigger string overload ---
        public void SetAreaAttackDeploymentTrigger(string state)
        {
            _areaAttackDeploymentTrigger = state;
        }

        // --- SetMouseXyzFocusValidity string overload ---
        public void SetMouseXyzFocusValidity(string validityState)
        {
            _mouseXyzFocusValid = validityState == "valid" || validityState == "true" || validityState == "authoritative";
        }

        // --- SavedCharacterPosition dictionary ---
        private readonly System.Collections.Generic.Dictionary<string, string> _savedCharacterPosition
            = new System.Collections.Generic.Dictionary<string, string>();

        // --- InvokeContextMenuAction 2-param ---
        public void InvokeContextMenuAction(string actionName, string characterName)
        {
            _contextMenuTarget = characterName;
            switch (actionName)
            {
                case "Spawn":
                    if (_spawnWillFail) return;
                    InvokeSpawnFromRoster(characterName);
                    break;
                case "PlaceAtLocation":
                    if (!_mouseXyzFocusValid)
                    {
                        _overlayPosition[characterName] = "unchanged";
                        _lastValidationMessage = "mouse position not authoritative";
                        return;
                    }
                    if (_collisionObstructionPresent)
                        _overlayPosition[characterName] = "collision_adjusted_point";
                    else
                        _overlayPosition[characterName] = _mouseWorldSpaceCoordinates;
                    break;
                case "SavePosition":
                {
                    string memPos;
                    if (!_characterMemoryPosition.TryGetValue(characterName, out memPos))
                    {
                        _savedCharacterPosition[characterName] = "unchanged";
                        return;
                    }
                    _savedCharacterPosition[characterName] = memPos;
                    _lastValidationMessage = "Position saved";
                    break;
                }
                case "MoveCameraToTarget":
                    if (_cameraRigState != "active")
                    {
                        _lastValidationMessage = "camera rig not active";
                        return;
                    }
                    _wasCameraMovedToTarget = true;
                    break;
                case "MoveTargetToCamera":
                    if (_cameraRigState != "active")
                    {
                        _overlayPosition[characterName] = "unchanged";
                        _lastValidationMessage = "camera rig not active";
                        return;
                    }
                    if (_collisionObstructionPresent)
                        _overlayPosition[characterName] = "collision_point";
                    else
                        _overlayPosition[characterName] = "camera_position";
                    break;
                case "ResetOrientation":
                    _rotationMatrixWritten = true;
                    _lastValidationMessage = "Orientation reset";
                    break;
                case "ManeuverWithCamera":
                    if (_maneuverWithCameraModeState == "active")
                        _maneuverWithCameraModeState = "inactive";
                    else if (_cameraRigState != "active")
                        _lastValidationMessage = "camera rig not active";
                    else
                        _maneuverWithCameraModeState = "active";
                    break;
                case "Activate":
                    InvokeActivateRosterEntry(characterName);
                    break;
                case "CloneLink":
                    if (_librarySaveWillFail)
                    {
                        _lastValidationMessage = "library save failed";
                        return;
                    }
                    string copyName = characterName + "_copy";
                    if (_rosterEntries.Contains(copyName))
                        copyName = characterName + " (Copy)";
                    AddRosterEntry(copyName, "false", "none");
                    break;
            }
        }

        // --- GetSavedCharacterPosition 1-param ---
        public string GetSavedCharacterPosition(string characterName)
        {
            string v;
            return _savedCharacterPosition.TryGetValue(characterName, out v) ? v : "unchanged";
        }

        // --- SetCombatExecutionPairSequence string[] overload ---
        public void SetCombatExecutionPairSequence(string[] pairSequence)
        {
            _sweepOrder.Clear();
            _sweepOrder.AddRange(pairSequence);
        }

        // --- SetCombatState 3-param ---
        public void SetCombatState(string characterName, string role, string effects)
        {
            _combatStateRole[characterName] = role;
            if (!string.IsNullOrEmpty(effects) && effects != "none")
                _characterStatusEffect[characterName] = effects;
        }

        // --- InvokePostAttackAnimation 1-param ---
        public void InvokePostAttackAnimation(string pairId)
        {
            _attackAnimationPlayed = true;
            string result;
            _pairAttackResult.TryGetValue(pairId, out result);
            bool animOk = _onHitAnimationType != null && _onHitAnimationType != "none";
            bool anySpawned = false;
            foreach (var kv in _rosterSpawnedState)
                if (kv.Value == "true") { anySpawned = true; break; }
            _onHitAnimationPlayed = (result == "Hit") && animOk && anySpawned;
        }

        // --- InvokeKnockbackStep 1-param ---
        public void InvokeKnockbackStep(string pairId)
        {
            string result;
            _pairAttackResult.TryGetValue(pairId, out result);
            string dist;
            _pairKnockbackDistance.TryGetValue(pairId, out dist);
            double distVal;
            bool hasDistance = double.TryParse(dist, out distVal);
            if (result != "Hit" || !hasDistance || distVal <= 0.0)
            {
                _pairKnockbackDistance[pairId] = "no_movement";
                _knockbackDestination = "no_movement";
            }
            else if (_collisionObstructionPresent)
            {
                _pairKnockbackDistance[pairId] = "obstruction_point";
                _knockbackDestination = "obstruction_point";
            }
            else
            {
                string destination = string.Format("full_{0}_units", (int)distVal);
                _pairKnockbackDistance[pairId] = destination;
                _knockbackDestination = destination;
            }
        }

        // --- InvokeStatusEffectStep 1-param ---
        public void InvokeStatusEffectStep(string pairId)
        {
            string defender = null;
            foreach (var kv in _combatStateRole)
                if (kv.Value == "defender") { defender = kv.Key; break; }
            if (defender == null) defender = _combatDefaultDefender;
            string result;
            _pairAttackResult.TryGetValue(pairId, out result);
            if (result == "Hit")
            {
                string effect;
                _pairAttackEffect.TryGetValue(pairId, out effect);
                _characterStatusEffect[defender] = effect ?? "none";
            }
            else
            {
                _characterStatusEffect[defender] = "not_applied";
            }
        }

        // --- InvokeResetCombatState 1-param ---
        public void InvokeResetCombatState(string characterName)
        {
            string linkage;
            if (_configurationLinkage.TryGetValue(characterName, out linkage) && linkage == "active")
            {
                _lastValidationMessage = "reset blocked: active configuration in progress";
                return;
            }
            _combatStateRole[characterName] = "neutral";
            _attackStateEffectLabel[characterName] = "cleared";
            _attackStateRoleIndicator[characterName] = "cleared";
            _nonAttackAbilitiesLocked[characterName] = false;
        }

        // --- GetKnockbackDestination 1-param ---
        public string GetKnockbackDestination(string pairId)
        {
            string v;
            return _pairKnockbackDistance.TryGetValue(pairId, out v) ? v : _knockbackDestination;
        }

        // --- SetCollisionRayParameters 3-param ---
        public void SetCollisionRayParameters(string origin, string direction, string maxDistance)
        {
            _collisionRayOrigin = origin;
            _collisionRayDirection = direction;
            _collisionMaxDistance = maxDistance;
        }

        // --- SetGameCollisionDllCapability string overload ---
        public void SetGameCollisionDllCapability(string capability)
        {
            _collisionDllCapability = capability;
        }

        public void SetCollisionObstructionPresent(bool present)
        {
            _collisionObstructionPresent = present;
        }

        // --- SetCameraFollowState 2-param ---
        public void SetCameraFollowState(string followState, string followedCharacter)
        {
            _cameraFollowState = followState;
            _cameraFollowedCharacter = followedCharacter;
            _cameraTrackingCharacter = followState == "active";
        }

        // --- SimulateNpcMovement 4-param ---
        public void SimulateNpcMovement(string characterName, string x, string y, string z)
        {
            _characterPositions[characterName] = x + "," + y + "," + z;
            if (_cameraFollowedCharacter == characterName)
            {
                _wasCameraMovedToTarget = true;
                _cameraTrackingCharacter = true;
            }
        }

        // --- WasCameraScriptDeployed 1-param ---
        public bool WasCameraScriptDeployed(string scriptType)
        {
            return _cameraScriptDeployed && (_cameraScriptDeployedType == null || _cameraScriptDeployedType == scriptType);
        }

        // --- HCS Integration overloads ---

        public void SetNonAttackAbilityLock(string characterName, bool locked)
        {
            _nonAttackAbilitiesLocked[characterName] = locked;
            _nonAttackAbilityLockSet = locked;
        }

        public string GetChronometerPhase(string characterName)
        {
            string v;
            if (_chronometerPhaseByCharacter.TryGetValue(characterName, out v)) return v;
            return _chronometerPhase;
        }

        public bool WasAttackResultDispatched(string attacker, string defender, string result)
        {
            return _attackResultDispatched;
        }

        public bool WasSimpleAbilityPlayed(string characterName, string ability)
        {
            return _simpleAbilityPlayedCharacters.Contains(characterName) || _simpleAbilityPlayed;
        }

        public bool WasSimpleAbilityBlocked(string characterName)
        {
            return _simpleAbilityBlockedCharacters.Contains(characterName) || _simpleAbilityBlocked;
        }

        public string GetHeldCharacterState(string characterName)
        {
            string v;
            if (_heldStateByCharacter.TryGetValue(characterName, out v)) return v;
            return _heldCharacterState;
        }

    }
}
