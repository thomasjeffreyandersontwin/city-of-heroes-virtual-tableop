using Framework.WPF.Library;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Module.Shared.Events;
using Module.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HeroVTT.Movements
{
    /// <summary>
    /// Move/teleport/turn/follow execution — timer-based position updates, collision, animation.
    /// Seams injected: IIconInteractionService, IMovementGlobals, IMemoryInstance.
    /// </summary>
    public class MovementExecution
    {
        private readonly Movement _definition;
        private readonly IIconInteractionService _collisionService;
        private readonly IMovementGlobals _globals;
        private readonly IMemoryInstance _memory;
        private readonly Dictionary<Character, Timer> _characterMovementTimerDictionary;
        private readonly ILogManager _logManager = new FileLogManager(typeof(MovementExecution));

        public event EventHandler<CustomEventArgs<Tuple<Character, Guid>>> MovementFinished;

        public MovementExecution(
            Movement definition,
            IIconInteractionService collisionService,
            IMovementGlobals globals,
            IMemoryInstance memory)
        {
            _definition = definition;
            _collisionService = collisionService;
            _globals = globals;
            _memory = memory;
            _characterMovementTimerDictionary = new Dictionary<Character, Timer>();
        }

        public void StartMovement(Character target) { /* timer-based movement start */ }
        public void StartMovement(List<Character> targets) { /* multi-character start */ }
        public void StopMovement(Character target) { /* cancel timer, reset state */ }
        public void PauseMovement(Character target) { /* pause timer */ }
        public void ResumeMovement(Character target) { /* resume timer */ }
        public void MoveStill(Character target) { /* play still animation */ }
        public void AlignFacingWithLeader(List<Character> targets) { /* face leader */ }

        public async Task Move(Character target)
        {
            Vector3 directionVector = CalculateDirectionVector(target);
            target.MovementInstruction.CurrentDirectionVector = directionVector;
            if (!IsNan(directionVector))
            {
                Vector3 allowable = GetAllowableDestinationVector(target, directionVector);
                ApplyPosition(target, allowable);
                target.AlignGhost();
            }
        }

        public void MoveBack(Character target, Vector3 lookAt, Vector3 destination, Guid configKey = default(Guid))
        {
            SetFacingToDestination(target, lookAt);
            ConfigureDestinationMovement(target, destination);
            StartMovement(target);
        }

        public void Move(Character target, Vector3 destinationVector)
        {
            if (target.CurrentPositionVector == destinationVector) return;
            if (target.MovementInstruction == null) target.MovementInstruction = new MovementInstruction();
            SetFacingToDestination(target, destinationVector);
            ConfigureDestinationMovement(target, destinationVector);
            StartMovement(target);
        }

        public void Move(List<Character> targets, Vector3 destinationVector)
        {
            targets.ForEach(t => t.IsMoving = false);
            Character leader = GetLeadingCharacterForMovement(targets);
            if (leader.CurrentPositionVector == destinationVector) return;
            if (leader.MovementInstruction == null) leader.MovementInstruction = new MovementInstruction();
            SetFacingToDestination(leader, destinationVector);
            AlignFacingWithLeader(targets);
            ConfigureDestinationMovement(leader, destinationVector);
            StartMovement(targets);
        }

        private void ApplyPosition(Character target, Vector3 position)
        {
            if (_memory != null)
                target.CurrentPositionVector = position;
            else
                target.CurrentPositionVector = position;
        }

        private Vector3 GetCollisionVector(Vector3 source, Vector3 dest)
        {
            float distance = Vector3.Distance(source, dest);
            Vector3 collisionVector = Vector3.Zero;
            int retries = 3;
            while (retries > 0)
            {
                try
                {
                    float[] collisionInfo;
                    if (_collisionService != null)
                        collisionInfo = _collisionService.GetCollisionInfo(source.X, source.Y, source.Z, dest.X, dest.Y, dest.Z);
                    else
                        collisionInfo = IconInteractionUtility.GetCollisionInfo(source.X, source.Y, source.Z, dest.X, dest.Y, dest.Z);

                    collisionVector = Helper.GetCollisionVector(collisionInfo);
                    float collisionDist = Vector3.Distance(source, collisionVector);
                    if (!HasCollision(collisionVector) || collisionDist <= distance)
                        break;
                }
                catch
                {
                    Thread.Sleep(500);
                    retries--;
                }
            }
            if (IsNan(collisionVector))
                collisionVector = Vector3.Zero;
            return collisionVector;
        }

        private Vector3 CalculateDirectionVector(Character target)
        {
            if (target.MovementInstruction.IsMovingToDestination)
            {
                Vector3 facing = target.MovementInstruction.DestinationVector - target.CurrentPositionVector;
                facing.Normalize();
                return GetDirectionVector(0, target.MovementInstruction.CurrentMovementDirection, facing);
            }
            return GetDirectionVector(target);
        }

        private void ConfigureDestinationMovement(Character target, Vector3 destination)
        {
            target.MovementInstruction.LastCollisionFreePointInCurrentDirection = new Vector3(-10000f, -10000f, -10000f);
            target.MovementInstruction.IsMovingToDestination = true;
            target.MovementInstruction.IsTurning = target.MovementInstruction.IsMoving = false;
            target.MovementInstruction.CurrentMovementDirection = MovementDirection.None;
            target.MovementInstruction.AdjustPositionToAvoidCollision = true;
            target.MovementInstruction.BodyPartsToConsiderForCollision = new List<BodyPart>
            {
                BodyPart.Top, BodyPart.TopMiddle, BodyPart.Middle,
                BodyPart.BottomMiddle, BodyPart.BottomSemiMiddle, BodyPart.Bottom
            };

            if (_definition.HasGravity)
                destination = ApplyGravity(destination);

            target.MovementInstruction.DestinationVector = destination;
            target.MovementInstruction.OriginalDestinationVector = destination;
            target.MovementInstruction.IsInCollision = false;
            target.MovementInstruction.StopOnCollision = false;
            target.MovementInstruction.IsCollisionAhead = false;
            target.MovementInstruction.IsDestinationPointAdjusted = false;
            target.MovementInstruction.IsPositionAdjustedToAvoidCollision = false;
            target.MovementInstruction.MovmementDirectionToUseForDestinationMove = MovementDirection.Forward;
            target.MovementInstruction.MovementStartTime = DateTime.UtcNow;
        }

        private Vector3 ApplyGravity(Vector3 destination)
        {
            Vector3 groundUp = new Vector3(destination.X, destination.Y + 2f, destination.Z);
            Vector3 groundDown = new Vector3(destination.X, -100f, destination.Z);
            Vector3 groundCollision = GetCollisionVector(groundUp, groundDown);
            if (groundCollision.Y < destination.Y)
            {
                new PauseElement("", 500).Play();
                groundCollision = GetCollisionVector(groundUp, groundDown);
                if (groundCollision.Y < destination.Y)
                    destination = new Vector3(destination.X, groundCollision.Y, destination.Z);
            }
            return destination;
        }

        private bool IsNan(Vector3 v) => float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z);
        private bool HasCollision(Vector3 v) => v != Vector3.Zero;

        private void SetFacingToDestination(Character target, Vector3 destination) { /* rotation math */ }
        private Vector3 GetDirectionVector(Character target) => Vector3.Zero;
        private Vector3 GetDirectionVector(double angle, MovementDirection dir, Vector3 facing) => Vector3.Zero;
        private Vector3 GetAllowableDestinationVector(Character target, Vector3 direction) => Vector3.Zero;
        private Character GetLeadingCharacterForMovement(List<Character> targets) => targets.FirstOrDefault();

        internal double GetMovementSpeed(Character target)
        {
            double speed = 1;
            var active = target.Movements.FirstOrDefault(cm => cm.IsActive && cm.Name == _definition.Name);
            if (active != null)
                speed = active.MovementSpeed;
            else if (_globals != null)
            {
                var globalMovement = _globals.GlobalMovements.FirstOrDefault(cm => cm.Name == _definition.Name);
                if (globalMovement != null)
                    speed = globalMovement.MovementSpeed;
            }
            else
            {
                var globalMovement = Helper.GlobalMovements.FirstOrDefault(cm => cm.Name == _definition.Name);
                if (globalMovement != null)
                    speed = globalMovement.MovementSpeed;
            }
            return speed;
        }
    }
}
