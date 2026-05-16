using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShortNet.Data;
using ShortNet.Models;

namespace ShortNet.Controllers;

public class UrlsController : Controller
{
    private ApplicationDbContext _context;
    public UrlsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: UrlsController
    [Authorize]
    public ActionResult Index()
    {
        return View();
    }

    public ActionResult Create()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    public ActionResult Create(Url url)
    {
        if (ModelState.IsValid)
        {
            url.CreatedBy = User.Identity.Name;
            _context.Urls.Add(url);
            _context.SaveChanges();
        }
        return View("Index");
    }

    public ActionResult List()
    {
        var userUrls = _context.Urls.Where(u => u.CreatedBy == User.Identity.Name).ToList();
        ViewData["UserUrls"] = userUrls;
        return View();
    }

    [Authorize]
    [HttpGet]
    public ActionResult Edit(int id)
    {
        var url = _context.Urls.Find(id);
        if (url == null)
        {
            return View("NotFound");
        }
        if (url.CreatedBy != User.Identity.Name)
        {
            return View("NotFound");
        }
        return View(url);
    }

    [Authorize]
    [HttpPost]
    public ActionResult Edit(int id, Url url)
    {
        if (id != url.Id)
        {
            return View("NotFound");
        }

        var existingUrl = _context.Urls.Find(id);
        if (existingUrl == null)
        {
            return View("NotFound");
        }

        if (existingUrl.CreatedBy != User.Identity.Name)
        {
            return View("NotFound");
        }

        if (ModelState.IsValid)
        {
            try
            {
                existingUrl.Name = url.Name;
                existingUrl.LongUrl = url.LongUrl;
                existingUrl.ShortUrl = url.ShortUrl;
                
                _context.Urls.Update(existingUrl);
                _context.SaveChanges();
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
            }
        }

        return View(url);
    }
}
