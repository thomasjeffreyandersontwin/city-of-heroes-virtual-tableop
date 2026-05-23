using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.GameStateQuery
{
    [TestClass]
    public class CheckGameDoneState : GameStateQueryHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenRosterWithEntries();
        }

        [TestMethod]
        public void SessionActiveGameRunningReturnsFalse()
        {
            GivenGameStateQueryAvailable();
            WhenApplicationPollsGameDoneState();
            ThenGameDoneState("false");
        }

        [TestMethod]
        public void SessionEndedMapUnloadReturnsTrue()
        {
            GivenGameStateQueryAvailable();
            GivenGameSessionEnded();
            WhenApplicationPollsGameDoneState();
            ThenGameDoneState("true");
        }

        [TestMethod]
        public void GameDoneBlocksCommandsReturnsTrue()
        {
            GivenGameStateQueryAvailable();
            GivenGameDoneEventReceived();
            WhenApplicationPollsGameDoneState();
            ThenGameDoneState("true");
        }

        [TestMethod]
        public void NewSessionAfterGameDoneResetsFalse()
        {
            GivenGameStateQueryAvailable();
            GivenNewSessionStartedAfterGameDone();
            WhenApplicationPollsGameDoneState();
            ThenGameDoneState("false");
        }

        [TestMethod]
        public void GameBridgeUnreachableReturnsIndeterminate()
        {
            GivenGameStateQueryUnavailable();
            WhenApplicationPollsGameDoneState();
            ThenGameDoneState("indeterminate");
        }
    }
}
