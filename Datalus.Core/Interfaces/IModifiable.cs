using System;

namespace Datalus.Core
{
    public interface IModifiable : IDisposable, IRuntime
    {
        bool IsModified { get; }
    }
}
