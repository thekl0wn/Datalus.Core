using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datalus.Core
{
    public interface IDatalusObject : IDisposable, IEquatable<IDatalusObject>, IHasRuntime
    {

    }

    public interface IHasComponents
    {
        IReadOnlyList<IDatalusComponent> Components { get; }
    }
    public interface IHasEntity
    {
        IDatalusEntity Entity { get; }
    }
    public interface IHasModified
    {
        bool IsModified { get; }
    }
    public interface IHasRuntime
    {
        int RuntimeID { get; }
    }

    public interface ICanFormat
    {
        bool Format();
    }
    public interface ICanSave : ICanValidate, IHasModified
    {
        bool Save();
        bool Save(bool validate);
    }
    public interface ICanValidate : ICanFormat
    {
        bool Validate();
    }
}
