using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Movements;
using System.Collections.Generic;

namespace HeroVTT.Movements
{
    public class MovementProcessor
    {
        public static IMemoryElementPosition IncrementPosition(MovementDirection direction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }

        public static List<IMemoryElementPosition> CalculateIncrementalPositions(MovementDirection direction, Vector3 facing, Vector3 pitch)
        {
            return new List<IMemoryElementPosition>();
        }

        public static IMemoryElementPosition IncrementPositionFromInstruction(MovementInstruction instruction, IMemoryElementPosition currentPosition, double distance)
        {
            return new Position();
        }

        public static Vector3 CalculateNewFacingPitch(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }

        public static bool ArePositionsBesideEachOther(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return false;
        }

        public static Vector3 CalculateTurn(IMemoryElementPosition currentPosition, IMemoryElementPosition destination)
        {
            return new Vector3();
        }

        public static Vector3 CalculateCameraDistance(IMemoryElementPosition cameraPosition, IMemoryElementPosition characterPosition)
        {
            return new Vector3();
        }
    }
}
