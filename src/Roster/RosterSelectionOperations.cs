using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;

namespace HeroVTT.Roster
{
    public class RosterSelectionOperations
    {
        public List<Character> GetCharactersToOperateOn(IList selectedParticipants, HashedObservableCollection<ICrowdMemberModel, string> participants, bool considerGangMode = true)
        {
            var characters = new List<Character>();

            foreach (Character c in selectedParticipants)
            {
                if (characters.Contains(c))
                    continue;

                if (considerGangMode && (c as CrowdMemberModel).RosterCrowd.IsGangMode)
                {
                    foreach (Character gangmember in participants.Where(p => c != p && p.RosterCrowd.IsGangMode && (c as CrowdMemberModel).RosterCrowd == p.RosterCrowd))
                        characters.Add(gangmember);
                }
                characters.Add(c);
            }

            return characters.Distinct().ToList();
        }

        public Character GetLastSelectedCharacter(IList selectedParticipants, HashedObservableCollection<ICrowdMemberModel, string> participants)
        {
            int highestIndex = 0;
            foreach (Character c in selectedParticipants)
            {
                int currentIndex = participants.IndexOf(c as CrowdMemberModel);
                if (currentIndex > highestIndex)
                    highestIndex = currentIndex;
            }
            return participants[highestIndex] as Character;
        }

        public ICrowdMemberModel GetCurrentTarget(HashedObservableCollection<ICrowdMemberModel, string> participants)
        {
            var target = new MemoryElement();
            return participants.FirstOrDefault(x => (x as CrowdMemberModel).Label == target.Label);
        }

        public Character GetHoveredCharacter(HashedObservableCollection<ICrowdMemberModel, string> participants)
        {
            var target = new MemoryElement();
            return participants.FirstOrDefault(x => (x as CrowdMemberModel).Label == target.Label) as Character;
        }
    }
}
