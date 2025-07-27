namespace SmartSpend.Core.Repositories.Wire;

public class WireQueryParameters : IWireQueryParameters
{
    public string Query { get; set; }

    public int? Page { get; set; }

    public string Order { get; set; }

    public string View { get; set; }

    public bool All { get; set; }
}
