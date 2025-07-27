using System;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SmartSpend.Core.Repositories;


public class PayeeRepository : BaseRepository<Payee>, IPayeeRepository
{
    public PayeeRepository(IDataProvider context) : base(context)
    {
    }

    protected override IQueryable<Payee> ForQuery(string q) => string.IsNullOrEmpty(q) ? OrderedQuery : OrderedQuery.Where(x => x.Category.Contains(q) || x.Name.Contains(q));

    public async Task BulkEditAsync(string category)
    {
        foreach (var item in All.Where(x => x.Selected == true))
        {
            item.Selected = false;
            if (!string.IsNullOrEmpty(category))
            {
                if (category.Contains('('))
                {
                    var originals = item.Category?.Split(":") ?? default;
                    var result = new List<string>();
                    foreach (var component in category.Split(":"))
                    {
                        if (component.StartsWith('(') && component.EndsWith("+)"))
                        {
                            if (Int32.TryParse(component[1..^2], out var position))
                                if (originals.Length >= position)
                                    result.AddRange(originals.Skip(position - 1));
                        }
                        else if (component.StartsWith('(') && component.EndsWith(')'))
                        {
                            if (Int32.TryParse(component[1..^1], out var position))
                                if (originals.Length >= position)
                                    result.AddRange(originals.Skip(position - 1).Take(1));
                        }
                        else
                            result.Add(component);
                    }

                    if (result.Any())
                        item.Category = string.Join(":", result);
                }
                else
                {
                    item.Category = category;
                }
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task BulkDeleteAsync()
    {
        _context.RemoveRange(All.Where(x => x.Selected == true));
        await _context.SaveChangesAsync();
    }

    public Task<Payee> NewFromTransactionAsync(int txid)
    {
        var transaction = _context.Get<Transaction>().Where(x => x.ID == txid).Single();
        var result = new Payee() { Category = transaction.Category, Name = transaction.Payee.Trim() };

        return Task.FromResult(result);
    }

    public async Task LoadCacheAsync()
    {

        payeecache = await _context.ToListNoTrackingAsync(All);
    }

    public Task<string> GetCategoryMatchingPayeeAsync(string Name)
    {
        string result = null;

        if (!string.IsNullOrEmpty(Name))
        {
            IQueryable<Payee> payees = payeecache?.AsQueryable<Payee>() ?? All;

#pragma warning disable CA1866 // Use char overload
            regexpayees = payees.Where(x => x.Name.StartsWith("/") && x.Name.EndsWith("/"));
#pragma warning restore CA1866 // Use char overload

            Payee payee = null;
            foreach (var regexpayee in regexpayees)
                if (new Regex(regexpayee.Name[1..^1]).Match(Name).Success)
                {
                    payee = regexpayee;
                    break;
                }

            if (null == payee)
                payee = payees.FirstOrDefault(x => Name.Contains(x.Name));

            if (null != payee)
                result = payee.Category;
        }

        return Task.FromResult(result);
    }

    public async Task SetSelectedAsync(int id, bool value)
    {
        var item = await GetByIdAsync(id);
        item.Selected = value;
        await UpdateAsync(item);
    }


    List<Payee> payeecache;

    IEnumerable<Payee> regexpayees;
}
