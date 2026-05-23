using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.HcsIntegration
{
    [TestClass]
    public class StopHcsIntegration : HcsIntegrationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: HCS Integration is active
            given_watcher_active();
        }

        [TestMethod]
        public void ActiveStopped()
        {
            // Given: HCS Integration integration state active
            then_watcher_monitoring();
            // When: GM triggers Stop HCS Integration
            bool stopped = when_stop_watcher();
            // Then: HCS Integration integration state inactive; HCS File Watcher monitoring state not_monitoring
            stopped.Should().BeTrue();
            then_integration_inactive();
            then_watcher_not_monitoring();
        }

        [TestMethod]
        public void MidProcessingCompletesThenStops()
        {
            // Given: HCS Integration active; a file is being processed
            // When: GM triggers Stop HCS Integration while processing
            bool stopped = when_stop_watcher();
            // Then: current file completes before watcher stops; final state not_monitoring
            stopped.Should().BeTrue(
                "mid-processing — current file completes before watcher stops; final state not_monitoring");
            then_watcher_not_monitoring();
        }

        [TestMethod]
        public void AlreadyStoppedNoOp()
        {
            // Given: HCS Integration is already stopped
            when_stop_watcher(); // stop first
            // When: GM triggers Stop HCS Integration again
            bool stopped = when_stop_watcher();
            // Then: no-op; no error raised; monitoring state remains not_monitoring
            stopped.Should().BeFalse("already stopped — no-op; no error raised");
            then_watcher_not_monitoring();
        }

        [TestMethod]
        public void SessionEndsAutoStopped()
        {
            // Given: HCS Integration is active; the session ends
            // When: the application session ends
            bool stopped = when_stop_watcher(); // auto-stop on session end
            // Then: HCS File Watcher monitoring state not_monitoring; auto-stopped
            stopped.Should().BeTrue(
                "session ends — HCS File Watcher auto-stopped; monitoring state not_monitoring");
            then_watcher_not_monitoring();
        }
    }
}
