using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.WPF.Library
{
    public class DuplicateKeyException : Exception
    {

        public string Key { get; private set; }
        public DuplicateKeyException(string key)
            : base("Attempted to insert duplicate key " + key + " in collection")
        {
            Key = key;
        }
    }
}
