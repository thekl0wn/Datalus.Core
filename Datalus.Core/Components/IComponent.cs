using System;
using System.Reflection;

namespace Datalus.Core
{
    public interface IComponent : IHasEntity, IRuntime
    {
        void OnError(string error);
        bool OnPropertyChanging(PropertyInfo property, object old_value, object new_value);
        void OnPropertyChanged(PropertyInfo property, object value);
        void OnValidationFailed(PropertyInfo property, string message);
    }
}
