namespace Datalus.Core
{
    public interface ISavable : IValidatable
    {
        bool SaveData();
        bool SaveData(bool validate);
    }
}
