using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Repositories;

public interface IReceiptRepository
{
    Task<Receipt> UploadReceiptAsync(string filename, Stream stream, string contenttype);
    Task<IEnumerable<Receipt>> GetAllAsync();
    Task<IEnumerable<Receipt>> GetAllOrderByMatchAsync(Transaction tx);
    Task<IEnumerable<Receipt>> GetAllOrderByMatchAsync(int txid);
    Task<Receipt> GetByIdAsync(int id);
    Task<bool> TestExistsByIdAsync(int id);
    Task<bool> AnyAsync();
    Task<ReceiptMatchResult> GetMatchingAsync(Transaction tx);
    Task DeleteAsync(Receipt receipt);
    Task AssignReceipt(Receipt receipt, Transaction tx);
    Task AssignReceipt(int id, int txid);
    Task<int> AssignAll();
    Task<Transaction> CreateTransactionAsync(int? id);

    Task AddTransactionAsync(Transaction tx);
}

public record ReceiptMatchResult
{
    public bool Any { get; init; }
    public int? Matches { get; init; }
    public Receipt Suggested{ get; init; }
}
