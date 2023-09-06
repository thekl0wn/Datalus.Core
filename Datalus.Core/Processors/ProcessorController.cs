using System.Collections.Generic;

namespace Datalus.Core
{
    public static class ProcessorController
    {
        public static IReadOnlyList<IProcessor> Processors { get => _processors; }
        private static List<IProcessor> _processors = new List<IProcessor>();

        internal static void Register<T>(Processor<T> processor) where T : Component
        {
            _processors.Add(processor);
        }
    }
}
