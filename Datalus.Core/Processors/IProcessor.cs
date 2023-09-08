using System.Collections.Generic;

namespace Datalus.Core
{
    public interface IProcessor : IRuntime
    {
        void Update();
    }

    public interface IProcessor<T> : IProcessor where T : IComponent
    {
        IReadOnlyList<IComponent> Components { get; }

        void Register(T component);
        void Unregister(T component);
    }
}
