using System.Data.SqlClient;

namespace Datalus.Core
{
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
}
