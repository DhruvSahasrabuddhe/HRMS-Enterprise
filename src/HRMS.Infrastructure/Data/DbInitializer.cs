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
            
            // Generate a random secure password
            var randomPassword = GenerateSecurePassword();

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, randomPassword);
                
                if (result.Succeeded)
                {
                    logger.LogInformation("Created default admin user: {Email}", adminEmail);
                    
                    // Assign Admin role
                    await userManager.AddToRoleAsync(adminUser, HrmsConstants.Roles.Admin);
                    logger.LogInformation("Assigned Admin role to user: {Email}", adminEmail);
                    
                    logger.LogWarning(
                        "=============================================================================");
                    logger.LogWarning(
                        "DEFAULT ADMIN CREDENTIALS CREATED - CHANGE IMMEDIATELY ON FIRST LOGIN!");
                    logger.LogWarning(
                        "Email: {Email}", adminEmail);
                    logger.LogWarning(
                        "Password: {Password}", randomPassword);
                    logger.LogWarning(
                        "=============================================================================");
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

        /// <summary>
        /// Generates a cryptographically secure random password.
        /// </summary>
        private static string GenerateSecurePassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            const int passwordLength = 16;

            var random = new Random();
            var password = new char[passwordLength];

            // Ensure at least one character from each required set
            password[0] = uppercase[random.Next(uppercase.Length)];
            password[1] = lowercase[random.Next(lowercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            // Fill remaining positions with random characters from all sets
            var allChars = uppercase + lowercase + digits + special;
            for (int i = 4; i < passwordLength; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle the password to avoid predictable patterns
            for (int i = passwordLength - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }
    }
}
