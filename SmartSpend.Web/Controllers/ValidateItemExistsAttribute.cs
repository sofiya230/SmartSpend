using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories;

namespace SmartSpend.AspNet.Controllers
{
    public class ValidateTransactionExistsAttribute : TypeFilterAttribute
    {
        public ValidateTransactionExistsAttribute() : base(typeof
          (ValidateItemExists<Transaction>))
        {
        }
    }

    public class ValidatePayeeExistsAttribute : TypeFilterAttribute
    {
        public ValidatePayeeExistsAttribute() : base(typeof
          (ValidateItemExists<Payee>))
        {
        }
    }

    public class ValidateBudgetTxExistsAttribute : TypeFilterAttribute
    {
        public ValidateBudgetTxExistsAttribute() : base(typeof
          (ValidateItemExists<BudgetTx>))
        {
        }
    }

    public class ValidateSplitExistsAttribute : TypeFilterAttribute
    {
        public ValidateSplitExistsAttribute() : base(typeof
          (ValidateItemExists<Split>))
        {
        }
    }

    public class ValidateReceiptExistsAttribute : TypeFilterAttribute
    {
        public ValidateReceiptExistsAttribute() : base(typeof
          (ValidateItemExists<Receipt>))
        {
        }
    }


    internal class ValidateItemExists<T> : IAsyncActionFilter where T : class, IID
    {
        private readonly IDataProvider _context;
        public ValidateItemExists(IDataProvider context)
        {
            _context = context;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context,
          ActionExecutionDelegate next)
        {
            if (context.ActionArguments.ContainsKey("id"))
            {
                var id = context.ActionArguments["id"] as int?;
                if (id.HasValue)
                {
                    var query = _context.Get<T>().Where(x => x.ID == id.Value);
                    var exists = await _context.AnyAsync(query);

                    if (!exists)
                    {
                        context.Result = new NotFoundResult();
                        return;
                    }
                }
                else
                {
                    context.Result = new BadRequestResult();
                    return;
                }
            }
            else
            {
                context.Result = new BadRequestResult();
                return;
            }
            await next();
        }
    }
}
