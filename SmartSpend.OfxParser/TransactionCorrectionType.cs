using System.ComponentModel;

namespace OfxSharpLib
{
    public enum TransactionCorrectionType
    {
        [Description("No correction needed")]
        NA,
        [Description("Replace this transaction with one referenced by CORRECTFITID")]
        REPLACE,
        [Description("Delete transaction")]
        DELETE,
    }
}
