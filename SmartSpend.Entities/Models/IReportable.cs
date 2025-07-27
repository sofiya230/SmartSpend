using System;

namespace SmartSpend.Core.Models
{
    public interface IReportable
    {
        decimal Amount { get; }

        DateTime Timestamp { get; }

        string Category { get; }
    }
}
