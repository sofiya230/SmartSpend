using System.Collections.Generic;
using System.Linq;

namespace Common.DotNet;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class DemoConfig
{
    public const string Section = "Demo";

    public bool IsOpenAccess { get; set; }
    public bool IsEnabled { get; set; }

    public bool IsHomePageRoot { get; set; } = true;

    public override string ToString()
    {
        var props = new List<string>();
        if (IsOpenAccess)
            props.Add(nameof(IsOpenAccess));
        if (IsEnabled)
            props.Add(nameof(IsEnabled));
        if (IsHomePageRoot)
            props.Add(nameof(IsHomePageRoot));
        if (!props.Any())
            props.Add("Empty");

        return $"Demo: {string.Join(' ', props)}";
    }
}
