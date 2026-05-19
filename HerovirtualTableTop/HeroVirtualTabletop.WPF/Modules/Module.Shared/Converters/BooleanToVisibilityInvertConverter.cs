using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Module.Shared.Converters
{
    /// <summary>   Boolean to visibility invert converter. </summary>
    public class BooleanToVisibilityInvertConverter : IValueConverter
    {
        /// <summary>   Converts boolean to inverted visibility. </summary>
        /// <param name="value">        The value. </param>
        /// <param name="targetType">   Type of the target. </param>
        /// <param name="parameter">    The parameter. </param>
        /// <param name="culture">      The culture. </param>
        /// <returns>   The converted object. </returns>
        public object Convert(object value,
                              Type targetType,
                              object parameter,
                              System.Globalization.CultureInfo culture)
        {
            Visibility vRet = Visibility.Visible;
            if (value is bool)
            {
                if ((bool)value)
                {
                    if ((string)parameter != "invert") 
                        vRet = Visibility.Collapsed;
                    else
                        vRet = Visibility.Visible;
                }
                else
                {
                    if ((string)parameter != "invert")
                        vRet = Visibility.Visible;
                    else
                        vRet = Visibility.Collapsed;
                }
            }

            return vRet;
        }

        /// <summary>   Convert back. </summary>
        /// <exception cref="NotImplementedException">  Thrown when the requested operation is
        ///                                             unimplemented. </exception>
        /// <param name="value">        The value. </param>
        /// <param name="targetType">   Type of the target. </param>
        /// <param name="parameter">    The parameter. </param>
        /// <param name="culture">      The culture. </param>
        /// <returns>   The converted object. </returns>
        public object ConvertBack(object value,
                                  Type targetType,
                                  object parameter,
                                  System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
