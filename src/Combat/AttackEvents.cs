using Prism.Events;
using System;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.AnimatedAbilities;

namespace HeroVTT.Combat
{
    public class AttackInitiatedEvent : PubSubEvent<Tuple<Character, Attack>> { }
    public class AttackCompletedEvent : PubSubEvent<Tuple<Character, Attack>> { }
    public class AttackTargetSelectedEvent : PubSubEvent<Tuple<Character, Attack>> { }
    public class ResetCharacterStateEvent : PubSubEvent<Character> { }
    public class AttackTargetUpdatedEvent : PubSubEvent<Tuple<List<Character>, Attack>> { }
    public class ConfigureActiveAttackEvent : PubSubEvent<Tuple<List<Character>, Attack>> { }
    public class SetActiveAttackEvent : PubSubEvent<Tuple<List<Character>, Attack>> { }
    public class CloseActiveAttackEvent : PubSubEvent<object> { }
}
