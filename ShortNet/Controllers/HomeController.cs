using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShortNet.Data;
using ShortNet.Models;

namespace ShortNet.Controllers;

public class HomeController : Controller
{
    private ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

        [HttpGet("T/{shortenedUrl}")] //T is short for to
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
