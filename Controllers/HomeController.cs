using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DigitalX.Models;

namespace DigitalX.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Login", "Account", "");
        }

        public ActionResult UnAuthorized()
        {
            return View();
        }

        public ActionResult _AlertBox()
        {
            return PartialView();
        }
    }
}
