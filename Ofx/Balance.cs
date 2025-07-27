using System;
using System.Globalization;
using System.Xml;

namespace SmartSpend.Ofx

{
    public class Balance
   {
      public decimal LedgerBalance { get; set; }

      public DateTime LedgerBalanceDate { get; set; }

      public decimal AvaliableBalance { get; set; }

      public DateTime AvaliableBalanceDate { get; set; }

      public Balance(XmlNode ledgerNode, XmlNode avaliableNode)
      {
         var tempLedgerBalance = ledgerNode.GetValue("//BALAMT");

         if (!String.IsNullOrEmpty(tempLedgerBalance))
         {
            LedgerBalance = Convert.ToDecimal(tempLedgerBalance, CultureInfo.InvariantCulture);
         }
         else
         {
             LedgerBalance = 0;
         }

         if (avaliableNode == null)
         {
            AvaliableBalance = 0;

            AvaliableBalanceDate = new DateTime();
         }
         else
         {
            var tempAvaliableBalance = avaliableNode.GetValue("//BALAMT");

            if (!String.IsNullOrEmpty(tempAvaliableBalance))
            {
               AvaliableBalance = Convert.ToDecimal(tempAvaliableBalance, CultureInfo.InvariantCulture);
            }
            else
            {
               throw new OfxParseException("Avaliable balance has not been set");
            }
            AvaliableBalanceDate = avaliableNode.GetValue("//DTASOF").ToDate();
         }

         LedgerBalanceDate = ledgerNode.GetValue("//DTASOF").ToDate();
      }
   }
}
