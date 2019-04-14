// -----------------------------------------------------------------------
// <copyright file="HomeController.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

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