using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Module.HeroVirtualTabletop.OptionGroups
{
    public class OptionListBox : ListBox
    {
        public ICharacterOption DefaultOption
        {
            get { return (ICharacterOption)GetValue(DefaultOptionProperty); }
            set { SetValue(DefaultOptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultOption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultOptionProperty =
            DependencyProperty.Register("DefaultOption", typeof(ICharacterOption), typeof(OptionListBox), new PropertyMetadata(null));

        public ICharacterOption ActiveOption
        {
            get { return (ICharacterOption)GetValue(ActiveOptionProperty); }
            set { SetValue(ActiveOptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveOption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveOptionProperty =
            DependencyProperty.Register("ActiveOption", typeof(ICharacterOption), typeof(OptionListBox), new PropertyMetadata(null));
    }
}
