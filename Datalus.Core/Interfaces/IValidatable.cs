namespace Datalus.Core
{
    public interface IValidatable : IFormattable
    {
        bool ValidateData();
    }
}
