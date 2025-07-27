using System.Threading.Tasks;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Repositories;

public interface IBudgetTxRepository : IRepository<BudgetTx>
{
    Task BulkDeleteAsync();

    Task SetSelectedAsync(int id, bool value);
}
