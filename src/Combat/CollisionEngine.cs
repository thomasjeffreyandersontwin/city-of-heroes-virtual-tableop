using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HeroVTT.Combat
{
    public class CollisionEngine
    {
        private readonly ICollisionDetector _collisionDetector;

        public CollisionEngine(ICollisionDetector collisionDetector)
        {
            _collisionDetector = collisionDetector;
        }

        public List<CollisionInfo> FindObstructingObjects(Character attacker, Character target, List<Character> otherCharacters)
        {
            return FindObstructingObjects(attacker.CurrentPositionVector, target.CurrentPositionVector, otherCharacters);
        }

        public List<CollisionInfo> CalculateKnockbackObstructions(Character attacker, Character target, int distance, List<Character> otherCharacters)
        {
            if (target.CurrentPositionVector == attacker.CurrentPositionVector)
                return null;

            float knockbackDistance = distance * HEXES_TO_UNITS;
            Vector3 direction = target.CurrentPositionVector - attacker.CurrentPositionVector;
            direction.Normalize();
            Vector3 dest = new Vector3(
                target.CurrentPositionVector.X + direction.X * knockbackDistance,
                target.CurrentPositionVector.Y + direction.Y * knockbackDistance,
                target.CurrentPositionVector.Z + direction.Z * knockbackDistance);

            return FindObstructingObjects(target.CurrentPositionVector, dest, otherCharacters);
        }

        private const float HEXES_TO_UNITS = 8f;
        private const float MAX_COLLISION_SENTINEL = 10000f;
        private const int COLLISION_RETRY_COUNT = 3;
        private const int COLLISION_RETRY_DELAY_MS = 500;
        private const int BODY_PART_SCAN_DELAY_MS = 5;

        private List<CollisionInfo> FindObstructingObjects(Vector3 source, Vector3 target, List<Character> otherCharacters)
        {
            var collisions = new List<CollisionInfo>();
            Vector3 sourceToTarget = target - source;
            Vector3 targetToSource = source - target;

            if (sourceToTarget == targetToSource)
                return null;

            if (sourceToTarget != Vector3.Zero) sourceToTarget.Normalize();
            if (targetToSource != Vector3.Zero) targetToSource.Normalize();

            Vector3 pointA = Helper.GetAdjacentPoint(source, sourceToTarget, true);
            Vector3 pointB = Helper.GetAdjacentPoint(source, sourceToTarget, false);
            Vector3 pointC = Helper.GetAdjacentPoint(target, targetToSource, false);
            Vector3 pointD = Helper.GetAdjacentPoint(target, targetToSource, true);

            var obstructingCharacters = FindCharactersInQuadRegion(pointA, pointB, pointC, pointD, otherCharacters, source, target);
            var bodyPartCollisions = GetCollisionPointsForBodyParts(source, target);

            AddWallCollisions(collisions, bodyPartCollisions);
            AddCharacterCollisions(collisions, obstructingCharacters, source);

            return collisions;
        }

        private List<Character> FindCharactersInQuadRegion(Vector3 a, Vector3 b, Vector3 c, Vector3 d, List<Character> others, Vector3 source, Vector3 target)
        {
            var result = new List<Character>();
            try
            {
                foreach (Character ch in others)
                {
                    if (Helper.IsPointWithinQuadraticRegion(a, b, c, d, ch.CurrentPositionVector))
                        result.Add(ch);
                }
            }
            catch
            {
                FileLogManager.ForceLog(
                    "Boundary case found for obstacle collision. Source vector {0}, Target vector {1}, other characters {2}",
                    source, target, string.Join(", ", others.Select(ch => ch.Name)));
            }
            return result;
        }

        private void AddWallCollisions(List<CollisionInfo> collisions, Dictionary<BodyPart, CollisionInfo> bodyPartMap)
        {
            bool hasCollision = bodyPartMap.Values.Any(v => v != null);
            if (!hasCollision) return;

            float collisionDistance = bodyPartMap.Values.Min(d => d != null ? d.CollisionDistance : MAX_COLLISION_SENTINEL);
            collisions.Add(new CollisionInfo { CollidingObject = "WALL", CollisionDistance = collisionDistance });
        }

        private void AddCharacterCollisions(List<CollisionInfo> collisions, List<Character> characters, Vector3 source)
        {
            foreach (Character ch in characters)
            {
                float dist = Vector3.Distance(source, ch.CurrentPositionVector);
                collisions.Add(new CollisionInfo { CollidingObject = ch, CollisionDistance = dist });
            }
        }

        private Vector3 GetCollisionVector(Vector3 source, Vector3 dest)
        {
            float distance = Vector3.Distance(source, dest);
            Vector3 collision = Vector3.Zero;
            int retries = COLLISION_RETRY_COUNT;

            while (retries > 0)
            {
                try
                {
                    var info = _collisionDetector.GetCollisionInfo(source.X, source.Y, source.Z, dest.X, dest.Y, dest.Z);
                    collision = Helper.GetCollisionVector(info);
                    float collDist = Vector3.Distance(source, collision);
                    if (!IsNonZero(collision) || collDist <= distance)
                        break;
                }
                catch
                {
                    Thread.Sleep(COLLISION_RETRY_DELAY_MS);
                    retries--;
                }
            }

            if (float.IsNaN(collision.X) || float.IsNaN(collision.Y) || float.IsNaN(collision.Z))
                collision = Vector3.Zero;

            return collision;
        }

        private bool IsNonZero(Vector3 v)
        {
            return !(v.X == 0f && v.Y == 0f && v.Z == 0f);
        }

        private Dictionary<BodyPart, CollisionInfo> GetCollisionPointsForBodyParts(Vector3 current, Vector3 destination)
        {
            float totalDistance = Vector3.Distance(current, destination);
            float yDistance = Math.Abs(destination.Y - current.Y);
            bool considerY = yDistance > totalDistance / 10;

            var result = new Dictionary<BodyPart, CollisionInfo>
            {
                { BodyPart.BottomMiddle, null },
                { BodyPart.Middle, null },
                { BodyPart.TopMiddle, null },
                { BodyPart.Top, null }
            };

            CheckBodyPartCollision(result, BodyPart.Top, current, destination, considerY);
            Thread.Sleep(BODY_PART_SCAN_DELAY_MS);
            CheckBodyPartCollision(result, BodyPart.TopMiddle, current, destination, considerY);
            Thread.Sleep(BODY_PART_SCAN_DELAY_MS);
            CheckBodyPartCollision(result, BodyPart.Middle, current, destination, considerY);
            Thread.Sleep(BODY_PART_SCAN_DELAY_MS);
            CheckBodyPartCollision(result, BodyPart.BottomMiddle, current, destination, considerY);

            return result;
        }

        private void CheckBodyPartCollision(Dictionary<BodyPart, CollisionInfo> map, BodyPart part, Vector3 current, Vector3 destination, bool considerY)
        {
            Vector3 offset = GetBodyPartOffsetVector(part);
            Vector3 src = new Vector3(current.X + offset.X, current.Y + offset.Y, current.Z + offset.Z);
            Vector3 dst = new Vector3(destination.X + offset.X, considerY ? destination.Y + offset.Y : src.Y, destination.Z + offset.Z);

            Vector3 collision = GetCollisionVector(src, dst);
            if (IsNonZero(collision))
            {
                float dist = Vector3.Distance(src, collision);
                if (dist < MAX_COLLISION_SENTINEL)
                {
                    map[part] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = offset,
                        CollisionBodyPart = part,
                        CollisionPoint = collision,
                        CollisionDistance = dist
                    };
                }
            }
        }

        private Vector3 GetBodyPartOffsetVector(BodyPart bodyPart)
        {
            switch (bodyPart)
            {
                case BodyPart.Bottom: return new Vector3(0, 0, 0);
                case BodyPart.BottomSemiMiddle: return new Vector3(0, 0.75f, 0);
                case BodyPart.BottomMiddle: return new Vector3(0, 1.5f, 0);
                case BodyPart.Middle: return new Vector3(0, 3, 0);
                case BodyPart.TopMiddle: return new Vector3(0, 4.5f, 0);
                case BodyPart.Top: return new Vector3(0, 6, 0);
                default: return new Vector3(-10000, -10000, -10000);
            }
        }
    }
}
