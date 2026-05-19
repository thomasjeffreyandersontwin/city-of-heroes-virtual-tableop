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

namespace HeroVirtualTabletop.Movements
{
    #region Movement Events
    public class EditMovementEvent : PubSubEvent<CharacterMovement> { };
    public class RemoveMovementEvent : PubSubEvent<String> { };
    #endregion
}
