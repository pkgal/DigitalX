using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DigitalX.Models;

namespace DigitalX.Controllers
{
    [CustomAuthenticationFilter]
    [AuthorizeRoles("Admin", "Service")]
    public class CustomerController : Controller
    {
        private DigitalXDBEntities db = new DigitalXDBEntities();

        // GET: Customer
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Search()
        {
            List<Customer> cusList= null;
            return View(cusList);
        }

        [HttpPost]
        public ActionResult SearchByOrderID(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                DisplayNothingFound();
                return RedirectToAction("Search");
            }
            else
            {
                long ordID = Convert.ToInt64(orderId);
                List<Customer> cusItem = (from o in db.Orders
                               join c in db.Customers on o.CustomerID equals c.CustomerID
                               where o.OrderID == ordID
                               select c).ToList<Customer>();

                if (cusItem.Count()== 0)
                {
                    DisplayNothingFound();
                    return RedirectToAction("Search");
                }
                return View("Search", cusItem);
            }         
        }

        [HttpPost]
        public ActionResult SearchByCustomer(string customerName)
        {

            if (string.IsNullOrEmpty(customerName))
            {
                DisplayNothingFound();
                return RedirectToAction("Search");
            }
            List<Customer> cusList = db.Customers.Where(c => c.FirstName.Contains(customerName) || c.LastName.Contains(customerName)).ToList<Customer>();
            if (cusList.Count() == 0)
            {
                DisplayNothingFound();
                return RedirectToAction("Search");
            }
            return View("Search", cusList);
        }

        private void DisplayNothingFound()
        {
            TempData["alertMsg"] = "No Customers found matching your search";
            TempData["alertClass"] = "alert-info";
        }
    }
}