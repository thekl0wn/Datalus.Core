using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Datalus.Core
{
    public interface IComponentSubscriber
    {
        void OnComponentChanged(string property_name, object new_value);
    }
    public interface IComponentSubscriber<T> : IComponentSubscriber where T : Component
    {

    }
    public interface IEntitySubscriber
    {
        void OnEntityComponentChanged(IComponent component, string property_name, object new_value);
        void OnEntityError(string error);
    }
    public interface IFormattable
    {
        bool FormatData();
    }
    public interface IHasEntity
    {
        IEntity Entity { get; }
    }
    public interface IModifiable
    {
        bool IsModified { get; }
    }
    public interface IRuntime
    {
        int RuntimeID { get; }
    }
    public interface ISavable : IValidatable
    {
        bool SaveData();
        bool SaveData(bool validate);
    }
    public interface IValidatable : IFormattable
    {
        bool ValidateData();
    }

    public interface IEntity : IModifiable, IRuntime, ISavable
    {
        IReadOnlyList<IComponent> Components { get; }
        int EntityID { get; }
        bool IsNew { get; }
        IEntityNotifier Notifier { get; }
        
        void AddComponent<T>() where T : Component, new();
        void AddComponent<T>(T component) where T : Component;
        T GetComponent<T>() where T : Component;
        void RemoveComponent<T>() where T : Component;
    }
    public sealed class Entity : IDisposable, IEntity
    {
        /// <summary>
        /// Declared as internal. Only a manager can create an entity.
        /// </summary>
        internal Entity()
        {
            // set runtime id
            _runtime_id = RuntimeController.NextID();

            // create new component listing
            _components = new List<Component>();

            // create notifier object
            _notifier = new EntityNotifier(this);
        }

        /// <summary>
        /// List of components for the entity.
        /// </summary>
        public IReadOnlyList<IComponent> Components { get => _components; }
        internal List<Component> _components { get; set; }

        /// <summary>
        /// Entity ID. Unique identifier for the entity. Set to -1 when the entity is newly created.
        /// </summary>
        public int EntityID { get => _entity_id; }
        private int _entity_id = -1;

        /// <summary>
        /// Flag denoting whether or not the entity has been modified (or is new) since the last save/refresh.
        /// </summary>
        public bool IsModified
        {
            get
            {
                // check if new
                if (this.IsNew) return true;
                else return _components.Exists(x => x.IsModified);
            }
        }

        /// <summary>
        /// Flag denoting whether or not the entity is newly created. (has not been saved)
        /// </summary>
        public bool IsNew
        {
            get
            {
                if (_entity_id == -1) return true;
                else return false;
            }
        }

        /// <summary>
        /// Notification provider
        /// </summary>
        public IEntityNotifier Notifier { get => _notifier; }
        private EntityNotifier _notifier;

        /// <summary>
        /// Runtime ID. Set at runtime as a unique identifier across the system.
        /// </summary>
        public int RuntimeID { get => _runtime_id; }
        private int _runtime_id;

        /// <summary>
        /// Add a component to the entity.
        /// </summary>
        /// <typeparam name="T">type of component to add</typeparam>
        public void AddComponent<T>() where T : Component, new()
        {
            // add component as a new instance
            AddComponent<T>(new T());
        }

        /// <summary>
        /// Add a component to the entity.
        /// </summary>
        /// <typeparam name="T">type of component to add</typeparam>
        /// <param name="component">the component to add</param>
        public void AddComponent<T>(T component) where T : Component
        {
            // check
            if (_components.Exists(x => x.GetType() == typeof(T))) return;

            // setup
            component._entity = this;

            // subscribe to events
            component.PropertyChanged += OnComponentChanged;

            // component has been added
            _notifier.OnComponentAdded(component);
        }

        /// <summary>
        /// Call when entity incurs an error.
        /// </summary>
        /// <param name="error">error message</param>
        internal bool Error(string error)
        {
            // raise event
            _notifier.OnError(error);

            // always false
            return false;
        }

        /// <summary>
        /// Call when entity incurs an exception.
        /// </summary>
        /// <param name="ex">the exception incurred</param>
        internal bool Error(Exception ex)
        {
            // raise event
            _notifier.OnError(ex);

            // always false
            return false;
        }

        /// <summary>
        /// Call from component when an error is incurred.
        /// </summary>
        /// <param name="component">component where the error occurred</param>
        /// <param name="error">error message</param>
        /// <returns></returns>
        internal bool Error(Component component, string error)
        {
            return Error($"{component.GetType().Name}\n\n{error}");
        }

        /// <summary>
        /// Call from component when an exception is incurred.
        /// </summary>
        /// <param name="component">component where the error occurred</param>
        /// <param name="ex">the exceptoin</param>
        internal bool Error(Component component, Exception ex)
        {
            return Error(component, ex.Message);
        }

        /// <summary>
        /// Format's the entity's data.
        /// </summary>
        /// <returns>true on success. false on fail.</returns>
        public bool FormatData()
        {
            // format


            // raise event
            _notifier.OnFormatted();

            // default
            return true;
        }

        /// <summary>
        /// Get the component of the passed generic type (component) for the entity.
        /// </summary>
        /// <typeparam name="T">component type to get</typeparam>
        public T GetComponent<T>() where T : Component
        {
            // check for the component
            if (!_components.Exists(x => x.GetType() == typeof(T))) return default(T);

            // return the component
            return (T)_components.Find(x => x.GetType() == typeof(T));
        }

        /// <summary>
        /// Handler for a component's PropertyChanged event.
        /// </summary>
        /// <param name="sender">the containing component</param>
        /// <param name="e">the values of the property</param>
        private void OnComponentChanged(object sender, PropertyChangedEventArgs e)
        {
            // sender = component that was changed
            // e = property data for property that was changed
            //      Property = property that was changed
            //      OldValue = old value of property
            //      NewValue = new value of property
            if (sender is Component component) _notifier.OnComponentChanged(component, e);
        }

        /// <summary>
        /// Dispose the entity.
        /// </summary>
        public void Dispose()
        {
            // dispose the notifier
            _notifier.Dispose();

            // remove components
            while (_components.Count > 0)
            {
                // remove the component
                RemoveComponent(_components[0]);
            }
        }

        /// <summary>
        /// Sets the EntityID & loads data from the database
        /// </summary>
        /// <param name="id">entity ID</param>
        internal bool LoadData(int id)
        {
            // set id
            _entity_id = id;

            // load
            if (!LoadEntity()) return false;
            if (!LoadComponents()) return false;

            // default
            return true;
        }
        private bool LoadEntity()
        {
            // default
            return true;
        }
        private bool LoadComponents()
        {
            // list of component classnames to add
            var classes = new List<string>();

            // sql
            var sql = $"select [Class] from [ecp].[EntityComponent] EC left outer join [ecp].[Component] C on C.[ComponentID] = EC.[ComponentID] where EC.[EntityID] = {_entity_id}";

            // load from database
            try
            {
                using (var adapter = new SqlDataAdapter(sql, DatabaseController.GetConnectionString()))
                {
                    using (var table = new DataTable())
                    {
                        // fill
                        adapter.Fill(table);

                        // loop through rows
                        foreach(DataRow row in table.Rows)
                        {
                            classes.Add(row["Class"].ToString());
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                return this.Error(ex);
            }

            // loop through the classes
            foreach(var c in classes)
            {
                // get/check type
                var type = Type.GetType(c);
                if (type == null) return this.Error($"Could not resolve type for {c}.");

                // create instance
                var component = (Component)Activator.CreateInstance(type);
                if (component == null) return this.Error($"Could not create instance of the component type {type.Name}.");

                // load the component
                if (!component.LoadData(this)) return false;

                // add the component
                this.AddComponent(component);
            }

            // default
            return true;
        }

        /// <summary>
        /// Remove component from entity
        /// </summary>
        /// <typeparam name="T">type of component to remove</typeparam>
        public void RemoveComponent<T>() where T : Component
        {
            // get the component
            var component = _components.Find(x => x.GetType() == typeof(T));

            // remove the component
            RemoveComponent(component);
        }
        private void RemoveComponent<T>(T component) where T : Component
        {
            // check
            if (component == null) return;
            if (!_components.Contains(component)) return;

            // unsubscribe
            component.PropertyChanged -= OnComponentChanged;

            // get rid of the component
            _components.Remove(component);

            // raise event
            _notifier.OnComponentRemoved(component);

            // dispose
            component.Dispose();
        }

        /// <summary>
        /// Saves the entity. By default, validates it first.
        /// </summary>
        /// <returns>true on success. false on fail.</returns>
        public bool SaveData()
        {
            return SaveData(true);
        }

        /// <summary>
        /// Saves the entity.
        /// </summary>
        /// <param name="validate">flag for whether or not to validate before saving.</param>
        /// <returns>true on success. false on fail.</returns>
        public bool SaveData(bool validate)
        {
            // validation
            if (validate) if (!ValidateData()) return false;

            // save
            if (!SaveEntity()) return false;
            if (!SaveComponents(!validate)) return false;

            // raise saved event
            _notifier.OnSaved();

            // default
            return true;
        }
        private bool SaveComponents(bool validate = true)
        {
            // build list of component ids
            var component_ids = new List<int>();
            foreach (var component in _components) component_ids.Add(component.ComponentID);

            // save to entity-component table
            var sql = $"select [EntityID], [ComponentID] from [ecp].[EntityComponent] where [EntityID] = {this.EntityID}";
            try
            {
                using (var adapter = new SqlDataAdapter(sql, DatabaseController.GetConnectionString()))
                {
                    using (var commands = new SqlCommandBuilder(adapter))
                    {
                        using (var table = new DataTable())
                        {
                            // fill
                            adapter.Fill(table);
                            
                            // load db ids
                            var db_ids = new List<int>();
                            foreach (DataRow row in table.Rows) db_ids.Add(int.Parse(row["ComponentID"].ToString()));

                            // add / remove lists
                            var adds = component_ids.Except(db_ids).ToList();
                            var rems = db_ids.Except(component_ids).ToList();
                            if (adds.Count + rems.Count == 0) return true;

                            // set key
                            table.PrimaryKey = new DataColumn[] { table.Columns["ComponentID"] };

                            // remove
                            foreach (var rem in rems) table.Rows.Remove(table.Rows.Find(rem));

                            // add
                            foreach(var add in adds)
                            {
                                // create row
                                DataRow row = table.NewRow();

                                // edit row
                                row.BeginEdit();
                                row["EntityID"] = _entity_id;
                                row["ComponentID"] = add;
                                row.EndEdit();

                                // add row
                                table.Rows.Add(row);
                            }

                            // update
                            adapter.Update(table);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                return Error(ex);
            }

            // save each component
            foreach (var component in _components) if (!component.SaveData(validate)) return false;

            // default
            return true;
        }
        private bool SaveEntity()
        {
            // check for new
            if (!IsNew) return true;

            // set entity id
            _entity_id = EntityController.GetNextID(this);
            if (_entity_id == -1) return false;

            // add to entity table
            var sql = $"select [EntityID] from [ecp].[Entity] where [EntityID] = {_entity_id}";
            try
            {
                using(var adapter = new SqlDataAdapter(sql, DatabaseController.GetConnectionString()))
                {
                    using(var commands = new SqlCommandBuilder(adapter))
                    {
                        using(var table = new DataTable())
                        {
                            // load table
                            adapter.Fill(table);

                            // check
                            if (table.Rows.Count > 0) return Error("Rows were returned for a new entity.");

                            // create row
                            DataRow row = table.NewRow();

                            // edit row
                            row.BeginEdit();
                            row["EntityID"] = _entity_id;
                            row.EndEdit();

                            // add row
                            table.Rows.Add(row);

                            // update
                            adapter.Update(table);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                return Error(ex);
            }

            // default
            return true;
        }
        
        /// <summary>
        /// Validates the entity's data.
        /// </summary>
        /// <returns>true on success. false on fail.</returns>
        public bool ValidateData()
        {
            // format
            if (!FormatData()) return false;

            // validate
            foreach(var component in _components)
            {
                if(!component.ValidateData()) return false;
            }

            // raise event
            _notifier.OnValidated();

            // default
            return true;
        }
    }

    public interface IEntityNotifier : IHasEntity
    {
        IReadOnlyList<IComponentSubscriber> ComponentSubscribers { get; }
        IReadOnlyList<IEntitySubscriber> EntitySubscribers { get; }

        event EventHandler<ComponentEventArgs> ComponentAdded;
        event EventHandler<ComponentEventArgs> ComponentRemoved;
        event EventHandler<ComponentChangedEventArgs> ComponentChanged;
        event EventHandler<EntityErrorEventArgs> Error;
        event EventHandler Formatted;
        event EventHandler Saved;
        event EventHandler Validated;
    }
    public sealed class EntityNotifier : IDisposable, IEntityNotifier
    {
        internal EntityNotifier(Entity entity)
        {
            // set entity to track
            _entity = entity;

            // create subscriber lists
            _component_subscribers = new Dictionary<IComponentSubscriber, Type>();
            _entity_subscribers = new List<IEntitySubscriber>();
        }

        /// <summary>
        /// Reference to the entity the notifier is observing.
        /// </summary>
        public IEntity Entity { get => _entity; }
        private Entity _entity;

        /// <summary>
        /// List of subscribers to specific components.
        /// </summary>
        public IReadOnlyList<IComponentSubscriber> ComponentSubscribers { get => _component_subscribers.Keys.ToList(); }
        private Dictionary<IComponentSubscriber, Type> _component_subscribers;
        
        /// <summary>
        /// List of subscribers to the entity.
        /// </summary>
        public IReadOnlyList<IEntitySubscriber> EntitySubscribers { get => _entity_subscribers; }
        private List<IEntitySubscriber> _entity_subscribers;

        /// <summary>
        /// Dispose the notifier.
        /// </summary>
        public void Dispose()
        {
            // unsubscribe all subscribers
            foreach(var subscriber in _entity_subscribers)
            {
                if (subscriber == null) continue;
                UnsubscribeFromEntity(subscriber);
            }
        }

        /// <summary>
        /// Called from the entity to signify that a component has been added.
        /// </summary>
        /// <param name="component">the component that has been added</param>
        internal void OnComponentAdded(Component component)
        {
            // raise event
            ComponentAdded?.Invoke(component, new ComponentEventArgs(component));
        }

        /// <summary>
        /// Called from the entity to signify a property of a component has changed.
        /// </summary>
        /// <param name="component">the component the property resides</param>
        /// <param name="e">the property's arguments</param>
        internal void OnComponentChanged(Component component, PropertyChangedEventArgs e)
        {
            // raise the event
            ComponentChanged?.Invoke(component, new ComponentChangedEventArgs(component, e.Property, e.OldValue, e.NewValue));

            // loop through component subscribers
            foreach (var pair in _component_subscribers)
            {
                // check
                if (pair.Key == null) continue;
                if (pair.Value != component.GetType()) continue;

                // notify
                pair.Key.OnComponentChanged(e.Property, e.NewValue);
            }

            // loop through entity subscribers
            foreach (var subscriber in _entity_subscribers)
            {
                // check subscriber
                if (subscriber == null) continue;

                // notify
                subscriber.OnEntityComponentChanged(component, e.Property, e.NewValue);
            }
        }

        /// <summary>
        /// Called from the entity to signify a component has been removed.
        /// </summary>
        /// <param name="component">the removed component</param>
        internal void OnComponentRemoved(Component component)
        {
            // raise event
            ComponentRemoved?.Invoke(component, new ComponentEventArgs(component));
        }

        /// <summary>
        /// Called from the entity when an error is incurred
        /// </summary>
        /// <param name="error">error message</param>
        internal void OnError(string error)
        {
            // raise event
            Error?.Invoke(_entity, new EntityErrorEventArgs(_entity, error));

            // loop through subscribers
            foreach (var subscriber in _entity_subscribers)
            {
                if (subscriber == null) continue;
                subscriber.OnEntityError(error);
            }
        }

        /// <summary>
        /// Called from the entity when an exception is incurred.
        /// </summary>
        /// <param name="ex">the incurred exception</param>
        internal void OnError(Exception ex)
        {
            // raise event
            Error?.Invoke(_entity, new EntityErrorEventArgs(_entity, ex));

            // loop through subscribers
            foreach (var subscriber in _entity_subscribers)
            {
                if (subscriber == null) continue;
                subscriber.OnEntityError(ex.Message);
            }
        }

        /// <summary>
        /// Called from the entity to signify a successfyl format.
        /// </summary>
        internal void OnFormatted()
        {
            // raise event
            Formatted?.Invoke(_entity, EventArgs.Empty);
        }

        /// <summary>
        /// Called from the entity to signify a successful save.
        /// </summary>
        internal void OnSaved()
        {
            Saved?.Invoke(_entity, EventArgs.Empty);
        }

        /// <summary>
        /// Called from the entity to signify a successful validate.
        /// </summary>
        internal void OnValidated()
        {
            // raise the event
            Validated?.Invoke(_entity, EventArgs.Empty);
        }

        /// <summary>
        /// Subscribe to a specific component of the entity.
        /// </summary>
        /// <typeparam name="T">type of component to subscribe to</typeparam>
        /// <param name="subscriber">object inheriting the IComponentSubscriber interface</param>
        public void SubscribeToComponent<T>(IComponentSubscriber<T> subscriber) where T : Component
        {
            // check
            if (subscriber == null) return;
            if (!_entity._components.Exists(x => x.GetType() == typeof(T))) return;
            if (_component_subscribers.ContainsKey(subscriber)) return;

            // subscribe
            _component_subscribers.Add(subscriber, typeof(T));
        }

        /// <summary>
        /// Subscribe to basic entity events (component changed & error)
        /// </summary>
        /// <param name="subscriber">object inheriting the IEntitySubscriber interface</param>
        public void SubscribeToEntity(IEntitySubscriber subscriber)
        {
            // check
            if (subscriber == null) return;
            if (_entity_subscribers.Contains(subscriber)) return;
            
            // subscribe
            _entity_subscribers.Add(subscriber);
        }

        /// <summary>
        /// Unsubscribe from a specific component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subscriber"></param>
        public void UnsubscribeFromComponent<T>(IComponentSubscriber<T> subscriber) where T : Component
        {
            // check
            if (subscriber == null) return;
            if (!_component_subscribers.ContainsKey(subscriber)) return;

            // remove
            _component_subscribers.Remove(subscriber);
        }

        /// <summary>
        /// Unsubscribe from basic entity events.
        /// </summary>
        /// <param name="subscriber">object inheriting the IEntitySubscriber interface</param>
        public void UnsubscribeFromEntity(IEntitySubscriber subscriber)
        {
            // check
            if (subscriber == null) return;
            if (!_entity_subscribers.Contains(subscriber)) return;

            // remove
            _entity_subscribers.Remove(subscriber);
        }

        /// <summary>
        /// Event raised when a component has been added.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ComponentAdded;

        /// <summary>
        /// Event raised when a component has been changed.
        /// </summary>
        public event EventHandler<ComponentChangedEventArgs> ComponentChanged;

        /// <summary>
        /// Event raised when a component has been removed.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ComponentRemoved;

        /// <summary>
        /// Event raised when an error is incurred.
        /// </summary>
        public event EventHandler<EntityErrorEventArgs> Error;

        /// <summary>
        /// Event raised when the entity has been successfully formatted.
        /// </summary>
        public event EventHandler Formatted;

        /// <summary>
        /// Event raised when the entity has been successfully saved.
        /// </summary>
        public event EventHandler Saved;

        /// <summary>
        /// Event raised when the entity has been successfully validated.
        /// </summary>
        public event EventHandler Validated;
    }

    public interface IComponent : IHasEntity, IModifiable, IRuntime
    {
        int EntityID { get; }
        int ComponentID { get; }
    }
    public abstract class Component : IDisposable, IComponent
    {
        protected Component()
        {
            // set runtime
            _runtime_id = RuntimeController.NextID();

            // set component id
            _component_id = ComponentController.GetComponentID(this);
        }

        public int ComponentID { get => _component_id; }
        internal int _component_id = -1;

        /// <summary>
        /// Referenced entity's ID.
        /// </summary>
        [Savable]
        public int EntityID
        {
            get
            {
                if (_entity == null) return -1;
                else return _entity.EntityID;
            }
        }

        /// <summary>
        /// Reference to the containing entity
        /// </summary>
        public IEntity Entity { get => _entity; }
        internal Entity _entity { get; set; }

        /// <summary>
        /// Flag denoting whether or not the component data has been changed since last save/refresh.
        /// </summary>
        public bool IsModified { get; }
        private bool _modified = false;

        /// <summary>
        /// Runtime ID. Set at runtime as a unique identifier across the system.
        /// </summary>
        public int RuntimeID { get => _runtime_id; }
        private int _runtime_id;

        /// <summary>
        /// Disposes the component.
        /// </summary>
        public virtual void Dispose()
        {

        }

        /// <summary>
        /// Called from the entity, processor, or validate function to format the component's data.
        /// </summary>
        internal bool FormatData()
        {
            // loop through component properties
            foreach (var property in this.GetType().GetProperties())
            {
                // get/check for format attribute
                var attribute = property.GetCustomAttribute<FormattableAttribute>();
                if (attribute == null) continue;

                // get data
                var value = property.GetValue(this);

                // by type
                if(property.PropertyType == typeof(string))
                {
                    // convert to string
                    var str_value = value.ToString();

                    // auto-trim
                    if (attribute.AutoTrim) property.SetValue(this, str_value.Trim());

                    // case
                    switch (attribute.Case)
                    {
                        case FormattableAttribute.FormatCase.Upper:
                            property.SetValue(this, str_value.ToUpper());
                            break;
                        case FormattableAttribute.FormatCase.Lower:
                            property.SetValue(this, str_value.ToLower());
                            break;
                    }
                }
            }

            // default
            return true;
        }

        /// <summary>
        /// Called from the containing entity to load the component
        /// </summary>
        internal bool LoadData(Entity entity)
        {
            // set entity
            _entity = entity;

            // sql
            var sql = new StringBuilder("select");
            int count = 0;
            var columns = new List<PropertyInfo>();

            // loop through the savable rows & build sql
            foreach(var property in this.GetType().GetProperties())
            {
                // get/check savable attribute
                var attribute = property.GetCustomAttribute<SavableAttribute>();
                if (attribute == null) continue;

                // build sql
                if (count > 0) sql.AppendLine(",");
                sql.Append(property.Name);

                // check if writable
                if (property.CanWrite) columns.Add(property);

                // increment counter
                count++;
            }

            // check for columns
            if (count < 1) return true;

            // table name
            string tablename = this.GetType().Name;
            if (tablename.EndsWith("Component")) tablename = tablename.Substring(0, tablename.Length - 9);

            // from/where
            sql.AppendLine($"from [ecp].[{tablename}]");
            sql.AppendLine($"where [EntityID] = {EntityID}");

            // load
            try
            {
                using (var adapter = new SqlDataAdapter(sql.ToString(), DatabaseController.GetConnectionString()))
                {
                    using (var table = new DataTable())
                    {
                        // fill
                        adapter.Fill(table);

                        // get row
                        if (table.Rows.Count < 1) return true;
                        DataRow row = table.Rows[0];

                        // loop through columns
                        foreach(var property in columns)
                        {
                            // set value
                            property.SetValue(this, row[property.Name]);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                return _entity.Error(this, ex);
            }

            // default
            return true;
        }

        /// <summary>
        /// Called when a property has been modified
        /// </summary>
        /// <param name="property">name of property</param>
        /// <param name="old_value">old property value</param>
        /// <param name="new_value">new property value</param>
        protected void Modified(string property, object old_value, object new_value)
        {
            // set modified flag
            _modified = true;

            // raise property changed event
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property, old_value, new_value));
        }

        /// <summary>
        /// Called from the entity or processor only
        /// </summary>
        /// <returns>true on success. false on error.</returns>
        internal bool SaveData(bool validate = true)
        {
            // validate
            if (validate) if (!ValidateData()) return false;

            // build sql & data
            var sql = new StringBuilder("select");
            var data = new Dictionary<string, object>();
            int count = 0;

            // loop through component properties
            foreach(var property in this.GetType().GetProperties())
            {
                // get/check for save attribute
                var attribute = property.GetCustomAttribute<SavableAttribute>();
                if (attribute == null) continue;

                // build sql
                if (count > 0) sql.AppendLine(",");
                sql.Append($" [{property.Name}]");

                // add data
                data.Add(property.Name, property.GetValue(this));

                // increment the column counter
                count++;
            }

            // check
            if (count < 1) return true;

            // save
            try
            {
                using (var adapter = new SqlDataAdapter(sql.ToString(), DatabaseController.GetConnectionString()))
                {
                    using(var commands = new SqlCommandBuilder(adapter))
                    {
                        using (var table = new DataTable())
                        {
                            // fill
                            adapter.Fill(table);

                            // handle row
                            bool new_row = false;
                            int mods = 0;
                            DataRow row;
                            if(table.Rows.Count == 1)row = table.Rows[0];
                            else { row = table.NewRow(); new_row = true; }

                            // loop through data
                            foreach(var datum in data)
                            {
                                if (row[datum.Key] != datum.Value)
                                {
                                    row[datum.Key] = datum.Value;
                                    mods++;
                                }
                            }

                            // add row
                            if (new_row) table.Rows.Add(row);

                            // update
                            if (new_row || mods > 0) adapter.Update(table);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _entity.Error(this, ex);
            }

            // default
            return true;
        }

        /// <summary>
        /// Called from SaveData or the entity to validate the data.
        /// </summary>
        /// <returns>true on success. false on error.</returns>
        internal bool ValidateData()
        {
            // loop through component properties
            foreach (var property in this.GetType().GetProperties())
            {
                // get/check for validate attribute
                var attribute = property.GetCustomAttribute<ValidatableAttribute>();
                if (attribute == null) continue;

                // get value
                var value = property.GetValue(this);

                // allow null
                if (!attribute.AllowNull && value == null) return _entity.Error($"{property.Name} cannot be null.");

                // by type
                if(property.PropertyType == typeof(string))
                {
                    // convert to string value
                    var str_value = value.ToString();

                    // string
                    if (!attribute.AllowBlank && string.IsNullOrEmpty(str_value)) return _entity.Error($"{property.Name} cannot be blank.");
                }
            }

            // default
            return true;
        }

        /// <summary>
        /// Raised when a value changes on the component
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs> PropertyChanged;
    }
    
    public interface IProcessor : IRuntime
    {

    }
    public interface IProcessor<T> : IProcessor
    {
        
    }
    public abstract class Processor<T> : IProcessor<T> where T : Component
    {
        protected Processor()
        {
            // set runtime
            _runtime_id = RuntimeController.NextID();

            // register
            ProcessorController.Register(this);
        }

        /// <summary>
        /// Runtime ID. Set at runtime as a unique identifier across the system.
        /// </summary>
        public int RuntimeID { get => _runtime_id; }
        private int _runtime_id;
    }

    public interface IManager : IRuntime
    {
        int Count { get; }
        IReadOnlyList<IEntity> Entities { get; }

        IEntity Create();
        IEntity Get(int id);
    }
    public abstract class Manager : IManager
    {
        protected Manager()
        {
            // setup runtime
            _runtime_id = RuntimeController.NextID();

            // create entity list
            _entities = new List<Entity>();

            // register the manager
            ManagerController.Register(this);
        }

        /// <summary>
        /// Number of entities managed by the manager.
        /// </summary>
        public int Count { get => _entities.Count; }

        /// <summary>
        /// List of entities managed by manager.
        /// </summary>
        public IReadOnlyList<IEntity> Entities { get => _entities; }
        private List<Entity> _entities;

        /// <summary>
        /// Runtime ID. Set at runtime as a unique identifier across the system.
        /// </summary>
        public int RuntimeID { get => _runtime_id; }
        private int _runtime_id;

        /// <summary>
        /// Create new instance of an entity.
        /// </summary>
        /// <returns>reference to the newly created entity</returns>
        public IEntity Create()
        {
            // create new entity
            var entity = new Entity();

            // register
            Register(entity);

            // return entity
            return entity;
        }

        /// <summary>
        /// Create new instance of a specific entity.
        /// </summary>
        /// <param name="id">ID of the entity to instantiate</param>
        /// <returns>instatiated entity (or null if id not fould)</returns>
        public IEntity Get(int id)
        {
            // create entity
            var entity = new Entity();

            // load it
            if (!entity.LoadData(id)) return null;

            // register
            Register(entity);

            // return entity
            return entity;
        }

        /// <summary>
        /// Internally registers the entity to the manager.
        /// </summary>
        /// <param name="entity">entity to register</param>
        private void Register(Entity entity)
        {
            // add
            _entities.Add(entity);

            // register centrally
            EntityController.Register(entity);
        }
    }

    public sealed class ComponentEventArgs
    {
        internal ComponentEventArgs(IComponent component)
        {
            this.Component = component;
        }

        public IComponent Component { get; }
    }
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
    public sealed class EntityErrorEventArgs : EventArgs
    {
        internal EntityErrorEventArgs(IEntity entity, string error)
        {
            this.Entity = entity;
            this.Message = error;
        }
        internal EntityErrorEventArgs(IEntity entity, Exception exception) : this(entity, exception.Message)
        {
            this.Exception = exception;
        }

        public IEntity Entity { get; }
        public Exception Exception { get; }
        public string Message { get; }
    }
    public sealed class PropertyChangedEventArgs
    {
        public PropertyChangedEventArgs(string property, object old_value, object new_value)
        {
            this.Property = property;
            this.OldValue = old_value;
            this.NewValue = new_value;
        }

        public string Property { get; }
        public object OldValue { get; }
        public object NewValue { get; }
    }

    public static class ComponentController
    {
        private static Dictionary<string, int> _classes = new Dictionary<string, int>();
        private static bool _initialized = false;
        private static Dictionary<Type, int> _types = new Dictionary<Type, int>();

        /// <summary>
        /// Get component ID from passed component
        /// </summary>
        /// <typeparam name="T">type of the passed component</typeparam>
        /// <param name="component">the component to get component ID from</param>
        /// <returns>component ID</returns>
        internal static int GetComponentID<T>(T component) where T : Component
        {
            return GetComponentID(typeof(T));
        }

        /// <summary>
        /// Get component ID from passed component type
        /// </summary>
        /// <param name="type">type of component</param>
        /// <returns>component ID</returns>
        internal static int GetComponentID(Type type)
        {
            // id
            int id = -1;

            // initialize
            if (!Initialize()) return id;

            // check dictionary
            if (_types.ContainsKey(type)) return _types[type];

            // return id
            return id;
        }

        /// <summary>
        /// Initialization of component data. GetComponent(x) calls this to ensure the controller is initialized.
        /// </summary>
        internal static bool Initialize()
        {
            // check
            if (_initialized) return true;

            // sql
            var sql = "select [ComponentID], [Class] from [ecp].[Component]";

            // retrieve
            try
            {
                using (var adapter = new SqlDataAdapter(sql, DatabaseController.GetConnectionString()))
                {
                    using (var table = new DataTable())
                    {
                        // fill
                        adapter.Fill(table);

                        // loop through rows
                        foreach(DataRow row in table.Rows)
                        {
                            // add to class dictionary
                            _classes.Add(row["Class"].ToString(), int.Parse(row["ComponentID"].ToString()));
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                return false;
            }

            // loop through classes
            foreach(var c in _classes)
            {
                // get type
                var type = Type.GetType(c.Key);
                if (type == null) continue;
                _types.Add(type, c.Value);
            }

            // set flag
            _initialized = true;

            // default
            return true;
        }
    }
    public static class DatabaseController
    {
        public static string Database
        {
            get => _database;
            set => _database = value;
        }
        private static string _database = "Datalus_db";

        public static bool IntegratedSecurity
        {
            get => _security;
            set => _security = value;
        }
        private static bool _security = true;

        public static string Server
        {
            get => _server;
            set => _server = value;
        }
        private static string _server = "EV701307\\LMR";

        /// <summary>
        /// Returns the formatted connection string based on the properties of the DatabaseController object.
        /// </summary>
        internal static string GetConnectionString()
        {
            // builder
            var sb = new SqlConnectionStringBuilder();

            // build
            sb.DataSource = _server;
            sb.InitialCatalog = _database;
            sb.IntegratedSecurity = _security;

            // return
            return sb.ToString();
        }
    }
    internal static class EntityController
    {
        private static Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();

        /// <summary>
        /// Get the next available entity ID at the time of call. This is database-based.
        /// </summary>
        /// <param name="entity">entity passed as a reference to allow for message handling</param>
        /// <returns>next available ID</returns>
        internal static int GetNextID(Entity entity)
        {
            // default
            int id = -1;

            // build sql
            var sql = "select isnull( max( [EntityID] ), 1000 ) + 1 from [ecp].[Entity]";

            try
            {
                using (var connection = new SqlConnection(DatabaseController.GetConnectionString()))
                {
                    // open connection
                    connection.Open();

                    // verify connection
                    if(connection.State == ConnectionState.Open)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            // setup
                            command.CommandText = sql;

                            // retrieve
                            var raw = command.ExecuteScalar();

                            // convert
                            if(raw == null)
                            {
                                entity.Error("Next ID was returned as null.");
                                return -1;
                            }
                            if(!int.TryParse(raw.ToString(), out id))
                            {
                                entity.Error("Could not convert next ID from database value.");
                                return -1;
                            }
                        }
                    }

                    // close connection
                    connection.Close();
                }
            }
            catch(Exception ex)
            {
                entity.Error(ex);
                return -1;
            }

            // return
            return id;
        }

        /// <summary>
        /// Centrally register the entity
        /// </summary>
        internal static void Register(Entity entity)
        {
            // register in central registry
            _entities.Add(entity.EntityID, entity);
        }
    }
    internal static class RuntimeController
    {
        internal static int NextID()
        {
            int id = _next_id;
            _next_id++;
            return id;
        }

        private static int _next_id = 1;
    }
    public static class ProcessorController
    {
        public static IReadOnlyList<IProcessor> Processors { get => _processors; }
        private static List<IProcessor> _processors = new List<IProcessor>();

        internal static void Register<T>(Processor<T> processor) where T : Component
        {
            _processors.Add(processor);
        }
    }
    public static class ManagerController
    {
        /// <summary>
        /// List of active managers
        /// </summary>
        public static IReadOnlyList<IManager> Managers { get => _managers; }
        private static List<Manager> _managers = new List<Manager>();

        internal static void Register<T>(T manager) where T : Manager
        {
            // check
            if (manager == null) return;
            if (_managers.Contains(manager)) return;

            // add
            _managers.Add(manager);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FormattableAttribute : Attribute
    {
        public enum FormatCase
        {
            Any,
            Upper,
            Lower
        }

        public bool AutoTrim { get; set; } = true;
        public FormatCase Case { get; set; } = FormatCase.Any;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SavableAttribute : ValidatableAttribute
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ValidatableAttribute : FormattableAttribute
    {
        public bool AllowBlank { get; set; } = true;
        public bool AllowNull { get; set; } = false;
    }
}
