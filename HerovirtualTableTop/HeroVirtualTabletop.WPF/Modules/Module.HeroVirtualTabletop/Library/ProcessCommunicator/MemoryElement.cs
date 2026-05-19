using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public class MemoryElement : MemoryInstance, IMemoryElement
    {
        public MemoryElement(bool initFromCurrentTarget = true) : base(initFromCurrentTarget) { }

        public string Label
        {
            get
            {
                try
                {
                    return GetAttributeAsString(12740, Encoding.UTF8);
                }
                catch
                {
                    return string.Empty;
                }
            }
            set
            {
                try
                {
                    SetTargetAttribute(12740, value, Encoding.UTF8);
                }
                catch { }
            }
        }

        public string Name {
            get
            {
                string label = Label;
                int start = 7;
                int end = 1 + label.IndexOf("]", start);
                return label.Substring(start, end - start);
            }
        }
    
        public IMemoryElementPosition Position
        {
            get;
            set;
        }

        public void Target()
        {
            WriteCurrentTargetToGameMemory();
        }

        public void Untarget()
        {
            WriteToMemory((uint)0);
        }
    }
}
