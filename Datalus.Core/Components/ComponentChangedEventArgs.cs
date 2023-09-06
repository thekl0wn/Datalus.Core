using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datalus.Core
{
    public sealed class ComponentChangedEventArgs : EventArgs
    {
        internal ComponentChangedEventArgs(IComponent component, string property, object old_value, object new_value)
        {
            this.Component = component;
            this.PropertyName = property;
            this.OldValue = old_value;
            this.NewValue = new_value;
        }

        public IComponent Component { get; }
        public string PropertyName { get; }
        public object OldValue { get; }
        public object NewValue { get; }
    }
}
