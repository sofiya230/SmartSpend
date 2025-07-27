using System;
using System.IO;
using System.Xml;

namespace SmartSpend.Ofx

{
    public static class OfxHelperMethods
    {
        public static BankAccountType GetBankAccountType(this string bankAccountType)
        {
            return (BankAccountType)Enum.Parse(typeof(BankAccountType), bankAccountType);
        }

        public static DateTime ToDate(this string date)
        {
            try
            {
                if (date.Length < 8)
                {
                    return new DateTime();
                }

                var dd = Int32.Parse(date.Substring(6, 2));
                var mm = Int32.Parse(date.Substring(4, 2));
                var yyyy = Int32.Parse(date.Substring(0, 4));

                if (dd == 0 || mm == 0 || yyyy == 0) //Bradesco, alguns campos datas vem com 8 caracteres 0
                    return DateTime.Now.Date;

                return new DateTime(yyyy, mm, dd);
            }
            catch
            {
                throw new OfxParseException("Unable to parse date");
            }
        }

        public static string GetValue(this XmlNode node, string xpath)
        {
            var fixedNode = new XmlDocument();
            fixedNode.Load(new StringReader(node.OuterXml));

            var tempNode = fixedNode.SelectSingleNode(xpath);
            return tempNode != null && tempNode.FirstChild != null ? tempNode.FirstChild.Value : "";
        }
    }
}
