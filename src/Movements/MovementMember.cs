using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Framework.WPF.Library;
using Newtonsoft.Json;
using System.Windows.Input;

namespace HeroVTT.Movements
{
    public class MovementMember : NotifyPropertyChanged
    {
        private ReferenceAbility memberAbility;
        public ReferenceAbility MemberAbility
        {
            get { return memberAbility; }
            set { memberAbility = value; OnPropertyChanged("MemberAbility"); }
        }

        private string memberName;
        public string MemberName
        {
            get { return memberName; }
            set { memberName = value; OnPropertyChanged("MemberName"); }
        }

        private MovementDirection movementDirection;
        public MovementDirection MovementDirection
        {
            get { return movementDirection; }
            set { movementDirection = value; OnPropertyChanged("MovementDirection"); }
        }

        [JsonIgnore]
        public Key AssociatedKey
        {
            get
            {
                switch (MovementDirection)
                {
                    case MovementDirection.Forward: return Key.W;
                    case MovementDirection.Backward: return Key.S;
                    case MovementDirection.Left: return Key.A;
                    case MovementDirection.Right: return Key.D;
                    case MovementDirection.Upward: return Key.Space;
                    case MovementDirection.Downward: return Key.Z;
                    case MovementDirection.Still: return Key.X;
                    default: return Key.None;
                }
            }
        }

        public MovementMember Clone()
        {
            return new MovementMember
            {
                MemberName = this.MemberName,
                MemberAbility = this.MemberAbility,
                MovementDirection = this.MovementDirection
            };
        }
    }
}
