using System;
using System.Xml;

namespace SmartSpend.Ofx

{
    public class Account
    {
        public string AccountId { get; set; }
        public string AccountKey { get; set; }
        public AccountType AccountType { get; set; }

        #region Bank Only

        private BankAccountType _bankAccountType = BankAccountType.NA;

        public string BankId { get; set; }

        public string BranchId { get; set; }


        public BankAccountType BankAccountType
        {
            get
            {
                if (AccountType == AccountType.Bank)
                    return _bankAccountType;
                
                return BankAccountType.NA;
            }
            set 
            {
                _bankAccountType = AccountType == AccountType.Bank ? value : BankAccountType.NA;
            }
        }

        #endregion

        public Account(XmlNode node, AccountType type)
        {
            AccountType = type;

            AccountId = node.GetValue("//ACCTID");
            AccountKey = node.GetValue("//ACCTKEY");

            switch (AccountType)
            {
                case AccountType.Bank:
                    InitializeBank(node);
                    break;
                case AccountType.Ap:
                    InitializeAP(node);
                    break;
                case AccountType.Ar:
                    InitializeAR(node);
                    break;
            }
        }

        private void InitializeBank(XmlNode node)
        {
            BankId = node.GetValue("//BANKID");
            BranchId = node.GetValue("//BRANCHID");

            string bankAccountType = node.GetValue("//ACCTTYPE");

            if (String.IsNullOrEmpty(bankAccountType))
                throw new OfxParseException("Bank Account type unknown");

            _bankAccountType = bankAccountType.GetBankAccountType();
        }

        #region Account types not supported

        private void InitializeAP(XmlNode node)
        {
            throw new OfxParseException("AP Account type not supported");
        }

        private void InitializeAR(XmlNode node)
        {
            throw new OfxParseException("AR Account type not supported");
        }

        #endregion
    }
}
