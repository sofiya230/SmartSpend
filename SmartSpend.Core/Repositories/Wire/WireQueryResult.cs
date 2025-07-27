using System.Collections.Generic;

namespace SmartSpend.Core.Repositories.Wire;

public class WireQueryResult<T> : IWireQueryResult<T> where T : class
{
    public IWireQueryParameters Parameters { get; set; }

    public IEnumerable<T> Items { get; set; }

    public IWirePageInfo PageInfo { get; set; }
}
