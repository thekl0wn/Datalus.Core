namespace Datalus.Core
{
    public interface ISavable : IValidatable, IModifiable
    {
        bool SaveData();
        bool SaveData(bool validate);
    }
}
