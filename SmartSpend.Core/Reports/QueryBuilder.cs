using System;
using System.Collections.Generic;
using System.Linq;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Reports;

public class QueryBuilder
{
    #region Properties

    public int Year { get; set; }

    public int Month { get; set; }

    #endregion

    #region Constructor(s)
    public QueryBuilder(IDataProvider context)
    {
        _context = context;
    }
    #endregion

    #region Public Methods

    public IEnumerable<NamedQuery> LoadFrom(ReportDefinition definition)
    {
        string top = null;
        IEnumerable<string> excluded = null;
        bool leafrows = false;

        if (!string.IsNullOrEmpty(definition.SourceParameters))
        {
            var parms = definition.SourceParameters.Split('=');
            if (parms.Length != 2)
                throw new ArgumentException("Expected: Parameter:Value(s)",nameof(definition));

            if (parms[0] == "top")
                top = parms[1];
            else if (parms[0] == "excluded")
                excluded = parms[1].Split(',');
            else if (parms[0] == "leafrows")
                leafrows = true;
        }

        if (definition.Source == "Actual")
            return QueryActual(top, excluded);
        else if (definition.Source == "Budget")
            return QueryBudget(excluded);
        else if (definition.Source == "ActualVsBudget")
            return QueryActualVsBudget(leafrows, excluded);
        else if (definition.Source == "ManagedBudget")
            return QueryManagedBudget();
        else if (definition.Source == "YearOverYear")
            return QueryYearOverYear(out _); // TODO: How do I get years back out??
        else
            return Enumerable.Empty<NamedQuery>();
    }

    public IEnumerable<NamedQuery> QueryActual(string top = null, IEnumerable<string> excluded = null)
    {
        IQueryable<IReportable> txs = QueryTransactions(top,excluded).Query;
        IQueryable<IReportable> splits = QuerySplits(top,excluded).Query;

        return new List<NamedQuery>() {
            new NamedQuery()
            {
                Query = txs, //.Concat(splits),
                IsMultiSigned = (top == null) && (excluded == null)
            },
            new NamedQuery()
            {
                Query = splits,
                IsMultiSigned = (top == null) && (excluded == null)
            }
        };
    }

    /*
     * OK, I managed to concatenate the transactions and splits queries now, and hold it in EF Core all
     * the way to the end, which generates the following.
     * 
        SELECT [t1].[Category] AS [Name], DATEPART(month, [t1].[Timestamp]) AS [Month], SUM([t1].[Amount]) AS [Total]
        FROM (
            SELECT [t].[Amount], [t].[Timestamp], [t].[Category]
            FROM [Transactions] AS [t]
            WHERE (((DATEPART(year, [t].[Timestamp]) = @__Year_0) AND (([t].[Hidden] <> CAST(1 AS bit)) OR [t].[Hidden] IS NULL)) AND (DATEPART(month, [t].[Timestamp]) <= @__Month_1)) AND NOT (EXISTS (
                SELECT 1
                FROM [Split] AS [s]
                WHERE [t].[ID] = [s].[TransactionID]))
            UNION ALL
            SELECT [s0].[Amount], [t0].[Timestamp], [s0].[Category]
            FROM [Split] AS [s0]
            INNER JOIN [Transactions] AS [t0] ON [s0].[TransactionID] = [t0].[ID]
            WHERE ((DATEPART(year, [t0].[Timestamp]) = @__Year_0) AND (([t0].[Hidden] <> CAST(1 AS bit)) OR [t0].[Hidden] IS NULL)) AND (DATEPART(month, [t0].[Timestamp]) <= @__Month_1)
        ) AS [t1]
        GROUP BY [t1].[Category], DATEPART(month, [t1].[Timestamp])
     *
     * I will still need to figure out how to do the same on the "except" queries.
     */

    public IEnumerable<NamedQuery> QueryBudget(IEnumerable<string> excluded = null)
    {
        return new List<NamedQuery>() { QueryBudgetSingle(excluded) };
    }

    public IEnumerable<NamedQuery> QueryActualVsBudget(bool leafrows = false, IEnumerable<string> excluded = null)
    {
        var result = new List<NamedQuery>();

        result.AddRange(QueryActual(excluded: excluded).Select(x => x.Labeled("Actual").AsLeafRowsOnly(leafrows)));
        result.Add(QueryBudgetSingle(excluded).Labeled("Budget").AsLeafRowsOnly(leafrows));

        return result;
    }

