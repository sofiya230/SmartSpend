#define __DEMO_OPEN_ACCESS__

using Common.DotNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using SmartSpend.AspNet.Boilerplate.Models;
using SmartSpend.Data;
using SmartSpend.Main.Seeders;
using SmartSpend.AspNet.Pages;
using SmartSpend.Core;
using YoFi.Core.Importers;
using SmartSpend.Core.Models;
using SmartSpend.Core.Reports;
using SmartSpend.Core.Repositories;
using SmartSpend.Core.SampleData;
using SmartSpend.Services;
using System.Reflection;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using YoFi.Core.Importers;

#if __DEMO_OPEN_ACCESS__
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
#endif

namespace SmartSpend.AspNet.Main
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly Queue<string> logme = new Queue<string>();

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var databaseAssemblyName = typeof(ApplicationDbContext).Assembly.GetName().Name!;
            var connectionString =
                Configuration.GetConnectionString(databaseAssemblyName) ??
                Configuration.GetConnectionString("DefaultConnection") ??
                throw new KeyNotFoundException("No connection string found. Please check config.");

            #if POSTGRES
                        services.AddDbContext<ApplicationDbContext>(options =>
                            options.UseNpgsql(connectionString));
                        logme.Enqueue("Using Postgres");
            #else
                        services.AddDbContext<ApplicationDbContext>(options =>
                            options.UseSqlServer(connectionString));
                        logme.Enqueue("Using SQL Server");
            #endif
          


            services.AddDatabaseDeveloperPageExceptionFilter();
            
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

            services.AddDistributedMemoryCache();
            services.AddSession();

            services.AddApplicationInsightsTelemetry();

            services.AddControllersWithViews()
                .AddJsonOptions(options => 
                { 
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                }
            );

            services.AddScoped<IBudgetTxRepository, BudgetTxRepository>();
            services.AddScoped<IRepository<BudgetTx>, BudgetTxRepository>();
            services.AddScoped<IPayeeRepository, PayeeRepository>();
            services.AddScoped<IRepository<Payee>, PayeeRepository>();
            services.AddScoped<IRepository<Transaction>, TransactionRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IReceiptRepository, ReceiptRepositoryInDb>();
            services.AddScoped<AllRepositories>();
            services.AddScoped<UniversalImporter>();
            services.AddScoped<TransactionImporter>();
            services.AddScoped<SplitImporter>();
            services.AddScoped<IImporter<Payee>, BaseImporter<Payee>>();
            services.AddScoped<IImporter<BudgetTx>, BaseImporter<BudgetTx>>();
            services.AddScoped<IReportEngine, ReportBuilder>();
            services.AddScoped<IDataProvider, ApplicationDbContext>();
            services.AddScoped<ISampleDataProvider, SampleDataProvider>();
            services.AddScoped<ISampleDataConfiguration, SampleDataConfiguration>();
            services.AddScoped<IDataAdminProvider, DataAdminProvider>();

            if (Configuration.GetSection(SendGridEmailOptions.Section).Exists())
            {
                services.Configure<SendGridEmailOptions>(Configuration.GetSection(SendGridEmailOptions.Section));
                services.AddTransient<IEmailSender, SendGridEmailService>();
            }

            var assembly = Assembly.GetEntryAssembly();
            var resource = assembly!.GetManifestResourceNames().Where(x => x.EndsWith(".version.txt")).SingleOrDefault();
            if (resource is not null)
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                using var streamreader = new StreamReader(stream!);
                var version = streamreader.ReadLine();
                Configuration["Codebase:Release"] = version;
                logme.Enqueue($"Version: {version}");
            }

            services.Configure<CodebaseConfig>(Configuration.GetSection(CodebaseConfig.Section));
            services.Configure<BrandConfig>(Configuration.GetSection(BrandConfig.Section));
            services.Configure<ApiConfig>(Configuration.GetSection(ApiConfig.Section));
            services.Configure<AdminUserConfig>(Configuration.GetSection(AdminUserConfig.Section));
            services.Configure<AdminModel.PageConfig>(Configuration.GetSection(AdminModel.PageConfig.Section));


            var democonfig = new DemoConfig();
            Configuration.GetSection(DemoConfig.Section).Bind(democonfig);

            democonfig.IsOpenAccess = false;

#if __DEMO_OPEN_ACCESS__
            if (!democonfig.IsEnabled)
                ConfigureAuthorizationNormal(services);
            else
                ConfigureAuthorizationDemo(services);

            democonfig.IsOpenAccess = true;
#else
            ConfigureAuthorizationNormal(services);
