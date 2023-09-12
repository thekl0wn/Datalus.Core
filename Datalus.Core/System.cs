using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datalus.Core
{
    public interface IDatalusSystem : IDatalusObject
    {
        bool Update();
    }
    public interface IDatalusSystem<Tinterface, Tclass> : IDatalusSystem where Tinterface : IDatalusSystem where Tclass : DatalusSystem, Tinterface
    {
        IReadOnlyList<Tinterface> Components { get; }

        void Register(Tclass component);
        void Unregister(Tinterface component);
    }

    public abstract class DatalusSystem : IDatalusSystem
    {
        protected DatalusSystem()
        {
            // set runtime id
            this.RuntimeID = RuntimeController.GetNextID();
        }

        public int RuntimeID { get; private set; }

        public virtual void Dispose() { }
        public virtual bool Equals(IDatalusObject other)
        {
            return this.RuntimeID.Equals(other.RuntimeID);
        }
        public abstract bool Update();
    }
    public abstract class DatalusSystem<Tinterface, Tclass> : DatalusSystem, IDatalusSystem<Tinterface, Tclass> where Tinterface : IDatalusSystem where Tclass : DatalusSystem, Tinterface
    {
        protected DatalusSystem() : base() { }

        public IReadOnlyList<Tinterface> Components { get => _components; }
        private List<Tclass> _components = new List<Tclass>();

        public void Register(Tclass component)
        {
            // check
            if (component == null) return;
            if (_components.Contains(component)) return;

            // add
            _components.Add(component);
        }
        public void Unregister(Tinterface component)
        {
            // check
            if (component == null) return;
            
            // get index
            int index = _components.FindIndex(x => x.RuntimeID == component.RuntimeID);
            if (index == -1) return;

            // remove
            _components.RemoveAt(index);
        }
        public override bool Update()
        {
            // loop through components
            foreach(var component in this.Components)
            {
                // update each component
                if (!component.Update()) return false;
            }

            // default
            return true;
        }
    }
}
