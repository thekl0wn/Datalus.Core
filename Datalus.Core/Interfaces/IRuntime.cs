using System;

namespace Datalus.Core
{
    public interface IRuntime : IDisposable
    {
        int RuntimeID { get; }
    }
}
