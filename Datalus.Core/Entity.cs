using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace Datalus.Core
{
    public interface IDatalusEntity : IDatalusObject, ICanSave, IHasComponents
    {
        string EntityCode { get; }

        void AddComponent<Tinterface, Tclass>() where Tinterface : IDatalusComponent where Tclass : DatalusComponent, Tinterface, new();
        T GetComponent<T>() where T : IDatalusComponent;
        bool HasComponent<T>() where T : IDatalusComponent;
        void RemoveComponent<T>() where T : IDatalusComponent;

        event EventHandler<DatalusEntityChangedEventArgs> EntityModified;
    }
    public sealed class DatalusEntity : IDatalusEntity
    {
        public DatalusEntity()
        {
            // set runtime id
            this.RuntimeID = RuntimeController.GetNextID();
        }

        public IReadOnlyList<IDatalusComponent> Components { get => _components.Values.ToList(); }
        private Dictionary<Type, IDatalusComponent> _components = new Dictionary<Type, IDatalusComponent>();
        public string EntityCode { get; private set; }
        public bool IsModified { get; private set; } = false;
        public int RuntimeID { get; private set; }

        public void AddComponent<Tinterface, Tclass>() where Tinterface : IDatalusComponent where Tclass : DatalusComponent, Tinterface, new()
        {
            // check for it
            if (this.HasComponent<Tinterface>()) return;

            // create it
            var component = new Tclass();

            // set it up
            component.Entity = this;

            // subscribe
            component.PropertyChanged += OnComponentChanged;

            // add it
            _components.Add(typeof(Tinterface), component);
        }
        public void Dispose()
        {

        }
        public bool Equals(IDatalusObject other)
        {
            return this.RuntimeID.Equals(other.RuntimeID);
        }
        public bool Format()
        {

            // default
            return this.OnFormatted();
        }
        public T GetComponent<T>() where T : IDatalusComponent
        {
            // check
            if (!this.HasComponent<T>()) return default(T);

            // return
            return (T)_components[typeof(T)];
        }
        public bool HasComponent<T>() where T : IDatalusComponent
        {
            // check
            return _components.ContainsKey(typeof(T));
        }
        public void RemoveComponent<T>() where T : IDatalusComponent
        {
            // check
            if (!this.HasComponent<T>()) return;
            
            // remove
            _components.Remove(typeof(T));
        }
        public bool Save()
        {
            // validate by default
            return this.Save(true);
        }
        public bool Save(bool validate)
        {
            // validate
            if (validate) if (!this.Validate()) return false;

            // save


            // default
            return this.OnSaved();
        }
        public bool Validate()
        {

            // default
            return this.OnValidated();
        }

        private void OnComponentChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // modified
            OnModified(sender, new DatalusComponentChangedEventArgs(e.PropertyName));
        }
        private bool OnFormatted()
        {
            // default
            return true;
        }
        private void OnModified(object sender, DatalusComponentChangedEventArgs e)
        {
            // set modified flag
            if (!this.IsModified) this.IsModified = true;

            // check that sender is component
            if(sender is IDatalusComponent component)
            {
                EntityModified?.Invoke(this, new DatalusEntityChangedEventArgs(component, e.PropertyName));
            }
        }
        private bool OnSaved()
        {
            // drop modified flag
            this.IsModified = false;

            // default
            return true;
        }
        private bool OnValidated()
        {
            // default
            return true;
        }

        public event EventHandler<DatalusEntityChangedEventArgs> EntityModified;
    }
}
