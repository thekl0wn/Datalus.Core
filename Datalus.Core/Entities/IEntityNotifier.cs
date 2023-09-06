using System;
using System.Collections.Generic;

namespace Datalus.Core
{
    public interface IEntityNotifier : IHasEntity
    {
        IReadOnlyList<IComponentSubscriber> ComponentSubscribers { get; }
        IReadOnlyList<IEntitySubscriber> EntitySubscribers { get; }

        event EventHandler<ComponentEventArgs> ComponentAdded;
        event EventHandler<ComponentEventArgs> ComponentRemoved;
        event EventHandler<ComponentChangedEventArgs> ComponentChanged;
        event EventHandler<EntityErrorEventArgs> Error;
        event EventHandler Formatted;
        event EventHandler Saved;
        event EventHandler Validated;
    }
}
