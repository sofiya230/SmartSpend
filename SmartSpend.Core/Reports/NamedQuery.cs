using System.Linq;
using SmartSpend.Core.Models;

namespace SmartSpend.Core.Reports;

public class NamedQuery
{
    public IQueryable<IReportable> Query { get; set; }

    public string Name { get; set; }

    public bool LeafRowsOnly { get; set; } = false;

    public bool IsMultiSigned { get; set; } = false;

    public NamedQuery Labeled(string newname) => new() { Name = newname, Query = Query, LeafRowsOnly = LeafRowsOnly, IsMultiSigned = IsMultiSigned };

    public NamedQuery AsLeafRowsOnly(bool leafrows) => new() { Name = Name, Query = Query, IsMultiSigned = IsMultiSigned, LeafRowsOnly = leafrows };
}
