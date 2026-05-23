namespace HeroVTT.DomainTests.Support
{
    /// <summary>
    /// Lightweight test stand-in for characters participating in roster, desktop,
    /// attack configuration, combat execution, and HCS integration scenarios.
    /// Uses plain properties so domain tests compile without binding to production
    /// ViewModel or game-bridge infrastructure.
    /// </summary>
    public class TestCombatant
    {
        public string Name { get; set; }
        public bool HasBeenSpawned { get; set; }
        public bool IsActive { get; set; }
        public bool IsGangLeader { get; set; }
        public string CombatRole { get; set; }
        public string StatusEffect { get; set; }
        public string MoveMode { get; set; }
        public int ScreenX { get; set; }
        public int ScreenY { get; set; }
        public float TargetX { get; set; }
        public float TargetY { get; set; }
        public float TargetZ { get; set; }

        public TestCombatant(string name)
        {
            Name = name;
            CombatRole = "neutral";
            StatusEffect = string.Empty;
        }
    }
}
