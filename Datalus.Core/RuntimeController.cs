using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datalus.Core
{
    internal static class RuntimeController
    {
        private static int _next_id = 1;

        internal static int GetNextID()
        {
            int next = _next_id;
            _next_id++;
            return next;
        }
    }
}
