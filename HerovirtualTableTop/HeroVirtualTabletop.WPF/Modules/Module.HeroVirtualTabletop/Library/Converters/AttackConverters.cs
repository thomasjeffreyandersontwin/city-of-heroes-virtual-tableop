using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using System.Windows;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.HeroVirtualTabletop.Library.Converters
{
    /*
         fa-ambulance [&#xf0f9;] - dying
         fa-power-off [&#xf011;] - dead
         fa-bed [&#xf236;] - unconcious
         fa-bullseye [&#xf140;] - defend?
         fa-frown-o [&#xf119;] - stunned
         fa-bolt [&#xf0e7;] - attack
         fa-bullseye [&#xf140;] - defend
    */
    public class ActiveAttackEffectToAnimationIconTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string iconText = null;
            var activeAttackEffectOption = (AttackEffectOption)value;                                   
            //Dictionary<Attack, AttackConfiguration> attackConfigMap = value as Dictionary<Attack, AttackConfiguration>;
            //if(attackConfigMap.Any(ac => ac.Value.AttackEffectOption == AttackEffectOption.Stunned))
            //    iconText = "\uf119";
            //if(attackConfigMap.Any(ac => ac.Value.AttackEffectOption == AttackEffectOption.Unconcious))
            //    iconText = "\uf236";
            //if (attackConfigMap.Any(ac => ac.Value.AttackEffectOption == AttackEffectOption.Stunned))
            //    iconText = "\uf119";
            //if (attackConfigMap.Any(ac => ac.Value.AttackEffectOption == AttackEffectOption.Unconcious))
            //    iconText = "\uf236";
            switch (activeAttackEffectOption)
            {
                case Enumerations.AttackEffectOption.Stunned:
                    iconText = "\uf119";
                    break;
                case Enumerations.AttackEffectOption.Unconcious:
                    iconText = "\uf236";
                    break;
                case Enumerations.AttackEffectOption.Dying:
                    iconText = "\uf0f9";
                    break;
                case Enumerations.AttackEffectOption.Dead:
                    iconText = "\uf011";
                    break;
            }
            return iconText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /*
         fa-bolt [&#xf0e7;] - attack
         fa-bullseye [&#xf140;] - defend
    */
    public class ActiveAttackModeToAnimationIconTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string iconText = null;
            AttackMode activeAttackMode = (AttackMode)value;
            switch (activeAttackMode)
            {
                case Enumerations.AttackMode.Attack:
                    iconText = "\uf0e7";
                    break;
                case Enumerations.AttackMode.Defend:
                    iconText = "\uf140";
                    break;
            }
            return iconText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class AttackConfigMapToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = Visibility.Collapsed;
            Dictionary<Attack, AttackConfiguration> attackConfigMap = value as Dictionary<Attack, AttackConfiguration>;
            bool attackModeExists = attackConfigMap.Any(ac => ac.Value.AttackMode != AttackMode.None);
            if (attackModeExists)
                visibility = Visibility.Visible;
            return visibility;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ActiveAttackEffectToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = Visibility.Collapsed;
            AttackEffectOption activeAttackEffect = (AttackEffectOption)value;
            if (activeAttackEffect != Enumerations.AttackEffectOption.None)
                visibility = Visibility.Visible;
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ActiveAttackResultsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = Visibility.Collapsed;
            AttackResult activeAttackResult = (AttackResult)value;
            if (activeAttackResult != null)
                visibility = Visibility.Visible;
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}