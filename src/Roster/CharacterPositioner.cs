using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.Shared;
using Prism.Events;
using Module.Shared.Events;

namespace HeroVTT.Roster
{
    public class CharacterPositioner
    {
        private readonly EventAggregator eventAggregator;

        public CharacterPositioner(EventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        public void SpawnCharacters(List<Character> characters)
        {
            foreach (CrowdMemberModel member in characters.Where(c => c.IsInViewForTargeting))
            {
                member.Spawn(false);
            }
            var kg = new KeyBindsGenerator();
            kg.CompleteEvent();
            foreach (CrowdMemberModel member in characters.Where(c => !c.IsInViewForTargeting))
            {
                member.Spawn();
            }

            Character mainCharacter = characters[0];
            Vector3 nextReferenceVector = Vector3.Zero;
            var usedUpPositions = new List<Vector3>();
            foreach (CrowdMemberModel member in characters)
            {
                member.Target(false);
                member.WaitUntilTargetIsRegistered();
                member.PlaceOptimallyAround(mainCharacter, ref nextReferenceVector, ref usedUpPositions);
                if (member.ActiveIdentity.Type == IdentityType.Model)
                    member.SuperImposeGhost();
                member.UpdateDistanceCount();
            }
            var kgFinalize = new KeyBindsGenerator();
            kgFinalize.ExecutePendingWithoutPersistingToBindFile();
        }

        public void SpawnCharactersAtLocation(List<Character> characters, Vector3 location)
        {
            foreach (CrowdMemberModel member in characters)
            {
                member.Spawn(false);
            }
            var kg = new KeyBindsGenerator();
            kg.CompleteEvent();

            Vector3 nextReferenceVector = Vector3.Zero;
            var usedUpPositions = new List<Vector3>();
            foreach (CrowdMemberModel member in characters)
            {
                member.Target(false);
                member.WaitUntilTargetIsRegistered();

                if (characters.Count > 1)
                    member.PlaceOptimallyAround(location, ref nextReferenceVector, ref usedUpPositions);
                else
                    member.CurrentPositionVector = location;
                if (member.ActiveIdentity.Type == IdentityType.Model)
                    member.SuperImposeGhost();
                member.UpdateDistanceCount();
            }
            var kgFinalizeSpawn = new KeyBindsGenerator();
            kgFinalizeSpawn.ExecutePendingWithoutPersistingToBindFile();
        }

        public void ClearFromDesktop(List<Character> characters)
        {
            foreach (CrowdMemberModel member in characters)
            {
                member.ClearFromDesktop();
            }
        }

        public void SavePosition(List<Character> characters)
        {
            foreach (CrowdMemberModel member in characters.Where(c => c.HasBeenSpawned))
            {
                member.SaveCurrentPosition();
            }
        }

        public void PlaceCharacters(List<Character> characters)
        {
            foreach (CrowdMemberModel member in characters.Where(c => c.HasBeenSpawned))
            {
                member.Place();
            }
        }

        public void MoveToCamera(List<Character> characters)
        {
            var camera = new Camera();
            Vector3 cameraPosition = camera.GetPositionVector();
            Vector3 nextReferenceVector = Vector3.Zero;
            var usedUpPositions = new List<Vector3>();

            foreach (CrowdMemberModel member in characters.Where(c => c.HasBeenSpawned))
            {
                if (characters.Count > 1)
                    member.PlaceOptimallyAround(cameraPosition, ref nextReferenceVector, ref usedUpPositions);
                else
                    member.MoveToPosition(cameraPosition);
            }
        }

        public void TeleportToCamera(List<Character> characters)
        {
            var camera = new Camera();
            Vector3 cameraPosition = camera.GetPositionVector();

            foreach (CrowdMemberModel member in characters.Where(c => c.HasBeenSpawned))
            {
                member.CurrentPositionVector = cameraPosition;
            }
        }

        public void MoveToCharacter(List<Character> characters, HashedObservableCollection<ICrowdMemberModel, string> participants)
        {
            Character targetCharacter = null;
            var target = new MemoryElement();
            targetCharacter = participants.FirstOrDefault(p => (p as CrowdMemberModel).Label == target.Label) as Character;

            if (targetCharacter == null || !targetCharacter.HasBeenSpawned)
                return;

            Vector3 targetPosition = targetCharacter.CurrentPositionVector;
            Vector3 nextReferenceVector = Vector3.Zero;
            var usedUpPositions = new List<Vector3>();

            foreach (CrowdMemberModel member in characters.Where(c => c.HasBeenSpawned && c != targetCharacter))
            {
                if (characters.Count > 1)
                    member.PlaceOptimallyAround(targetPosition, ref nextReferenceVector, ref usedUpPositions);
                else
                    member.MoveToPosition(targetPosition);
            }
        }

        public void ResetOrientation(List<Character> characters)
        {
            foreach (CrowdMemberModel member in characters.Where(c => c.HasBeenSpawned))
            {
                member.ResetOrientation();
            }
        }
    }
}
