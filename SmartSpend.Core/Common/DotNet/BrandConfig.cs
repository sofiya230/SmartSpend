namespace Common.DotNet;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class BrandConfig
{
    public const string Section = "Brand";

    public string Name { get; set; }
    public string Icon { get; set; }
    public string Link { get; set; }
    public string Owner { get; set; }

    public bool Exists => Name != null;
}
