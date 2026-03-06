using HRMS.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Data
{
    /// <summary>
    /// Database seeding service to initialize default roles and admin user.
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Seeds the database with default roles and admin user.
        /// </summary>
        public static async Task SeedAsync(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger logger)
        {
            try
            {
                // Seed roles
                await SeedRolesAsync(roleManager, logger);

                // Seed default admin user
                await SeedAdminUserAsync(userManager, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            var roles = new[]
            {
                HrmsConstants.Roles.Admin,
                HrmsConstants.Roles.HR,
                HrmsConstants.Roles.Manager,
                HrmsConstants.Roles.Employee
            };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                    else
                    {
                        logger.LogError("Failed to create role {RoleName}: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<IdentityUser> userManager, ILogger logger)
        {
            const string adminEmail = "admin@hrms.com";
            const string adminPassword = "Admin@123456"; // Should be changed on first login

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                
                if (result.Succeeded)
                {
                    logger.LogInformation("Created default admin user: {Email}", adminEmail);
                    
                    // Assign Admin role
                    await userManager.AddToRoleAsync(adminUser, HrmsConstants.Roles.Admin);
                    logger.LogInformation("Assigned Admin role to user: {Email}", adminEmail);
                    
                    logger.LogWarning(
                        "DEFAULT ADMIN CREDENTIALS - Email: {Email}, Password: {Password} - CHANGE IMMEDIATELY!",
                        adminEmail, adminPassword);
                }
                else
                {
                    logger.LogError("Failed to create admin user: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Ensure admin user has Admin role
                if (!await userManager.IsInRoleAsync(adminUser, HrmsConstants.Roles.Admin))
                {
                    await userManager.AddToRoleAsync(adminUser, HrmsConstants.Roles.Admin);
                    logger.LogInformation("Assigned Admin role to existing user: {Email}", adminEmail);
                }
            }
        }
    }
}
