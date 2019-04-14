using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebCrawler.Web.Controllers
{
    public class HomeController : Controller
    {
        public static readonly string Version = typeof(HomeController).Assembly.GetName().Version.ToString();

        public IActionResult Index()
        {
            ViewBag.AppVersion = Version;
            return View();
        }
    }
}