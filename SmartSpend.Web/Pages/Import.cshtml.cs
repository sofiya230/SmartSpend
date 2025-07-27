using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.AspNet;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartSpend.Core;
using YoFi.Core.Importers;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories;
using SmartSpend.Core.Repositories.Wire;
using SmartSpend.Core.SampleData;
using YoFi.Core.Importers;

namespace SmartSpend.AspNet.Pages
{
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanRead")]
    public class ImportModel : PageModel
    {
        public const int MaxOtherItemsToShow = 10;
        private readonly ITransactionRepository _repository;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISampleDataProvider _loader;

        public IWireQueryResult<Transaction> Transactions { get; private set; }
        public IEnumerable<ISampleDataDownloadOffering> Offerings { get; private set; }

        public IEnumerable<BudgetTx> BudgetTxs { get; private set; } = Enumerable.Empty<BudgetTx>();
        public IEnumerable<Payee> Payees { get; private set; } = Enumerable.Empty<Payee>();
        public IEnumerable<Receipt> Receipts { get; private set; } = Enumerable.Empty<Receipt>();

        public int NumBudgetTxsUploaded { get; private set; }
        public int NumPayeesUploaded { get; private set; }
        public int NumReceiptsUploaded { get; private set; }

        public HashSet<int> Highlights { get; private set; } = new HashSet<int>();

        public string Error { get; private set; }

        public ImportModel(ITransactionRepository repository, IAuthorizationService authorizationService, ISampleDataProvider loader)
        {
            _repository = repository;
            _authorizationService = authorizationService;
            _loader = loader;
        }

        public async Task<IActionResult> OnGetAsync(int? p = null)
        {
            Transactions = await _repository.GetByQueryAsync(new WireQueryParameters() { Query = "i=1,y=*", Page = p, View = "h" } );

            Offerings = await _loader.ListDownloadOfferingsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostGoAsync(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command))
                    throw new ArgumentException();

                var canwrite = await _authorizationService.AuthorizeAsync(User, "CanWrite");
                if (!canwrite.Succeeded)
                    throw new UnauthorizedAccessException();

                if (command == "cancel")
                {
                    await _repository.CancelImportAsync();
                }
                else if (command == "ok")
                {
                    await _repository.FinalizeImportAsync();
                    return RedirectToAction(nameof(Index),"Transactions");
                }
                else
                    throw new ArgumentException();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/Account/AccessDenied", new { area = "Identity" });
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> files, [FromServices] UniversalImporter importer)
        {
            try
            {
                var canwrite = await _authorizationService.AuthorizeAsync(User, "CanWrite");
                if (!canwrite.Succeeded)
                    return RedirectToPage("/Account/AccessDenied", new { area = "Identity" });


                foreach (var formFile in files)
                {
                    using var stream = formFile.OpenReadStream();

                    var filetype = Path.GetExtension(formFile.FileName).ToLowerInvariant();

                    if (filetype == ".ofx")
                        await importer.QueueImportFromOfxAsync(stream);
                    else if (filetype == ".xlsx")
                        importer.QueueImportFromXlsx(stream);
                    else if (filetype == ".pdf" || filetype == ".png" || filetype == ".jpg")
                        await importer.QueueImportFromImageAsync(formFile.FileName,stream,formFile.ContentType);
                }

                await importer.ProcessImportAsync();
                BudgetTxs = importer.ImportedBudgetTxs.Take(MaxOtherItemsToShow);
                Payees = importer.ImportedPayees.Take(MaxOtherItemsToShow);
                Receipts = importer.ImportedReceipts.Take(MaxOtherItemsToShow);
                NumBudgetTxsUploaded = importer.ImportedBudgetTxs.Count();
                NumPayeesUploaded = importer.ImportedPayees.Count();
                NumReceiptsUploaded = importer.ImportedReceipts.Count();

                await OnGetAsync();

                Highlights = importer.HighlightIDs.ToHashSet();

            }
            catch (Exception ex)
            {
                Error = $"The import failed. This error was given: {ex.GetType().Name}: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnGetSampleAsync(string what) //, string how, [FromServices] IWebHostEnvironment e)
        {
            IActionResult result = NotFound();
            try
            {
                var offerings = await _loader.ListDownloadOfferingsAsync();

                var offering = offerings.Where(x => x.ID == what).Single();
                if (null != offering)
                {
                    var stream = await _loader.DownloadSampleDataAsync(what);
                    if (offering.FileType == SampleDataDownloadFileType.XLSX)
                        result = File(stream, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileDownloadName: $"{offering.Description}.{offering.FileType}");
                    else if (offering.FileType == SampleDataDownloadFileType.OFX)
                        result = File(stream, contentType: "application/ofx", fileDownloadName: $"{offering.Description}.{offering.FileType}");
                }
            }
            catch (Exception)
            {
                result = BadRequest();
            }

            return result;
        }
    }
}
