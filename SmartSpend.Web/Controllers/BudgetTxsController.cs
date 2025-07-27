using Common.AspNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core.Repositories;
using SmartSpend.Core.Models;
using YoFi.Core.Importers;
using Ardalis.Filters;
using SmartSpend.Core;
using SmartSpend.Core.Repositories.Wire;
using YoFi.Core.Importers;

namespace SmartSpend.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class BudgetTxsController : Controller, IController<BudgetTx>
    {
        private readonly IBudgetTxRepository _repository;

        public BudgetTxsController(IBudgetTxRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(string q = null, string v = null, int? p = null)
        {

            bool showSelected = v?.ToLowerInvariant().Contains("s") == true;
            ViewData["ShowSelected"] = showSelected;
            ViewData["ToggleSelected"] = showSelected ? null : "s";


            var qresult = await _repository.GetByQueryAsync(new WireQueryParameters() { Query = q, Page = p, View = v });
            return View(qresult);
        }

        [ValidateBudgetTxExists]
        public async Task<IActionResult> Details(int? id)
        {
            return View(await _repository.GetByIdAsync(id));
        }

        public Task<IActionResult> Create()
        {
            return Task.FromResult(View() as IActionResult);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        public async Task<IActionResult> Create([Bind("ID,Amount,Timestamp,Category,Memo,Frequency")] BudgetTx item)
        {
            await _repository.AddAsync(item);

            return RedirectToAction(nameof(Index));
        }

        [ValidateBudgetTxExists]
        public async Task<IActionResult> Edit(int? id) => await Details(id);

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        [ValidateBudgetTxExists]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Amount,Timestamp,Category,Memo,Frequency")] BudgetTx item)
        {
            try
            {
                if (id != item.ID)
                    throw new ArgumentException();

                await _repository.UpdateAsync(item);

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        [ValidateBudgetTxExists]
        public async Task<IActionResult> Delete(int? id) => await Details(id);

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateBudgetTxExists]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.RemoveAsync(await _repository.GetByIdAsync(id));

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkDelete()
        {
            await _repository.BulkDeleteAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateFilesProvided(multiplefilesok: true)]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Upload(List<IFormFile> files, [FromServices] IImporter<BudgetTx> importer)
        {
            foreach (var file in files)
            {
                if (file.FileName.ToLower().EndsWith(".xlsx"))
                {
                    using var stream = file.OpenReadStream();
                    importer.QueueImportFromXlsx(stream);
                }
            }

            var imported = await importer.ProcessImportAsync();

            return View(imported);
        }

        public Task<IActionResult> Download()
        {
            var stream = _repository.AsSpreadsheet();

            var result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"BudgetTx.xlsx");

            return Task.FromResult(result as IActionResult);
        }

        Task<IActionResult> IController<BudgetTx>.Index() => Index();

        public Task<IActionResult> Upload(List<IFormFile> files) => Upload(files, new BaseImporter<BudgetTx>(_repository));
    }
}
