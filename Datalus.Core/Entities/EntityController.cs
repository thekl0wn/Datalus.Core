using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datalus.Core
{
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
                    if (connection.State == ConnectionState.Open)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            // setup
                            command.CommandText = sql;

                            // retrieve
                            var raw = command.ExecuteScalar();

                            // convert
                            if (raw == null)
                            {
                                entity.Error("Next ID was returned as null.");
                                return -1;
                            }
                            if (!int.TryParse(raw.ToString(), out id))
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
            catch (Exception ex)
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
}
