using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories;

namespace SmartSpend.AspNet.Controllers
{
    [Route("ajax/budget")]
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class AjaxBudgetController: Controller
    {
        private readonly IBudgetTxRepository _repository;

        public AjaxBudgetController(IBudgetTxRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("select/{id}")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateBudgetTxExists]
        public async Task<IActionResult> Select(int id, bool value)
        {
            await _repository.SetSelectedAsync(id, value);

            return new OkResult();
        }
    }
}
