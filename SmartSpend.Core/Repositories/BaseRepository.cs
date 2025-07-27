using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories.Wire;

namespace SmartSpend.Core.Repositories;

public class BaseRepository<T> : IRepository<T> where T: class, IModelItem<T>, new()
{
    #region Constructor

    public BaseRepository(IDataProvider context)
    {
        _context = context;
    }
    #endregion

    #region CRUD Operations

    public IQueryable<T> All => _context.Get<T>();

    protected IQueryable<T> OrderedQuery => new T().InDefaultOrder(All);

    protected virtual IQueryable<T> ForQuery(string q) => throw new NotImplementedException();

    protected virtual IQueryable<T> ForQuery(IWireQueryParameters parms) => ForQuery(parms.Query);

    public async Task<T> GetByIdAsync(int? id) 
    {
        var list = await _context.ToListNoTrackingAsync(_context.Get<T>().Where(x => x.ID == id.Value));
        return list.Single();
    }

    public Task<bool> TestExistsByIdAsync(int id) => _context.AnyAsync(_context.Get<T>().Where(x => x.ID == id));

    public async Task AddAsync(T item)
    {
        item.ID = 0;
        _context.Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> items)
    {
        _context.AddRange(items);
        await _context.SaveChangesAsync();
    }

    public Task BulkInsertAsync(IList<T> items)
    {
        return _context.BulkInsertAsync(items);
    }

    public Task UpdateAsync(T item)
    {
        _context.Update(item);
        return _context.SaveChangesAsync();
    }

    public Task UpdateRangeAsync(IEnumerable<T> items)
    {
        _context.UpdateRange(items);
        return _context.SaveChangesAsync();
    }

    public Task RemoveAsync(T item)
    {
        _context.Remove(item);
        return _context.SaveChangesAsync();
    }

    public Task RemoveRangeAsync(IEnumerable<T> items)
    {
        _context.RemoveRange(items);
        return _context.SaveChangesAsync();
    }
    #endregion

    #region Wire Interface
    public async Task<IWireQueryResult<T>> GetByQueryAsync(IWireQueryParameters parms)
    {
        var query = ForQuery(parms);

        IWirePageInfo pages = null;
        if (!parms.All)
        {
            var count = await _context.CountAsync(query);
            pages = new WirePageInfo(totalitems: count, page: parms.Page ?? 1, pagesize: PageSize);
            if (count > PageSize)
            {
                query = query.Skip(pages.FirstItem - 1).Take(pages.NumItems);
            }
        }

        var list = await _context.ToListNoTrackingAsync(query);
        IWireQueryResult<T> result = new WireQueryResult<T>() { Items = list, PageInfo = pages, Parameters = parms };
        return result;
    }

    public const int DefaultPageSize = 25;

    private int PageSize = DefaultPageSize;

    public Task<int> GetPageSizeAsync() => Task.FromResult(PageSize);

    public Task SetPageSizeAsync(int value)
    {
        PageSize = value;

        return Task.CompletedTask;
    }

    #endregion

    #region Exporter


    public Stream AsSpreadsheet()
    {
        var stream = new MemoryStream();
        using (var ssw = new SpreadsheetWriter())
        {
            ssw.Open(stream);
            ssw.Serialize(OrderedQuery);
        }

        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }
    #endregion

    #region Fields
    protected readonly IDataProvider _context;

    #endregion

}
