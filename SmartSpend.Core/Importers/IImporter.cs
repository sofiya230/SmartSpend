using jcoliz.OfficeOpenXml.Serializer;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YoFi.Core.Importers;

public interface IImporter<T>
{
    void QueueImportFromXlsx(Stream stream);

    void QueueImportFromXlsx(ISpreadsheetReader reader);

    Task<IEnumerable<T>> ProcessImportAsync();
}
