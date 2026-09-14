using ECommerce.Api.Identity.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Api.Identity;

public sealed class IdentityDataSeeder
{
    private const string BootstrapAdminEmailKey =
        "Identity:BootstrapAdminEmail";

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<IdentityDataSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var adminEmail = _configuration[BootstrapAdminEmailKey]
            ?.Trim();

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            _logger.LogInformation(
                "No development bootstrap administrator is configured.");

            return;
        }

        await EnsureRoleExistsAsync(AppRoles.Administrator);

        var user = await _userManager.FindByEmailAsync(adminEmail);

        if (user is null)
        {
            _logger.LogWarning(
                "The configured development bootstrap administrator account does not exist.");

            return;
        }

        if (await _userManager.IsInRoleAsync(
                user,
                AppRoles.Administrator))
        {
            return;
        }

        var result = await _userManager.AddToRoleAsync(
            user,
            AppRoles.Administrator);

        EnsureSucceeded(
            result,
            "assign the development administrator role");
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await _roleManager.CreateAsync(
            new IdentityRole<Guid>
            {
                Name = roleName
            });

        EnsureSucceeded(result, $"create the {roleName} role");
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error =>
                $"{error.Code}: {error.Description}"));

        throw new InvalidOperationException(
            $"Failed to {operation}. {errors}");
    }
}
