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
        return View("Url created!");
    }

    public ActionResult List()
    {
        var userUrls = _context.Urls.Where(u => u.CreatedBy == User.Identity.Name).ToList();
        ViewData["UserUrls"] = userUrls;
        return View();
    }

    [HttpGet("Urls/To/{shortenedUrl}")]
    public ActionResult To(string shortenedUrl)
    {
        var foundUrl = _context.Urls.Where(u => u.ShortUrl == shortenedUrl).FirstOrDefault();
        if (foundUrl == null)
        {
            return View("NotFound");
        }
        return Redirect(foundUrl.LongUrl);
    }
}
