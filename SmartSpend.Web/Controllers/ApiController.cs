using Common.AspNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core;
using SmartSpend.Core.Reports;
using SmartSpend.Core.Repositories;
using SmartSpend.Core.Repositories.Wire;

namespace SmartSpend.AspNet.Controllers
{
    [Route("api")]
    [Produces("application/json")]
    [SkipStatusCodePages]
    public class ApiController : Controller
    {
        public ApiController()
        {
        }

        [HttpGet("{id}", Name = "Get")]
        [ApiBasicAuthorization]
        [ValidateTransactionExists]
        public async Task<IActionResult> Get(int id, [FromServices] ITransactionRepository repository)
        {
            return new OkObjectResult(await repository.GetByIdAsync(id));
        }

        [HttpGet("ReportV2/{slug}")]
        [ApiBasicAuthorization]
        public IActionResult ReportV2([Bind("slug,year,month,showmonths,level")] ReportParameters parms, [FromServices] IReportEngine reports)
        {
            try
            {
                var json = reports.Build(parms).ToJson();
                return Content(json, "application/json");
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }
        }

        [HttpGet("txi")]
        [ApiBasicAuthorization]
        public async Task<IActionResult> GetTransactions([FromServices] ITransactionRepository repository, string q = null)
        {
            var result = await repository.GetByQueryAsync(new WireQueryParameters() { Query = q, All = true });
            return new OkObjectResult(result.Items);
        }

        [HttpPost("ClearTestData/{id}")]
        [ApiBasicAuthorization]
        public async Task<IActionResult> ClearTestData(string id, [FromServices] IDataAdminProvider dbadmin)
        {
            await dbadmin.ClearTestDataAsync(id);
            return new OkResult();
        }
    }
}
