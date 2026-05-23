using Module.HeroVirtualTabletop.Movements;
using Prism.Events;
using System;

namespace HeroVTT.Movements
{
    public class EditMovementEvent : PubSubEvent<CharacterMovement> { }
    public class RemoveMovementEvent : PubSubEvent<string> { }
}
