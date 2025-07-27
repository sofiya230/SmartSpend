using Common.DotNet;
using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories.Wire;
using Transaction = SmartSpend.Core.Models.Transaction;

namespace SmartSpend.Core.Repositories;
public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(
        IDataProvider context,
        IClock clock,
        IPayeeRepository payeeRepository,
        IStorageService storage = null
    ) : base(context)
    {
        _storage = storage;
        _clock = clock;
        _payeeRepository = payeeRepository;
    }

    #region Read

    protected override IQueryable<Transaction> ForQuery(string q)
    {
        var qbuilder = new TransactionsQueryBuilder(Transaction.InDefaultOrder(_context.GetIncluding<Transaction, ICollection<Split>>(x => x.Splits)), _clock);
        qbuilder.BuildForQ(q);
        return qbuilder.Query;
    }

    protected override IQueryable<Transaction> ForQuery(IWireQueryParameters parms)
    {
        var qbuilder = new TransactionsQueryBuilder(Transaction.InDefaultOrder(_context.GetIncluding<Transaction, ICollection<Split>>(x => x.Splits)), _clock);
        qbuilder.BuildForQ(parms.Query);
        qbuilder.ApplyOrderParameter(parms.Order);
        qbuilder.ApplyViewParameter(parms.View);
        return qbuilder.Query;
    }

    public async Task<Transaction> GetWithSplitsByIdAsync(int? id)
    {
        var query = _context.GetIncluding<Transaction, ICollection<Split>>(x => x.Splits).Where(x => x.ID == id.Value);
        var list = await _context.ToListNoTrackingAsync(query);
        return list.Single();
    }

    public async Task<(Transaction,bool)> GetWithSplitsAndMatchCategoryByIdAsync(int? id)
    {
        var auto_category = false;
        var transaction = await GetWithSplitsByIdAsync(id);
        if (string.IsNullOrEmpty(transaction.Category))
        {
            var category = await _payeeRepository.GetCategoryMatchingPayeeAsync(transaction.StrippedPayee);
            if (category != null)
            {
                transaction.Category = category;
                auto_category = true;
            }
        }

        return (transaction, auto_category);
    }

    public IQueryable<Split> Splits => _context.GetIncluding<Split, Transaction>(x => x.Transaction);

    #endregion

    #region Update

    public async Task<Transaction> EditAsync(int id, Transaction newvalues)
    {
        var item = await GetByIdAsync(id);
        item.Memo = newvalues.Memo;
        item.Payee = newvalues.Payee;
        item.Category = newvalues.Category;
        await UpdateAsync(item);

        return item;
    }

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
                            if (int.TryParse(component[1..^2], out var position))
                            {
                                if (originals.Length >= position)
                                {
                                    result.AddRange(originals.Skip(position - 1));
                                }
                            }
                        }
                        else if (component.StartsWith('(') && component.EndsWith(')'))
                        {
                            if (int.TryParse(component[1..^1], out var position))
                            {
                                if (originals.Length >= position)
                                {
                                    result.AddRange(originals.Skip(position - 1).Take(1));
                                }
                            }
                        }
                        else
                        {
                            result.Add(component);
                        }
                    }

                    if (result.Count > 0)
                    {
                        item.Category = string.Join(":", result);
                    }
                }
                else
                {
                    item.Category = category;
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> AddSplitToAsync(int id)
    {
        var transaction = await GetWithSplitsByIdAsync(id);
        var result = new Split() { Category = transaction.Category };


        var currentamount = transaction.Splits.Select(x => x.Amount).Sum();
        var remaining = transaction.Amount - currentamount;
        result.Amount = remaining;

        transaction.Splits.Add(result);


        transaction.Category = null;

        await UpdateAsync(transaction);

        return result.ID;
    }

    public async Task<int> RemoveSplitAsync(int id)
    {
        var split = await GetSplitByIdAsync(id);
        var category = split.Category;
        var txid = split.TransactionID;

        await RemoveSplitAsync(split);

        if (await TestExistsByIdAsync(txid))
        {
            var tx = await GetByIdAsync(txid);
            if (!tx.HasSplits)
            {
                tx.Category = category;
                await UpdateAsync(tx);
            }
        }
   

        return txid;
    }

    public async Task<Split> GetSplitByIdAsync(int id)
    {
        var list = await _context.ToListNoTrackingAsync(_context.Get<Split>().Where(x => x.ID == id));
        return list.Single();
    }

    public Task UpdateSplitAsync(Split split)
    {
        _context.Update(split);
        return _context.SaveChangesAsync();
    }

    private Task RemoveSplitAsync(Split split)
    {
        _context.Remove(split);
        return _context.SaveChangesAsync();
    }

    public async Task AssignBankReferences()
    {

        var needbankrefs = All.Where(x => null == x.BankReference);
        var any = await _context.AnyAsync(needbankrefs);
        if (any)
        {
            foreach (var tx in needbankrefs)
            {
                tx.GenerateBankReference();
            }

            await UpdateRangeAsync(needbankrefs);
        }
    }

    public async Task SetSelectedAsync(int id, bool value)
    {
        var item = await GetByIdAsync(id);
        item.Selected = value;
        await UpdateAsync(item);
    }

    public async Task SetHiddenAsync(int id, bool value)
    {
        var item = await GetByIdAsync(id);
        item.Hidden = value;
        await UpdateAsync(item);
    }

    public async Task<string> ApplyPayeeAsync(int id)
    {
        var item = await GetByIdAsync(id);

        var category = await _payeeRepository.GetCategoryMatchingPayeeAsync(item.StrippedPayee) 
            ?? throw new KeyNotFoundException($"No payee found for {item.StrippedPayee}");
        var result = category;

        var customsplits = CalculateCustomSplitRules(item, category);
        if (customsplits.Any())
        {
            item.Splits = customsplits.ToList();
            result = "SPLIT"; // This is what we display in the UI to indicate a transaction has a split
        }
        else
        {
            item.Category = category;
        }

        await UpdateAsync(item);

        return result;
    }

    #endregion

    #region Export

    public async Task<Stream> AsSpreadsheetAsync(int Year, bool allyears, string q)
    {
        var transactionsquery = ForQuery(q);

        transactionsquery = transactionsquery.Where(x => x.Hidden != true);
        if (!allyears)
            transactionsquery = transactionsquery.Where(x => x.Timestamp.Year == Year);
        transactionsquery = transactionsquery
            .OrderByDescending(x => x.Timestamp);

        var transactionsdtoquery = transactionsquery
            .Select(x => new TransactionExportDto()
            {
                ID = x.ID,
                Amount = x.Amount,
                Timestamp = x.Timestamp,
                Category = x.Category,
                Payee = x.Payee,
                Memo = x.Memo,
                ReceiptUrl = x.ReceiptUrl,
                BankReference = x.BankReference
            }
            );
        var transactions = await _context.ToListNoTrackingAsync(transactionsdtoquery);


        var splitsquery = _context.GetIncluding<Split, Transaction>(x => x.Transaction).Where(x => transactionsquery.Contains(x.Transaction)).OrderByDescending(x => x.Transaction.Timestamp);
        var splits = await _context.ToListNoTrackingAsync(splitsquery);


        var stream = new MemoryStream();
        using (var ssw = new SpreadsheetWriter())
        {
            ssw.Open(stream);
            ssw.Serialize(transactions, sheetname: nameof(Transaction));

            if (splits.Count > 0)
                ssw.Serialize(splits);
        }


        stream.Seek(0, SeekOrigin.Begin);

        return stream as Stream;
    }

    #endregion

    #region Receipts

    public Task<Transaction> CreateAsync()
    {
        return Task.FromResult( new Transaction() { Timestamp = _clock.Now.Date } );
    }

    public async Task UploadReceiptAsync(Transaction transaction, Stream stream, string contenttype)
    {

        if (null == _storage)
            throw new ApplicationException("Storage is not defined");

        var blobname = transaction.ID.ToString();

        await _storage.UploadBlobAsync(blobname, stream, contenttype);


        transaction.ReceiptUrl = blobname;
        await UpdateAsync(transaction);
    }

    public async Task UploadReceiptAsync(int id, Stream stream, string contenttype)
    {
        var item = await GetByIdAsync(id);

        if (!string.IsNullOrEmpty(item.ReceiptUrl))
            throw new ApplicationException($"This transaction already has a receipt. Delete the current receipt before uploading a new one.");

        await UploadReceiptAsync(item, stream, contenttype);
    }

    public async Task<(Stream stream, string contenttype, string name)> GetReceiptAsync(Transaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.ReceiptUrl))
            return (null, null, null);

        if (null == _storage)
            throw new ApplicationException("Storage is not defined");

        var name = transaction.ID.ToString();


        if (int.TryParse(transaction.ReceiptUrl, out _))
            name = transaction.ReceiptUrl;


        if (transaction.ReceiptUrl.StartsWith("r/"))
            name = transaction.ReceiptUrl;

        var stream = new MemoryStream();
        var contenttype = await _storage.DownloadBlobAsync(name, stream);

        if ("application/octet-stream" == contenttype)
            contenttype = "application/pdf";

        stream.Seek(0, SeekOrigin.Begin);

        return (stream, contenttype, name);
    }

    public async Task DeleteReceiptAsync(int id)
    {
        var transaction = await GetByIdAsync(id);
        transaction.ReceiptUrl = null;
        await UpdateAsync(transaction);
    }

