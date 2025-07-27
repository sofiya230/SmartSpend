using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using YoFi.Core.Models;

namespace YoFi.SampleGen
{
    public class SampleDataPattern: ISplitPattern
    {
        public string Payee { get; set; }

        public FrequencyEnum DateFrequency { get; set; }

        public JitterEnum DateJitter { get; set; }

        public int DateRepeats { get; set; } = 1;

        public string Category { get; set; }

        public decimal AmountYearly { get; set; }

        public JitterEnum AmountJitter { get; set; }

        public string Group { get; set; }

        public string Loan { get; set; }

        public Loan LoanObject
        {
            get
            {
                if (!string.IsNullOrEmpty(Loan))
                {
                    var loan = JsonSerializer.Deserialize<Loan>(Loan, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    loan.Principal = Category;
                    return loan;
                }
                else
                    return null;
            }
        }

        public static int Year 
        { 
            get
            {
                return _Year ?? throw new ApplicationException("Must set a year first");
            }
            set
            {
                _Year = value;
            }
        }
        private static int? _Year;

        public static Dictionary<JitterEnum, double> AmountJitterValues = new Dictionary<JitterEnum, double>()
        {
            { JitterEnum.None, 0 },
            { JitterEnum.Low, 0.1 },
            { JitterEnum.Moderate, 0.4 },
            { JitterEnum.High, 0.9 }
        };

        public static Dictionary<JitterEnum, double> DateJitterValues = new Dictionary<JitterEnum, double>()
        {
            { JitterEnum.None, 0 },
            { JitterEnum.Low, 0.25 },
            { JitterEnum.Moderate, 0.4 },
            { JitterEnum.High, 1.0 }
        };

        public static Dictionary<FrequencyEnum, TimeSpan> SchemeTimespans = new Dictionary<FrequencyEnum, TimeSpan>()
        {
            { FrequencyEnum.Weekly, TimeSpan.FromDays(7) },
            { FrequencyEnum.Monthly, TimeSpan.FromDays(28) },
            { FrequencyEnum.Quarterly, TimeSpan.FromDays(90) },
            { FrequencyEnum.Yearly, TimeSpan.FromDays(365) },
        };

        const int MonthsPerYear = 12;
        const int WeeksPerYear = 52;
        const int DaysPerWeek = 7;
        const int MonthsPerQuarter = 3;

        public static Dictionary<FrequencyEnum, int> FrequencyPerYear = new Dictionary<FrequencyEnum, int>()
        {
            { FrequencyEnum.Weekly, WeeksPerYear },
            { FrequencyEnum.SemiMonthly, 2 * MonthsPerYear },
            { FrequencyEnum.Monthly, MonthsPerYear },
            { FrequencyEnum.Quarterly, MonthsPerYear / MonthsPerQuarter },
            { FrequencyEnum.Yearly, 1 },
        };

        private readonly int[] SemiWeeklyDays = new int[] { 1, 15 };

        decimal ISplitPattern.Amount => MakeAmount(AmountYearly / DateRepeats / FrequencyPerYear[DateFrequency]);

        public IEnumerable<Transaction> GetTransactions(IEnumerable<ISplitPattern> group = null)
        {

            if (DateFrequency == FrequencyEnum.SemiMonthly && ((DateJitter != JitterEnum.None && DateJitter != JitterEnum.Invalid) || DateRepeats != 1))
                throw new NotImplementedException("SemiMonthly with date jitter or date repeats is not implemented");
            if (DateFrequency == FrequencyEnum.Invalid)
                throw new ApplicationException("Invalid date frequency");


            if (DateFrequency != FrequencyEnum.SemiMonthly)
            {
                DateWindowLength = (DateJitter == JitterEnum.None) ? TimeSpan.FromDays(1) : SchemeTimespans[DateFrequency] * DateJitterValues[DateJitter];
                DateWindowStarts = TimeSpan.FromDays(random.Next(0, SchemeTimespans[DateFrequency].Days - DateWindowLength.Days));
            }


            ITransactionDetailsFactory detailsfactory;

            var loanobject = LoanObject;
            if (loanobject != null)
            {
                detailsfactory = new LoanDetails(loanobject);
            }
            else if (group?.Any() == true)
            {
                detailsfactory = new GroupDetails(group);
            }
            else
            {
                detailsfactory = new SingleDetails(this);
            }


            if (DateFrequency == FrequencyEnum.SemiMonthly)
                return Enumerable.Range(1, MonthsPerYear).SelectMany(month => SemiWeeklyDays.Select(day => GenerateTransaction(detailsfactory.ForDate(new DateTime(Year, month, day)))));
            else
                return Enumerable.Repeat(1, DateRepeats).SelectMany(i=>Enumerable.Range(1, FrequencyPerYear[DateFrequency]).Select(x => GenerateTransaction(detailsfactory.ForDate(MakeDate(x)))));
        }

        private static readonly Random random = new Random();

        private TimeSpan DateWindowStarts;

        private TimeSpan DateWindowLength;

        private Transaction GenerateTransaction(ITransactionDetails details) // IEnumerable<ISplitPattern> group, DateTime timestamp)
        {
            var generatedsplits = details.Splits.Select(s => new Split()
            {
                Category = s.Category,
                Amount = s.Amount
            }).ToList();

            return new Transaction()
            {
                Payee = MakePayee,
                Splits = generatedsplits.Count > 1 ? generatedsplits : null,
                Timestamp = details.Date,
                Category = generatedsplits.Count == 1 ? generatedsplits.Single().Category : null,
                Amount = generatedsplits.Sum(x => x.Amount)
            };
        }

        private IEnumerable<string> Payees => Payee.Split(',');

        private decimal MakeAmount(decimal amount) =>
            Math.Round(
                (AmountJitter == JitterEnum.None) ? amount :
                    (decimal)((double)amount * (1.0 + 2.0 * (random.NextDouble() - 0.5) * AmountJitterValues[AmountJitter]))
                , 2);

        private DateTime MakeDate(int periodindex) => DateFrequency switch
            {
                FrequencyEnum.Monthly => new DateTime(Year, periodindex, 1),
                FrequencyEnum.Yearly => new DateTime(Year, 1, 1),
                FrequencyEnum.Quarterly => new DateTime(Year, periodindex * MonthsPerQuarter - 2, 1),
                FrequencyEnum.Weekly => new DateTime(Year, 1, 1) + TimeSpan.FromDays(DaysPerWeek * (periodindex - 1)),
                _ => throw new NotImplementedException()
            } 
            + DateWindowStarts 
            + ((DateJitter != JitterEnum.None) ? TimeSpan.FromDays(random.Next(0, DateWindowLength.Days)) : TimeSpan.Zero);

        private string MakePayee => Payees.Skip(random.Next(0, Payees.Count())).First();

        #region Helper Classes

        class SplitPattern : ISplitPattern
        {
            public decimal Amount { get; set; }

            public string Category { get; set; }
        }

        interface ITransactionDetails
        {
            IEnumerable<ISplitPattern> Splits { get; }

            DateTime Date { get; }
        }

        class TransactionDetails : ITransactionDetails
        {
            public IEnumerable<ISplitPattern> Splits { get; set; }

            public DateTime Date { get; set; }
        }


        interface ITransactionDetailsFactory
        {
            ITransactionDetails ForDate(DateTime date);
        }

        class SingleDetails : ITransactionDetailsFactory
        {
            private readonly SampleDataPattern _single;

            public SingleDetails(SampleDataPattern single)
            {
                _single = single;
            }

            public ITransactionDetails ForDate(DateTime date)
            {
                return new TransactionDetails() { Date = date, Splits = Enumerable.Repeat(_single, 1) };
            }
        }

        class GroupDetails : ITransactionDetailsFactory
        {
            private readonly IEnumerable<ISplitPattern> _group;

            public GroupDetails(IEnumerable<ISplitPattern> group)
            {
                _group = group;
            }

            public ITransactionDetails ForDate(DateTime date)
            {
                return new TransactionDetails() { Date = date, Splits = _group };
            }
        }


        class LoanDetails : ITransactionDetailsFactory
        {
            private readonly Loan _loan;

            public LoanDetails(Loan loan)
            {
                _loan = loan;
            }

            public ITransactionDetails ForDate(DateTime date)
            {
                return new TransactionDetails() { Date = date, Splits = _loan.PaymentSplitsForDate(date).Select(x => new SplitPattern() { Category = x.Key, Amount = x.Value }) };
            }
        }
        #endregion
    }

    public enum FrequencyEnum { Invalid = 0, Weekly, SemiMonthly, Monthly, Quarterly, Yearly };

    public enum JitterEnum { Invalid = 0, None, Low, Moderate, High };

    public interface ISplitPattern
    {
        decimal Amount { get; }

        string Category { get; }
    }
}
