using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DigitalX.Models;
using System.Configuration;
using System.Data.Entity;

namespace DigitalX.Controllers
{
    [CustomAuthenticationFilter]
    [AuthorizeRoles("Admin", "Dispatch")]
    public class OrderController : Controller
    {
        public enum OrderProcessingCodes { AWS, PBC, RTP, RTS, STA }

        private DigitalXDBEntities db = new DigitalXDBEntities();

        // GET: Order
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult Search()
        {
            List<Order> newOrders = db.Orders.Where(o => o.Packages.Count() == 0).OrderByDescending(x => x.OrderID).ToList<Order>();
            List<Order> inCompleteOrders = (from o in db.Orders
                                            join m in db.OrderItems on o.OrderID equals m.OrderID
                                            join p in db.Packages on m.ShipmentID equals p.PackageID
                                            join s in db.OrderStatus on p.StatusCode equals s.StatusCode
                                            where p.StatusCode != OrderProcessingCodes.PBC.ToString()
                                            orderby o.OrderID descending
                                            select o).ToList<Order>();
            ViewBag.ordersHeading = "List of Incomplete Orders ";
            if (newOrders.Count() == 0 && inCompleteOrders.Count() == 0) DisplayNothingFound();

            return View(newOrders.Union(inCompleteOrders).OrderByDescending(x => x.OrderID));
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
                var orderItem = db.Orders.Find(Convert.ToInt64(orderId));
                if (orderItem == null)
                {
                    DisplayNothingFound();
                    return RedirectToAction("Search");
                }
            }
            return RedirectToAction("Detail", new { id = orderId });
        }

        [HttpPost]
        public ActionResult SearchByCustomer(string customerName)
        {
            List<Order> orderList = db.Orders.Where(o => o.Customer.FirstName.Contains(customerName) || o.Customer.LastName.Contains(customerName)).ToList<Order>();
            if(orderList.Count()==0)
            {
                DisplayNothingFound();
                return RedirectToAction("Search");
            }
            return View("Search",orderList);
        }

        public ActionResult Initiate(int id)
        {
            Order order = db.Orders.Find(id);
            List<OrderItem> inStockProducts = order.OrderItems.Where(item => item.Product.Stock >= item.Quantity).ToList<OrderItem>();
            List<OrderItem> outOfStockProducts = order.OrderItems.Where(item => item.Product.Stock < item.Quantity).ToList<OrderItem>();

            if (inStockProducts.Count() > 0)
            {
                Package pkg1 = new Package();
                pkg1.OrderID = order.OrderID;
                pkg1.LastUpdated = System.DateTime.Now;
                pkg1.IsBackOrder = false;
                pkg1.StatusCode = OrderProcessingCodes.RTP.ToString();
                pkg1.UpdatedBy = 1;
                db.Packages.Add(pkg1);
                foreach (OrderItem oItem in inStockProducts)
                {
                    oItem.ShipmentID = pkg1.PackageID;                  
                    db.Entry(oItem).State = EntityState.Modified;
                    db.SaveChanges();
                    //Deduct Stock
                    Product p = db.Products.Find(oItem.Product.ProductID);
                    p.Stock = p.Stock - oItem.Quantity;
                    db.Entry(p).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            if (outOfStockProducts.Count() > 0)
            {
                Package pkg2 = new Package();
                pkg2.OrderID = order.OrderID;
                pkg2.LastUpdated = System.DateTime.Now;
                pkg2.IsBackOrder = true;
                pkg2.StatusCode = OrderProcessingCodes.AWS.ToString();
                pkg2.UpdatedBy = 1;
                db.Packages.Add(pkg2);
                foreach (OrderItem oItem in outOfStockProducts)
                {
                    oItem.ShipmentID = pkg2.PackageID;
                    db.Entry(oItem).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Detail", new { id = id });
        }

        public ActionResult Detail(int id)
        {
            Order order = db.Orders.Find(id);
            return View(order);
        }

        public ActionResult _PackageDetail(Package pkg)
        {           
            return PartialView(pkg);
        }

        public ActionResult _EditStatus(int id)
        {
            TempData["StatusList"] = db.OrderStatus.ToList<OrderStatus>();
            return PartialView(db.Packages.Find(id));
        }

        [HttpPost]
        public ActionResult _EditStatus()
        {
            TempData["StatusList"] = db.OrderStatus.ToList<OrderStatus>();

            long pkgId = Convert.ToInt64(Request.Form["packageId"]);
            string statusCode = Request.Form["radioStatus"].ToString().Trim();
            var trackingId = Request.Form["trackingId"];
            var invoiceId = Request.Form["invoiceId"];

            Package itemToModify = db.Packages.Find(pkgId);
            itemToModify.StatusCode = statusCode;
            itemToModify.LastUpdated = System.DateTime.Now;
            itemToModify.UpdatedBy = 1;

            if (statusCode == OrderProcessingCodes.RTS.ToString() && !string.IsNullOrEmpty(invoiceId))
                itemToModify.InvoiceID = Convert.ToInt64(invoiceId);
            else if (statusCode == OrderProcessingCodes.PBC.ToString() && !string.IsNullOrEmpty(trackingId))
                itemToModify.TrackingNo = trackingId;
            else
            {
                itemToModify.InvoiceID = null;
                itemToModify.TrackingNo = null;
            }

            db.Entry(itemToModify).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Detail", new { id = itemToModify.OrderID });
            
        }

        private void DisplayNothingFound()
        {
            TempData["alertMsg"] = "No products found matching your search";
            TempData["alertClass"] = "alert-info";
        }

        [ChildActionOnly]
        public string StatusLabelStyle(string statusCode)
        {
            string statusStyle= "";
            switch (statusCode)
            {
                case "RTP": //Ready to Pack"
                    statusStyle = "label-warning";
                    break;
                case "RTS": //Ready to Ship"
                    statusStyle = "label-info";
                    break;
                case "AWS": //Awaiting Stock"
                    statusStyle = "label-danger";
                    break;
                case "STA": //Stock Arrived"
                    statusStyle = "label-default";
                    break;
                case "PBC": //Picked-up by Courier"
                    statusStyle = "label-success";
                    break;
            }
            TempData["css"] = statusStyle;
            return statusStyle;
        }
    }
}