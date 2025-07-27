using Common.DotNet;
using System;
using System.Linq;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Repositories;

public class TransactionsQueryBuilder
{
    #region Properties

    public IQueryable<Transaction> Query { get; private set; }

    #endregion

    #region Fields

    private readonly IClock _clock;

    #endregion

    #region Constructor

    public TransactionsQueryBuilder(IQueryable<Transaction> initial, IClock clock)
    {
        Query = initial;
        _clock = clock;
    }

    #endregion

    #region Methods

    public void BuildForQ(string q)
    {
        if (!string.IsNullOrEmpty(q))
        {
            var terms = q.Split(',');


            if (!terms.Any(x=>x.ToLowerInvariant().StartsWith('y')))
            {
                Query = Query.Where(x => x.Timestamp > _clock.Now - TimeSpan.FromDays(366));
            }

            foreach (var term in terms)
            {
                if (term.Length > 2 && term[1] == '=')
                {
                    var key = term.ToLowerInvariant().First();
                    var value = term[2..];

                    Query = key switch
                    {
                        'p' => TransactionsForQuery_Payee(Query, value),
                        'c' => TransactionsForQuery_Category(Query, value),
                        'y' => TransactionsForQuery_Year(Query, value),
                        'm' => TransactionsForQuery_Memo(Query, value),
                        'r' => TransactionsForQuery_HasReceipt(Query, value),
                        'a' => TransactionsForQuery_Amount(Query, value),
                        'd' => TransactionsForQuery_Date(Query, value),
                        'i' => TransactionsForQuery_Imported(Query, value),
                        _ => throw new ArgumentException($"Unknown query parameter {key}", nameof(q))
                    };

                }
                else if (Int32.TryParse(term, out Int32 intval))
                {

                    DateTime? dtval = null;
                    try
                    {
                        dtval = new DateTime(_clock.Now.Year, intval / 100, intval % 100);
                    }
                    catch
                    {
                    }

                    if (dtval.HasValue)
                    {
                        Query = Query.Where(x =>
                            x.Category.Contains(term) ||
                            x.Memo.Contains(term) ||
                            x.Payee.Contains(term) ||
                            x.Amount == (decimal)intval ||
                            x.Amount == ((decimal)intval) / 100 ||
                            x.Amount == -(decimal)intval ||
                            x.Amount == -((decimal)intval) / 100 ||
                            (x.Timestamp >= dtval && x.Timestamp <= dtval.Value.AddDays(7)) ||
                            x.Splits.Any(s =>
                                s.Category.Contains(term) ||
                                s.Memo.Contains(term) ||
                                s.Amount == (decimal)intval ||
                                s.Amount == ((decimal)intval) / 100 ||
                                s.Amount == -(decimal)intval ||
                                s.Amount == -((decimal)intval) / 100
                            )
                        );
                    }
                    else
                    {
                        Query = Query.Where(x =>
                            x.Category.Contains(term) ||
                            x.Memo.Contains(term) ||
                            x.Payee.Contains(term) ||
                            x.Amount == (decimal)intval ||
                            x.Amount == ((decimal)intval) / 100 ||
                            x.Amount == -(decimal)intval ||
                            x.Amount == -((decimal)intval) / 100 ||
                            x.Splits.Any(s =>
                                s.Category.Contains(term) ||
                                s.Memo.Contains(term) ||
                                s.Amount == (decimal)intval ||
                                s.Amount == ((decimal)intval) / 100 ||
                                s.Amount == -(decimal)intval ||
                                s.Amount == -((decimal)intval) / 100
                            )
                        );
                    }

                }
                else
                {
                    Query = Query.Where(x =>
                        x.Category.Contains(term) ||
                        x.Memo.Contains(term) ||
                        x.Payee.Contains(term) ||
                        x.Splits.Any(s =>
                            s.Category.Contains(term) ||
                            s.Memo.Contains(term)
                        )
                    );

                }
            }
        }
    }

    internal void ApplyOrderParameter(string o)
    {
        Query = o switch
        {
            "aa" => Query.OrderBy(s => s.Amount),
            "ad" => Query.OrderByDescending(s => s.Amount),
            "ra" => Query.OrderBy(s => s.BankReference),
            "rd" => Query.OrderByDescending(s => s.BankReference),
            "pa" => Query.OrderBy(s => s.Payee),
            "pd" => Query.OrderByDescending(s => s.Payee),
            "ca" => Query.OrderBy(s => s.Category),
            "cd" => Query.OrderByDescending(s => s.Category),
            "da" => Query.OrderBy(s => s.Timestamp).ThenBy(s => s.Payee),
            "dd" => Query.OrderByDescending(s => s.Timestamp).ThenBy(s => s.Payee),
            null => Query.OrderByDescending(s => s.Timestamp).ThenBy(s => s.Payee),
            _ => throw new ArgumentException($"Unexpected order parameter {o}", nameof(o))
        };
    }

