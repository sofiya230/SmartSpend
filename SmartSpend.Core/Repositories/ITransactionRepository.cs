using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Repositories;

public interface ITransactionRepository: IRepository<Transaction>
{

    Task<Transaction> CreateAsync();

    Task<Transaction> GetWithSplitsByIdAsync(int? id);

    Task<(Transaction,bool)> GetWithSplitsAndMatchCategoryByIdAsync(int? id);

    IQueryable<Split> Splits { get; }

    Task<Transaction> EditAsync(int id, Transaction newvalues);

    Task<int> AddSplitToAsync(int id);

    Task<int> RemoveSplitAsync(int id);

    Task<Split> GetSplitByIdAsync(int id);

    Task UpdateSplitAsync(Split split);

    Task BulkEditAsync(string category);

    Task<Stream> AsSpreadsheetAsync(int year, bool allyears, string q);

    Task UploadReceiptAsync(Transaction transaction, Stream stream, string contenttype);

    Task UploadReceiptAsync(int id, Stream stream, string contenttype);

    Task<(Stream stream, string contenttype, string name)> GetReceiptAsync(Transaction transaction);

    Task DeleteReceiptAsync(int id);

    Task FinalizeImportAsync();

    Task CancelImportAsync();

    IEnumerable<Split> CalculateCustomSplitRules(Transaction transaction, string json);

    Task<IEnumerable<string>> CategoryAutocompleteAsync(string q);

    Task AssignBankReferences();

    Task SetSelectedAsync(int id, bool value);

    Task SetHiddenAsync(int id, bool value);

    Task<string> ApplyPayeeAsync(int id);

    Task UpdateTransactionAsync(int id, Transaction item);
}
