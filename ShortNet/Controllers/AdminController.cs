using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShortNet.Data;
using ShortNet.Interfaces;
using ShortNet.Models;

namespace ShortNet.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUrlService _urlService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUrlService urlService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _urlService = urlService;
        _logger = logger;
    }

    // ============= Users Management =============

    public async Task<IActionResult> Users()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new List<(IdentityUser User, List<string> Roles)>();

            foreach (var user in users)
            {
                var roles = (await _userManager.GetRolesAsync(user)).ToList();
                userRoles.Add((user, roles));
            }

            ViewData["UserRoles"] = userRoles;
            return View("Users/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users list.");
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            ViewData["UserRoles"] = userRoles;
            ViewData["AvailableRoles"] = new[] { "Admin", "User" };
            return View("Users/EditUser", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit user view for id {UserId}.", id);
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(string id, string[] roles)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            // Prevent admin from removing themselves as admin
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id && !roles.Contains("Admin"))
            {
                ModelState.AddModelError(string.Empty, "You cannot remove the Admin role from yourself.");
                ViewData["UserRoles"] = userRoles;
                ViewData["AvailableRoles"] = new[] { "Admin", "User" };
                return View("Users/EditUser", user);
            }

            var rolesToRemove = userRoles.Except(roles).ToList();
            var rolesToAdd = roles.Except(userRoles).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove roles from user {UserId}: {Errors}", id, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    ModelState.AddModelError(string.Empty, "Unable to remove some roles. Please try again.");
                    ViewData["UserRoles"] = userRoles;
                    ViewData["AvailableRoles"] = new[] { "Admin", "User" };
                    return View("Users/EditUser", user);
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add roles to user {UserId}: {Errors}", id, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                    ModelState.AddModelError(string.Empty, "Unable to add some roles. Please try again.");
                    ViewData["UserRoles"] = userRoles;
                    ViewData["AvailableRoles"] = new[] { "Admin", "User" };
                    return View("Users/EditUser", user);
                }
            }

            TempData["SuccessMessage"] = $"Roles updated successfully for {user.Email}.";
            return RedirectToAction(nameof(Users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating user roles for id {UserId}.", id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please contact support.");
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                ViewData["UserRoles"] = await _userManager.GetRolesAsync(user);
            }
            ViewData["AvailableRoles"] = new[] { "Admin", "User" };
            return View("Users/EditUser", user);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DeleteUserConfirm(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            return View("Users/DeleteUser", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading delete user confirmation for id {UserId}.", id);
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting yourself
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            // Remove user's URLs first
            var userUrls = _context.Urls.Where(u => u.CreatedBy == user.UserName).ToList();
            if (userUrls.Count > 0)
            {
                _context.Urls.RemoveRange(userUrls);
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User {user.Email} deleted successfully.";
                return RedirectToAction(nameof(Users));
            }
            else
            {
                _logger.LogWarning("Failed to delete user {UserId}: {Errors}", id, string.Join(", ", result.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = "Unable to delete user. Please try again.";
                return RedirectToAction(nameof(Users));
            }
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error deleting user id {UserId}.", id);
            TempData["ErrorMessage"] = "Unable to delete user right now. Please try again later.";
            return RedirectToAction(nameof(Users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting user id {UserId}.", id);
            TempData["ErrorMessage"] = "An unexpected error occurred while deleting the user.";
            return RedirectToAction(nameof(Users));
        }
    }

    // ============= URLs Management =============

    public IActionResult Urls()
    {
        try
        {
            var allUrls = _context.Urls.OrderByDescending(u => u.CreatedAt).ToList();
            ViewData["AllUrls"] = allUrls;
            return View("Urls/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all URLs.");
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpGet]
    public IActionResult EditUrl(int id)
    {
        try
        {
            var url = _context.Urls.Find(id);
            if (url == null)
            {
                return NotFound();
            }

            return View("Urls/EditUrl", url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit URL view for id {UrlId}.", id);
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUrl(int id, Url url)
    {
        try
        {
            if (url is null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (id != url.Id)
            {
                return NotFound();
            }

            var existingUrl = _context.Urls.Find(id);
            if (existingUrl == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View("Urls/EditUrl", url);
            }

            if (!_urlService.IsValid(url.LongUrl))
            {
                ModelState.AddModelError(nameof(url.LongUrl), "Url must be a valid absolute HTTP or HTTPS URL.");
                return View("Urls/EditUrl", url);
            }

            var longUrlTrimmed = url.LongUrl.Trim();
            var shortUrlTrimmed = url.ShortUrl.Trim();
            var nameTrimmed = string.IsNullOrWhiteSpace(url.Name) ? existingUrl.Name : url.Name.Trim();

            // Check for duplicates (excluding current URL)
            var duplicateLongUrl = _context.Urls.Any(u => u.Id != id && u.LongUrl == longUrlTrimmed);
            var duplicateShortUrl = _context.Urls.Any(u => u.Id != id && u.ShortUrl == shortUrlTrimmed);
            var duplicateName = _context.Urls.Any(u => u.Id != id && u.Name == nameTrimmed);

            if (duplicateLongUrl || duplicateShortUrl || duplicateName)
            {
                if (duplicateLongUrl)
                {
                    ModelState.AddModelError(nameof(url.LongUrl), "This original URL already exists.");
                }

                if (duplicateShortUrl)
                {
                    ModelState.AddModelError(nameof(url.ShortUrl), "This shortened URL is already in use.");
                }

                if (duplicateName)
                {
                    ModelState.AddModelError(nameof(url.Name), "A URL with this name already exists.");
                }

                return View("Urls/EditUrl", url);
            }

            existingUrl.Name = nameTrimmed;
            existingUrl.LongUrl = longUrlTrimmed;
            existingUrl.ShortUrl = shortUrlTrimmed;

            _context.Urls.Update(existingUrl);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "URL updated successfully.";
            return RedirectToAction(nameof(Urls));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Error updating URL id {UrlId}.", id);
            ModelState.AddModelError(string.Empty, "Unable to update the URL right now. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating URL id {UrlId}.", id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please contact support.");
        }

        return View("Urls/EditUrl", url);
    }

    [HttpGet]
    public IActionResult DeleteUrlConfirm(int id)
    {
        try
        {
            var url = _context.Urls.Find(id);
            if (url == null)
            {
                return NotFound();
            }

            return View("Urls/DeleteUrl", url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading delete URL confirmation for id {UrlId}.", id);
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUrl(int id)
    {
        try
        {
            var url = _context.Urls.Find(id);
            if (url == null)
            {
                return NotFound();
            }

            _context.Urls.Remove(url);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "URL deleted successfully.";
            return RedirectToAction(nameof(Urls));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Error deleting URL id {UrlId}.", id);
            TempData["ErrorMessage"] = "Unable to delete the URL right now. Please try again later.";
            return RedirectToAction(nameof(Urls));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting URL id {UrlId}.", id);
            TempData["ErrorMessage"] = "An unexpected error occurred while deleting the URL.";
            return RedirectToAction(nameof(Urls));
        }
    }
}
