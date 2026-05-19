using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public interface IMemoryElementTargeter: IMemoryInstance
    {
        void Target();
        void Untarget();
    }
}
