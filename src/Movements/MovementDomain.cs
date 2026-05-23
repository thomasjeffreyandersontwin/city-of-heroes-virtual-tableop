using Framework.WPF.Library;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Movements;
using Module.Shared.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HeroVTT.Movements
{
    /// <summary>
    /// Movement definition — name, members, gravity. Execution delegated to <see cref="MovementExecution"/>.
    /// </summary>
    public class Movement : NotifyPropertyChanged
    {
        private MovementExecution _execution;

        [JsonConstructor]
        private Movement() { }

        public Movement(string name)
        {
            Name = name;
            AddDefaultMemberAbilities();
        }

        public Movement(string name, IIconInteractionService collisionService, IMovementGlobals globals, IMemoryInstance memory)
            : this(name)
        {
            _execution = new MovementExecution(this, collisionService, globals, memory);
        }

        public event EventHandler<CustomEventArgs<Tuple<Character, Guid>>> MovementFinished
        {
            add { EnsureExecution().MovementFinished += value; }
            remove { if (_execution != null) _execution.MovementFinished -= value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged("Name"); }
        }

        private ObservableCollection<MovementMember> movementMembers;
        public ObservableCollection<MovementMember> MovementMembers
        {
            get { return movementMembers; }
            set { movementMembers = value; OnPropertyChanged("MovementMembers"); }
        }

        private bool hasGravity;
        public bool HasGravity
        {
            get { return hasGravity; }
            set { hasGravity = value; OnPropertyChanged("HasGravity"); }
        }

        public Movement Clone()
        {
            var clone = new Movement(Name);
            clone.HasGravity = HasGravity;
            clone.MovementMembers.Clear();
            foreach (var member in MovementMembers)
                clone.MovementMembers.Add(member.Clone());
            return clone;
        }

        public void StartMovement(Character target) { EnsureExecution().StartMovement(target); }
        public void StartMovement(List<Character> targets) { EnsureExecution().StartMovement(targets); }
        public void StopMovement(Character target) { EnsureExecution().StopMovement(target); }
        public void PauseMovement(Character target) { EnsureExecution().PauseMovement(target); }
        public void ResumeMovement(Character target) { EnsureExecution().ResumeMovement(target); }
        public void MoveStill(Character target) { EnsureExecution().MoveStill(target); }
        public void AlignFacingWithLeader(List<Character> targets) { EnsureExecution().AlignFacingWithLeader(targets); }
        public Task Move(Character target) { return EnsureExecution().Move(target); }
        public void MoveBack(Character target, Microsoft.Xna.Framework.Vector3 lookAt, Microsoft.Xna.Framework.Vector3 destination, Guid configKey = default(Guid))
        {
            EnsureExecution().MoveBack(target, lookAt, destination, configKey);
        }
        public void Move(Character target, Microsoft.Xna.Framework.Vector3 destinationVector)
        {
            EnsureExecution().Move(target, destinationVector);
        }
        public void Move(List<Character> targets, Microsoft.Xna.Framework.Vector3 destinationVector)
        {
            EnsureExecution().Move(targets, destinationVector);
        }

        private MovementExecution EnsureExecution()
        {
            if (_execution == null)
                _execution = new MovementExecution(this, null, null, null);
            return _execution;
        }

        private void AddDefaultMemberAbilities()
        {
            MovementMembers = new ObservableCollection<MovementMember>();
        }
    }
}
