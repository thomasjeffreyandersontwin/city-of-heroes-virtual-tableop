using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace HeroVTT.Movements
{
    public class CollisionInfo
    {
        public Vector3 BodyCollisionOffsetVector { get; set; }
        public BodyPart CollisionBodyPart { get; set; }
        public float CollisionDistance { get; set; }
        public Vector3 CollisionPoint { get; set; }
        public object CollidingObject { get; set; }
    }
}
