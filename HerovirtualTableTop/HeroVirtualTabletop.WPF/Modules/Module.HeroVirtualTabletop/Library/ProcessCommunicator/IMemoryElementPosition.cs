using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public interface IMemoryElementPosition: IMemoryInstance
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        float[,] RotationMatrix { get; set; }
        IMemoryElementPosition Clone(bool preserveTargetPointer = true, uint oldTargetPointer = 0);
        bool IsWithin(float maxDistance, IMemoryElementPosition From, out float calculatedDistance);
    }
}