    public IEnumerable<NamedQuery> QueryManagedBudget()
    {
#pragma warning disable IDE0028 // Simplify collection initialization
        var result = new List<NamedQuery>();

        result.Add(QueryManagedBudgetSingle().Labeled("Budget"));
        result.AddRange(QueryActual().Select(x => x.Labeled("Actual")));

        return result;
#pragma warning restore IDE0028 // Simplify collection initialization
    }

    public IEnumerable<NamedQuery> QueryYearOverYear(out int[] years)
    {
        var result = new List<NamedQuery>();

        years = _context.Get<Transaction>().Select(x => x.Timestamp.Year).Distinct().ToArray();
        foreach (var year in years)
        {
            var txsyear = _context.GetIncluding<Transaction,ICollection<Split>>(x=>x.Splits).Where(x => x.Hidden != true && x.Timestamp.Year == year).Where(x => !x.Splits.Any());
            var splitsyear = _context.GetIncluding<Split, Transaction>(x => x.Transaction).Where(x => x.Transaction.Hidden != true && x.Transaction.Timestamp.Year == year);

            result.Add(new NamedQuery() { Name = year.ToString(), Query = txsyear, IsMultiSigned = true });
            result.Add(new NamedQuery() { Name = year.ToString(), Query = splitsyear, IsMultiSigned = true });
        }

        return result;
    }

    #endregion

    #region Fields
    private readonly IDataProvider _context;
    #endregion

    #region Internals

    /*
     * EF Core does a great job of the transactions query. This is the final single query that it creates later
     * when doing GroupBy.
     *
        SELECT [t].[Category] AS [Name], DATEPART(month, [t].[Timestamp]) AS [Month], SUM([t].[Amount]) AS [Total]
        FROM [Transactions] AS [t]
        WHERE (((DATEPART(year, [t].[Timestamp]) = @__Year_0) AND (([t].[Hidden] <> CAST(1 AS bit)) OR [t].[Hidden] IS NULL)) AND (DATEPART(month, [t].[Timestamp]) <= @__Month_1)) AND NOT (EXISTS (
            SELECT 1
            FROM [Split] AS [s]
            WHERE [t].[ID] = [s].[TransactionID]))
        GROUP BY [t].[Category], DATEPART(month, [t].[Timestamp])
     */


    private NamedQuery QueryTransactions(string top = null, IEnumerable<string> excluded = null)
    {
        IQueryable<IReportable> result = _context.Get<Transaction>()
            .Where(x => x.Timestamp.Year == Year && x.Hidden != true && x.Timestamp.Month <= Month)
            .Where(x => !x.Splits.Any());

        if (top != null)
        {
            string ecolon = $"{top}:";
            result = result.Where(x => x.Category == top || x.Category.StartsWith(ecolon));
        }

        result = result
            .Select(x => new ReportableDto() { Amount = x.Amount, Timestamp = x.Timestamp, Category = x.Category });

        if (excluded?.Any() == true)
        {
            if (!string.IsNullOrEmpty(top))
                throw new ArgumentException("Cannot set top and excluded in the same query");

            var excludetopcategoriesstartswith = excluded
                .Select(x => $"{x}:")
                .ToList();

            result = result
                .Where(x => x.Category != null && !excluded.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excludetopcategoriesstartswith.Exists(y => x.Category.StartsWith(y)))
                .AsQueryable();
        }

        return new NamedQuery() { Query = result };
    }

    /*
     * EF Core does a decent job of the above as well. It's straightforward because we have the AsQueryable() here
     * which is needed for an of the Except* reports.
     * 
        SELECT [s].[Amount], [t].[Timestamp], [s].[Category]
        FROM [Split] AS [s]
        INNER JOIN [Transactions] AS [t] ON [s].[TransactionID] = [t].[ID]
        WHERE ((DATEPART(year, [t].[Timestamp]) = @__Year_0) AND (([t].[Hidden] <> CAST(1 AS bit)) OR [t].[Hidden] IS NULL)) AND (DATEPART(month, [t].[Timestamp]) <= @__Month_1)
     *
     * Leaving the AsQueryable() out above works fine on All report, and generates the following
     * when doing GroupBy, which is perfect.
     * 
        SELECT [s].[Category] AS [Name], DATEPART(month, [t].[Timestamp]) AS [Month], SUM([s].[Amount]) AS [Total]
        FROM [Split] AS [s]
        INNER JOIN [Transactions] AS [t] ON [s].[TransactionID] = [t].[ID]
        WHERE ((DATEPART(year, [t].[Timestamp]) = @__Year_0) AND (([t].[Hidden] <> CAST(1 AS bit)) OR [t].[Hidden] IS NULL)) AND (DATEPART(month, [t].[Timestamp]) <= @__Month_1)
        GROUP BY [s].[Category], DATEPART(month, [t].[Timestamp])
     
     * Unfortunately, taking out the AsEnumerable() leads "QuerySplitsExcept" below to fail with a
     * "could not be translated" error.
     * 
     * OK so what I did was move the AsEnumerable() down to QuerySplitsExcept(), which is the only
     * place it was really needed.
     */

