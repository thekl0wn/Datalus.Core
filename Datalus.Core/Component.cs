using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Datalus.Core
{
    public interface IDatalusComponent : IDatalusObject, IHasEntity, ICanSave, INotifyPropertyChanged
    {
        bool Update();
    }
    public abstract class DatalusComponent : IDatalusComponent
    {
        protected DatalusComponent()
        {
            // set runtime id
            this.RuntimeID = RuntimeController.GetNextID();
        }

        public IDatalusEntity Entity { get; internal set; }
        public bool IsModified { get; private set; } = false;
        public int RuntimeID { get; private set; } = -1;

        public virtual void Dispose() { }
        public virtual bool Equals(IDatalusObject other)
        {
            return this.RuntimeID.Equals(other.RuntimeID);
        }
        public virtual bool Format()
        {
            // format


            // default
            return this.OnFormatted();
        }
        public virtual bool Save()
        {
            return this.Save(true);
        }
        public virtual bool Save(bool validate)
        {
            // validate
            if (validate) if (!this.Validate()) return false;

            // check if modified
            if (!this.IsModified) return true;

            // build sql & data
            var sql = new StringBuilder("select");
            int count = 0;
            var data = new Dictionary<string, object>();

            // loop through properties
            foreach(var property in this.GetType().GetProperties())
            {
                // get / check savable attribute

                // sql
                if (count > 0) sql.AppendLine(", ");
                sql.Append(property.Name);

                // data
                data.Add(property.Name, property.GetValue(this));

                // increment the counter
                count++;
            }

            // save
            try
            {

            }
            catch(Exception ex)
            {
                return this.OnError(ex);
            }

            // drop modified flag
            this.IsModified = false;

            // default
            return true;
        }
        public virtual bool Update()
        {
            // default
            return true;
        }
        public virtual bool Validate()
        {
            // format
            if (!this.Format()) return false;

            // validate


            // default
            return this.OnValidated();
        }

        protected bool OnError(string message)
        {

            // default
            return false;
        }
        protected bool OnError(Exception ex)
        {
            return this.OnError(ex.Message);
        }
        protected bool OnFormatted()
        {
            // default
            return true;
        }
        protected void OnModified(string property)
        {
            // set modified flag
            if (!this.IsModified) this.IsModified = true;

            // event
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        protected bool OnSaved()
        {
            // drop modified flag
            this.IsModified = false;

            // default
            return true;
        }
        protected bool OnValidated()
        {
            // default
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public interface IListComponent : IDatalusComponent
    {
        IReadOnlyList<IDatalusEntity> Entities { get; }

        void AddEntity(IDatalusEntity entity);
        IDatalusEntity GetEntity(string entity_code);
        bool HasEntity(IDatalusEntity entity);
        bool HasEntity(string entity_code);
        void RemoveEntity(IDatalusEntity entity);
    }
    public abstract class ListComponent : DatalusComponent, IListComponent
    {
        protected ListComponent() : base() { }

        public IReadOnlyList<IDatalusEntity> Entities { get => _entities; }
        private List<IDatalusEntity> _entities = new List<IDatalusEntity>();

        public void AddEntity(IDatalusEntity entity)
        {
            // check
            if (this.HasEntity(entity)) return;

            // add
            _entities.Add(entity);
        }
        public IDatalusEntity GetEntity(string entity_code)
        {
            // check
            if (!this.HasEntity(entity_code)) return null;
            else return _entities.Find(x => x.EntityCode == entity_code);
        }
        public bool HasEntity(IDatalusEntity entity)
        {
            return _entities.Contains(entity);
        }
        public bool HasEntity(string entity_code)
        {
            return _entities.Exists(x => x.EntityCode == entity_code);
        }
        public void RemoveEntity(IDatalusEntity entity)
        {
            // check
            if (!this.HasEntity(entity)) return;

            // remove
            _entities.Remove(entity);
        }
        public override bool Save(bool validate)
        {
            // validate
            if (validate) if (!this.Validate()) return false;

            // check if modified
            if (!this.IsModified) return true;

            // save


            // default
            return this.OnSaved();
        }
    }
}
