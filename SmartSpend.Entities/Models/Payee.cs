using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SmartSpend.Core.Models
{
    public class Payee: IModelItem<Payee>, IImportDuplicateComparable
    {
        public int ID { get; set; }

        [Editable(true)]
        [Category("TestKey")]
        public string Name { get; set; }

        [Editable(true)]
        public string Category { get; set; }

        public bool? Selected { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Payee payee &&
                   Name == payee.Name &&
                   Category == payee.Category;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Category);
        }

        public IQueryable<Payee> InDefaultOrder(IQueryable<Payee> original)
        {
            return original.OrderBy(x => x.Category).ThenBy(x=>x.Name);
        }

        int IImportDuplicateComparable.GetImportHashCode() => Name?.GetHashCode() ?? 0;

        bool IImportDuplicateComparable.ImportEquals(object other) => other is Payee && Name == (other as Payee).Name;
    }
}
