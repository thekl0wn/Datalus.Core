using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Datalus.Core.Components
{
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
                        foreach (DataRow row in table.Rows)
                        {
                            // add to class dictionary
                            _classes.Add(row["Class"].ToString(), int.Parse(row["ComponentID"].ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            // loop through classes
            foreach (var c in _classes)
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
}
