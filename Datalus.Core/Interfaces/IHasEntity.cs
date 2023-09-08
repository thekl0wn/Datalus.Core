namespace Datalus.Core
{
    public interface IHasEntity : IRuntime
    {
        IEntity Entity { get; }
    }
}
