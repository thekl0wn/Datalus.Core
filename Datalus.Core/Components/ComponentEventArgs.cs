namespace Datalus.Core
{
    public sealed class ComponentEventArgs
    {
        internal ComponentEventArgs(IComponent component)
        {
            this.Component = component;
        }

        public IComponent Component { get; }
    }
}
