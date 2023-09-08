using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Datalus.Core
{
    public sealed class Entity : IEntity
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
                        foreach (DataRow row in table.Rows)
                        {
                            classes.Add(row["Class"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return this.Error(ex);
            }

            // loop through the classes
            foreach (var c in classes)
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
                            foreach (var add in adds)
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
            catch (Exception ex)
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
                using (var adapter = new SqlDataAdapter(sql, DatabaseController.GetConnectionString()))
                {
                    using (var commands = new SqlCommandBuilder(adapter))
                    {
                        using (var table = new DataTable())
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
            catch (Exception ex)
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
            foreach (var component in _components)
            {
                if (!component.ValidateData()) return false;
            }

            // raise event
            _notifier.OnValidated();

            // default
            return true;
        }
    }
}
