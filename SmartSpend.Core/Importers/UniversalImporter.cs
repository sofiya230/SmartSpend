using jcoliz.OfficeOpenXml.Serializer;
using YoFi.Core.Importers;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories;

namespace YoFi.Core.Importers;

public class UniversalImporter : TransactionImporter
{
    private readonly IImporter<Payee> _payeeImporter;
    private readonly IImporter<BudgetTx> _budgettxImporter;
    private readonly IReceiptRepository _receiptRepository;

    public IEnumerable<BudgetTx> ImportedBudgetTxs { get; private set; } = Enumerable.Empty<BudgetTx>();
    public IEnumerable<Payee> ImportedPayees { get; private set; } = Enumerable.Empty<Payee>();
    public List<Receipt> ImportedReceipts { get; } = new List<Receipt>();

    public UniversalImporter(AllRepositories repos) : base(repos)
    {
        _budgettxImporter = new BaseImporter<BudgetTx>(repos.BudgetTxs);
        _payeeImporter = new BaseImporter<Payee>(repos.Payees);
        _receiptRepository = repos.Receipts;
    }

    public void QueueImportFromXlsx(Stream stream)
    {

        using var ssr = new SpreadsheetReader();
        ssr.Open(stream);
        if (ssr.SheetNames.Any())
        {
            if (ssr.SheetNames.Contains(nameof(BudgetTx)))
            {
                _budgettxImporter.QueueImportFromXlsx(ssr);
            }
            if (ssr.SheetNames.Contains(nameof(Payee)))
            {
                _payeeImporter.QueueImportFromXlsx(ssr);
            }
            if (ssr.SheetNames.Contains(nameof(Transaction)))
            {
                base.QueueImportFromXlsx(ssr);
            }

            var firstname = ssr.SheetNames.First();
            var others = new string[] { nameof(BudgetTx), nameof(Payee), nameof(Transaction), nameof(Split) };
            if (!others.Contains(firstname))
            {
                base.QueueImportFromXlsx(ssr);
            }
        }
    }

    public void QueueImportFromXlsx<T>(Stream stream)
    {
        if (typeof(T) == typeof(BudgetTx))
        {
            _budgettxImporter.QueueImportFromXlsx(stream);
        }
        else
        {
            if (typeof(T) == typeof(Payee))
            {
                _payeeImporter.QueueImportFromXlsx(stream);
            }
            else
                throw new NotImplementedException();
        }
    }

    public async Task QueueImportFromImageAsync(string filename, Stream stream, string contenttype)
    {
        var receipt = await _receiptRepository.UploadReceiptAsync(filename, stream, contenttype);
        if (receipt != null)
            ImportedReceipts.Add(receipt);
    }

    public new async Task ProcessImportAsync()
    {
        ImportedBudgetTxs = await _budgettxImporter.ProcessImportAsync();
        ImportedPayees = await _payeeImporter.ProcessImportAsync();
        await base.ProcessImportAsync();
    }
}
