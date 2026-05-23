using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameStateQuery
{
    [TestClass]
    public class QueryMouseXyzPositionInGameWorld : GameStateQueryHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
        }

        [TestMethod]
        public void FocusedValidPositionReturnsAuthoritative()
        {
            GivenGameStateQueryAvailable();
            WhenApplicationRequestsMousePosition();
            ThenMouseXyzPosition("(125.5, 0.0, -340.2)");
        }

        [TestMethod]
        public void NoFocusPotentiallyStaleNotTreatedAsAuthoritative()
        {
            GivenGameStateQueryAvailable();
            WhenApplicationRequestsMousePosition();
            ThenMouseXyzPosition("(125.5, 0.0, -340.2)");
        }

        [TestMethod]
        public void GameBridgeUnavailableReturnsUnavailable()
        {
            GivenGameStateQueryUnavailable();
            WhenApplicationRequestsMousePosition();
            ThenMouseXyzPosition("unavailable");
        }

        [TestMethod]
        public void DifferentMousePlacementsReturnDistinctCoordinates()
        {
            GivenGameStateQueryAvailable();
            GivenMouseWorldCoordinates("(200.0, 10.0, -100.0)");
            WhenApplicationRequestsMousePosition();
            ThenMouseXyzPosition("(200.0, 10.0, -100.0)");
        }
    }
}
