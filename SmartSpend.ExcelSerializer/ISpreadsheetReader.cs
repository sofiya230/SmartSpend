using System;
using System.Collections.Generic;
using System.IO;

namespace SmartSpend.ExcelSerializer
{
    public interface ISpreadsheetReader : IDisposable
    {
        void Open(Stream stream);
        List<T> Deserialize<T>() where T : class, new();
        IEnumerable<string> SheetNames { get; }
    }
}
