using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class StartHcsFileWatcherIntegration : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: application is running
        }

        [TestMethod]
        public void GameBridgeReadyStartSucceeds()
        {
            // Given: HCS Integration integration state inactive; Game Bridge ready; output directory exists
            _gameBridgeReady = true; _outputDirExists = true;
            // When: GM triggers Start HCS File Watcher Integration
            bool started = when_start_watcher();
            // Then: HCS Integration integration state active; HCS File Watcher monitoring state monitoring
            started.Should().BeTrue();
            then_watcher_monitoring();
            then_integration_active();
        }

        [TestMethod]
        public void GameBridgeNotInitializedBlocked()
        {
            // Given: HCS Integration integration state inactive; Game Bridge not initialized
            _gameBridgeReady = false;
            // When: GM triggers Start HCS File Watcher Integration
            bool started = when_start_watcher();
            // Then: HCS Integration integration state inactive; HCS File Watcher monitoring state not_monitoring
            started.Should().BeFalse(
                "Game Bridge not initialized — start must be blocked with feedback");
            then_watcher_not_monitoring();
        }

        [TestMethod]
        public void OutputDirectoryMissingBlocked()
        {
            // Given: HCS Integration integration state inactive; output directory does not exist
            _outputDirExists = false;
            // When: GM triggers Start HCS File Watcher Integration
            bool started = when_start_watcher();
            // Then: start blocked; HCS File Watcher monitoring state not_monitoring
            started.Should().BeFalse(
                "output directory missing — start must be blocked");
            then_watcher_not_monitoring();
        }

        [TestMethod]
        public void AlreadyActiveNoOp()
        {
            // Given: HCS Integration is already active
            given_watcher_active();
            // When: GM triggers Start HCS File Watcher Integration again
            bool started = when_start_watcher();
            // Then: no-op; HCS File Watcher monitoring state remains monitoring
            started.Should().BeTrue("already active — no-op; watcher remains in monitoring state");
            then_watcher_monitoring();
        }
    }
}
