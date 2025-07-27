using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SmartSpend.ExcelSerializer
{
    public static class ExcelWorksheetExtensions
    {
        public static IEnumerable<T> ConvertSheetToObjects<T>(this ExcelWorksheet worksheet) where T : class, new()
        {
            var result = new List<T>();
            if (worksheet.Dimension == null)
                return result;

            var columnNames = new List<string>();
            var props = typeof(T).GetProperties();

            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                columnNames.Add(worksheet.Cells[1, col].Text);
            }

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var item = new T();
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    var header = columnNames[col - 1];
                    var prop = props.FirstOrDefault(p => string.Equals(p.Name, header, StringComparison.OrdinalIgnoreCase));
                    if (prop != null && worksheet.Cells[row, col].Value != null)
                    {
                        var value = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromInvariantString(worksheet.Cells[row, col].Text);
                        prop.SetValue(item, value);
                    }
                }
                result.Add(item);
            }

            return result;
        }
    }
}
