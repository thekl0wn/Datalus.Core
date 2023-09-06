using System;

namespace Datalus.Core
{
    public sealed class EntityErrorEventArgs : EventArgs
    {
        internal EntityErrorEventArgs(IEntity entity, string error)
        {
            this.Entity = entity;
            this.Message = error;
        }
        internal EntityErrorEventArgs(IEntity entity, Exception exception) : this(entity, exception.Message)
        {
            this.Exception = exception;
        }

        public IEntity Entity { get; }
        public Exception Exception { get; }
        public string Message { get; }
    }
}
