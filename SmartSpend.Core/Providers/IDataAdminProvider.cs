using System.Threading.Tasks;

namespace SmartSpend.Core;

public interface IDataAdminProvider
{
    Task<IDataStatus> GetDatabaseStatus();

    Task ClearTestDataAsync(string id);

    Task ClearDatabaseAsync(string id);

    Task UnhideTransactionsToToday();

    Task SeedDemoSampleData(bool hiddenaftertoday, SampleData.ISampleDataProvider loader);
}

public interface IDataStatus
{
    bool IsEmpty { get; }
    int NumTransactions { get; }
    int NumBudgetTxs { get; }
    int NumPayees { get; }
}
