using System.Collections.Generic;

namespace SmartSpend.Core.Repositories.Wire;

public interface IWireQueryParameters
{
    string Query { get; }
    int? Page { get; }
    string Order { get; }
    public string View { get; }
    
    public bool All { get; }
}

public interface IWireQueryResultBase
{
    IWireQueryParameters Parameters { get; }

    IWirePageInfo PageInfo { get; }
}

public interface IWireQueryResult<out T>: IWireQueryResultBase
{
    IEnumerable<T> Items { get; }
}

public interface IWirePageInfo
{
    int Page { get; }

    int PageSize { get; }

    int FirstItem { get; }

    int NumItems { get; }

    int TotalPages { get; }

    int TotalItems { get; }
}
