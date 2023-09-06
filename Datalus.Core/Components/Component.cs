using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace Datalus.Core
{
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
                if (property.PropertyType == typeof(string))
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
            foreach (var property in this.GetType().GetProperties())
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
                        foreach (var property in columns)
                        {
                            // set value
                            property.SetValue(this, row[property.Name]);
                        }
                    }
                }
            }
            catch (Exception ex)
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
            foreach (var property in this.GetType().GetProperties())
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
                    using (var commands = new SqlCommandBuilder(adapter))
                    {
                        using (var table = new DataTable())
                        {
                            // fill
                            adapter.Fill(table);

                            // handle row
                            bool new_row = false;
                            int mods = 0;
                            DataRow row;
                            if (table.Rows.Count == 1) row = table.Rows[0];
                            else { row = table.NewRow(); new_row = true; }

                            // loop through data
                            foreach (var datum in data)
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
            catch (Exception ex)
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
                if (property.PropertyType == typeof(string))
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
}
