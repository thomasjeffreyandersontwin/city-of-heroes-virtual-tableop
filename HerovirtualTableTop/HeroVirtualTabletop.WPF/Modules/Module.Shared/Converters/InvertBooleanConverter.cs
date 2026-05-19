using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Module.Shared.Converters
{
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bRet = false;
            bool bParam = parameter != null ? Boolean.Parse(parameter.ToString()) : true;
            if (value != null)
                bRet = ((bool)value ^ bParam);
            return bRet;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bParam = parameter != null ? Boolean.Parse(parameter.ToString()) : true;
            return ((bool)value ^ bParam);
        }
    }
}