    private NamedQuery QuerySplits(string top = null, IEnumerable<string> excluded = null)
    {
        var result = _context.GetIncluding<Split, Transaction>(x => x.Transaction)
            .Where(x => x.Transaction.Timestamp.Year == Year && x.Transaction.Hidden != true && x.Transaction.Timestamp.Month <= Month)
            .Select(x => new ReportableDto() { Amount = x.Amount, Timestamp = x.Transaction.Timestamp, Category = x.Category })
            .AsQueryable<IReportable>();

        if (top != null)
        {
            string ecolon = $"{top}:";
            result = result.Where(x => x.Category == top || x.Category.StartsWith(ecolon));
        }

        if (excluded?.Any() == true)
        {
            if (!string.IsNullOrEmpty(top))
                throw new ArgumentException("Cannot set top and excluded in the same query");

            var excludetopcategoriesstartswith = excluded
                .Select(x => $"{x}:")
                .ToList();

            result = result
                .Where(x => !excluded.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !excludetopcategoriesstartswith.Exists(y => x.Category?.StartsWith(y) ?? false))
                .AsQueryable();
        }

        return new NamedQuery() { Query = result };
    }

    private NamedQuery QueryBudgetSingle(IEnumerable<string> excluded = null)
    {
        var result = QueryBudgetSingle_AsBudgetTx(excluded);
        return new NamedQuery() { Query = result, IsMultiSigned = (excluded == null) };
    }

    private IQueryable<BudgetTx> QueryBudgetSingle_AsBudgetTx(IEnumerable<string> excluded = null)
    {
        var result = _context.Get<BudgetTx>()
            .Where(x => x.Timestamp.Year == Year)
            .AsQueryable();

        if (excluded?.Any() == true)
        {
            var topstarts = excluded
                .Select(x => $"{x}:")
                .ToList();

            result = result
                .Where(x => x.Category != null && !excluded.Contains(x.Category))
                .AsEnumerable()
                .Where(x => !topstarts.Exists(y => x.Category.StartsWith(y)))
                .AsQueryable();
        }

        return result;
    }

    private NamedQuery QueryManagedBudgetSingle()
    {
        var budgettxs = QueryBudgetSingle_AsBudgetTx();

        var managedtxs = budgettxs.Where(x => x.Frequency > 1).AsEnumerable();

        var result = managedtxs.SelectMany(x => x.Reportables.Where(r=>r.Timestamp.Month <= Month)).AsQueryable();

#if false
        foreach (var it in managedtxs.ToList())
            Console.WriteLine($"{it.Timestamp} {it.Category} {it.Amount}");
#endif

        return new NamedQuery() { Query = result, LeafRowsOnly = true };
    }

    /*
     * Here's the query for above:
     * 
        SELECT [b].[Category] AS [Name], SUM([b].[Amount]) AS [Total]
        FROM [BudgetTxs] AS [b]
        WHERE (DATEPART(year, [b].[Timestamp]) = @__Year_0) AND ((DATEPART(month, [b].[Timestamp]) <= @__Month_1) AND [b].[Category] IN (
            SELECT [b0].[Category]
            FROM [BudgetTxs] AS [b0]
            WHERE DATEPART(year, [b0].[Timestamp]) = @__Year_0
            GROUP BY [b0].[Category]
            HAVING COUNT(*) > 1
        )
        )
        GROUP BY [b].[Category]
     */

    #endregion
}

class ReportableDto : IReportable
{
    public decimal Amount { get; set; }

    public DateTime Timestamp { get; set; }

    public string Category { get; set; }
}
