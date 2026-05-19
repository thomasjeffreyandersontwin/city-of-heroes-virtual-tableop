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

namespace HeroVirtualTabletop.Crowds
{
    public class EditCharacterEvent : PubSubEvent<Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>> { }
    public class AddOptionEvent : PubSubEvent<ICharacterOption> { };
    public class RemoveOptionEvent : PubSubEvent<ICharacterOption> { };
    public class EditIdentityEvent : PubSubEvent<Tuple<Identity, Character>> { };
    public class EditAbilityEvent : PubSubEvent<Tuple<AnimatedAbility, Character>> { };

    
    
    public class SaveCrowdEvent : PubSubEvent<object> { }
    

}
