namespace Datalus.Core
{
    public interface IEntitySubscriber
    {
        void OnEntityComponentChanged(IComponent component, string property_name, object new_value);
        void OnEntityError(string error);
    }
}
