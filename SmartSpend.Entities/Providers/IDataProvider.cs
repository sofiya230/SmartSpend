using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SmartSpend.Core.Models;

namespace SmartSpend.Core
{
    public interface IDataProvider
    {
        IQueryable<T> Get<T>() where T : class;

        IQueryable<TEntity> GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class;

        void Add(object item);

        void AddRange(IEnumerable<object> items);
       
        void Update(object item);
        
        void UpdateRange(IEnumerable<object> items);
        
        void Remove(object item);
        
        void RemoveRange(IEnumerable<object> items);
        
        Task SaveChangesAsync();

        Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class;

        Task<int> CountAsync<T>(IQueryable<T> query) where T : class;

        Task<bool> AnyAsync<T>(IQueryable<T> query) where T : class;

        Task<int> ClearAsync<T>() where T : class;

        Task BulkInsertAsync<T>(IList<T> items) where T : class;

        Task BulkDeleteAsync<T>(IQueryable<T> items) where T : class;

        Task BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns) where T : class;
    }
}
