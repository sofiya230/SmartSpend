using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;

namespace SmartSpend.Core.Models
{
    public class Split: IModelItem<Split>, IReportable
    {
        public int ID { get; set; }

        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
        [Editable(true)]
        public decimal Amount { get; set; }

        [Editable(true)]
        public string Category { get; set; }

        [Editable(true)]
        [Category("TestKey")]
        public string Memo { get; set; }

        public int TransactionID { get; set; }

        [JsonIgnore]
        public Transaction Transaction { get; set; }

        DateTime IReportable.Timestamp => Transaction?.Timestamp ?? DateTime.MinValue;

        public override bool Equals(object obj)
        {
            return obj is Split split &&
                   Amount == split.Amount &&
                   Category == split.Category &&
                   Memo == split.Memo;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Category, Memo);
        }

        IQueryable<Split> IModelItem<Split>.InDefaultOrder(IQueryable<Split> original)
        {
            throw new NotImplementedException();
        }
    }
}
