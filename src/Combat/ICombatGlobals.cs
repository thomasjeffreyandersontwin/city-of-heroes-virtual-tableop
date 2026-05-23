namespace HeroVTT.Combat
{
    public interface ICombatGlobals
    {
        string CurrentActiveWindowName { get; set; }
        bool IntegrateWithHcs { get; }
    }
}
