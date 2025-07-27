using Ardalis.Filters;
using Common.AspNet;
using Common.DotNet;
using Common.DotNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SmartSpend.AspNet.Boilerplate.Models;
using SmartSpend.AspNet.Pages.Helpers;
using SmartSpend.Core;
using YoFi.Core.Importers;
using SmartSpend.Core.Repositories;
using SmartSpend.Core.Repositories.Wire;
using SmartSpend.Core.SampleData;
using Transaction = SmartSpend.Core.Models.Transaction;
using YoFi.Core.Importers;

namespace SmartSpend.AspNet.Controllers
{
    [Authorize(Policy = "CanRead")]
    public class TransactionsController : Controller, IController<Transaction>
    {
        #region Constructor

        public TransactionsController(ITransactionRepository repository, IClock clock)
        {
            _repository = repository;
            _clock = clock;
        }

        #endregion

        #region Action Handlers: Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateTransactionExists]
        public async Task<IActionResult> CreateSplit(int id)
        {
            var result = await _repository.AddSplitToAsync(id);

            return RedirectToAction("Edit", "Splits", new { id = result });
        }

        public async Task<IActionResult> Create([FromServices] IReceiptRepository rrepo, int? rid)
        {
            if (rrepo != null)
                return View(await rrepo.CreateTransactionAsync(rid));
            else
                return View(await _repository.CreateAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        public async Task<IActionResult> Create([Bind("ID,Timestamp,Amount,Memo,Payee,Category,BankReference,ReceiptUrl")] Transaction transaction, [FromServices] IReceiptRepository rrepo)
        {

            await rrepo.AddTransactionAsync(transaction);

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Action Handlers: Read (Index, Details)

        public async Task<IActionResult> Index(string o = null, int? p = null, string q = null, string v = null)
        {
            try
            {
                var qresult = await _repository.GetByQueryAsync(new WireQueryParameters() { Query = q, Page = p, View = v, Order = o });
                var viewmodel = new TransactionsIndexPresenter(qresult);
                return View(viewmodel);
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        [ValidateTransactionExists]
        public async Task<IActionResult> Details(int? id)
        {
            return View(await _repository.GetByIdAsync(id));
        }

        [ValidateTransactionExists]
        public async Task<IActionResult> Print(int? id)
        {
            return View(await _repository.GetByIdAsync(id));
        }

        #endregion

        #region Action Handlers: Update (Edit)

        [ValidateTransactionExists]
        public async Task<IActionResult> Edit(int? id, [FromServices] IReceiptRepository rrepo)
        {
            (var transaction, var auto_category) = await _repository.GetWithSplitsAndMatchCategoryByIdAsync(id);
            ViewData["AutoCategory"] = auto_category;

            var matches = await rrepo.GetMatchingAsync(transaction);
            ViewData["Receipt.Any"] = matches.Any;
            ViewData["Receipt.Matches"] = matches.Matches;
            ViewData["Receipt.Suggested"] = matches.Suggested;

            return View(transaction);

        }

        [ValidateTransactionExists]
        public async Task<IActionResult> EditModal(int? id)
        {
            (var transaction, var auto_category) = await _repository.GetWithSplitsAndMatchCategoryByIdAsync(id);
            ViewData["AutoCategory"] = auto_category;

            return PartialView("EditPartial", transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateModel]
        [ValidateTransactionExists]
        public async Task<IActionResult> Edit(int id, bool? duplicate, [Bind("ID,Timestamp,Amount,Memo,Payee,Category,BankReference")] Transaction transaction)
        {
            if (duplicate == true)
            {
                await _repository.AddAsync(transaction);
            }
            else
            {
                await _repository.UpdateTransactionAsync(id, transaction);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> BulkEdit(string Category)
        {
            await _repository.BulkEditAsync(Category);

            return RedirectToAction(nameof(Index));
        }

#endregion

#region Action Handlers: Delete

        [ValidateTransactionExists]
        public async Task<IActionResult> Delete(int? id) => await Details(id);

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateTransactionExists]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.RemoveAsync(await _repository.GetByIdAsync(id));

            return RedirectToAction(nameof(Index));
        }

#endregion

#region Action Handlers: Download/Export

        [HttpPost]
        public async Task<IActionResult> Download(bool allyears, string q = null)
        {
            var sessionvars = new SessionVariables(HttpContext);

            Stream stream = await _repository.AsSpreadsheetAsync(sessionvars.Year ?? _clock.Now.Year, allyears,q);

            IActionResult result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: "Transactions.xlsx");

            return result;
        }

        public IActionResult DownloadPartial()
        {
            var sessionvars = new SessionVariables(HttpContext);
            var year = sessionvars.Year ?? _clock.Now.Year;

            return PartialView(year);
        }

#endregion

#region Action Handlers: Upload/Import

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        [ValidateFilesProvided(multiplefilesok: true)]
        [ValidateTransactionExists]
        public async Task<IActionResult> UpSplits(List<IFormFile> files, int id, [FromServices] SplitImporter importer)
        {
            var transaction = await _repository.GetWithSplitsByIdAsync(id);

            foreach (var file in files.Where(x => Path.GetExtension(x.FileName).ToLowerInvariant() == ".xlsx"))
            {
                using var stream = file.OpenReadStream();
                importer.QueueImportFromXlsx(stream);
            }

            await importer.ProcessImportAsync(transaction);

            return RedirectToAction("Edit", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Seed(string id, [FromServices] ISampleDataProvider loader)
        {
            var result = string.Empty;
            var resultdetails = string.Empty;
            try
            {
                resultdetails = await loader.SeedAsync(id);
                result = "Completed";
            }
            catch (ApplicationException ex)
            {
                result = "Sorry";
                resultdetails = ex.Message + " (E1)";
            }
            catch (Exception ex)
            {
                result = "Sorry";
                resultdetails = $"The operation failed. Please file an issue on GitHub. {ex.GetType().Name}: {ex.Message} (E2)";
            }

            return PartialView("Seed",(result,resultdetails));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DatabaseDelete(string id, [FromServices] IDataAdminProvider dbadmin)
        {
            await dbadmin.ClearDatabaseAsync(id);

            return (IActionResult)RedirectToPage("/Admin");
        }

#endregion

#region Action Handlers: Receipts

        [HttpPost]
        [Authorize(Policy = "CanWrite")]
        [ValidateTransactionExists]
        [ValidateFilesProvided(multiplefilesok: false)]
        [ValidateStorageAvailable]
        public async Task<IActionResult> UpReceipt(List<IFormFile> files, int id)
        {
            var transaction = await _repository.GetByIdAsync(id);

            var formFile = files.Single();
            using var stream = formFile.OpenReadStream();
            await _repository.UploadReceiptAsync(transaction, stream, formFile.ContentType);

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        [ValidateStorageAvailable]
        [ValidateTransactionExists]
        public async Task<IActionResult> ReceiptAction(int id, string action)
        {
            if (action == "delete")
                return await DeleteReceipt(id);
            else if (action == "get")
                return await GetReceipt(id);
            else
                return RedirectToAction(nameof(Edit), new { id });
        }

        private async Task<IActionResult> DeleteReceipt(int id)
        {
            await _repository.DeleteReceiptAsync(id);

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpGet]
        [ValidateTransactionExists]
        [ValidateStorageAvailable]
        public async Task<IActionResult> GetReceipt(int id)
        {
            var transaction = await _repository.GetByIdAsync(id);
            var (stream, contenttype, name) = await _repository.GetReceiptAsync(transaction);
            return File(stream, contenttype, name);
        }

#endregion

#region Action Handlers: Error

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

#endregion

#region Internals

        private readonly ITransactionRepository _repository;
        private readonly IClock _clock;
#endregion

#region IController
        Task<IActionResult> IController<Transaction>.Index() => Index();
        Task<IActionResult> IController<Transaction>.Edit(int id, Transaction item) => Edit(id, false, item);
        Task<IActionResult> IController<Transaction>.Download() => Download(false);
        Task<IActionResult> IController<Transaction>.Upload(List<IFormFile> files) => throw new NotImplementedException();
        Task<IActionResult> IController<Transaction>.Edit(int? id) => Edit(id, null);

        Task<IActionResult> IController<Transaction>.Create() => Create(rrepo:null,rid:null);

        Task<IActionResult> IController<Transaction>.Create(Transaction item) => Create(item, rrepo: null);
#endregion
    }
}

