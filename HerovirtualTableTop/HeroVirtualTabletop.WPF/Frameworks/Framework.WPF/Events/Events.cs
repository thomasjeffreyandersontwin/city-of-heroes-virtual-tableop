using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using System.Windows;

namespace Framework.WPF.Events
{
    public class PopupOpened : PubSubEvent<Window> { }
}
