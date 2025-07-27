using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Repositories;

public class BudgetTxRepository: BaseRepository<BudgetTx>, IBudgetTxRepository
{
    public BudgetTxRepository(IDataProvider context): base(context)
    {
    }

    public async Task BulkDeleteAsync()
    {
        _context.RemoveRange(All.Where(x => x.Selected == true));
        await _context.SaveChangesAsync();
    }

    public async Task SetSelectedAsync(int id, bool value)
    {
        var item = await GetByIdAsync(id);
        item.Selected = value;
        await UpdateAsync(item);
    }

    protected override IQueryable<BudgetTx> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => (x.Category != null && x.Category.Contains(q)) || (x.Memo != null && x.Memo.Contains(q)));
}
