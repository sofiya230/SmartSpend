using jcoliz.OfficeOpenXml.Serializer;
using OfxSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories;
using Transaction = SmartSpend.Core.Models.Transaction;

namespace YoFi.Core.Importers;

public class TransactionImporter
{
    public TransactionImporter(AllRepositories repos)
    {
        _repository = repos.Transactions;
        _payees = repos.Payees;
    }

    public IEnumerable<int> HighlightIDs => highlights.Select(x => x.ID);

    public async Task QueueImportFromOfxAsync(Stream stream)
    {
        OfxDocument Document = await OfxDocumentReader.FromSgmlFileAsync(stream);

        var created = Document.Statements.SelectMany(x => x.Transactions).Select(
            tx => {
                var payee = (tx.Name ?? tx.Memo)?.Trim();
                var memo = !string.IsNullOrEmpty(tx.Name) && !string.IsNullOrEmpty(tx.Memo) ? tx.Memo.Trim() : null;
                if (!string.IsNullOrEmpty(memo) && !string.IsNullOrEmpty(payee) && memo.StartsWith(payee))
                {
                    payee = memo;
                    memo = null;
                }
                return new Transaction()
                {
                    Amount = tx.Amount,
                    Payee = payee,
                    Memo = memo,
                    BankReference = tx.ReferenceNumber?.Trim(),
                    Timestamp = tx.Date.Value.DateTime
                };
            }
        );

        incoming.AddRange(created);
    }

    public void QueueImportFromXlsx(ISpreadsheetReader reader)
    {
        var items = reader.Deserialize<Transaction>();
        incoming.AddRange(items);

        if (reader.SheetNames.Contains("Split"))
            splits.AddRange(reader.Deserialize<Split>()?.ToLookup(x => x.TransactionID));
    }


    public async Task ProcessImportAsync()
    {
        foreach (var item in incoming)
        {
            item.Selected = true;
            item.Imported = true;
            item.Hidden = true;

            if (string.IsNullOrEmpty(item.BankReference))
                item.GenerateBankReference();
        }




        await _repository.AssignBankReferences();


        var highlightme = ManageConflictingImports(incoming);
        highlights.AddRange(highlightme);


        await _payees.LoadCacheAsync();


        foreach (var item in incoming)
        {

            item.Payee = item.StrippedPayee;
            if (string.IsNullOrEmpty(item.Category))
            {
                var category = await _payees.GetCategoryMatchingPayeeAsync(item.Payee);

                var customsplits = _repository.CalculateCustomSplitRules(item, category);
                if (customsplits.Any())
                    item.Splits = customsplits.ToList();
                else
                    item.Category = category;
            }


            var mysplits = splits.Where(x => x.Key == item.ID).SelectMany(x => x);
            if (mysplits.Any())
            {
                item.Splits = mysplits.ToList();
                item.Category = null;
                foreach (var split in item.Splits)
                {
                    split.ID = 0;
                    split.TransactionID = 0;
                }
            }

            item.ID = 0;
        }


        await _repository.BulkInsertAsync(incoming);
    }

    #region Internals

    private IEnumerable<Transaction> ManageConflictingImports(IEnumerable<Transaction> incoming)
    {
        var result = new List<Transaction>();



        /*
            SELECT [x].[ID], [x].[AccountID], [x].[Amount], [x].[BankReference], [x].[Category], [x].[Hidden], [x].[Imported], [x].[Memo], [x].[Payee], [x].[ReceiptUrl], [x].[Selected], [x].[SubCategory], [x].[Timestamp]
            FROM [Transactions] AS [x]
            WHERE [x].[BankReference] IN (N'A1ABC7FE34871F02304982126CAF5C5C', N'EE49717DE89A3D97A9003230734A94B7')
            */
        var uniqueids = incoming.Select(x => x.BankReference).ToHashSet();
        var conflicts = _repository.All.Where(x => uniqueids.Contains(x.BankReference)).ToLookup(x => x.BankReference, x => x);

        if (conflicts.Any())
        {
            foreach (var tx in incoming)
            {

                if (conflicts[tx.BankReference].Any())
                {
                    Console.WriteLine($"{tx.Payee} ({tx.BankReference}) has a conflict");

                    tx.Selected = false;


                    if (!conflicts[tx.BankReference].Any(x => x.Equals(tx)))
                    {
                        Console.WriteLine($"Conflict may be a false positive, flagging for user.");
                        result.Add(tx);
                    }
                }
            }
        }

        return result;
    }

    #endregion

    #region Fields

    private readonly List<Transaction> incoming = new();

    private readonly List<IGrouping<int, Split>> splits = new();

    private readonly List<Transaction> highlights = new();

    private readonly ITransactionRepository _repository;

    private readonly IPayeeRepository _payees;

    #endregion

}
