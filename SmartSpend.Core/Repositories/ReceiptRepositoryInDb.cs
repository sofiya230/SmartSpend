using Common.DotNet;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Repositories;

public class ReceiptRepositoryInDb : IReceiptRepository
{
    #region Fields

    private readonly IDataProvider _context;
    private readonly ITransactionRepository _txrepo;
    private readonly IStorageService _storage;
    private readonly IClock _clock;
    public const string Prefix = "r/";
    #endregion

    #region Constructor

    public ReceiptRepositoryInDb(IDataProvider context, ITransactionRepository txrepo, IStorageService storage, IClock clock)
    {
        _context = context;
        _txrepo = txrepo;
        _storage = storage;
        _clock = clock;
    }

    #endregion

    #region Public Interface


    public async Task<Transaction> CreateTransactionAsync(int? id)
    {
        if (id.HasValue && await TestExistsByIdAsync(id.Value))
        {
            var r = await GetByIdAsync(id.Value);
            var tx = r.AsTransaction();

            return tx;
        }
        else
        {
            return await _txrepo.CreateAsync();
        }
    }

    public async Task AddTransactionAsync(Transaction tx)
    {

        int? rid = null;
        if (!string.IsNullOrEmpty(tx.ReceiptUrl))
        {
            var idregex = new Regex("\\[ID (?<id>[0-9]+)\\]$");
            var match = idregex.Match(tx.ReceiptUrl);
            if (match.Success)
            {
                rid = int.Parse(match.Groups["id"].Value);
            }
            tx.ReceiptUrl = null;
        }

        await _txrepo.AddAsync(tx);

        if (rid.HasValue && await TestExistsByIdAsync(rid.Value))
        {
            var r = await GetByIdAsync(rid.Value);
            await AssignReceipt(r, tx);
        }
    }


    public async Task<int> AssignAll()
    {
        var result = 0;
        var receipts = await GetAllAsync();
        foreach (var receipt in receipts)
        {
            if (receipt.Matches.Count > 0 && !receipt.Matches.Skip(1).Any())
            {
                var tx = receipt.Matches.Single();
                await AssignReceipt(receipt, tx);
                ++result;
            }
        }

        return result;
    }


    public async Task AssignReceipt(Receipt receipt, Transaction tx)
    {
#if false
        var match = receipt.MatchesTransaction(tx);
        if (match <= 0)
            throw new ArgumentException("Receipt and transaction do not match");
#endif
        tx.ReceiptUrl = $"{Prefix}{receipt.ID}";

        if (!string.IsNullOrEmpty(receipt.Memo))
        {
            tx.Memo = receipt.Memo;
        }

        await _txrepo.UpdateAsync(tx);

        _context.Remove(receipt);
        await _context.SaveChangesAsync();
    }

    public async Task AssignReceipt(int id, int txid)
    {
        var receipt = await GetByIdAsync(id);
        var tx = await _txrepo.GetByIdAsync(txid);
        await AssignReceipt(receipt, tx);
    }

    public async Task DeleteAsync(Receipt receipt)
    {
        var query = _context.Get<Receipt>().Where(x => x.ID == receipt.ID);
        if (await _context.AnyAsync(query))
        {
            _context.Remove(receipt);
        }
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Receipt>> GetAllAsync()
    {

        var receipts = await _context.ToListNoTrackingAsync(_context.Get<Receipt>().OrderByDescending(x => x.Timestamp).ThenBy(x => x.Name).ThenByDescending(x => x.Amount)) as IEnumerable<Receipt>;


        if (receipts.Any())
        {
            var query = Receipt.TransactionsForReceipts(_txrepo.All, receipts);
            var txs = await _context.ToListNoTrackingAsync(query);

            foreach (var receipt in receipts)
            {
                receipt.Matches = txs
                                    .Select(t => (quality: receipt.MatchesTransaction(t), t))
                                    .Where(x => x.quality > 0)
                                    .OrderByDescending(x => x.quality)
                                    .Select(x => x.t)
                                    .ToList();
            }
        }

        return receipts;
    }

    public async Task<ReceiptMatchResult> GetMatchingAsync(Transaction tx)
    {
        var any = await AnyAsync();
        var receipts = await _context.ToListNoTrackingAsync(_context.Get<Receipt>());
        var result = receipts
                .Select(r => (quality: r.MatchesTransaction(tx), r))
                .Where(x => x.quality > 0)
                .OrderByDescending(x => x.quality)
                .Select(x => x.r)
                .ToList();

        return new ReceiptMatchResult() { Any = any, Matches = result.Count, Suggested = result.FirstOrDefault() };
    }

    public async Task<IEnumerable<Receipt>> GetAllOrderByMatchAsync(Transaction tx)
    {
        var receipts = await _context.ToListNoTrackingAsync(_context.Get<Receipt>());

        var result = receipts
                .Select(r => (quality: r.MatchesTransaction(tx), r))
                .OrderByDescending(x => x.quality)
                .ThenByDescending(x => x.r.Timestamp)
                .Select(x => x.r)
                .ToList();

        return result;
    }

    public async Task<IEnumerable<Receipt>> GetAllOrderByMatchAsync(int txid)
    {
        var tx = await _txrepo.GetByIdAsync(txid);
        var qresult = await GetAllOrderByMatchAsync(tx);

        return qresult;
    }

    public async Task<Receipt> UploadReceiptAsync(string filename, Stream stream, string contenttype)
    {
        var item = Receipt.FromFilename(filename, _clock);
        _context.Add(item);
        await _context.SaveChangesAsync();
        await _storage.UploadBlobAsync($"{Prefix}{item.ID}", stream, contenttype);

        return item;
    }

    public async Task<Receipt> GetByIdAsync(int id)
    {
        var query = _context.Get<Receipt>().Where(x => x.ID == id);
        var list = await _context.ToListNoTrackingAsync(query);
        var result = list[0];

        var qt = Receipt.TransactionsForReceipts(_txrepo.All, new[] { result });
        var txs = await _context.ToListNoTrackingAsync(qt);
        result.Matches = txs
                            .Select(t => (quality: result.MatchesTransaction(t), t))
                            .Where(x => x.quality > 0)
                            .OrderByDescending(x => x.quality)
                            .Select(x => x.t)
                            .ToList();
        return result;
    }
    public Task<bool> TestExistsByIdAsync(int id) => _context.AnyAsync(_context.Get<Receipt>().Where(x => x.ID == id));

    public Task<bool> AnyAsync() => _context.AnyAsync(_context.Get<Receipt>());

#endregion
}
