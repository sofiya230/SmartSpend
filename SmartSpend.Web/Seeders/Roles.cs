using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartSpend.Data;

namespace SmartSpend.Main.Seeders;

public static class IdentitySeeder
{
    public static async Task SeedIdentity(IServiceProvider serviceProvider)
    {
        var RoleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var UserManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUserConfig = serviceProvider.GetRequiredService<IOptions<AdminUserConfig>>();

        string[] roleNames = { "Admin", "Verified", "Unknown" };
        IdentityResult roleResult;

        foreach (var roleName in roleNames)
        {
            var roleExist = await RoleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                roleResult = await RoleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        if (adminUserConfig?.Value?.Email != null)
        {
            var poweruser = new ApplicationUser
            {
                UserName = adminUserConfig.Value.Email,
                Email =  adminUserConfig.Value.Email
            };

            var _user = await UserManager.FindByEmailAsync( adminUserConfig.Value.Email);

            if (_user == null)
            {
                string UserPassword = adminUserConfig.Value.Password;
                var createPowerUser = await UserManager.CreateAsync(poweruser, UserPassword);
                if (createPowerUser.Succeeded)
                {
                    await UserManager.AddToRoleAsync(poweruser, "Admin");
                    await UserManager.AddToRoleAsync(poweruser, "Verified");
                }
            }
        }
    }
}
