using Datalus.Core.Components;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Text;

namespace Datalus.Core
{
    public static class ComponentExtensions
    {
        public static int ComponentID<T>(this T component) where T : IComponent
        {
            return ComponentController.GetComponentID(typeof(T));
        }
        public static int EntityID<T>(this T component) where T : IComponent
        {
            // check for entity
            if (component.Entity == null) return -1;
            else return (component.Entity.EntityID);
        }

        internal static bool Error<T>(this T component, Exception ex) where T : IComponent
        {
            // convert exception to string
            return component.Error(ex.Message);
        }
        internal static bool Error<T>(this T component, string error) where T : IComponent
        {
            // trigger the handler method
            component.OnError(error);

            // always false
            return false;
        }

        public static bool Format<T>(this T component) where T : IComponent
        {
            // loop through properties
            foreach(var property in typeof(T).GetProperties())
            {
                if (!component.Format(property)) return false;
            }

            // default
            return true;
        }
        private static bool Format<T>(this T component, PropertyInfo property) where T : IComponent
        {
            // get/check formattable attribute
            var attribute = property.GetCustomAttribute<FormattableAttribute>();
            if (attribute == null) return true;

            // get value
            var value = property.GetValue(component);

            // format
            return component.Format(property, attribute, value);
            
        }
        private static bool Format<T>(this T component, PropertyInfo property, FormattableAttribute attribute, object value) where T : IComponent
        {
            // check
            if (attribute == null) return true;

            // format based on type
            if (property.PropertyType == typeof(string)) return component.Format(property, attribute, value.ToString());
            else if (property.PropertyType == typeof(bool)) return component.Format(property, bool.Parse(value.ToString()));
            else if (property.PropertyType == typeof(DateTime)) return component.Format(property, DateTime.Parse(value.ToString()));
            else if (property.PropertyType == typeof(float)) return component.Format(property, float.Parse(value.ToString()));
            else if (property.PropertyType == typeof(int)) return component.Format(property, int.Parse(value.ToString()));

            // default
            return true;
        }
        private static bool Format<T>(this T component, PropertyInfo property, FormattableAttribute attribute, string value)
        {
            // auto-trim
            if (attribute.AutoTrim)
            {
                var new_value = value.Trim();
                if (value != new_value) property.SetValue(component, new_value);
            }

            // default
            return true;
        }
        private static bool Format<T>(this T component, PropertyInfo property, int value)
        {
            // default
            return true;
        }
        private static bool Format<T>(this T component, PropertyInfo property, float value)
        {
            // default
            return true;
        }
        private static bool Format<T>(this T component, PropertyInfo property, DateTime value)
        {
            // default
            return true;
        }
        private static bool Format<T>(this T component, PropertyInfo property, bool value)
        {
            // default
            return true;
        }

        public static bool Load<T>(this T component, IEntity entity) where T : IComponent
        {
            // get actual component
            var ref_comp = ComponentController.GetComponentFromRuntime(component.RuntimeID);

            // set entity reference
            ref_comp.SetEntity(entity);

            // default
            return component.Refresh();
        }
        public static bool Refresh<T>(this T component) where T : IComponent
        {
            // verify entity
            if (component.Entity == null) return component.Error("Entity is not set.");

            // sql
            var sql = new StringBuilder("select");
            int count = 0;
            var columns = new List<PropertyInfo>();

            // loop through the savable rows & build sql
            foreach (var property in component.GetType().GetProperties())
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
            string tablename = component.GetType().Name;
            if (tablename.EndsWith("Component")) tablename = tablename.Substring(0, tablename.Length - 9);
            if (tablename.StartsWith("I")) tablename = tablename.Substring(1);

            // from/where
            sql.AppendLine($"from [ecp].[{tablename}]");
            sql.AppendLine($"where [EntityID] = {component.EntityID()}");

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
                            property.SetValue(component, row[property.Name]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return component.Error(ex);
            }

            // default
            return true;
        }

        public static bool Save<T>(this T component) where T : IComponent
        {
            // save... validate by default
            return component.Save(true);
        }
        public static bool Save<T>(this T component, bool validate) where T : IComponent
        {
            // validate
            if (validate) if (!component.Validate()) return false;

            // build sql & data
            var sql = new StringBuilder("select");
            var data = new Dictionary<string, object>();
            int count = 0;

            // loop through component properties
            foreach (var property in component.GetType().GetProperties())
            {
                // get/check for save attribute
                var attribute = property.GetCustomAttribute<SavableAttribute>();
                if (attribute == null) continue;

                // build sql
                if (count > 0) sql.AppendLine(",");
                sql.Append($" [{property.Name}]");

                // add data
                data.Add(property.Name, property.GetValue(component));

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
                return component.Error(ex);
            }

            // default
            return true;
        }

        public static bool Validate<T>(this T component) where T : IComponent
        {
            // validate at component level
            if (component.ComponentID() < 1000) return component.ValidationFailed(component.GetType().GetProperty("ComponentID"), "Component ID is not set.");

            // format
            if (!component.Format()) return false;

            // loop through properties
            foreach(var property in typeof(T).GetProperties())
            {
                // validate each property
                if (!component.Validate(property)) return false;
            }

            // default
            return true;
        }
        private static bool Validate<T>(this T component, PropertyInfo property) where T : IComponent
        {
            // get/check attribute
            var attribute = property.GetCustomAttribute<ValidatableAttribute>();
            if (attribute == null) return true;

            // get value
            var value = property.GetValue(component);

            // default
            return component.Validate(property, attribute, value);
        }
        private static bool Validate<T>(this T component, PropertyInfo property, ValidatableAttribute attribute, object value) where T : IComponent
        {
            // allow null?
            if (!attribute.AllowNull) if (value == null) return ValidationFailed($"{property.Name} cannot be null.");

            // by type
            if (property.PropertyType == typeof(string)) return component.Validate(property, attribute, value.ToString());

            // default
            return true;
        }
        private static bool Validate<T>(this T component, PropertyInfo property, ValidatableAttribute attribute, string value) where T : IComponent
        {
            // allow blank
            if (!attribute.AllowBlank) if (string.IsNullOrEmpty(value)) return component.ValidationFailed(property, $"{property.Name} cannot be blank.");

            // default
            return true;
        }

        private static bool ValidationFailed<T>(this T component, PropertyInfo property, string message) where T : IComponent
        {
            // trigger the handler method
            component.OnValidationFailed(property, message);

            // always false
            return false;
        }
    }
}
