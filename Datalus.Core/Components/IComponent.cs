namespace Datalus.Core
{
    public interface IComponent : IHasEntity, IModifiable, IRuntime
    {
        int EntityID { get; }
        int ComponentID { get; }
    }
}
