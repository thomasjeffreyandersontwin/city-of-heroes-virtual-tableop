using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System;
using System.Collections.Generic;

namespace HeroVTT.Movements
{
    public class MovementInstruction
    {
        private object lockObj = new object();

        public bool IsMoving { get; set; }
        public bool IsTurning { get; set; }
        public bool IsMovingToDestination { get; set; }
        public Vector3 DestinationVector { get; set; }
        public Vector3 OriginalDestinationVector { get; set; }
        public Vector3 CurrentDirectionVector { get; set; }
        public MovementDirection CurrentMovementDirection { get; set; }

        private MovementDirection _lastMovementDirection = MovementDirection.Forward;
        public MovementDirection LastMovementDirection
        {
            get { return _lastMovementDirection; }
            set { if (_lastMovementDirection != value) _lastMovementDirection = value; }
        }

        public MovementDirection CurrentRotationAxisDirection { get; set; }
        public MovementDirection MovmementDirectionToUseForDestinationMove { get; set; }
        public bool StopOnCollision { get; set; }
        public float MovementUnit { get; set; }

        private bool isInCollision;
        public bool IsInCollision
        {
            get { lock (lockObj) { return isInCollision; } }
            set { lock (lockObj) { isInCollision = value; } }
        }

        public Vector3 LastCollisionFreePointInCurrentDirection { get; set; }
        public bool IsCollisionAhead { get; set; }
        public Vector3 CharacterBodyCollisionOffsetVector { get; set; }
        public float DistanceFromCollisionFreePoint { get; set; }
        public bool IsPositionAdjustedToAvoidCollision { get; set; }
        public float DestinationPointHeightAdjustment { get; set; }
        public bool IsDestinationPointAdjusted { get; set; }
        public DateTime? LastMovmentSupportingAnimationPlayTime { get; set; }
        public bool IsMovementPaused { get; set; }
        public DateTime MovementStartTime { get; set; }
        public List<BodyPart> BodyPartsToConsiderForCollision { get; set; }
        public bool AdjustPositionToAvoidCollision { get; set; }
    }
}
