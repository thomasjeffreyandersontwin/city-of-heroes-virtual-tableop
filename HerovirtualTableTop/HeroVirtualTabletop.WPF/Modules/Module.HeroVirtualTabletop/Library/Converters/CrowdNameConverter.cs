using Module.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Module.HeroVirtualTabletop.Library.Converters
{
    public class CrowdNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string crowdName = value.ToString();
            if (crowdName == Constants.ALL_CHARACTER_CROWD_NAME)
                crowdName = Constants.NO_CROWD_CROWD_NAME;
            return crowdName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
