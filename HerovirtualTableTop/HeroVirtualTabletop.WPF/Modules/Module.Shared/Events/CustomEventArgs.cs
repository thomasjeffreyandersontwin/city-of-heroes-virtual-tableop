using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Events
{
    public class CustomEventArgs<T> : EventArgs
    {
        public T Value { get; set; }
    }
}
