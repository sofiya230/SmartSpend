using System.Threading.Tasks;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Repositories;

public interface IPayeeRepository : IRepository<Payee>
{
    Task BulkEditAsync(string category);

    Task BulkDeleteAsync();

    Task<Payee> NewFromTransactionAsync(int txid);

    Task LoadCacheAsync();

    Task<string> GetCategoryMatchingPayeeAsync(string Name);

    Task SetSelectedAsync(int id, bool value);
}
