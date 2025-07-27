using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartSpend.ExcelSerializer
{
    public class SpreadsheetReader : ISpreadsheetReader
    {
        private ExcelPackage _package;

        public IEnumerable<string> SheetNames => _package.Workbook.Worksheets.Select(x => x.Name);

        public void Open(Stream stream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _package = new ExcelPackage(stream);
        }

        public List<T> Deserialize<T>() where T : class, new()
        {
            var sheet = _package.Workbook.Worksheets.FirstOrDefault(x => x.Name == typeof(T).Name)
                     ?? _package.Workbook.Worksheets.First();

            if (sheet == null)
                return new List<T>();

            return sheet.ConvertSheetToObjects<T>().ToList();
        }

        public void Dispose()
        {
            _package?.Dispose();
        }
    }
}
