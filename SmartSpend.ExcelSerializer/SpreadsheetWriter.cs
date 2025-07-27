using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;

namespace SmartSpend.ExcelSerializer
{
    public class SpreadsheetWriter : IDisposable
    {
        private ExcelPackage _package;
        private Stream _outputStream;

        public void Open(Stream stream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _outputStream = stream;
            _package = new ExcelPackage(_outputStream);
        }

        public void Serialize<T>(IEnumerable<T> items)
        {
            var sheet = _package.Workbook.Worksheets.Add(typeof(T).Name);
            sheet.Cells["A1"].LoadFromCollection(items, true);
        }

        public void Dispose()
        {
            _package?.Save();
            _package?.Dispose();
        }
    }
}
