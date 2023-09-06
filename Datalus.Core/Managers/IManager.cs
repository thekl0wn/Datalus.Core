using System.Collections.Generic;

namespace Datalus.Core
{
    public interface IManager : IRuntime
    {
        int Count { get; }
        IReadOnlyList<IEntity> Entities { get; }

        IEntity Create();
        IEntity Get(int id);
    }
}
