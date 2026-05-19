using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared
{
    public class RegionNames
    {
        #region Main Regions

        public string MainRegion { get; private set; }
        public string BusyRegion { get; private set; }
        public string NavigationBarRegion { get; private set; }

        #endregion

        #region HeroVirtualTabletop Regions

        public string HeroVirtualTabletopRegion { get; private set; }

        #endregion

        static RegionNames _instance;
        public static RegionNames Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new RegionNames();
                return _instance;
            }
        }

        RegionNames()
        {
            MainRegion = Constants.MAIN_REGION;
            BusyRegion = Constants.BUSY_REGION;
            NavigationBarRegion = Constants.NAVIGATION_BAR_REGION;
            HeroVirtualTabletopRegion = Constants.HEROVIRTUALTABLETOP_REGION;
        }
    }
}
