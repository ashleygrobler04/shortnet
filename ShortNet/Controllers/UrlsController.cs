using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShortNet.Controllers
{
    public class UrlsController : Controller
    {
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
    }
}
