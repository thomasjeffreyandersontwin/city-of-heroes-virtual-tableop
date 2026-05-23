using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Library.GameCommunicator;

namespace HeroVTT.DomainTests.Support
{
    /// <summary>
    /// Ensures slash-command paths never load HookCostume or attach to the game during domain tests.
    /// </summary>
    [TestClass]
    public static class GameCommandTestAssemblyHooks
    {
        [AssemblyInitialize]
        public static void IsolateFromLiveGame(TestContext _)
        {
            GameCommandExecution.ActiveExecutor = NoOpGameCommandExecutor.Instance;
        }
    }
}
