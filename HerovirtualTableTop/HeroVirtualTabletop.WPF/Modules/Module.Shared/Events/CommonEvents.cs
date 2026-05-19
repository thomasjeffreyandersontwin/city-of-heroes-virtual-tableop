using Module.Shared.Error;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Events
{
    public class CommonEvents
    {
        public class ErrorOccuredEvent : PubSubEvent<ErrorItems> { }
        public class HideErrorEvent : PubSubEvent<ErrorItems> { }
    }
}
