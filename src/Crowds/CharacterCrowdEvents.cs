using Prism.Events;
using System;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.OptionGroups;

namespace HeroVTT.Crowds
{
    public class EditCharacterEvent : PubSubEvent<Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>> { }
    public class AddOptionEvent : PubSubEvent<ICharacterOption> { };
    public class RemoveOptionEvent : PubSubEvent<ICharacterOption> { };
    public class EditIdentityEvent : PubSubEvent<Tuple<Identity, Character>> { };
    public class EditAbilityEvent : PubSubEvent<Tuple<AnimatedAbility, Character>> { };

    
    
    public class SaveCrowdEvent : PubSubEvent<object> { }
    

}
