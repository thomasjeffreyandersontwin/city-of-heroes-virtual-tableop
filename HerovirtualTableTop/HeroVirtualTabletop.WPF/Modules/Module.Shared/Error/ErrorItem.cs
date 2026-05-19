using Module.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Error
{
    public class ErrorItem
    {
        public int ID { get; set; }
        public string FriendlyMessage { get; set; }
        public Exception Error { get; set; }
        public ErrorDisplayType ErrorDisplayType { get; set; }
    }

    public class ErrorItems
    {
        public string Caption { get; set; }
        public ObservableCollection<ErrorItem> Errors { get; set; }

        public ErrorItems()
        {
            Caption = Constants.APPLICATION_NAME;
            Errors = new ObservableCollection<ErrorItem>();
        }
    }
}
