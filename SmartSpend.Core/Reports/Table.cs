using System;
using System.Collections.Generic;

namespace SmartSpend.Core.Reports;

public class Table<TColumn, TRow, TValue>
{
    class Key
    {
        public TColumn column { get; }

        public TRow row { get; }

        public Key(TColumn _column, TRow _row)
        {
            column = _column;
            row = _row;
        }

        public override bool Equals(object obj)
        {
            return obj is Key key &&
                   EqualityComparer<TColumn>.Default.Equals(column, key.column) &&
                   EqualityComparer<TRow>.Default.Equals(row, key.row);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(column, row);
        }
    }

    readonly Dictionary<Key,TValue> DataSet = new();

    public HashSet<TColumn> ColumnLabels { get; private set; } = new HashSet<TColumn>();

    public HashSet<TRow> RowLabels { get; set; } = new HashSet<TRow>();

    public TValue this[TColumn columnlabel, TRow rowlabel]
    {
        get
        {
            var key = new Key(_column: columnlabel, _row: rowlabel);

            return DataSet.GetValueOrDefault(key);
        }
        set
        {
            var key = new Key(_column: columnlabel, _row: rowlabel);

            DataSet[key] = value;
            ColumnLabels.Add(columnlabel);
            RowLabels.Add(rowlabel);
       }
    }
}
