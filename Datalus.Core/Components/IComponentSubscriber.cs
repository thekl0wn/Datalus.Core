namespace Datalus.Core
{
    public interface IComponentSubscriber
    {
        void OnComponentChanged(string property_name, object new_value);
    }

    public interface IComponentSubscriber<T> : IComponentSubscriber where T : Component
    {

    }
}
