using Common.DotNet;


using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.SampleData;

public class SampleDataProvider : ISampleDataProvider
{
    private readonly IDataProvider _context;
    private readonly IClock _clock;
    private readonly ISampleDataConfiguration _config;

    public SampleDataProvider(IDataProvider context, IClock clock, ISampleDataConfiguration config)
    {
        _context = context;
        _clock = clock;
        _config = config;
    }

    public Task<Stream> DownloadSampleDataAsync(string id)
    {
        Stream stream;

        var instream = File.OpenRead($"{_config.Directory}/SampleData-Full.xlsx");

        if ("full" == id)
        {
            stream = instream;
        }
        else if (nameof(BudgetTx) == id)
        {
            using var ssr = new SpreadsheetReader();
            ssr.Open(instream);
            var items = ssr.Deserialize<BudgetTx>();

            stream = new MemoryStream();
            using (var ssw = new SpreadsheetWriter())
            {
                ssw.Open(stream);
                ssw.Serialize(items);
            }
            stream.Seek(0, SeekOrigin.Begin);
        }
        else if (nameof(Payee) == id)
        {
            using var ssr = new SpreadsheetReader();
            ssr.Open(instream);
            var items = ssr.Deserialize<Payee>();

            stream = new MemoryStream();
            using (var ssw = new SpreadsheetWriter())
            {
                ssw.Open(stream);
                ssw.Serialize(items);
            }
            stream.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            var split = id.Split('-');
            if (split.Length == 2 && int.TryParse(split[1], out var month))
            {
                using var ssr = new SpreadsheetReader();
                ssr.Open(instream);
                var txs = ssr.Deserialize<Transaction>();
                var splits = ssr.Deserialize<Split>();

                var outtxs = txs.Where(x => x.Timestamp.Month == month);
                var outtxids = outtxs.Where(x => x.ID > 0).Select(x => x.ID).ToHashSet();
                var outsplits = splits.Where(x => outtxids.Contains(x.TransactionID));

                if (SampleDataDownloadFileType.XLSX == Enum.Parse<SampleDataDownloadFileType>(split[0]))
                {
                    stream = new MemoryStream();
                    using (var ssw = new SpreadsheetWriter())
                    {
                        ssw.Open(stream);
                        ssw.Serialize(outtxs);
                        ssw.Serialize(outsplits);
                    }
                    stream.Seek(0, SeekOrigin.Begin);
                }
                else if (SampleDataDownloadFileType.OFX == Enum.Parse<SampleDataDownloadFileType>(split[0]))
                {
                    stream = new MemoryStream();
                    SampleDataOfx.WriteToOfx(outtxs, stream);

                    stream.Seek(0, SeekOrigin.Begin);
                }
                else
                    throw new ApplicationException($"Not found sample data ID {id}");
            }
            else
                throw new ApplicationException($"Not found sample data ID {id}");
        }

        return Task.FromResult(stream);
    }

    public async Task<IEnumerable<ISampleDataDownloadOffering>> ListDownloadOfferingsAsync()
    {
        using var stream = Common.DotNet.Data.SampleData.Open("SampleDataDownloadOfferings.json");

        var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var inputs = await JsonSerializer.DeserializeAsync<List<DownloadOffering>>(stream, options);

        var result = 
            inputs
                .Where(x => x.Kind == SampleDataDownloadOfferingKind.Primary)
                .Concat
                (
                    inputs
                        .Where(x => x.Kind == SampleDataDownloadOfferingKind.Monthly)
                        .SelectMany(o => 
                            Enumerable.Range(1, 12)
                            .Select(m=> 
                                new DownloadOffering() 
                                { 
                                    FileType = o.FileType, 
                                    Kind = o.Kind,
                                    Description = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m),
                                    ID = $"{o.FileType}-{m}"
                                } 
                            )
                        )
                )
                .ToList();

