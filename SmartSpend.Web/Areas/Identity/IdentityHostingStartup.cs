using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(SmartSpend.AspNet.Areas.Identity.IdentityHostingStartup))]
namespace SmartSpend.AspNet.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}
