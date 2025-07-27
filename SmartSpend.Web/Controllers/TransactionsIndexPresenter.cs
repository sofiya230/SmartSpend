using Common.AspNet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SmartSpend.Core;
using SmartSpend.Core.Models;
using SmartSpend.Core.Repositories.Wire;

namespace SmartSpend.AspNet.Controllers
{
    public class TransactionsIndexPresenter : IWireQueryResult<Transaction>
    {
        public TransactionsIndexPresenter(IWireQueryResult<Transaction> qresult)
        {
            _qresult = qresult;
            ShowHidden = qresult.Parameters.View?.ToLowerInvariant().Contains("h") == true;
            ShowSelected = qresult.Parameters.View?.ToLowerInvariant().Contains("s") == true;
        }

        private readonly IWireQueryResult<Transaction> _qresult;

        #region IWireQueryParameters
        public IWireQueryParameters Parameters => _qresult.Parameters;

        public IEnumerable<Transaction> Items => _qresult.Items;

        public IWirePageInfo PageInfo => _qresult.PageInfo;

        #endregion

        #region Public Properties -- Used by View to display

        public bool ShowHidden { get; set; }

        public bool ShowSelected { get; set; }

        public string NextDateOrder => (Parameters.Order == null) ? "da" : null; /* not "dd", which is default */

        public string NextPayeeOrder => (Parameters.Order == "pa") ? "pd" : "pa";

        public string NextCategoryOrder => (Parameters.Order == "ca") ? "cd" : "ca";

        public string NextAmountOrder => (Parameters.Order == "aa") ? "as" : "aa";

        public string NextBankReferenceOrder => (Parameters.Order == "ra") ? "rd" : "ra";

        public string ToggleHidden => (ShowHidden ? string.Empty : "h") + (ShowSelected ? "s" : string.Empty);

        public string ToggleSelected => (ShowHidden ? "h" : string.Empty) + (ShowSelected ? string.Empty : "s");
        #endregion

    }
}
