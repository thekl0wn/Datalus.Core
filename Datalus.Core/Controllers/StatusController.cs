using Datalus.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datalus.Core.Controllers
{
    public static class StatusController
    {
        public static IHasStatusText TextObject { get; set; }

        /// <summary>
        /// Set error status (exception message) and returns false.
        /// </summary>
        /// <param name="ex">the exception incurred</param>
        /// <returns>false... always false</returns>
        public static bool Error(Exception ex)
        {
            // set & return
            return Error(ex.Message);
        }

        /// <summary>
        /// Set error status and returns false.
        /// </summary>
        /// <param name="error">the error text to display</param>
        /// <returns>false... always false</returns>
        public static bool Error(string error)
        {
            // set
            Set(error);

            // always false
            return false;
        }

        /// <summary>
        /// Set the ready status and return true.
        /// </summary>
        /// <returns>true... always true</returns>
        public static bool Ready()
        {
            // set
            Set("Ready");

            // always true
            return true;
        }

        /// <summary>
        /// Set current status. TextObject must be set.
        /// </summary>
        /// <param name="status">the status to set</param>
        public static void Set(string status)
        {
            // check text object
            if (TextObject == null) return;

            // set
            if (TextObject.StatusText == status) return;
            TextObject.StatusText = status;
        }
    }
}
