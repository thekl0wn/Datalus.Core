using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace Datalus.Core
{
    public abstract class Component : IComponent
    {
        protected Component()
        {
            // set runtime
            this.RuntimeID = RuntimeController.NextID();
        }

        public IEntity Entity { get; private set; } = null;
        public bool IsModified { get; private set; } = false;
        public int RuntimeID { get; } = -1;

        public virtual void Dispose()
        {

        }
        public virtual void OnError(string error) { }
        public virtual void OnPropertyChanged(PropertyInfo property, object value) { }
        public virtual bool OnPropertyChanging(PropertyInfo property, object old_value, object new_value)
        {
            // default
            return true;
        }
        public virtual void OnValidationFailed(PropertyInfo property, string message) { }

        internal protected void Modified(string property, object value)
        {
            // get the property
            var prop = this.GetType().GetProperty(property);
            if (prop == null) return;

            // modified
            this.Modified(prop, value);
        }
        internal protected void Modified(PropertyInfo property, object value)
        {
            // set the flag
            this.IsModified = true;

            // call function
            OnPropertyChanged(property, value);
        }
        internal protected bool Modifying(string property, object new_value)
        {
            // get the property
            var prop = this.GetType().GetProperty(property);
            if (prop == null) return true;

            // handle
            return Modifying(property, new_value);
        }
        internal protected bool Modifying(PropertyInfo property, object new_value)
        {
            // get current value (old)
            object old_value = property.GetValue(this);

            // check if the same
            if (old_value == null && new_value == null) return false;
            if (old_value == new_value) return false;

            // let inheritable function decide
            return OnPropertyChanging(property, old_value, new_value);
        }
        internal void SetEntity(IEntity entity)
        {
            // set it
            this.Entity = entity;
        }
    }
}
