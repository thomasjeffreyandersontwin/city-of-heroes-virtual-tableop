using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class CollisionEngine
    {
        public List<CollisionInfo> FindObstructingObjects(Character attacker, Character target, List<Character> otherCharacters)
        {
            return FindObstructingObjects(attacker.CurrentPositionVector, target.CurrentPositionVector, otherCharacters);
        }

        private List<CollisionInfo> FindObstructingObjects(Vector3 sourcePositionVector, Vector3 targetPositionVector, List<Character> otherCharacters)
        {
            List<CollisionInfo> collisions = new List<CollisionInfo>();
            Vector3 sourceFacingTargetVector = targetPositionVector - sourcePositionVector;
            Vector3 targetFacingSourceVector = sourcePositionVector - targetPositionVector;
            if (sourceFacingTargetVector == targetFacingSourceVector)
                return null;
            if(sourceFacingTargetVector != Vector3.Zero)
                sourceFacingTargetVector.Normalize();
            if(targetFacingSourceVector != Vector3.Zero)
                targetFacingSourceVector.Normalize();
            // Calculate points A and B to the left and right of source
            Vector3 pointA = Helper.GetAdjacentPoint(sourcePositionVector, sourceFacingTargetVector, true);
            Vector3 pointB = Helper.GetAdjacentPoint(sourcePositionVector, sourceFacingTargetVector, false);
            // Calculate points C and D to left and right of target
            Vector3 pointC = Helper.GetAdjacentPoint(targetPositionVector, targetFacingSourceVector, false);
            Vector3 pointD = Helper.GetAdjacentPoint(targetPositionVector, targetFacingSourceVector, true);
            // Now we have four co-ordinates of rectangle ABCD.  Need to check if any of the other characters falls within this rectangular region
            List<Character> obstructingCharacters = new List<Characters.Character>();
            try
            {
                foreach (Character otherCharacter in otherCharacters)
                {
                    if (Helper.IsPointWithinQuadraticRegion(pointA, pointB, pointC, pointD, otherCharacter.CurrentPositionVector))
                    {
                        obstructingCharacters.Add(otherCharacter);
                    }
                }
            }
            catch
            {
                FileLogManager.ForceLog(string.Format("Boundary case found for obstacle collision. Source vector {0}, Target vector {1}, other characters {2}", sourcePositionVector, targetPositionVector, string.Join(", ", otherCharacters.Select(c => c.Name))));
            }

            Dictionary<BodyPart, bool> bodyPartMap = new Dictionary<BodyPart, bool>();
            bodyPartMap.Add(BodyPart.BottomMiddle, true);
            bodyPartMap.Add(BodyPart.Middle, true);
            bodyPartMap.Add(BodyPart.TopMiddle, true);
            bodyPartMap.Add(BodyPart.Top, true);

            Dictionary<BodyPart, CollisionInfo> bodyPartCollisionMap = GetCollisionPointsForBodyParts(sourcePositionVector, targetPositionVector, bodyPartMap);
            bool hasCollision = false;
            foreach (BodyPart key in bodyPartCollisionMap.Keys)
            {
                if (bodyPartCollisionMap[key] != null)
                {
                    hasCollision = true;
                    break;
                }

            }
            if (hasCollision)
            {
                var collisionDistance = bodyPartCollisionMap.Values.Min(d => d != null ? d.CollisionDistance : 10000f);
                collisions.Add(new CollisionInfo { CollidingObject = "WALL", CollisionDistance = collisionDistance});
            }
            foreach (Character obsChar in obstructingCharacters)
            {
                float obsDist = Vector3.Distance(sourcePositionVector, obsChar.CurrentPositionVector);
                collisions.Add(new CollisionInfo { CollidingObject = obsChar, CollisionDistance = obsDist });
            }

            return collisions;
        }

        public List<CollisionInfo> CalculateKnockbackObstructions(Character attacker, Character target, int distance, List<Character> otherCharacters)
        {
            if (target.CurrentPositionVector == attacker.CurrentPositionVector)
                return null;
            float knockbackDistance = distance * 8f;
            Vector3 directionVector = target.CurrentPositionVector - attacker.CurrentPositionVector;
            directionVector.Normalize();
            var destX = target.CurrentPositionVector.X + directionVector.X * knockbackDistance;
            var destY = target.CurrentPositionVector.Y + directionVector.Y * knockbackDistance;
            var destZ = target.CurrentPositionVector.Z + directionVector.Z * knockbackDistance;
            Vector3 destVector = new Vector3(destX, destY, destZ);
            return FindObstructingObjects(target.CurrentPositionVector, destVector, otherCharacters);
        }

        private Vector3 GetCollisionVector(Vector3 sourceVector, Vector3 destVector)
        {
            float distance = Vector3.Distance(sourceVector, destVector);
            Vector3 collisionVector = new Vector3(0, 0, 0);
            int numRetry = 3; // try 3 times
            while (numRetry > 0)
            {
                try
                {
                    var collisionInfo = IconInteractionUtility.GetCollisionInfo(sourceVector.X, sourceVector.Y, sourceVector.Z, destVector.X, destVector.Y, destVector.Z);
                    collisionVector = Helper.GetCollisionVector(collisionInfo);
                    float collisionDistance = Vector3.Distance(sourceVector, collisionVector);
                    if (!HasCollision(collisionVector) || collisionDistance <= distance) // proper collision
                        break;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(500);
                    numRetry--;
                }
            }
            if (float.IsNaN(collisionVector.X) || float.IsNaN(collisionVector.Y) || float.IsNaN(collisionVector.Z))
                collisionVector = new Vector3(0, 0, 0);
            return collisionVector;
        }

        private bool HasCollision(Vector3 collisionVector)
        {
            return !(collisionVector.X == 0f && collisionVector.Y == 0f && collisionVector.Z == 0f);
        }

        private Dictionary<BodyPart, CollisionInfo> GetCollisionPointsForBodyParts(Vector3 currentPositionVector, Vector3 destinationVector, Dictionary<BodyPart, bool> bodyPartMap)
        {
            float totalDistance = Vector3.Distance(currentPositionVector, destinationVector);
            float yDistance = Math.Abs(destinationVector.Y - currentPositionVector.Y);
            bool considerY = yDistance > totalDistance / 10;

            Dictionary<BodyPart, CollisionInfo> bodyPartCollisionMap = new Dictionary<BodyPart, CollisionInfo>();
            bodyPartCollisionMap.Add(BodyPart.BottomMiddle, null);
            bodyPartCollisionMap.Add(BodyPart.Middle, null);
            bodyPartCollisionMap.Add(BodyPart.TopMiddle, null);
            bodyPartCollisionMap.Add(BodyPart.Top, null);

            Vector3 topOffsetVector = GetBodyPartOffsetVector(BodyPart.Top);
            Vector3 currentTopVector = new Vector3(currentPositionVector.X + topOffsetVector.X, currentPositionVector.Y + topOffsetVector.Y, currentPositionVector.Z + topOffsetVector.Z);
            Vector3 destinationTopVector = new Vector3(destinationVector.X + topOffsetVector.X, considerY ? destinationVector.Y + topOffsetVector.Y : currentTopVector.Y, destinationVector.Z + topOffsetVector.Z);
            Vector3 collisionVectorTop = GetCollisionVector(currentTopVector, destinationTopVector);

            Thread.Sleep(5);

            Vector3 topMiddleOffsetVector = GetBodyPartOffsetVector(BodyPart.TopMiddle);
            Vector3 currentTopMiddleVector = new Vector3(currentPositionVector.X + topMiddleOffsetVector.X, currentPositionVector.Y + topMiddleOffsetVector.Y, currentPositionVector.Z + topMiddleOffsetVector.Z);
            Vector3 destinationTopMiddleVector = new Vector3(destinationVector.X + topMiddleOffsetVector.X, considerY ? destinationVector.Y + topMiddleOffsetVector.Y : currentTopMiddleVector.Y, destinationVector.Z + topMiddleOffsetVector.Z);
            Vector3 collisionVectorTopMiddle = GetCollisionVector(currentTopMiddleVector, destinationTopMiddleVector);

            Thread.Sleep(5);

            Vector3 middleOffsetVector = GetBodyPartOffsetVector(BodyPart.Middle);
            Vector3 currentMiddleVector = new Vector3(currentPositionVector.X + middleOffsetVector.X, currentPositionVector.Y + middleOffsetVector.Y, currentPositionVector.Z + middleOffsetVector.Z);
            Vector3 destinationMiddleVector = new Vector3(destinationVector.X + middleOffsetVector.X, considerY ? destinationVector.Y + middleOffsetVector.Y : currentMiddleVector.Y, destinationVector.Z + middleOffsetVector.Z);
            Vector3 collisionVectorMiddle = GetCollisionVector(currentMiddleVector, destinationMiddleVector);

            Thread.Sleep(5);

            Vector3 bottomMiddleOffsetVector = GetBodyPartOffsetVector(BodyPart.BottomMiddle);
            Vector3 currentBottomMiddleVector = new Vector3(currentPositionVector.X + bottomMiddleOffsetVector.X, currentPositionVector.Y + bottomMiddleOffsetVector.Y, currentPositionVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 destinationBottomMiddleVector = new Vector3(destinationVector.X + bottomMiddleOffsetVector.X, considerY ? destinationVector.Y + currentBottomMiddleVector.Y : currentBottomMiddleVector.Y, destinationVector.Z + bottomMiddleOffsetVector.Z);
            Vector3 collisionVectorBottomMiddle = GetCollisionVector(currentBottomMiddleVector, destinationBottomMiddleVector);

            float distanceFromCollisionPoint = 10000f;
            
            if (HasCollision(collisionVectorBottomMiddle) && bodyPartMap[BodyPart.BottomMiddle])
            {
                float collDist = Vector3.Distance(currentBottomMiddleVector, collisionVectorBottomMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.BottomMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = bottomMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.BottomMiddle,
                        CollisionPoint = collisionVectorBottomMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.BottomMiddle] = false;
            }
            if (HasCollision(collisionVectorMiddle) && bodyPartMap[BodyPart.Middle])
            {
                float collDist = Vector3.Distance(currentMiddleVector, collisionVectorMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Middle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = middleOffsetVector,
                        CollisionBodyPart = BodyPart.Middle,
                        CollisionPoint = collisionVectorMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.Middle] = false;
            }
            if (HasCollision(collisionVectorTopMiddle) && bodyPartMap[BodyPart.TopMiddle])
            {
                float collDist = Vector3.Distance(currentTopMiddleVector, collisionVectorTopMiddle);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.TopMiddle] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = topMiddleOffsetVector,
                        CollisionBodyPart = BodyPart.TopMiddle,
                        CollisionPoint = collisionVectorTopMiddle,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.TopMiddle] = false;
            }
            if (HasCollision(collisionVectorTop) && bodyPartMap[BodyPart.Top])
            {
                float collDist = Vector3.Distance(currentTopVector, collisionVectorTop);
                if (collDist < distanceFromCollisionPoint)
                {
                    bodyPartCollisionMap[BodyPart.Top] = new CollisionInfo
                    {
                        BodyCollisionOffsetVector = topOffsetVector,
                        CollisionBodyPart = BodyPart.Top,
                        CollisionPoint = collisionVectorTop,
                        CollisionDistance = collDist
                    };
                }
            }
            else
            {
                bodyPartMap[BodyPart.Top] = false;
            }

            return bodyPartCollisionMap;
        }

        private Vector3 GetBodyPartOffsetVector(BodyPart bodyPart)
        {
            Vector3 bodyPartOffsetVector = new Vector3(-10000, -10000, -10000);
            switch (bodyPart)
            {
                case BodyPart.Bottom:
                    bodyPartOffsetVector = new Vector3(0, 0, 0);
                    break;
                case BodyPart.BottomSemiMiddle:
                    bodyPartOffsetVector = new Vector3(0, 0.75f, 0);
                    break;
                case BodyPart.BottomMiddle:
                    bodyPartOffsetVector = new Vector3(0, 1.5f, 0);
                    break;
                case BodyPart.Middle:
                    bodyPartOffsetVector = new Vector3(0, 3, 0);
                    break;
                case BodyPart.TopMiddle:
                    bodyPartOffsetVector = new Vector3(0, 4.5f, 0);
                    break;
                case BodyPart.Top:
                    bodyPartOffsetVector = new Vector3(0, 6, 0);
                    break;
            }

            return bodyPartOffsetVector;
        }
    }
}
