using Module.HeroVirtualTabletop.Library.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Module.HeroVirtualTabletop.Library.Converters
{
    public class MovementDirectionToIconTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string iconText = null;
            MovementDirection movementDirection = (MovementDirection)value;
            switch (movementDirection)
            {
                case MovementDirection.Right:
                    iconText = "\xf18e";
                    break;
                case MovementDirection.Left:
                    iconText = "\xf190";
                    break;
                case MovementDirection.Forward:
                    iconText = "\xf01b";
                    break;
                case MovementDirection.Backward:
                    iconText = "\xf01a";
                    break;
                case MovementDirection.Upward:
                    iconText = "\xf0ee";
                    break;
                case MovementDirection.Downward:
                    iconText = "\xf0ed";
                    break;
                case MovementDirection.Still:
                    iconText = "\xf28e";
                    break;
            }
            return iconText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
