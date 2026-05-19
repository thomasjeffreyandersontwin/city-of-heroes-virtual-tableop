using Module.HeroVirtualTabletop.Library.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Module.HeroVirtualTabletop.Library.Converters
{
    public class AnimationTypeToAnimationIconTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string iconText = null;
            AnimationElementType animationType = (AnimationElementType)value;
            switch(animationType)
            {
                case AnimationElementType.Movement:
                    iconText = "\uf008";
                    break;
                case AnimationElementType.FX:
                    iconText = "\uf0d0";
                    break;
                case AnimationElementType.Sound:
                    iconText = "\uf001";
                    break;
                case AnimationElementType.Pause:
                    iconText = "\uf04c";
                    break;
                case AnimationElementType.Sequence:
                    iconText = "\uf126";
                    break;
                case AnimationElementType.Reference:
                    iconText = "\uf08e";
                    break;
                case AnimationElementType.LoadIdentity:
                    iconText = "\uf129";
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
