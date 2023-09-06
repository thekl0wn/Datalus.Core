using System.Collections.Generic;

namespace Datalus.Core
{
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
}
