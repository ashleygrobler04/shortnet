using Microsoft.AspNetCore.Identity;

namespace ShortNet.Services;

public interface IRoleInitializerService
{
    Task InitializeRolesAsync();
}

public class RoleInitializerService : IRoleInitializerService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RoleInitializerService> _logger;

    public RoleInitializerService(RoleManager<IdentityRole> roleManager, ILogger<RoleInitializerService> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task InitializeRolesAsync()
    {
        try
        {
            var roles = new[] { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Role '{RoleName}' created successfully.", role);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create role '{RoleName}': {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing roles.");
        }
    }
}
