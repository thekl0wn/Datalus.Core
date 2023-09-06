namespace Datalus.Core
{
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
}
