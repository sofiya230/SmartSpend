using jcoliz.OfficeOpenXml.Serializer;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories;

namespace YoFi.Core.Importers;

public class SplitImporter
{
    public SplitImporter(IRepository<Transaction> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Split>> ProcessImportAsync(Transaction target)
    {
        if (incoming.Count > 0)
        {
            foreach (var split in incoming)
            {
                target.Splits.Add(split);
            }

            await _repository.UpdateAsync(target);
        }

        return incoming.ToList();
    }

    public void QueueImportFromXlsx(Stream stream)
    {
        using var reader = new SpreadsheetReader();
        reader.Open(stream);
        QueueImportFromXlsx(reader);
    }

    public void QueueImportFromXlsx(ISpreadsheetReader reader)
    {
        var items = reader.Deserialize<Split>(exceptproperties: new string[] { "ID" });
        incoming.UnionWith(items);
    }

    private readonly HashSet<Split> incoming = [];

    private readonly IRepository<Transaction> _repository;
}
