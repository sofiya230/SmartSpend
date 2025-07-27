using Ardalis.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories;

namespace SmartSpend.AspNet.Controllers
{
    [Route("ajax/payee")]
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class AjaxPayeeController: Controller
    {
        private readonly IPayeeRepository _repository;

        public AjaxPayeeController(IPayeeRepository repository) 
        {
            _repository = repository;
        }

        [HttpPost("select/{id}")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidatePayeeExists]
        public async Task<IActionResult> Select(int id, bool value)
        {
            await _repository.SetSelectedAsync(id, value);
            return new OkResult();
        }

        [HttpPost("add")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateModel]
        public async Task<IActionResult> Add([Bind("Name,Category")] Payee payee)
        {
            await _repository.AddAsync(payee);
            return new ObjectResult(payee);
        }

        [HttpPost("edit/{id}")]
        [Authorize(Policy = "CanWrite")]
        [ValidateAntiForgeryToken]
        [ValidateModel]
        [ValidatePayeeExists]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Category")] Payee payee)
        {
            await _repository.UpdateAsync(payee);
            return new ObjectResult(payee);
        }
    }
}