#endif
            services.AddSingleton<DemoConfig>(democonfig);

            logme.Enqueue($"*** DEMO CONFIG *** {democonfig}");

            var storageconnection = Configuration.GetConnectionString("StorageConnection");
            if (!string.IsNullOrEmpty(storageconnection))
            {
                logme.Enqueue($"*** AZURESTORAGE *** Found Storage Connection String");
                services.AddSingleton<IStorageService>(new Services.AzureStorageService(storageconnection));
            }

            var clock_now = Configuration["Clock:Now"];
            if (clock_now != null)
            {
                logme.Enqueue($"*** CLOCK *** Found clock setting {clock_now}");
                if (System.DateTime.TryParse(clock_now,out var clock_set))
                {
                    logme.Enqueue($"Setting system clock to {clock_set}");
                    services.AddSingleton<IClock>(new TestClock() { Now = clock_set });
                }
                else
                    logme.Enqueue($"Failed to parse as valid time");
            }
            else
                services.AddSingleton<IClock>(new SystemClock());

            if (democonfig.IsHomePageRoot)
            {
                services.AddRazorPages(options =>
                {
                    options.Conventions.AddPageRoute("/Home", "/");
                }).AddRazorRuntimeCompilation();
            }
            else
            {
                services.AddRazorPages().AddRazorRuntimeCompilation();
            }
        }

        private void ConfigureAuthorizationNormal(IServiceCollection services)
        {
            logme.Enqueue($"*** AUTHORIZATION *** Normal ***");

            services.AddAuthorization(options =>
            {
                options.AddPolicy("CanWrite", policy =>
                    policy.RequireAuthenticatedUser()); // ✅ Was: RequireRole("Verified")

                options.AddPolicy("CanRead", policy =>
                    policy.RequireAuthenticatedUser()); // ✅ Was: RequireRole("Verified")
            });
        }


#if __DEMO_OPEN_ACCESS__
        private void ConfigureAuthorizationDemo(IServiceCollection services)
        {
            logme.Enqueue($"*** AUTHORIZATION *** Demo Open Access ***");

            services.AddAuthorization(options =>
            {
                options.AddPolicy("CanRead", policy => policy.AddRequirements(new AnonymousAuth()));

                options.AddPolicy("CanWrite", policy => policy.RequireRole("Verified"));
            });
            services.AddScoped<IAuthorizationHandler, AnonymousAuthHandler>();
        }
#endif

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger, IEnumerable<IStorageService> storages, DemoConfig demo)
        {
            while (logme.Any())
                logger.LogInformation(logme.Dequeue());

            if (env.IsDevelopment())
            {
                logger.LogInformation($"*** CONFIGURE *** Running in Development");

                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                logger.LogInformation($"*** CONFIGURE *** Running in Production");

                app.UseExceptionHandler("/Transactions/Error");
            }

            app.UseStatusCodePagesWithReExecute("/StatusCode","?e={0}");

            var locale = "en-US"; // Configuration["SiteLocale"];
            RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions
            {
                SupportedCultures = new List<CultureInfo> { new CultureInfo(locale) },
                SupportedUICultures = new List<CultureInfo> { new CultureInfo(locale) },
                DefaultRequestCulture = new RequestCulture(locale)
            };
            app.UseRequestLocalization(localizationOptions);

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSession();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(x => 
            {
                if (!demo.IsHomePageRoot)
                    x.MapControllerRoute(name: "root", pattern: "/", defaults: new { controller = "Transactions", action = "Index" } );
                
                x.MapControllerRoute(name: "default", pattern: "{controller}/{action=Index}/{id?}");
                x.MapRazorPages();
            });

            foreach(var storage in storages)
                storage.ContainerName = SetupBlobContainerName(env.IsDevelopment());
        }

        private string SetupBlobContainerName(bool isdevelopment)
        {
            var key = "Storage:BlobContainerName";
            var value = Configuration[key];
            if (string.IsNullOrEmpty(value))
            {

                value = Configuration["Brand:Name"];
                if (string.IsNullOrEmpty(value))
                    value = Configuration["Codebase:Name"];
                if (string.IsNullOrEmpty(value))
                    value = "aspnet";

                if (isdevelopment)
                    value += "-development";
            }

            return value.ToLowerInvariant();
        }
    }

    public class SampleDataConfiguration : ISampleDataConfiguration
    {
        public SampleDataConfiguration(IWebHostEnvironment e)
        {
            Directory = e.WebRootPath + "/sample";
        }
        public string Directory { get; private set; }
    }

#if __DEMO_OPEN_ACCESS__

    public class AnonymousAuth: IAuthorizationRequirement
    {
    }

    public class AnonymousAuthHandler : AuthorizationHandler<AnonymousAuth>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AnonymousAuth requirement)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
#endif
}
