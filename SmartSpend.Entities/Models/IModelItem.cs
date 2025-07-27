using System.Linq;

namespace SmartSpend.Core.Models
{
    public interface IModelItem<T>: IID
    {
        IQueryable<T> InDefaultOrder(IQueryable<T> original);
    }
}
