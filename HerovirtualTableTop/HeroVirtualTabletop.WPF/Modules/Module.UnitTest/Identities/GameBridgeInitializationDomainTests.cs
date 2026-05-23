using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using System;

namespace Module.UnitTest.Identities
{
    // ─────────────────────────────────────────────────────────────────────────
    // Story: Game Bridge Initialization State Machine
    // SBE: docs/increment-2/specification-by-example-increment-2.md § Game Bridge
    // CRC: Uninitialized → Initializing → Polling → Ready
    // Architecture constraint: no game command or slash command may be issued
    //   until Game Bridge is in the Ready state.
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestGameBridgeInitializationStateMachine : BaseTest
    {
        private GameBridgeStateMachine _gameBridge;

        [TestInitialize]
        public void GivenANewGameBridgeStateMachine()
        {
            _gameBridge = new GameBridgeStateMachine();
        }

        // ── Uninitialized ─────────────────────────────────────────────────────

        // Scenario: Before any initialization — bridge is in Uninitialized state
        [TestMethod]
        public void BeforeAnyInitialization_BridgeIsInUninitializedState()
        {
            // Then
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Uninitialized,
                "a newly created Game Bridge has no DLL loaded and must start in Uninitialized");
        }

        // Scenario: Commands rejected when bridge is Uninitialized
        [TestMethod]
        public void CommandsRejected_WhenBridgeIsUninitialized()
        {
            // When
            Action route = () => _gameBridge.RouteSlashCommand("/target_name Guard_Captain");

            // Then
            route.ShouldThrow<InvalidOperationException>(
                "no slash command may be issued before the Game Bridge reaches Ready");
        }

        // Scenario: IsReadyForCommands is false when Uninitialized
        [TestMethod]
        public void IsReadyForCommands_IsFalse_WhenUninitialized()
        {
            _gameBridge.IsReadyForCommands.Should().BeFalse();
        }

        // ── Uninitialized → Initializing ──────────────────────────────────────