    internal void ApplyViewParameter(string v)
    {
        if ((v?.ToLowerInvariant().Contains('h') != true))
            Query = Query.Where(x => x.Hidden != true);
    }

#if false
    internal async Task ExecuteQueryAsync()
    {
        if (ShowHidden || ShowSelected)
        {
            var dtoquery = Query.Select(t => new TransactionIndexDto()
            {
                ID = t.ID,
                Timestamp = t.Timestamp,
                Payee = t.Payee,
                Amount = t.Amount,
                Category = t.Category,
                Memo = t.Memo,
                HasReceipt = t.ReceiptUrl != null,
                HasSplits = t.Splits.Any(),
                BankReference = t.BankReference,
                Hidden = t.Hidden ?? false,
                Selected = t.Selected ?? false
            });

            Items = await _queryExecution.ToListNoTrackingAsync(dtoquery);
        }
        else
        {
            var dtoquery = Query.Select(t => new TransactionIndexDto()
            {
                ID = t.ID,
                Timestamp = t.Timestamp,
                Payee = t.Payee,
                Amount = t.Amount,
                Category = t.Category,
                Memo = t.Memo,
                HasReceipt = t.ReceiptUrl != null,
                HasSplits = t.Splits.Any(),
            });

            Items = await _queryExecution.ToListNoTrackingAsync(dtoquery);
        }
    }
#endif

    #endregion

    #region Internals

    private static IQueryable<Transaction> TransactionsForQuery_Payee(IQueryable<Transaction> result, string value) =>
        result.Where(x => x.Payee.Contains(value));

    private static IQueryable<Transaction> TransactionsForQuery_Category(IQueryable<Transaction> result, string value) =>
        (value.ToLowerInvariant() == "[blank]")
            ? result.Where(x => string.IsNullOrEmpty(x.Category) && !x.Splits.Any())
            : result.Where(x =>
                    x.Category.Contains(value)
                    ||
                    x.Splits.Any(s => s.Category.Contains(value)
                )
            );

    private static IQueryable<Transaction> TransactionsForQuery_Year(IQueryable<Transaction> result, string value) =>
        (Int32.TryParse(value, out int year))
            ? result.Where(x => x.Timestamp.Year == year)
            : result;

    private static IQueryable<Transaction> TransactionsForQuery_Memo(IQueryable<Transaction> result, string value) =>
        result.Where(x => x.Memo.Contains(value) || x.Splits.Any(s=>s.Memo.Contains(value)));

    private static IQueryable<Transaction> TransactionsForQuery_HasReceipt(IQueryable<Transaction> result, string value) =>
        value switch
        {
            "0" => result.Where(x => x.ReceiptUrl == null),
            "1" => result.Where(x => x.ReceiptUrl != null),
            _ => throw new ArgumentException($"Unexpected query parameter {value}", nameof(value))
        };
    private static IQueryable<Transaction> TransactionsForQuery_Imported(IQueryable<Transaction> result, string value) =>
        value switch
        {
            "0" => result.Where(x => x.Imported != true),
            "1" => result.Where(x => x.Imported == true),
            _ => throw new ArgumentException($"Unexpected query parameter {value}", nameof(value))
        };


    private static IQueryable<Transaction> TransactionsForQuery_Amount(IQueryable<Transaction> result, string value)
    {
        if (Int32.TryParse(value, out Int32 ival))
        {
            var cents = ((decimal)ival) / 100;
            return result.Where(x => x.Amount == (decimal)ival || x.Amount == cents || x.Amount == -(decimal)ival || x.Amount == -cents);
        }
        else if (decimal.TryParse(value, out decimal dval))
        {
            return result.Where(x => x.Amount == dval || x.Amount == -dval);
        }
        else
            throw new ArgumentException($"Unexpected query parameter {value}", nameof(value));
    }


    private IQueryable<Transaction> TransactionsForQuery_Date(IQueryable<Transaction> result, string value)
    {
        DateTime? dtval = null;

        if (Int32.TryParse(value, out int ival) && ival >= 101 && ival <= 1231)
            dtval = new DateTime(_clock.Now.Year, ival / 100, ival % 100);
        else if (DateTime.TryParse(value, out DateTime dtvalout))
        {
            dtval = new DateTime(_clock.Now.Year, dtvalout.Month, dtvalout.Day);
        }

        if (dtval.HasValue)
            return result.Where(x => x.Timestamp >= dtval.Value && x.Timestamp < dtval.Value.AddDays(7));
        else
            throw new ArgumentException($"Unexpected query parameter {value}", nameof(value));
    }

#endregion

}

#if false

public class TransactionIndexDto
{
    public int ID { get; set; }
    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
    [Display(Name = "Date")]
    public DateTime Timestamp { get; set; }
    public string Payee { get; set; }
    [DisplayFormat(DataFormatString = "{0:C2}")]
    public decimal Amount { get; set; }
    public string Category { get; set; }
    public string Memo { get; set; }
    public bool HasReceipt { get; set; }
    public bool HasSplits { get; set; }


    public string BankReference { get; set; }
    public bool Hidden { get; set; }
    public bool Selected { get; set; }

    public static explicit operator Transaction(TransactionIndexDto o) => new Transaction()
    {
        Category = o.Category,
        Memo = o.Memo,
        Payee = o.Payee
    };

    public bool Equals(Transaction other)
    {
        return string.Equals(Payee, other.Payee) && Amount == other.Amount && Timestamp.Date == other.Timestamp.Date;
    }
}
#endif
