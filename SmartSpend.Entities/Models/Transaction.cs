using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartSpend.Core.Models
{
    public class Transaction : IModelItem<Transaction>, IReportable
    {
        public int ID { get; set; }

        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
        [Editable(true)]
        public DateTime Timestamp { get; set; }

        [Editable(true)]
        public string Payee { get; set; }

        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
        [Editable(true)]
        public decimal Amount { get; set; }

        [Editable(true)]
        public string Category { get; set; }

        [Editable(true)]
        [Category("TestKey")]
        public string Memo { get; set; }

        public string BankReference { get; set; }

        public bool? Hidden { get; set; }

        public bool? Imported { get; set; }

        public bool? Selected { get; set; }

        public string ReceiptUrl { get; set; }

        public ICollection<Split> Splits { get; set; }

        public bool HasSplits => Splits?.Any() == true;

        public bool IsSplitsOK => !HasSplits || ( Splits.Select(x=>x.Amount).Sum() == Amount );

        public string StrippedPayee => (Payee != null) ? new Regex(@"[^\s\w\d]+").Replace(Payee, new MatchEvaluator(x => string.Empty)) : null;


        
        public void GenerateBankReference()
        {
            var signature = $"/{Payee ?? "Null"}/{Amount:C2}/{Timestamp.Date.ToShortDateString()}";
            var buffer = UTF32Encoding.UTF32.GetBytes(signature);
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(buffer);
            var x = hash.Aggregate(new StringBuilder(), (sb, b) => sb.Append(b.ToString("X2")));

            BankReference = x.ToString();
        }

        public override bool Equals(object obj)
        {
            bool result = false;

            if (obj is Transaction)
            {
                var other = obj as Transaction;
                result = string.Equals(Payee, other.Payee) && Amount == other.Amount && Timestamp.Date == other.Timestamp.Date;
            }

            return result;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Payee, Amount, Timestamp.Date);
        }

        public static IQueryable<Transaction> InDefaultOrder(IQueryable<Transaction> original)
        {
            return original.OrderByDescending(x => x.Timestamp).ThenBy(x => x.Payee);
        }

        IQueryable<Transaction> IModelItem<Transaction>.InDefaultOrder(IQueryable<Transaction> original) => InDefaultOrder(original);
    }
}
