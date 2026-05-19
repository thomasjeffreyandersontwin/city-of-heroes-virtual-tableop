using Prism.Events;
using Module.HeroVirtualTabletop.Crowds;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.Events
{
    public class AddToRosterEvent : PubSubEvent<IEnumerable<CrowdMemberModel>> { }
    public class SpawnToRosterEvent : PubSubEvent<IEnumerable<CrowdMemberModel>> { }
    public class CloneAndSpawnCrowdMemberEvent : PubSubEvent<object> { }
    public class DeleteCrowdMemberEvent : PubSubEvent<ICrowdMemberModel> { }
    public class CloneLinkCrowdMemberEvent : PubSubEvent<ICrowdMemberModel> { }
    public class EditCharacterEvent : PubSubEvent<Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>> { }
    public class SaveCrowdEvent : PubSubEvent<object> { }
    public class SaveCrowdCompletedEvent : PubSubEvent<object> { }
    public class AddToRosterThruCharExplorerEvent : PubSubEvent<Tuple<CrowdMemberModel, CrowdModel>> { };
    public class AddOptionEvent : PubSubEvent<ICharacterOption> { }
    public class RemoveOptionEvent : PubSubEvent<ICharacterOption> { }
    public class EditIdentityEvent : PubSubEvent<Tuple<Identity, Character>> { }
    public class EditAbilityEvent : PubSubEvent<Tuple<AnimatedAbility, Character>> { }
    public class CheckRosterConsistencyEvent : PubSubEvent<IEnumerable<CrowdMemberModel>> { }
    public class RosterSyncCompletedEvent : PubSubEvent<object> { }
    public class CreateCrowdFromModelsEvent : PubSubEvent<CrowdModel> { }
    public class ActivateCharacterEvent : PubSubEvent<Tuple<Character, string, string>> { }
    public class ActivateGangEvent : PubSubEvent<List<Character>> { }
    public class DeactivateCharacterEvent : PubSubEvent<object> { }
    public class DeactivateGangEvent : PubSubEvent<object> { }
    public class NeedAbilityCollectionRetrievalEvent : PubSubEvent<object> { }
    public class NeedIdentityCollectionRetrievalEvent : PubSubEvent<object> { }
    public class NeedDefaultCharacterRetrievalEvent : PubSubEvent<Action<Character>> { }
    public class FinishedAbilityCollectionRetrievalEvent : PubSubEvent<ObservableCollection<AnimatedAbility>> { }
    public class FinishedIdentityCollectionRetrievalEvent : PubSubEvent<ObservableCollection<Identity>> { }
    public class StopAllActiveAbilitiesEvent : PubSubEvent<object> { }
    public class ListenForTargetChanged : PubSubEvent<object> { }
    public class StopListeningForTargetChanged : PubSubEvent<object> { }

    public class PanelClosedEvent : PubSubEvent<string> { }

    public class GameLoadedEvent : PubSubEvent<object> { }

    #region Attack Events
    public class PlayAnimatedAbilityEvent : PubSubEvent<Tuple<Character, AnimatedAbility>> { }
    public class StopAnimatedAbilityEvent : PubSubEvent<Tuple<Character, AnimatedAbility>> { }
    public class AttackInitiatedEvent : PubSubEvent<Tuple<Character, Attack>> { }
    public class AttackTargetUpdatedEvent : PubSubEvent<Tuple<Attack, List<Character>, Guid>> { }
    //public class ConfigureActiveAttackEvent : PubSubEvent<Tuple<List<Character>, Attack>> { }
    public class ExecuteSweepAttackEvent : PubSubEvent<object> { }
    public class ConfigureSweepAttackEvent : PubSubEvent<List<Tuple<Attack, List<Character>, Guid>>> { }
    public class ResetSweepEvent: PubSubEvent<object> { }
    public class ConfigureAttacksEvent : PubSubEvent<List<Tuple<Attack, List<Character>, Guid>>> { }
    //public class ConfirmAttackEvent : PubSubEvent<object> { }
    public class ConfirmAttacksEvent : PubSubEvent<object> { }
    //public class SetActiveAttackEvent : PubSubEvent<Tuple<List<Character>, Attack>> { }
    public class LaunchAttacksEvent : PubSubEvent<List<Tuple<Attack, List<Character>, Guid>>> { }
    public class CloseAttackConfigurationWidgetEvent : PubSubEvent<object> { }
    public class CancelActiveAttackEvent : PubSubEvent<Tuple<Attack, List<Character>, Guid>> { }
    public class CancelAttacksEvent : PubSubEvent<List<Tuple<Attack, List<Character>, Guid>>> { }
    public class AttackExecutionsFinishedEvent : PubSubEvent<object> { }
    public class LoadAttackTargetsSelectionWidgetEvent : PubSubEvent<List<Character>> { }
    public class AttackTargetsSelectionRequiredEvent : PubSubEvent<List<Character>> { }
    public class AttackTargetsConfirmedEvent : PubSubEvent<List<Character>> { }
    public class LoadAutoFireAttackShotSelectionWidgetEvent : PubSubEvent<Tuple<Attack, List<Character>, Guid>> { }
    public class CloseAutoFireAttackShotSelectionWidgetEvent : PubSubEvent<Object> { }
    public class AssignAutoFireAttackShotsEvent : PubSubEvent<Tuple<Attack, List<Character>, Guid>> { }
    public class AutoFireAttackShotsAssignedEvent : PubSubEvent<List<Character>> { }

    #endregion

    #region Movement Events

    public class EditMovementEvent : PubSubEvent<CharacterMovement> { }
    public class RemoveMovementEvent : PubSubEvent<String> { }
    public class PlayMovementInitiatedEvent: PubSubEvent<CharacterMovement> { }
    public class PlayMovementConfirmedEvent: PubSubEvent<Tuple<CharacterMovement, List<Character>>> { }
    public class StopMovementEvent: PubSubEvent<CharacterMovement> { }
    public class CombatMovementChangedEvent : PubSubEvent<CharacterMovement> { }

    #endregion
}
