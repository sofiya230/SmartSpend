using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSpend.Core.Models
{
    public class BudgetTx: IReportable, IModelItem<BudgetTx>, IImportDuplicateComparable
    {
        public int ID { get; set; }

        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
        [Editable(true)]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        [Editable(true)]
        [Column(TypeName = "date")]
        public DateTime Timestamp { get; set; }

        [Editable(true)]
        public string Category { get; set; }

        public int Frequency { get; set; }

        [NotMapped]
        public string FrequencyName
        {
            get
            {
                if (Frequency < 0)
                    return "Invalid";
                else return Frequency switch
                {
                    0 => "Yearly",
                    1 => "Yearly",
                    4 => "Quarterly",
                    12 => "Monthly",
                    52 => "Weekly",
                    _ => Frequency.ToString()
                };
            }
            set
            {
                Frequency = value switch
                {
                    "Quarterly" => 4,
                    "Monthly" => 12,
                    "Weekly" => 52,
                    _ => 1,
                };
            }
        }

        public enum FrequencyEnum { Yearly = 1, Quarterly = 4, Monthly = 12, Weekly = 52 };

        [Editable(true)]
        [Category("TestKey")]
        public string Memo { get; set; }

        public bool? Selected { get; set; }

        public class Reportable : IReportable
        {
            public decimal Amount { get; set; }

            public DateTime Timestamp { get; set; }

            public string Category { get; set; }
        }

        public IEnumerable<IReportable> Reportables
            => Enumerable
                .Range(0, Frequency)
                .Select(x => new Reportable()
                {
                    Timestamp = Period(x),
                    Category = Category,
                    Amount = Amount / Frequency
                });

        private DateTime Period(int which)
        {
            if (Frequency <= 1 || Frequency > 365)
                return Timestamp;
            if (Frequency == 365)
                return Timestamp + TimeSpan.FromDays(which);
            if (12 % Frequency == 0)
                return new DateTime(Timestamp.Year, 1 + which * (12/Frequency), 1);
            return Timestamp + TimeSpan.FromDays((364 / Frequency) * which);
        }


        public override bool Equals(object obj)
        {
            return obj is BudgetTx tx &&
                   Timestamp == tx.Timestamp &&
                   Category == tx.Category;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Timestamp, Category);
        }

        public IQueryable<BudgetTx> InDefaultOrder(IQueryable<BudgetTx> original)
        {
            return original.OrderByDescending(x => x.Timestamp.Year).ThenByDescending(x => x.Timestamp.Month).ThenByDescending(x => x.Timestamp.Day).ThenBy(x => x.Category);
        }

        int IImportDuplicateComparable.GetImportHashCode() =>
            HashCode.Combine(Timestamp.Year, Timestamp.Month, Category);

        bool IImportDuplicateComparable.ImportEquals(object other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other is not BudgetTx)
                throw new ArgumentException("Expected BudgetTx", nameof(other));

            var item = other as BudgetTx;

            return Timestamp.Year == item.Timestamp.Year && Timestamp.Month == item.Timestamp.Month && Category == item.Category;
        }
    }
}
