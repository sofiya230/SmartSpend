using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using YoFi.Core.Models;

namespace YoFi.SampleGen
{
    public class SampleDataGenerator
    {
        public void LoadDefinitions(Stream stream)
        {
            using var ssr = new SpreadsheetReader();
            ssr.Open(stream);
            Definitions.AddRange(ssr.Deserialize<SampleDataPattern>());
        }

        public void GenerateTransactions(bool addids = true)
        {
            var groupings = Definitions.ToLookup(x => x.Group);

            if (groupings.Contains(null))
            {
                Transactions.AddRange(groupings[null].SelectMany(x => x.GetTransactions()));
            }

            int nextid = 1;
            foreach(var group in groupings.Where(x => !string.IsNullOrEmpty(x.Key)))
            {
                var main = group.Where(x => !string.IsNullOrEmpty(x.Payee));

                if (!main.Any())
                    throw new ApplicationException($"Group {group.Key} has no definition with a payee");

                if (main.Skip(1).Any())
                    throw new ApplicationException($"Group {group.Key} has multiple definitions with a payee. Expecting only one");

                var txs = main.Single().GetTransactions(group).ToList();
                
                if (addids)
                {
                    foreach (var tx in txs)
                    {
                        var id = nextid++;
                        tx.ID = id;
                        foreach (var split in tx.Splits)
                            split.TransactionID = id;
                    }
                }

                Transactions.AddRange(txs);
            }

            Transactions.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
        }

        public void GeneratePayees()
        {
            var normal = Definitions.Where(def => def.Payee != null && string.IsNullOrEmpty(def.Loan)).ToLookup(def => def.Payee).SelectMany(lookup => lookup.Key.Split(',').Select(name => new Payee() { Name = name, Category = lookup.First().Category }));

            var loans = Definitions
                .Where(def => def.Payee != null && !string.IsNullOrEmpty(def.Loan))
                .Select(def => 
                    new Payee() 
                    { 
                        Name = def.Payee, 
                        Category = def.Category + " [Loan] " + def.Loan
                    }
                );

            Payees = normal.Concat(loans).ToList();
        }

        public void GenerateBudget()
        {
            static int frequency(SampleDataPattern p)
            {
                if (p.DateJitter == JitterEnum.High && p.AmountJitter == JitterEnum.High)
                {
                    if (p.DateFrequency == FrequencyEnum.Weekly)
                        return 52;
                    else
                        return 12;
                }
                else
                    return 1;
            }

            var regular = Definitions
                .Where(x => x.Category != null && string.IsNullOrEmpty(x.Loan))
                .ToLookup(x=>x.Category)
                .Select(l => new BudgetTx { Category = l.Key, Amount = l.Sum(y => y.AmountYearly), Frequency = frequency(l.First()), Timestamp = new DateTime(SampleDataPattern.Year, 1, 1) });

            var months = Enumerable.Range(1, 12).Select(m => new DateTime(SampleDataPattern.Year, m, 1));
            var loanbudgets = Definitions
                .Where(d => !string.IsNullOrEmpty(d.Loan))
                .SelectMany(d =>
                    months.SelectMany(m =>
                        d.LoanObject.PaymentSplitsForDate(m)                        
                    )
                )
                .ToLookup(kvp => kvp.Key, kvp => kvp.Value)
                .Select(l => new BudgetTx() { Category = l.Key, Amount = l.Sum(), Frequency = 1, Timestamp = new DateTime(SampleDataPattern.Year, 1, 1) });

            BudgetTxs = regular.Concat(loanbudgets).ToList();
        }

        public enum GenerateType { Full, Tx };

        public void Save(Stream stream, bool txonly = false)
        {
            using var ssr = new SpreadsheetWriter();
            ssr.Open(stream);
            ssr.Serialize(Transactions);
            ssr.Serialize(Transactions.Where(x=>x.Splits?.Count > 1).SelectMany(x => x.Splits),"Split");

            if (!txonly)
            {
                if (Payees.Any())
                    ssr.Serialize(Payees);
                if (BudgetTxs.Any())
                    ssr.Serialize(BudgetTxs);
            }
        }

        public void Save(Stream stream, SaveOptions options)
        {
            switch (options.Type)
            {
            case SaveOptions.FileType.Xlsx:
                Save(stream, options.TxOnly);
                break;
            case SaveOptions.FileType.Json:
                System.Text.Json.JsonSerializer.Serialize(stream, this);
                break;
            case SaveOptions.FileType.Ofx:
                var items = Transactions.Where(x => x.Timestamp.Month == options.Month);
                YoFi.Core.SampleData.SampleDataOfx.WriteToOfx(items, stream);
                break;
            }

        }

        [JsonIgnore]
        public List<SampleDataPattern> Definitions { get; set; } = new List<SampleDataPattern>();

        public List<Transaction> Transactions { get; set; } = new List<Transaction>();

        public List<Payee> Payees { get; set; } = new List<Payee>();

        public List<BudgetTx> BudgetTxs { get; set; } = new List<BudgetTx>();

        public class SaveOptions
        {
            public enum FileType { Xlsx, Json, Ofx };

            public FileType Type { get; set; }

            public bool TxOnly { get; set; }

            public int Month { get; set; }
        } 
    }
}
