using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SmartSpend.Core;

namespace SmartSpend.AspNet.Pages
{
    [AllowAnonymous]
    public class HomeModel : PageModel
    {
        public bool IsRouteToAdmin { get; set; }

        public HomeModel(DemoConfig democonfig)
        {
            isDemo = democonfig.IsEnabled;
        }

        public bool isDemo { get; private set; }

        public async Task OnGetAsync([FromServices] IDataAdminProvider dbadmin)
        {

            var status = await dbadmin.GetDatabaseStatus();

            var isempty = status.NumTransactions == 0;
            var loggedin = User.Identity.IsAuthenticated;
            var isadmin = User.IsInRole("Admin");

            IsRouteToAdmin = isempty && (!loggedin || isadmin);
        }
    }
}
