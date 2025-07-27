using System.Collections.Generic;

namespace SmartSpend.Core.Models
{
    public class Account
    {
        public int ID { get; set; }

        public string User { get; set; }

        public ICollection<Transaction> Transactions { get; set; }
    }
}
