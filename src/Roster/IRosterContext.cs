using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;
using Module.Shared;
using Prism.Events;

namespace HeroVTT.Roster
{
    public interface IRosterContext
    {
        HashedObservableCollection<ICrowdMemberModel, string> Participants { get; }
        IList SelectedParticipants { get; set; }
        ICrowdMemberModel ActiveCharacter { get; }
        bool IsPlayingAttack { get; set; }
        bool IsPlayingAreaEffect { get; set; }
        bool IsGangModeActive { get; }
        bool IsSequenceViewActive { get; }
        Character CurrentDistanceCountingCharacter { get; set; }
        EventAggregator EventAggregator { get; }
        List<Character> AttackingCharacters { get; }
        bool CharacterIsMoving { get; }
    }
}
