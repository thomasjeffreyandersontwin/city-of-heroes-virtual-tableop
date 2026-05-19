using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Module.HeroVirtualTabletop.Library.Utility;
namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    class MouseElement : ICharacterElement
    {
        public String HoveredInfo="";
        public MouseElement()
        {
            HoveredInfo = IconInteractionUtility.GeInfoFromNpcMouseIsHoveringOver();
        }
        public string Label
        {
            get
            {
                if (HoveredInfo != "")
                {
                    int start = 7;
                    int end = 1 + HoveredInfo.IndexOf("]", start);
                    return HoveredInfo.Substring(start, end - start);
                }
                return "";
            }
        }

        public string Name
        {
            get
            {
                if (HoveredInfo != "")
                {
                    int nameEnd = HoveredInfo.IndexOf("[", 7);
                    string name = HoveredInfo.Substring(7, nameEnd - 7).Trim();
                    if (name.EndsWith("] X:"))
                    {
                        name = name.Substring(0, name.LastIndexOf("]"));
                    }
                    return name;
                }
                return "";
            }
        }

        public Vector3 Position
        {
            get
            {
                string mouseXYZInfo = IconInteractionUtility.GetMouseXYZString();
                Vector3 vector3 = new Vector3();
                float f;
                int xStart = mouseXYZInfo.IndexOf("[");
                int xEnd = mouseXYZInfo.IndexOf("]");
                string xStr = mouseXYZInfo.Substring(xStart + 1, xEnd - xStart - 1);
                if (float.TryParse(xStr, out f))
                    vector3.X = f;
                int yStart = mouseXYZInfo.IndexOf("[", xEnd);
                int yEnd = mouseXYZInfo.IndexOf("]", yStart);
                string yStr = mouseXYZInfo.Substring(yStart + 1, yEnd - yStart - 1);
                if (float.TryParse(yStr, out f))
                    vector3.Y = f;
                int zStart = mouseXYZInfo.IndexOf("[", yEnd);
                int zEnd = mouseXYZInfo.IndexOf("]", zStart);
                string zStr = mouseXYZInfo.Substring(zStart + 1, zEnd - zStart - 1);
                if (float.TryParse(zStr, out f))
                    vector3.Z = f;
                return vector3;
            }
        }
    }
}
