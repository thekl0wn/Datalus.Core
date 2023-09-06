using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datalus.Core.Entities
{
    public sealed class EntityNotifier : IDisposable, IEntityNotifier
    {
        internal EntityNotifier(Entity entity)
        {
            // set entity to track
            _entity = entity;

            // create subscriber lists
            _component_subscribers = new Dictionary<IComponentSubscriber, Type>();
            _entity_subscribers = new List<IEntitySubscriber>();
        }

        /// <summary>
        /// Reference to the entity the notifier is observing.
        /// </summary>
        public IEntity Entity { get => _entity; }
        private Entity _entity;

        /// <summary>
        /// List of subscribers to specific components.
        /// </summary>
        public IReadOnlyList<IComponentSubscriber> ComponentSubscribers { get => _component_subscribers.Keys.ToList(); }
        private Dictionary<IComponentSubscriber, Type> _component_subscribers;

        /// <summary>
        /// List of subscribers to the entity.
        /// </summary>
        public IReadOnlyList<IEntitySubscriber> EntitySubscribers { get => _entity_subscribers; }
        private List<IEntitySubscriber> _entity_subscribers;

        /// <summary>
        /// Dispose the notifier.
        /// </summary>
        public void Dispose()
        {
            // unsubscribe all subscribers
            foreach (var subscriber in _entity_subscribers)
            {
                if (subscriber == null) continue;
                UnsubscribeFromEntity(subscriber);
            }
        }

        /// <summary>
        /// Called from the entity to signify that a component has been added.
        /// </summary>
        /// <param name="component">the component that has been added</param>
        internal void OnComponentAdded(Component component)
        {
            // raise event
            ComponentAdded?.Invoke(component, new ComponentEventArgs(component));
        }

        /// <summary>
        /// Called from the entity to signify a property of a component has changed.
        /// </summary>
        /// <param name="component">the component the property resides</param>
        /// <param name="e">the property's arguments</param>
        internal void OnComponentChanged(Component component, PropertyChangedEventArgs e)
        {
            // raise the event
            ComponentChanged?.Invoke(component, new ComponentChangedEventArgs(component, e.Property, e.OldValue, e.NewValue));

            // loop through component subscribers
            foreach (var pair in _component_subscribers)
            {
                // check
                if (pair.Key == null) continue;
                if (pair.Value != component.GetType()) continue;

                // notify
                pair.Key.OnComponentChanged(e.Property, e.NewValue);
            }

            // loop through entity subscribers
            foreach (var subscriber in _entity_subscribers)
            {
                // check subscriber
                if (subscriber == null) continue;

                // notify
                subscriber.OnEntityComponentChanged(component, e.Property, e.NewValue);
            }
        }

        /// <summary>
        /// Called from the entity to signify a component has been removed.
        /// </summary>
        /// <param name="component">the removed component</param>
        internal void OnComponentRemoved(Component component)
        {
            // raise event
            ComponentRemoved?.Invoke(component, new ComponentEventArgs(component));
        }

        /// <summary>
        /// Called from the entity when an error is incurred
        /// </summary>
        /// <param name="error">error message</param>
        internal void OnError(string error)
        {
            // raise event
            Error?.Invoke(_entity, new EntityErrorEventArgs(_entity, error));

            // loop through subscribers
            foreach (var subscriber in _entity_subscribers)
            {
                if (subscriber == null) continue;
                subscriber.OnEntityError(error);
            }
        }

        /// <summary>
        /// Called from the entity when an exception is incurred.
        /// </summary>
        /// <param name="ex">the incurred exception</param>
        internal void OnError(Exception ex)
        {
            // raise event
            Error?.Invoke(_entity, new EntityErrorEventArgs(_entity, ex));

            // loop through subscribers
            foreach (var subscriber in _entity_subscribers)
            {
                if (subscriber == null) continue;
                subscriber.OnEntityError(ex.Message);
            }
        }

        /// <summary>
        /// Called from the entity to signify a successfyl format.
        /// </summary>
        internal void OnFormatted()
        {
            // raise event
            Formatted?.Invoke(_entity, EventArgs.Empty);
        }

        /// <summary>
        /// Called from the entity to signify a successful save.
        /// </summary>
        internal void OnSaved()
        {
            Saved?.Invoke(_entity, EventArgs.Empty);
        }

        /// <summary>
        /// Called from the entity to signify a successful validate.
        /// </summary>
        internal void OnValidated()
        {
            // raise the event
            Validated?.Invoke(_entity, EventArgs.Empty);
        }

        /// <summary>
        /// Subscribe to a specific component of the entity.
        /// </summary>
        /// <typeparam name="T">type of component to subscribe to</typeparam>
        /// <param name="subscriber">object inheriting the IComponentSubscriber interface</param>
        public void SubscribeToComponent<T>(IComponentSubscriber<T> subscriber) where T : Component
        {
            // check
            if (subscriber == null) return;
            if (!_entity._components.Exists(x => x.GetType() == typeof(T))) return;
            if (_component_subscribers.ContainsKey(subscriber)) return;

            // subscribe
            _component_subscribers.Add(subscriber, typeof(T));
        }

        /// <summary>
        /// Subscribe to basic entity events (component changed & error)
        /// </summary>
        /// <param name="subscriber">object inheriting the IEntitySubscriber interface</param>
        public void SubscribeToEntity(IEntitySubscriber subscriber)
        {
            // check
            if (subscriber == null) return;
            if (_entity_subscribers.Contains(subscriber)) return;

            // subscribe
            _entity_subscribers.Add(subscriber);
        }

        /// <summary>
        /// Unsubscribe from a specific component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subscriber"></param>
        public void UnsubscribeFromComponent<T>(IComponentSubscriber<T> subscriber) where T : Component
        {
            // check
            if (subscriber == null) return;
            if (!_component_subscribers.ContainsKey(subscriber)) return;

            // remove
            _component_subscribers.Remove(subscriber);
        }

        /// <summary>
        /// Unsubscribe from basic entity events.
        /// </summary>
        /// <param name="subscriber">object inheriting the IEntitySubscriber interface</param>
        public void UnsubscribeFromEntity(IEntitySubscriber subscriber)
        {
            // check
            if (subscriber == null) return;
            if (!_entity_subscribers.Contains(subscriber)) return;

            // remove
            _entity_subscribers.Remove(subscriber);
        }

        /// <summary>
        /// Event raised when a component has been added.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ComponentAdded;

        /// <summary>
        /// Event raised when a component has been changed.
        /// </summary>
        public event EventHandler<ComponentChangedEventArgs> ComponentChanged;

        /// <summary>
        /// Event raised when a component has been removed.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ComponentRemoved;

        /// <summary>
        /// Event raised when an error is incurred.
        /// </summary>
        public event EventHandler<EntityErrorEventArgs> Error;

        /// <summary>
        /// Event raised when the entity has been successfully formatted.
        /// </summary>
        public event EventHandler Formatted;

        /// <summary>
        /// Event raised when the entity has been successfully saved.
        /// </summary>
        public event EventHandler Saved;

        /// <summary>
        /// Event raised when the entity has been successfully validated.
        /// </summary>
        public event EventHandler Validated;
    }
}
