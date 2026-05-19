using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Markup;
using Module.HeroVirtualTabletop.Characters;

namespace Module.HeroVirtualTabletop.Library.Converters
{
    public class characterComparer : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
               object parameter, System.Globalization.CultureInfo culture)
        {
            bool retV = true;
            if (values.Count() > 0)
            {
                foreach (object o in values)
                {
                    if (! (o is Character))
                    {
                        retV = false;
                        break;
                    }
                }
                if (retV)
                {
                    foreach (Character c in values)
                    {
                        if (c != (Character)values[0])
                        {
                            retV = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                retV = false;
            }

            return retV;
        }
        public object[] ConvertBack(object value, Type[] targetTypes,
               object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }
}
