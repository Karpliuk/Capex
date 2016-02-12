using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Capex.Models;

namespace Capex.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

      
        public ActionResult Close()
        {
            HttpCookie cookie = base.Request.Cookies["TSWA-Last-User"];
            if (base.User.Identity.IsAuthenticated == false || cookie == null || StringComparer.OrdinalIgnoreCase.Equals(base.User.Identity.Name, cookie.Value))
            {

                string name = string.Empty;
                if (base.Request.IsAuthenticated)
                {
                    name = this.User.Identity.Name;
                }

                cookie = new HttpCookie("TSWA-Last-User", name);
                base.Response.Cookies.Set(cookie);

                base.Response.AppendHeader("Connection", "close");
                base.Response.StatusCode = 0x191;
                base.Response.Clear();
                base.Response.Write("Доступ запрещен!");
                base.Response.End();
            }
            return RedirectToAction("Index", "Requests");
        }
    }
}