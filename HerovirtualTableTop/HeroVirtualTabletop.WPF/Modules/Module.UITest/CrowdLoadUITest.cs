using System;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Module.UITest
{
    /// <summary>
    /// UI automation tests that verify crowd files load correctly in the running WPF app.
    /// Uses FlaUI (https://github.com/FlaUI/FlaUI) which wraps Windows UI Automation.
    ///
    /// Run order:
    ///   1. Build and start HeroVirtualDesktop.exe
    ///   2. Run these tests (agent 2 role: load and evaluate)
    /// </summary>
    [TestClass]
    public class CrowdLoadUITest
    {
        private const string AppExeName = "HeroVirtualDesktop";
        private const string ArmageddonFile = @"C:\hero-desktop\city-of-heroes-virtual-tabletop\data\crowds\Armageddons.data";
        private const int AppStartupTimeoutMs = 30000;
        private const int ActionDelayMs = 1500;

        private static Application _app;
        private static AutomationBase _automation;
        private static Window _mainWindow;

        [ClassInitialize]
        public static void LaunchApp(TestContext ctx)
        {
            _automation = new UIA3Automation();

            // Try to attach to already-running instance first
            var process = System.Diagnostics.Process
                .GetProcessesByName(AppExeName)
                .FirstOrDefault();

            if (process != null)
            {
                _app = Application.Attach(process);
            }
            else
            {
                string exePath = FindAppExe();
                Assert.IsNotNull(exePath, "Could not locate HeroVirtualDesktop.exe. Build the solution first.");
                _app = Application.Launch(exePath);
            }

            _mainWindow = _app.GetMainWindow(_automation, TimeSpan.FromMilliseconds(AppStartupTimeoutMs));
            Assert.IsNotNull(_mainWindow, "Main window did not appear within timeout.");

            WaitForLoadingToFinish();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _automation?.Dispose();
        }

        // ──────────────────────────────────────────────────────────────
        // Tests
        // ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void BrowseButton_IsVisible_InCharacterExplorer()
        {
            ExpandCharacterExplorer();
            var browseBtn = FindByAutomationId("btBrowse");
            Assert.IsNotNull(browseBtn, "Browse button (AutomationId=btBrowse) not found in Character Explorer.");
        }

        [TestMethod]
        public void LoadArmageddonsCrowd_ShowsThreeSubCrowds()
        {
            // Clear active crowds so we start clean
            ClearActiveCrowds();

            ExpandCharacterExplorer();

            // Click Browse and load Armageddons.data
            LoadCrowdFile(ArmageddonFile);

            // Allow the background loader to finish
            Thread.Sleep(ActionDelayMs * 2);

            // Find the "Armageddons" tree item in the character tree
            var armageddonNode = FindTreeItemByName("Armageddons");
            Assert.IsNotNull(armageddonNode,
                "Armageddons crowd node not found in tree after loading Armageddons.data");

            // Expand it so children are realised
            ExpandTreeItem(armageddonNode);
            Thread.Sleep(ActionDelayMs);

            // Verify the three sub-crowd children
            var children = GetTreeItemChildren(armageddonNode);
            var childNames = children.Select(c => c.Name).ToArray();

            Assert.IsTrue(childNames.Length >= 3,
                string.Format("Expected at least 3 sub-crowds under Armageddons, got {0}: [{1}]",
                    childNames.Length, string.Join(", ", childNames)));

            CollectionAssert.Contains(childNames, "Pre-Emptive Strike");
            CollectionAssert.Contains(childNames, "Spyder");
            CollectionAssert.Contains(childNames, "Suzerain");
        }

        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────

        private static void WaitForLoadingToFinish()
        {
            // Wait for the loading text element to disappear (max 30 s)
            var deadline = DateTime.UtcNow.AddMilliseconds(AppStartupTimeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var loadingText = FindByAutomationId("tbLoadingText");
                if (loadingText == null || !loadingText.IsOffscreen)
                    break;
                Thread.Sleep(500);
            }
        }

        private static void ExpandCharacterExplorer()
        {
            // The Character Explorer accordion/expander may be collapsed
            var expander = _mainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("CharacterExplorerExpander"));
            if (expander != null)
            {
                var toggle = expander.Patterns.Toggle.PatternOrDefault;
                if (toggle != null && toggle.ToggleState.Value == ToggleState.Off)
                    toggle.Toggle();
            }
            Thread.Sleep(ActionDelayMs);
        }

        private static void LoadCrowdFile(string filePath)
        {
            var browseBtn = FindByAutomationId("btBrowse");
            Assert.IsNotNull(browseBtn, "Browse button not found");
            browseBtn.AsButton().Invoke();
            Thread.Sleep(ActionDelayMs);

            // Find the open-file dialog (child window of the app process)
            var dialog = WaitForDialog(5000);
            Assert.IsNotNull(dialog, "Open File dialog did not appear after clicking Browse");

            // Type the file path into the filename box
            var fileNameBox = dialog.FindFirstDescendant(
                cf => cf.ByControlType(ControlType.Edit));
            if (fileNameBox != null)
            {
                fileNameBox.AsTextBox().Enter(filePath);
            }
            else
            {
                // Fallback: SendKeys to the dialog
                System.Windows.Forms.SendKeys.SendWait(filePath);
            }

            Thread.Sleep(500);
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            Thread.Sleep(ActionDelayMs);
        }

        private static Window WaitForDialog(int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var modalWindows = _app.GetAllTopLevelWindows(_automation);
                var dialog = modalWindows.FirstOrDefault(
                    w => !string.Equals(w.AutomationId, "MainWindowV2", StringComparison.Ordinal));
                if (dialog != null) return dialog;
                Thread.Sleep(300);
            }
            return null;
        }

        private static AutomationElement FindByAutomationId(string automationId)
        {
            return _mainWindow.FindFirstDescendant(
                cf => cf.ByAutomationId(automationId));
        }

        private static AutomationElement FindTreeItemByName(string name)
        {
            var tree = FindByAutomationId("tvCharacterExplorer");
            if (tree == null) return null;
            return tree.FindFirstDescendant(
                cf => cf.ByControlType(ControlType.TreeItem).And(cf.ByName(name)));
        }

        private static void ExpandTreeItem(AutomationElement treeItem)
        {
            var expand = treeItem.Patterns.ExpandCollapse.PatternOrDefault;
            if (expand != null && expand.ExpandCollapseState.Value == ExpandCollapseState.Collapsed)
                expand.Expand();
        }

        private static AutomationElement[] GetTreeItemChildren(AutomationElement treeItem)
        {
            return treeItem.FindAllChildren(
                cf => cf.ByControlType(ControlType.TreeItem));
        }

        private static void ClearActiveCrowds()
        {
            string activeCrowdsPath = @"C:\hero-desktop\city-of-heroes-virtual-tabletop\data\active-crowds.json";
            if (File.Exists(activeCrowdsPath))
                File.WriteAllText(activeCrowdsPath, "[]");
            // The app reads active-crowds.json on startup, not dynamically,
            // so this only matters for fresh launches.
        }

        private static string FindAppExe()
        {
            string[] candidates = new[]
            {
                @"C:\hero-desktop\city-of-heroes-virtual-tabletop\HerovirtualTableTop\HeroVirtualTabletop.WPF\HeroVirtualDesktop\bin\Debug\HeroVirtualDesktop.exe",
                @"C:\hero-desktop\city-of-heroes-virtual-tabletop\HerovirtualTableTop\HeroVirtualTabletop.WPF\HeroVirtualDesktop\bin\Release\HeroVirtualDesktop.exe",
            };
            return candidates.FirstOrDefault(File.Exists);
        }
    }
}
