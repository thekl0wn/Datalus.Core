using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datalus.Core
{
    public class DatalusComponentChangedEventArgs : EventArgs
    {
        public DatalusComponentChangedEventArgs(string property_name)
        {
            this.PropertyName = property_name;
        }

        public string PropertyName { get; }
    }

    public class DatalusEntityChangedEventArgs : EventArgs
    {
        public DatalusEntityChangedEventArgs(IDatalusComponent component, string property_name)
        {
            this.Component = component;
            this.PropertyName = property_name;
        }

        public IDatalusComponent Component { get; }
        public string PropertyName { get; }
    }
}
