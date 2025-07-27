using Common.DotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartSpend.Core.Models
{
    public class Receipt : IID
    {
        public int ID { get; set; }

        [Editable(true)]
        public string Name { get; set; }

        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
        [Editable(true)]
        public decimal? Amount { get; set; }

        [Editable(true)]
        [Category("TestKey")]
        public string Memo { get; set; }

        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
        [Editable(true)]
        public DateTime Timestamp { get; set; }

        [Required]
        public string Filename { get; set; }

        [NotMapped]
        public ICollection<Transaction> Matches { get; set; } = new List<Transaction>();

        public override bool Equals(object obj)
        {
            return obj is Receipt receipt &&
                   Name == receipt.Name &&
                   Amount == receipt.Amount &&
                   Timestamp == receipt.Timestamp;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Amount, Timestamp);
        }

        public int MatchesTransaction(Transaction transaction)
        {

            var result = 0;

            if (transaction is null)
                return 0;

            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(transaction.Payee))
            {
                if (transaction.Payee.ToLowerInvariant().Contains(Name.ToLowerInvariant()))
                    result += 100;
            }

            if (Amount.HasValue)
            {
                if (Math.Abs(Amount.Value) == Math.Abs(transaction.Amount))
                    result += 100;
            }

            var margin = TimeSpan.FromDays(14);
            if (transaction.Timestamp > Timestamp - margin && transaction.Timestamp < Timestamp + margin)
            {
                if (result > 0)
                    result += 100 - (int)Math.Abs((transaction.Timestamp - Timestamp).TotalDays);
            }
            else
            {
                result = 0;
            }

            return result;
        }

        public Transaction AsTransaction()
        {
            var url = $"{Filename} [ID {ID}]";
            return new Transaction() { Timestamp = Timestamp, Payee = Name, Memo = Memo, Amount = Amount ?? default, ReceiptUrl = url };
        }

        public static IQueryable<Transaction> TransactionsForReceipts(IQueryable<Transaction> initial, IEnumerable<Receipt> receipts)
        {
            if (!receipts.Any())
                throw new ArgumentException("No receipts supplied", nameof(receipts));

            var margin = TimeSpan.FromDays(14);

            var from = receipts.Min(x => x.Timestamp);
            var to = receipts.Max(x => x.Timestamp);

            var result = initial.Where(x=>x.Timestamp > from - margin && x.Timestamp < to + margin);

            return result;
        }

        public static Receipt FromFilename(string filename, IClock clock)
        {
            var result = new Receipt() { Filename = filename };

            var given = Path.GetFileNameWithoutExtension(filename);

            var words = given.Split(' ').ToList();

            var unmatchedwords = new List<string>();
            var unmatchedterms = new Queue<string>();

            var amount_r = new Regex("^\\$([0-9]+(?:\\.[0-9][0-9])?)");
            var date_r = new Regex("^[0-9][0-9]?-[0-9][0-9]?$");
            var memo_r = new Regex("^\\((.+?)\\)$");
            foreach (var word in words)
            {
                var match = amount_r.Match(word);
                if (match.Success)
                {
                    result.Amount = decimal.Parse(match.Groups[1].Value);
                    if (unmatchedwords.Any())
                    {
                        unmatchedterms.Enqueue(String.Join(' ',unmatchedwords));
                        unmatchedwords.Clear();
                    }
                }
                else
                {
                    match = date_r.Match(word);
                    if (match.Success)
                    {
                        var parsed = DateTime.Parse(match.Groups[0].Value);
                        result.Timestamp = new DateTime(clock.Now.Year,parsed.Month,parsed.Day);
                        if (result.Timestamp > clock.Now)
                        {
                            result.Timestamp = new DateTime(clock.Now.Year - 1, parsed.Month, parsed.Day);
                        }
                        if (unmatchedwords.Any())
                        {
                            unmatchedterms.Enqueue(String.Join(' ', unmatchedwords));
                            unmatchedwords.Clear();
                        }
                    }
                    else
                    {
                        match = memo_r.Match(word);
                        if (match.Success)
                        {
                            result.Memo = match.Groups[1].Value;
                            if (unmatchedwords.Any())
                            {
                                unmatchedterms.Enqueue(String.Join(' ', unmatchedwords));
                                unmatchedwords.Clear();
                            }
                        }
                        else
                        {
                            unmatchedwords.Add(word);
                        }
                    }
                }
            }
            if (unmatchedwords.Any())
            {
                unmatchedterms.Enqueue(String.Join(' ', unmatchedwords));
                unmatchedwords.Clear();
            }

            if (unmatchedterms.Any())
                result.Name = unmatchedterms.Dequeue();

            if (unmatchedterms.Any() && result.Memo is null)
                result.Memo = unmatchedterms.Dequeue();

            if (result.Timestamp == default)
            {
                result.Timestamp = clock.Now.Date;
            }

            return result;
        }
    }
}
