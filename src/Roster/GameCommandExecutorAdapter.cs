using Module.HeroVirtualTabletop.Library.GameCommunicator;

namespace HeroVTT.Roster
{
    public class GameCommandExecutorAdapter : IGameCommandExecutor
    {
        public void ExecuteCmd(string command)
        {
            GameCommandExecution.ExecuteCmd(command);
        }
    }
}
