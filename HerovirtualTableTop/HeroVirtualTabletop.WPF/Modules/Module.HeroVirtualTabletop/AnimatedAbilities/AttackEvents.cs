using Prism.Events;
using HeroVirtualTabletop.Crowds;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using HeroVirtualTabletop.Identities;
using HeroVirtualTabletop.Characters;
using HeroVirtualTabletop.AnimatedAbilities;
using HeroVirtualTabletop.Movements;
using HeroVirtualTabletop.OptionGroups;

namespace HeroVirtualTabletop.AnimatedAbilities
{
    #region Attack Events
    public class AttackInitiatedEvent : PubSubEvent<Tuple<Character, Attack>> { };
    public class AttackCompletedEvent : PubSubEvent<Tuple<Character, Attack>> { };
    public class AttackTargetSelectedEvent : PubSubEvent<Tuple<Character, Attack>> { };
    public class ResetCharacterStateEvent : PubSubEvent<Character> { };
    public class AttackTargetUpdatedEvent : PubSubEvent<Tuple<List<Character>, Attack>> { };
    public class ConfigureActiveAttackEvent : PubSubEvent<Tuple<List<Character>, Attack>> { };
    public class SetActiveAttackEvent : PubSubEvent<Tuple<List<Character>, Attack>> { }
    public class CloseActiveAttackEvent : PubSubEvent<object> { }
    #endregion
}
