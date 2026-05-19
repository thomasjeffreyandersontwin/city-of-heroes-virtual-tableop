using Module.HeroVirtualTabletop.Characters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Module.HeroVirtualTabletop.Library.Converters
{
    public class DistanceCounterTextColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var color = System.Windows.Media.Brushes.White;
            if (values.Length == 2 && !values.Any(v => v == DependencyProperty.UnsetValue))
            {
                float currentDistanceCount = (float)values[0];
                float currentDistanceLimit = (float)values[1];
                if (currentDistanceCount > 0 && currentDistanceLimit > 0)
                {
                    if (currentDistanceCount >= currentDistanceLimit)
                        color = System.Windows.Media.Brushes.Red;
                    else if (currentDistanceCount >= currentDistanceLimit / 2)
                        color = System.Windows.Media.Brushes.Yellow;
                }
            }
            return color;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
