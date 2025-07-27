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

public class BaseImporter<T> : IImporter<T>, IEqualityComparer<T> where T : class, IModelItem<T>, IImportDuplicateComparable, new()
{
    public BaseImporter(IRepository<T> repository)
    {
        _repository = repository;
        _importing = new HashSet<T>(this);
    }

    public void QueueImportFromXlsx(Stream stream)
    {
        using var reader = new SpreadsheetReader();
        reader.Open(stream);
        QueueImportFromXlsx(reader);
    }

    public void QueueImportFromXlsx(ISpreadsheetReader reader)
    {
        var items = reader.Deserialize<T>(exceptproperties: new string[] { "ID" });
        _importing.UnionWith(items);
    }

    public async Task<IEnumerable<T>> ProcessImportAsync()
    {
        _importing.ExceptWith(_repository.All);

        var imported = _importing.ToList();
        await _repository.BulkInsertAsync(imported);

        _importing.Clear();

        return new T().InDefaultOrder(imported.AsQueryable());
    }

    bool IEqualityComparer<T>.Equals(T x, T y)
    {
        if (x == null)
            throw new ArgumentNullException(nameof(x));

        return x.ImportEquals(y);
    }

    int IEqualityComparer<T>.GetHashCode(T obj)
    {
        return obj.GetImportHashCode();
    }

    private readonly IRepository<T> _repository;

    private readonly HashSet<T> _importing;
}
