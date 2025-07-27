using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Sgml;

namespace SmartSpend.Ofx

{
    public class OfxDocumentParser
    {
        private Dictionary<string, string> PossibleHeaders = new Dictionary<string, string>
        {
            { "OFXHEADER", "100" },
            { "DATA", "OFXSGML" },
            { "VERSION", "102" },
            { "SECURITY", "NONE" },
            { "ENCODING", "USASCII,UTF-8" },
            { "CHARSET", "1252" },
            { "COMPRESSION", "NONE" },
            { "OLDFILEUID", "NONE" },
        };

        public OfxDocument Import(Stream stream, Encoding encoding)
        {
            using (var reader = new StreamReader(stream, encoding))
            {
                return Import(reader.ReadToEnd());
            }
        }

        public OfxDocument Import(Stream stream)
        {
            return Import(stream, Encoding.Default);
        }

        public OfxDocument Import(string ofx)
        {
            return ParseOfxDocument(ofx);
        }

        private OfxDocument ParseOfxDocument(string ofxString)
        {
            if (!IsXmlVersion(ofxString))
            {
                ofxString = SgmltoXml(ofxString);
            }

            return Parse(ofxString);
        }

        private OfxDocument Parse(string ofxString)
        {
            var ofx = new OfxDocument { AccType = GetAccountType(ofxString) };

            var doc = new XmlDocument();
            doc.Load(new StringReader(ofxString));

            var currencyNode = doc.SelectSingleNode(GetXPath(ofx.AccType, OfxSection.Currency));

            if (currencyNode != null)
            {
                ofx.Currency = currencyNode.FirstChild.Value;
            }
            else
            {
                throw new OfxParseException("Currency not found");
            }

            var signOnNode = doc.SelectSingleNode(Resources.SignOn);

            if (signOnNode != null)
            {
                ofx.SignOn = new SignOn(signOnNode);
            }
            else
            {
                throw new OfxParseException("Sign On information not found");
            }

            var accountNode = doc.SelectSingleNode(GetXPath(ofx.AccType, OfxSection.AccountInfo));

            if (accountNode != null)
            {
                ofx.Account = new Account(accountNode, ofx.AccType);
            }
            else
            {
                throw new OfxParseException("Account information not found");
            }

            ImportTransations(ofx, doc);

            var ledgerNode = doc.SelectSingleNode(GetXPath(ofx.AccType, OfxSection.Balance) + "/LEDGERBAL");
            var avaliableNode = doc.SelectSingleNode(GetXPath(ofx.AccType, OfxSection.Balance) + "/AVAILBAL");

            if (ledgerNode != null) // && avaliableNode != null
            {
                ofx.Balance = new Balance(ledgerNode, avaliableNode);
            }
            else
            {
                throw new OfxParseException("Balance information not found");
            }

            return ofx;
        }


        private string GetXPath(AccountType type, OfxSection section)
        {
            string xpath, accountInfo;

            switch (type)
            {
                case AccountType.Bank:
                    xpath = Resources.BankAccount;
                    accountInfo = "/BANKACCTFROM";
                    break;
                case AccountType.Cc:
                    xpath = Resources.CCAccount;
                    accountInfo = "/CCACCTFROM";
                    break;
                default:
                    throw new OfxException("Account Type not supported. Account type " + type);
            }

            switch (section)
            {
                case OfxSection.AccountInfo:
                    return xpath + accountInfo;
                case OfxSection.Balance:
                    return xpath;
                case OfxSection.Transactions:
                    return xpath + "/BANKTRANLIST";
                case OfxSection.Signon:
                    return Resources.SignOn;
                case OfxSection.Currency:
                    return xpath + "/CURDEF";
                default:
                    throw new OfxException("Unknown section found when retrieving XPath. Section " + section);
            }
        }

        private void ImportTransations(OfxDocument ofxDocument, XmlNode xmlDocument)
        {
            var xpath = GetXPath(ofxDocument.AccType, OfxSection.Transactions);

            ofxDocument.StatementStart = xmlDocument.GetValue(xpath + "//DTSTART").ToDate();
            ofxDocument.StatementEnd = xmlDocument.GetValue(xpath + "//DTEND").ToDate();

            var transactionNodes = xmlDocument.SelectNodes(xpath + "//STMTTRN");
            ofxDocument.Transactions = new List<Transaction>();

            if (transactionNodes == null) return;
            foreach (XmlNode node in transactionNodes)
            {
                ofxDocument.Transactions.Add(new Transaction(node, ofxDocument.Currency));
            }
        }


        private AccountType GetAccountType(string file)
        {
            if (file.IndexOf("<CREDITCARDMSGSRSV1>", StringComparison.Ordinal) != -1)
                return AccountType.Cc;

            if (file.IndexOf("<BANKMSGSRSV1>", StringComparison.Ordinal) != -1)
                return AccountType.Bank;

            throw new OfxException("Unsupported Account Type");
        }

        private bool IsXmlVersion(string file)
        {
            return (file.IndexOf("OFXHEADER:100", StringComparison.Ordinal) == -1);
        }

        private string SgmltoXml(string file)
        {
            var reader = new SgmlReader
            {
                InputStream = new StringReader(ParseHeader(file)),
                DocType = "OFX"
            };

            var sw = new StringWriter();
            var xml = new XmlTextWriter(sw);

            while (!reader.EOF)
                xml.WriteNode(reader, true);

            xml.Flush();
            xml.Close();

            var temp = sw.ToString().TrimStart().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            return String.Join("", temp);
        }

        private string ParseHeader(string file)
        {
            var header = file.Substring(0, file.IndexOf('<'))
               .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            CheckHeader(header);

            return file.Substring(file.IndexOf('<') - 1);
        }

        private void CheckHeader(string[] header)
        {
            foreach (var item in header)
            {
                var headerName = item.Split(':')[0];
                var headerValue = item.Split(':')[1];

                if (PossibleHeaders.ContainsKey(headerName))
                {
                    if (!PossibleHeaders[headerName].Contains(headerValue))
                        throw new OfxParseException($"The header {headerName}, cannot contain the {headerValue} value.\r\n\r\nPossible Values: {PossibleHeaders[headerName]}");
                }
            }
        }

        #region Nested type: OFXSection

        private enum OfxSection
        {
            Signon,
            AccountInfo,
            Transactions,
            Balance,
            Currency
        }

        #endregion
    }
}
