using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Repositories.Wire;

namespace SmartSpend.Core.Repositories;

public interface IRepository<T> where T: class //, IModelItem<T>
{
    #region CRUD operations

    Task<IWireQueryResult<T>> GetByQueryAsync(IWireQueryParameters parms);

    Task<int> GetPageSizeAsync();

    Task SetPageSizeAsync(int value);

    IQueryable<T> All { get; }

    Task<T> GetByIdAsync(int? id);

    Task<bool> TestExistsByIdAsync(int id);

    Task AddAsync(T item);

    Task AddRangeAsync(IEnumerable<T> items);

    Task BulkInsertAsync(IList<T> items);

    Task UpdateAsync(T item);

    Task UpdateRangeAsync(IEnumerable<T> items);

    Task RemoveAsync(T item);

    Task RemoveRangeAsync(IEnumerable<T> items);
    #endregion

    #region Spreadsheet import/export

    Stream AsSpreadsheet();

    #endregion
}
