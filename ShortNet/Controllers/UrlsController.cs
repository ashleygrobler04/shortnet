using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShortNet.Data;
using ShortNet.Interfaces;
using ShortNet.Models;

namespace ShortNet.Controllers;

[Authorize]
public class UrlsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUrlService _urlService;
    private readonly ILogger<UrlsController> _logger;

    public UrlsController(ApplicationDbContext context, IUrlService urlService, ILogger<UrlsController> logger)
    {
        _context = context;
        _urlService = urlService;
        _logger = logger;
    }

    public ActionResult Index()
    {
        return View();
    }

    public ActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(Url url)
    {
        if (url is null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        if (!ModelState.IsValid)
        {
            return View(url);
        }

        try
        {
            var createdUrl = _urlService.CreateUrl(url.LongUrl, url.Name, url.ShortUrl);
            createdUrl.CreatedBy = User.Identity?.Name ?? "Unknown";
            createdUrl.CreatedAt = DateTime.UtcNow;

            var longUrlTrimmed = createdUrl.LongUrl.Trim();
            var shortUrlTrimmed = createdUrl.ShortUrl.Trim();
            var nameTrimmed = createdUrl.Name.Trim();

            var isLongUrlDuplicate = _context.Urls.Any(u => u.LongUrl == longUrlTrimmed);
            var isShortUrlDuplicate = _context.Urls.Any(u => u.ShortUrl == shortUrlTrimmed);
            var isNameDuplicate = _context.Urls.Any(u => u.Name == nameTrimmed);

            if (isLongUrlDuplicate || isShortUrlDuplicate || isNameDuplicate)
            {
                if (isLongUrlDuplicate)
                {
                    ModelState.AddModelError(nameof(url.LongUrl), "This original URL already exists in your collection.");
                }

                if (isShortUrlDuplicate)
                {
                    ModelState.AddModelError(nameof(url.ShortUrl), "This shortened URL is already in use. Choose a different value.");
                }

                if (isNameDuplicate)
                {
                    ModelState.AddModelError(nameof(url.Name), "A URL with this name already exists. Please choose a different name.");
                }

                return View(url);
            }

            _context.Urls.Add(createdUrl);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Url created successfully.";
            return RedirectToAction(nameof(List));
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "Invalid URL submission from user {UserName}.", User.Identity?.Name);
            ModelState.AddModelError(string.Empty, argEx.Message);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Error saving new URL for user {UserName}.", User.Identity?.Name);
            ModelState.AddModelError(string.Empty, "Unable to save the URL right now. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating URL for user {UserName}.", User.Identity?.Name);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please contact support.");
        }

        return View(url);
    }

    public ActionResult List()
    {
        try
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Challenge();
            }

            var userUrls = _context.Urls.Where(u => u.CreatedBy == userName).ToList();
            ViewData["UserUrls"] = userUrls;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load URL list for user {UserName}.", User.Identity?.Name);
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpGet]
    public ActionResult Edit(int id)
    {
        try
        {
            var url = _context.Urls.Find(id);
            if (url == null || !string.Equals(url.CreatedBy, User.Identity?.Name, StringComparison.Ordinal))
            {
                return View("NotFound");
            }

            return View(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading URL edit view for id {UrlId}.", id);
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpGet]
    public ActionResult DeleteConfirm(int id)
    {
        try
        {
            var url = _context.Urls.Find(id);
            if (url == null || !string.Equals(url.CreatedBy, User.Identity?.Name, StringComparison.Ordinal))
            {
                return View("NotFound");
            }

            return View("Delete", url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading URL delete confirmation for id {UrlId}.", id);
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(int id, Url url)
    {
        if (url is null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        if (id != url.Id)
        {
            return View("NotFound");
        }

        var existingUrl = _context.Urls.Find(id);
        if (existingUrl == null || !string.Equals(existingUrl.CreatedBy, User.Identity?.Name, StringComparison.Ordinal))
        {
            return View("NotFound");
        }

        if (!ModelState.IsValid)
        {
            return View(url);
        }

        if (!_urlService.IsValid(url.LongUrl))
        {
            ModelState.AddModelError(nameof(url.LongUrl), "Url must be a valid absolute HTTP or HTTPS URL.");
            return View(url);
        }

        var longUrlTrimmed = url.LongUrl.Trim();
        var shortUrlTrimmed = url.ShortUrl.Trim();
        var nameTrimmed = string.IsNullOrWhiteSpace(url.Name) ? existingUrl.Name : url.Name.Trim();

        var duplicateLongUrl = _context.Urls.Any(u => u.Id != id && u.LongUrl == longUrlTrimmed);
        var duplicateShortUrl = _context.Urls.Any(u => u.Id != id && u.ShortUrl == shortUrlTrimmed);
        var duplicateName = _context.Urls.Any(u => u.Id != id && u.Name == nameTrimmed);

        if (duplicateLongUrl || duplicateShortUrl || duplicateName)
        {
            if (duplicateLongUrl)
            {
                ModelState.AddModelError(nameof(url.LongUrl), "This original URL already exists in your collection.");
            }

            if (duplicateShortUrl)
            {
                ModelState.AddModelError(nameof(url.ShortUrl), "This shortened URL is already in use. Choose a different value.");
            }

            if (duplicateName)
            {
                ModelState.AddModelError(nameof(url.Name), "A URL with this name already exists. Please choose a different name.");
            }

            return View(url);
        }

        try
        {
            existingUrl.Name = nameTrimmed;
            existingUrl.LongUrl = longUrlTrimmed;
            existingUrl.ShortUrl = shortUrlTrimmed;

            _context.Urls.Update(existingUrl);
            _context.SaveChanges();
            return RedirectToAction(nameof(List));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Error updating URL id {UrlId} for user {UserName}.", id, User.Identity?.Name);
            ModelState.AddModelError(string.Empty, "Unable to update the URL right now. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating URL id {UrlId}.", id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please contact support.");
        }

        return View(url);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(int id)
    {
        if (id <= 0)
        {
            return View("NotFound");
        }

        try
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Challenge();
            }

            var url = _context.Urls.Find(id);
            if (url == null || !string.Equals(url.CreatedBy, userName, StringComparison.Ordinal))
            {
                return View("NotFound");
            }

            _context.Urls.Remove(url);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Url deleted successfully.";
            return RedirectToAction(nameof(List));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Error deleting URL id {UrlId} for user {UserName}.", id, User.Identity?.Name);
            TempData["ErrorMessage"] = "Unable to delete the URL right now. Please try again later.";
            return RedirectToAction(nameof(List));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting URL id {UrlId}.", id);
            TempData["ErrorMessage"] = "An unexpected error occurred while deleting the URL.";
            return RedirectToAction(nameof(List));
        }
    }
}
