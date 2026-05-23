using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameStateQuery
{
    [TestClass]
    public class QueryHoveredNpcInfoFromGame : GameStateQueryHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
        }

        [TestMethod]
        public void MouseOverVisibleNpcReturnsPresent()
        {
            GivenGameStateQueryAvailable();
            WhenMouseHoversOverEntity("Mouse over visible NPC");
            ThenHoveredNpcInfo("present", "Guard_Captain_01");
        }

        [TestMethod]
        public void MouseNotOverAnyNpcReturnsAbsent()
        {
            GivenGameStateQueryAvailable();
            WhenMouseHoversOverEntity("Mouse not over any NPC");
            ThenHoveredNpcInfo("absent", "empty");
        }

        [TestMethod]
        public void GameBridgeNotInitializedReturnsAbsent()
        {
            GivenGameStateQueryUnavailable();
            WhenMouseHoversOverEntity("Game bridge not initialized");
            ThenHoveredNpcInfo("absent", "empty");
        }

        [TestMethod]
        public void MouseMovesFromNpcToNpcDiscardsPrevious()
        {
            GivenGameStateQueryAvailable();
            WhenMouseHoversOverEntity("Mouse moves from NPC to NPC");
            ThenHoveredNpcInfo("present", "Villain_Boss_03");
        }

        [TestMethod]
        public void RapidSuccessiveQueriesReturnIndependently()
        {
            GivenGameStateQueryAvailable();
            WhenMouseHoversOverEntity("Rapid successive queries");
            ThenHoveredNpcInfo("present", "Guard_Captain_01");
        }
    }
}
