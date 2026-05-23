using System;

namespace Module.HeroVirtualTabletop.Library.GameCommunicator
{
    /// <summary>
    /// Models the Game Bridge initialization state machine defined in the CRC.
    /// States: Uninitialized → Initializing → Polling → Ready.
    /// No game command or slash command may be issued before the Ready state is reached.
    /// </summary>
    public enum GameBridgeInitializationState
    {
        Uninitialized,
        Initializing,
        Polling,
        Ready
    }

    public class GameBridgeStateMachine
    {
        private GameBridgeInitializationState _state = GameBridgeInitializationState.Uninitialized;

        public GameBridgeInitializationState InitializationState
        {
            get { return _state; }
        }

        public bool IsReadyForCommands
        {
            get { return _state == GameBridgeInitializationState.Ready; }
        }

        /// <summary>
        /// Called when HookCostume DLL is successfully loaded.
        /// Transitions Uninitialized → Initializing.
        /// </summary>
        public void HookCostumeDllLoaded()
        {
            if (_state == GameBridgeInitializationState.Uninitialized)
                _state = GameBridgeInitializationState.Initializing;
        }

        /// <summary>
        /// Called when the DLL load fails (missing or wrong architecture).
        /// State remains or returns to Uninitialized.
        /// </summary>
        public void HookCostumeDllLoadFailed()
        {
            _state = GameBridgeInitializationState.Uninitialized;
        }

        /// <summary>
        /// Called when InitGame (DLL init entry point) succeeds.
        /// Transitions Initializing → Polling; starts the ready-poll loop.
        /// Requires DLL already loaded — rejects if called out of order.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when DLL has not been loaded first.</exception>
        public void InitGameSucceeded()
        {
            if (_state == GameBridgeInitializationState.Uninitialized)
                throw new InvalidOperationException("InitGame called before HookCostume DLL was loaded.");
            if (_state == GameBridgeInitializationState.Initializing)
                _state = GameBridgeInitializationState.Polling;
            // Duplicate call after ready: ignored (stays Ready)
        }

        /// <summary>
        /// Called when InitGame returns a failure code.
        /// Transitions back to Uninitialized; no polling loop started.
        /// </summary>
        public void InitGameFailed()
        {
            _state = GameBridgeInitializationState.Uninitialized;
        }

        /// <summary>
        /// Called when the polling loop confirms the COH game client is loaded.
        /// Transitions Polling → Ready and permits game commands.
        /// Published exactly once per session — subsequent calls are no-ops (idempotent).
        /// </summary>
        public void GameClientConfirmedLoaded()
        {
            if (_state == GameBridgeInitializationState.Polling)
                _state = GameBridgeInitializationState.Ready;
            // Already Ready: stay Ready (SBE: "Already ready, redundant not-ready" stays ready)
        }

        /// <summary>
        /// Routes a slash command. Throws if the bridge is not yet Ready.
        /// </summary>
        /// <exception cref="InvalidOperationException">Rejected when state is not Ready.</exception>
        public void RouteSlashCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException("Slash command string must not be null or empty.", "command");
            if (_state != GameBridgeInitializationState.Ready)
                throw new InvalidOperationException(
                    "Slash command rejected — Game Bridge not ready (state: " + _state + "). Command: " + command);
        }
    }
}