        // Scenario: HookCostume DLL loads successfully → bridge transitions to Initializing
        [TestMethod]
        public void HookCostumeDllLoaded_BridgeTransitionsToInitializing()
        {
            // When
            _gameBridge.HookCostumeDllLoaded();

            // Then
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Initializing,
                "successful DLL load is the first step; bridge moves to Initializing");
        }

        // Scenario: Commands still rejected in Initializing state
        [TestMethod]
        public void CommandsRejected_WhenBridgeIsInitializing()
        {
            // Given
            _gameBridge.HookCostumeDllLoaded();

            // When
            Action route = () => _gameBridge.RouteSlashCommand("/target_name Guard_Captain");

            // Then
            route.ShouldThrow<InvalidOperationException>(
                "bridge is not yet Ready; game may not have fully loaded");
        }

        // Scenario: HookCostume DLL load fails → bridge stays Uninitialized
        [TestMethod]
        public void HookCostumeDllLoadFailed_BridgeRemainsUninitialized()
        {
            // When
            _gameBridge.HookCostumeDllLoadFailed();

            // Then
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Uninitialized,
                "DLL load failure leaves the Game Bridge unable to proceed; state stays Uninitialized");
        }

        // Scenario: InitGame called before DLL was loaded → rejected with error
        [TestMethod]
        public void InitGameCalledBeforeDllLoaded_RejectedWithInvalidOperationException()
        {
            // Given: bridge still Uninitialized (DLL never loaded)

            // When
            Action initGame = () => _gameBridge.InitGameSucceeded();

            // Then
            initGame.ShouldThrow<InvalidOperationException>(
                "InitGame may not be called before the HookCostume DLL has been loaded");
        }

        // ── Initializing → Polling ────────────────────────────────────────────

        // Scenario: InitGame succeeds → bridge transitions from Initializing to Polling
        [TestMethod]
        public void InitGameSucceeded_BridgeTransitionsFromInitializingToPolling()
        {
            // Given
            _gameBridge.HookCostumeDllLoaded();

            // When
            _gameBridge.InitGameSucceeded();

            // Then
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Polling,
                "InitGame success moves the bridge to Polling; it now waits for the COH client to confirm load");
        }

        // Scenario: Commands still rejected while bridge is Polling
        [TestMethod]
        public void CommandsRejected_WhenBridgeIsPolling()
        {
            // Given
            _gameBridge.HookCostumeDllLoaded();
            _gameBridge.InitGameSucceeded();

            // When
            Action route = () => _gameBridge.RouteSlashCommand("/target_name Guard_Captain");

            // Then
            route.ShouldThrow<InvalidOperationException>(
                "bridge is still waiting for COH game client confirmation; commands not yet accepted");
        }

        // Scenario: InitGame fails → bridge returns to Uninitialized
        [TestMethod]
        public void InitGameFailed_BridgeReturnsToUninitialized()
        {
            // Given: DLL was loaded
            _gameBridge.HookCostumeDllLoaded();

            // When: InitGame returns failure code
            _gameBridge.InitGameFailed();

            // Then
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Uninitialized,
                "InitGame failure undoes any partial initialization; bridge returns to Uninitialized");
        }

        // ── Polling → Ready ───────────────────────────────────────────────────

        // Scenario: COH game client confirms loaded → bridge transitions from Polling to Ready
        [TestMethod]
        public void GameClientConfirmedLoaded_BridgeTransitionsFromPollingToReady()
        {
            // Given: bridge has completed DLL load + InitGame
            GivenBridgeIsPolling();

            // When: polling loop confirms COH game client is running
            _gameBridge.GameClientConfirmedLoaded();

            // Then
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Ready,
                "game client confirmation is the final step; bridge is now Ready");
        }

        // Scenario: Slash commands accepted when bridge is in Ready state
        [TestMethod]
        public void SlashCommandsAccepted_WhenBridgeIsReady()
        {
            // Given: bridge has reached Ready
            GivenBridgeIsReady();

            // When / Then: command does not throw
            Action route = () => _gameBridge.RouteSlashCommand("/target_name Guard_Captain");
            route.ShouldNotThrow("slash commands may be issued once the bridge is Ready");
        }

        // Scenario: IsReadyForCommands is true only in Ready state
        [TestMethod]
        public void IsReadyForCommands_IsTrue_WhenBridgeIsReady()
        {
            // Given
            GivenBridgeIsReady();

            // Then
            _gameBridge.IsReadyForCommands.Should().BeTrue();
        }

        // Scenario: Redundant confirmation while already Ready — stays Ready
        [TestMethod]
        public void RedundantConfirmationWhenAlreadyReady_BridgeRemainsReady()
        {
            // Given: bridge is already Ready
            GivenBridgeIsReady();

            // When: another "confirmed loaded" signal arrives
            _gameBridge.GameClientConfirmedLoaded();

            // Then: state is idempotent
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Ready,
                "duplicate confirmation is a no-op; bridge stays Ready");
        }

        // Scenario: Routing null/empty slash command → rejected with ArgumentException
        [TestMethod]
        public void RouteEmptySlashCommand_RejectedWithArgumentException()
        {
            // Given: bridge is Ready
            GivenBridgeIsReady();

            // When
            Action routeEmpty = () => _gameBridge.RouteSlashCommand(string.Empty);

            // Then
            routeEmpty.ShouldThrow<ArgumentException>(
                "an empty slash command string is never valid regardless of bridge state");
        }

        // ── Full happy-path sequence ──────────────────────────────────────────

        // Scenario: Full initialization sequence Uninitialized → Initializing → Polling → Ready
        [TestMethod]
        public void FullHappyPath_BridgeTraversesAllFourStates()
        {
            // Uninitialized
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Uninitialized);

            // Uninitialized → Initializing
            _gameBridge.HookCostumeDllLoaded();
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Initializing);

            // Initializing → Polling
            _gameBridge.InitGameSucceeded();
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Polling);

            // Polling → Ready
            _gameBridge.GameClientConfirmedLoaded();
            _gameBridge.InitializationState.Should().Be(GameBridgeInitializationState.Ready);

            // Ready → accepts commands
            _gameBridge.IsReadyForCommands.Should().BeTrue();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void GivenBridgeIsPolling()
        {
            _gameBridge.HookCostumeDllLoaded();
            _gameBridge.InitGameSucceeded();
        }

        private void GivenBridgeIsReady()
        {
            GivenBridgeIsPolling();
            _gameBridge.GameClientConfirmedLoaded();
        }
    }
}
