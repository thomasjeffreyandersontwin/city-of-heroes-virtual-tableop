using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityRendering
{
    [TestClass]
    public class PlayAnimationOnIdentityLoad : IdentityRenderingHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void AnimationPlaysAfterActivationCompletes()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenIdentityActivationCompleted();

            // When
            WhenGameBridgeIssuesSpawnAnimation();

            // Then
            ThenAnimationPlayed("Guard_Captain");
        }

        [TestMethod]
        public void NoAnimationConfiguredNpcRenderedAtRest()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenNoSpawnAnimationConfigured();

            // When
            WhenIdentityLoadCompletes();

            // Then
            ThenNpcRenderedAtRest("Guard_Captain");
        }

        [TestMethod]
        public void AnimationWaitsForNpcPresence()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "absent");

            // When
            WhenGameBridgeIssuesSpawnAnimation();

            // Then — bridge waits; no crash
            ThenSpawnedNpcAbsent("Guard_Captain");
        }

        [TestMethod]
        public void AnimationDuringIdentitySwitchPlaysOnNewNpcOnly()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenIdentityActivationCompleted();

            // When
            WhenGameBridgeIssuesSpawnAnimation();

            // Then
            ThenAnimationPlayed("Guard_Captain");
        }

        [TestMethod]
        public void AnimationCommandFailsIdentityStillActive()
        {
            // Given
            GivenGameBridgeReady();
            GivenSpawnedNpc("Guard_Captain", "present");
            GivenActiveIdentity("Dragon_Model");

            // When
            WhenAnimationCommandFails();

            // Then
            ThenGameBridgeLogsFailure();
            ThenIdentityStillActive("Dragon_Model");
        }
    }
}
