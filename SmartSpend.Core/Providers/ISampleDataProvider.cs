using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartSpend.Core.SampleData;

public interface ISampleDataProvider
{
    Task<IEnumerable<ISampleDataSeedOffering>> ListSeedOfferingsAsync();

    Task<string> SeedAsync(string id, bool hidden = false);

    Task<IEnumerable<ISampleDataDownloadOffering>> ListDownloadOfferingsAsync();

    Task<Stream> DownloadSampleDataAsync(string id);
}

public enum SampleDataSeedOfferingCondition { Always = 0, Empty, MoreTransactionsReady, NoTransactions, NoBudgetTxs, NoPayees };

public interface ISampleDataSeedOffering
{
    string ID { get; }
    string Title { get; }

    string Description { get; }

    bool IsAvailable { get; }

    IEnumerable<string> Rules { get; }
}

public enum SampleDataDownloadOfferingKind { None = 0, Primary, Monthly }

public enum SampleDataDownloadFileType { None = 0, XLSX, OFX }

public interface ISampleDataDownloadOffering
{
    string ID { get; }

    SampleDataDownloadFileType FileType { get; }

    string Description { get; }

    SampleDataDownloadOfferingKind Kind { get; }
}

public interface ISampleDataConfiguration
{
    string Directory { get; }
}