#endregion

#region Import

public async Task FinalizeImportAsync()
    {
        var accepted = All.Where(x => x.Imported == true && x.Selected == true);
        await _context.BulkUpdateAsync(accepted, new Transaction() { Hidden = false, Imported = false, Selected = false }, [ "Hidden", "Imported", "Selected" ]);

        var rejected = All.Where(x => x.Imported == true && x.Selected != true);
        await _context.BulkDeleteAsync(rejected);
    }

    public Task CancelImportAsync()
    {
        var allimported = OrderedQuery.Where(x => x.Imported == true);

        return _context.BulkDeleteAsync(allimported);
    }


    public IEnumerable<Split> CalculateCustomSplitRules(Transaction transaction, string rule)
    {
        try
        {
            if (string.IsNullOrEmpty(rule))
                throw new ArgumentNullException(nameof(rule));


            var trimmed = rule.Trim();
            if (!trimmed.Contains("[Loan]"))
                throw new ArgumentException("Missing Loan tag",nameof(rule));

            var split = trimmed.Split("[Loan]");
            if (split.Length != 2)
                throw new ArgumentException("Too many components", nameof(rule));

            var category = split[0].Trim();

            var loan = JsonSerializer.Deserialize<Loan>(split[1], jsonSerializerOptions);
            if (loan.Origination is null)
                throw new ArgumentException("No loan origination found", nameof(rule));

            if (string.IsNullOrEmpty(loan.Principal))
                loan.Principal = category;
            else if (string.IsNullOrEmpty(loan.Interest))
                loan.Interest = category;


            return loan.PaymentSplitsForDate(transaction.Timestamp).Select(x => new Split() { Amount = x.Value, Category = x.Key });
        }
        catch
        {

            return Enumerable.Empty<Split>();
        }
    }

    public async Task<IEnumerable<string>> CategoryAutocompleteAsync(string q)
    {
        if (string.IsNullOrEmpty(q))
            return Enumerable.Empty<string>();

        try
        {
            const int numresults = 10;

            var txd = All.Where(x => x.Timestamp > _clock.Now.AddMonths(-18) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);

            var spd = Splits.Where(x => x.Transaction.Timestamp > _clock.Now.AddMonths(-18) && x.Category.Contains(q)).GroupBy(x => x.Category).Select(g => new { g.Key, Value = g.Count() }).OrderByDescending(x => x.Value).Take(numresults);


            var txresult = await _context.ToListNoTrackingAsync(txd.GroupBy(x => x.Key).Select(x => new { x.Key, Value = x.Sum(g => g.Value) }));
            var splitresult = await _context.ToListNoTrackingAsync(spd.GroupBy(x => x.Key).Select(x => new { x.Key, Value = x.Sum(g => g.Value) }));

            var result = txresult.Concat(splitresult).ToLookup(x=>x.Key).Select(x=> new { x.Key, Value = x.Sum(y => y.Value) }).OrderByDescending(x => x.Value).Take(numresults).Select(x => x.Key).ToList();

            return result;
        }
        catch (Exception)
        {
            return Enumerable.Empty<string>();
        }
    }

    public async Task UpdateTransactionAsync(int id, Transaction item)
    {

        var oldtransaction = await GetByIdAsync(id);
        var oldreceipturl = oldtransaction.ReceiptUrl;

        item.ReceiptUrl = oldreceipturl;

        await UpdateAsync(item);
    }

    #endregion

    #region Fields

    private readonly IStorageService _storage;
    private readonly IClock _clock;
    private readonly IPayeeRepository _payeeRepository;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };


    #endregion

    #region Internal Query-builder helpers



    #endregion

    #region Data Transfer Objects

    internal class TransactionExportDto 
    {
        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Payee { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public string Memo { get; set; }
        public string BankReference { get; set; }
        public string ReceiptUrl { get; set; }
    }

    #endregion
}
