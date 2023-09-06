namespace Datalus.Core
{
    public sealed class PropertyChangedEventArgs
    {
        public PropertyChangedEventArgs(string property, object old_value, object new_value)
        {
            this.Property = property;
            this.OldValue = old_value;
            this.NewValue = new_value;
        }

        public string Property { get; }
        public object OldValue { get; }
        public object NewValue { get; }
    }
}
