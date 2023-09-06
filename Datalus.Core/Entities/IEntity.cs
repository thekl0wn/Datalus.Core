using System.Collections.Generic;

namespace Datalus.Core
{
    public interface IEntity : IModifiable, IRuntime, ISavable
    {
        IReadOnlyList<IComponent> Components { get; }
        int EntityID { get; }
        bool IsNew { get; }
        IEntityNotifier Notifier { get; }

        void AddComponent<T>() where T : Component, new();
        void AddComponent<T>(T component) where T : Component;
        T GetComponent<T>() where T : Component;
        void RemoveComponent<T>() where T : Component;
    }
}
