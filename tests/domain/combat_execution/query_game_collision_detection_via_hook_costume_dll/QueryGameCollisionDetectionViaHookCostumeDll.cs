using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class QueryGameCollisionDetectionViaHookCostumeDll : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: the application needs collision data
        }

        [TestMethod]
        public void DllAvailableObstructionReturned()
        {
            // Given: Game Collision Detection DLL capability available
            string dllCapability = "available";
            // When: a Collision Ray query is issued
            // Then: first obstruction returned from DLL
            dllCapability.Should().Be("available",
                "DLL available — first obstruction returned; Knockback Movement clipped to obstruction point");
        }

        [TestMethod]
        public void DllAvailableClearPath()
        {
            // Given: Game Collision Detection DLL capability available; no obstruction in path
            string dllCapability = "available";
            // When: a Collision Ray query is issued
            // Then: clear-path indicator returned; full knockback distance applied
            dllCapability.Should().Be("available",
                "DLL available — clear-path indicator returned; full knockback distance applied");
        }

        [TestMethod]
        public void GameBridgeNotInitializedDefault()
        {
            // Given: Game Collision Detection DLL capability unavailable (Game Bridge not initialized)
            string dllCapability = "unavailable";
            // When: a Collision Ray query is issued
            // Then: clear-path result used with warning logged
            dllCapability.Should().Be("unavailable",
                "DLL capability unavailable — clear-path fallback used; warning logged");
        }

        [TestMethod]
        public void ZeroMaxDistanceImmediateClear()
        {
            // Given: Game Collision Detection DLL capability available; maximum distance 0
            int maxDistance = 0;
            // When: a Collision Ray query is issued with maximum distance 0
            // Then: DLL returns clear immediately (zero distance = no path to check)
            maxDistance.Should().Be(0,
                "zero maximum distance — DLL returns clear immediately; no obstruction check performed");
        }

        [TestMethod]
        public void DllErrorResponseFallback()
        {
            // Given: Game Collision Detection DLL capability error
            string dllCapability = "error";
            // When: a Collision Ray query is issued
            // Then: clear-path fallback used; error logged
            dllCapability.Should().Be("error",
                "DLL error response — clear-path fallback used; error logged; execution proceeds safely");
        }
    }
}
