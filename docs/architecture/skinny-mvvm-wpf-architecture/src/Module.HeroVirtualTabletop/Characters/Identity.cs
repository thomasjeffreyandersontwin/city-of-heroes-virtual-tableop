namespace Module.HeroVirtualTabletop.Characters;

/// <summary>
/// A costume/model surface that can be applied to a character in COH.
/// Surface is the NPC model name used in the spawn_npc slash command.
/// </summary>
public class Identity
{
    public string Name    { get; }
    public string Surface { get; }   // e.g. "Model_Statesman", "Minion_Villain"

    public Identity(string name, string surface)
    {
        Name    = name;
        Surface = surface;
    }
}
