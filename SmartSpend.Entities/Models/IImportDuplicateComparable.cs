namespace SmartSpend.Core.Models
{
    public interface IImportDuplicateComparable
    {
        int GetImportHashCode();

        bool ImportEquals(object other);
    }
}
