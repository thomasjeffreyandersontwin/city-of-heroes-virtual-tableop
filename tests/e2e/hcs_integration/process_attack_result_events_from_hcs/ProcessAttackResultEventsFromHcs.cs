using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.HcsIntegration
{
    [TestClass]
    public class ProcessAttackResultEventsFromHcs : HcsIntegrationHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenApplicationRunning();
            GivenGameBridgeReady();
            GivenHcsFileWatcherActive();
        }

        [TestMethod]
        public void HitEventEffectsApplied()
        {
            WhenInfoFileArrives("attack_result", "Guard_A->Villain_B:Hit");
            ThenAttackResultDispatched("Guard_A", "Villain_B", "Hit");
        }

        [TestMethod]
        public void MissEventAnimationOnly()
        {
            WhenInfoFileArrives("attack_result", "Guard_A->Villain_B:Miss");
            ThenAttackResultDispatched("Guard_A", "Villain_B", "Miss");
        }

        [TestMethod]
        public void UnmatchedCharacterSkipped()
        {
            WhenInfoFileArrives("attack_result", "Guard_A->Unknown_X:Hit");
            ThenWarningLogged();
        }

        [TestMethod]
        public void MultipleEventsSequential()
        {
            WhenInfoFileArrives("attack_result", "Guard_A->Villain_B:Hit;Guard_A->Villain_C:Miss");
            ThenAttackResultDispatched("Guard_A", "Villain_B", "Hit");
            ThenAttackResultDispatched("Guard_A", "Villain_C", "Miss");
        }
    }
}