        return result;
    }

    public async Task<IEnumerable<ISampleDataSeedOffering>> ListSeedOfferingsAsync()
    {
        using var stream = Common.DotNet.Data.SampleData.Open("SampleDataSeedOfferings.json");

        var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var result = await JsonSerializer.DeserializeAsync<List<SeedOffering>>(stream, options);

        foreach (var offering in result)
        {
            offering.IsAvailable = RulesOK(offering.Rules);
            offering.Description = offering.Description.Replace("{today}", _clock.Now.ToString("D"));
        }

        return result;
    }

    public async Task<string> SeedAsync(string id, bool hidden = false)
    {
        var results = new List<string>();

        var offerings = await ListSeedOfferingsAsync();
        var found = offerings.Where(x => x.ID == id);
        if (!found.Any())
            throw new ApplicationException($"Not found seed ID {id}");
        var offering = found.Single();

        if (!offering.IsAvailable)
            throw new ApplicationException($"This data type is unavailable: {offering.Description}");

        var instream = File.OpenRead($"{_config.Directory}/SampleData-Full.xlsx");
        using var ssr = new SpreadsheetReader();
        ssr.Open(instream);

        if (offering.Rules.Contains(nameof(Transaction)))
        {
            var txs = ssr.Deserialize<Transaction>().ToList();

            if (hidden)
                foreach (var tx in txs)
                    tx.Hidden = true;

            if (ssr.SheetNames.Contains("Split"))
            {
                var splits = ssr.Deserialize<Split>().ToLookup(x => x.TransactionID);
                foreach(var tx in txs.Where(x=>x.ID > 0))
                {
                    if (splits.Contains(tx.ID))
                    {
                        tx.Splits = splits[tx.ID].ToList();
                        tx.Category = null;
                        foreach (var split in tx.Splits)
                        {
                            split.ID = 0;
                            split.TransactionID = 0;
                        }
                    }
                    tx.ID = 0;
                }
            }

            await _context.BulkInsertAsync(txs);

            results.Add($"{txs.Count} transactions");
        }
        if (offering.Rules.Contains("Today"))
        {
            var txs = ssr.Deserialize<Transaction>();

            DateTime last = DateTime.MinValue;
            var lastq = _context.Get<Transaction>().OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp);
            if (lastq.Any())
                last = lastq.First();

            var added = txs.Where(x => x.Timestamp > last && x.Timestamp <= _clock.Now).ToList();

            if (hidden)
                foreach (var tx in added)
                    tx.Hidden = true;

            if (ssr.SheetNames.Contains("Split"))
            {
                var splits = ssr.Deserialize<Split>().ToLookup(x => x.TransactionID);
                foreach (var tx in added.Where(x => x.ID > 0))
                {
                    if (splits.Contains(tx.ID))
                    {
                        tx.Splits = splits[tx.ID].ToList();
                        tx.Category = null;
                        foreach (var split in tx.Splits)
                        {
                            split.ID = 0;
                            split.TransactionID = 0;
                        }
                    }
                    tx.ID = 0;
                }
            }

            await _context.BulkInsertAsync(added);

            results.Add($"{added.Count} transactions");
        }
        if (offering.Rules.Contains(nameof(BudgetTx)))
        {
            var added = ssr.Deserialize<BudgetTx>().ToList();
            await _context.BulkInsertAsync(added);
            results.Add($"{added.Count} budget line items");
        }
        if (offering.Rules.Contains(nameof(Payee)))
        {
            var added = ssr.Deserialize<Payee>().ToList();
            await _context.BulkInsertAsync(added);
            results.Add($"{added.Count} payee matching rules");
        }

        return "Added " + string.Join(", ", results);
    }

    private bool RulesOK(IEnumerable<string> rules)
    {
        foreach(var rule in rules)
        {
            if (nameof(BudgetTx) == rule)
            {
                if (_context.Get<BudgetTx>().Any())
                    return false;
            }
            if (nameof(Payee) == rule)
            {
                if (_context.Get<Payee>().Any())
                    return false;
            }
            if (nameof(Transaction) == rule)
            {
                if (_context.Get<Transaction>().Any())
                    return false;
            }
            if ("Today" == rule)
            {
                if (_context.Get<Transaction>().Any(x=>x.Timestamp >= _clock.Now))
                    return false;
            }
        }

        return true;

    }
}

internal class DownloadOffering : ISampleDataDownloadOffering
{
    public string ID { get; set; }

    public SampleDataDownloadFileType FileType { get; set; }

    public string Description { get; set; }

    public SampleDataDownloadOfferingKind Kind { get; set; }
}

internal class SeedOffering : ISampleDataSeedOffering
{
    public string ID { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public bool IsAvailable { get; set; }

    public SampleDataSeedOfferingCondition Condition { get; set; }

    public IEnumerable<string> Rules { get; set; }
}
