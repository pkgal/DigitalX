using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DigitalX.Models;
using System.Configuration;
using System.Data.Entity;
using System.Net.Http;
using StockWebApi.Models;

namespace DigitalX.Controllers
{
    [CustomAuthenticationFilter]
    [AuthorizeRoles("Admin", "Product")]
    public class ProductController : Controller
    {
        private DigitalXDBEntities db = new DigitalXDBEntities();

        public ActionResult Search()
        {
            GetBrandsAndCategories();

            return View(db.Products.Where(p=>p.IsActive==true).ToList());
        }

        [HttpPost]
        public ActionResult SearchByName(string productName)
        {
            GetBrandsAndCategories();
            if (string.IsNullOrEmpty(productName))
            {
                DisplayNothingFound();
                return RedirectToAction("Search");
            }
    
            var productList = db.Products.Where(p => p.Name.Contains(productName) && p.IsActive == true).ToList();
            if (productList.Count() <= 0) DisplayNothingFound();
           
            return View("Search", productList);
        }

        [HttpPost]
        public ActionResult SearchByBrand()
        {
            int selectedValue = Convert.ToInt32(Request.Form["brandList"]);
            var productList = from p in db.Products
                              join b in db.Brands on p.BrandID equals b.BrandID
                              where b.BrandID == selectedValue && p.IsActive == true
                              orderby p.Name ascending
                              select p;

            GetBrandsAndCategories();
            if (productList.Count() <= 0) DisplayNothingFound();

            return View("Search", productList);
        }


        [HttpPost]
        public ActionResult SearchByCategory()
        {
            int selectedValue = Convert.ToInt32(Request.Form["categoryList"]);
            var productList = from p in db.Products
                              join pc in db.Product_Category on p.ProductID equals pc.ProductID 
                              join c in db.Categories on pc.CategoryID equals c.CategoryID
                              where c.CategoryID == selectedValue && p.IsActive == true
                              orderby p.Name ascending
                              select p;

            GetBrandsAndCategories();
            if (productList.Count() <= 0) DisplayNothingFound();

            return View("Search", productList);
        }

        private void DisplayNothingFound()
        {
            TempData["alertMsg"] = "No products found matching your search";
            TempData["alertClass"] = "alert-info";
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product newProduct)
        {
            if (ModelState.IsValid)
            {
                FileUpload fileToUpload = new FileUpload(Request.Files[0]);       
                if(!fileToUpload.IsUploadedSuccessfully(Server.MapPath(ConfigurationManager.AppSettings["ProductImagePath"])))
                {
                    TempData["alertMsg"] = fileToUpload.FileMessage;
                    TempData["alertClass"] = "alert-danger";
                }
                else
                {
                    short selectedBrandID = Convert.ToInt16(Request.Form["ddlBrand"].ToString());
                    if (selectedBrandID != 0) newProduct.BrandID = selectedBrandID;

                    newProduct.Picture = fileToUpload.ModifiedFileName;
                    newProduct.UpdatedBy = ((Employee)Session["CurrentUser"]).EmpID;
                    newProduct.IsActive = true;

                    db.Products.Add(newProduct);
                    db.SaveChanges();

                    //Link products to categories
                    if (Request.Form["chkCategory"] != null)
                    {
                        LinkCategories(newProduct.ProductID);
                    }
                    ModelState.Clear();
                    TempData["alertMsg"] = "New Product is added successfully ";
                    TempData["alertClass"] = "alert-success";
                }                    
            } 
            return View();
        }

        public ActionResult Edit(int id)
        {
            Product item = db.Products.Where(x => x.ProductID == id && x.IsActive == true).First();
            TempData["selectedBrandID"] = item.BrandID;
            TempData["selectedCategories"] =  item.Product_Category.Select(p => p.CategoryID).ToArray();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product editedProduct)
        {
            if (ModelState.IsValid)
            {
                Product item = db.Products.Where(x => x.ProductID == editedProduct.ProductID && x.IsActive == true).First();
                bool uploadFlag = true;
                if (Request.Files[0].ContentLength > 0)
                {
                    FileUpload fileToUpload = new FileUpload(Request.Files[0]);
                    if (fileToUpload.IsUploadedSuccessfully(Server.MapPath(ConfigurationManager.AppSettings["ProductImagePath"])))
                    {
                        item.Picture = fileToUpload.ModifiedFileName;                       
                    }
                    else
                    {
                        TempData["alertMsg"] = fileToUpload.FileMessage;
                        TempData["alertClass"] = "alert-danger";
                        uploadFlag = false;
                    }
                }              

                if(uploadFlag)
                { 
                    item.Name = editedProduct.Name;
                    item.Description = editedProduct.Description;
                    item.Price = editedProduct.Price;
                    item.Discount = editedProduct.Discount;
                    item.Stock = editedProduct.Stock;
                    item.IsActive = true;

                    short selectedBrandID = Convert.ToInt16(Request.Form["ddlBrand"].ToString());
                    if (selectedBrandID != 0)
                        item.BrandID = selectedBrandID;
                    else
                        item.BrandID = null;

                    item.UpdatedBy = ((Employee)Session["CurrentUser"]).EmpID;

                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();

                    DeleteLinkedCategories(editedProduct.ProductID); 

                    if (Request.Form["chkCategory"] != null)
                        LinkCategories(editedProduct.ProductID);
                    
                    ModelState.Clear();
                    TempData["alertMsg"] = "Product is updated successfully ";
                    TempData["alertClass"] = "alert-success";
                }
                return RedirectToAction("Edit", new { id = editedProduct.ProductID });
            }
            else
            {
                return View(editedProduct);
            }           
        }
        
        public ActionResult Detail(int id)
        {
            Product item = db.Products.Where(x => x.ProductID == id && x.IsActive == true).First();
            return View(item);
        }
        
        public ActionResult Delete(int id)
        {
            DeleteLinkedCategories(id);

            Product item = db.Products.Find(id);
            item.UpdatedBy = ((Employee)Session["CurrentUser"]).EmpID;
            item.IsActive = false;

            db.Entry(item).State = EntityState.Modified;
            db.SaveChanges();
            TempData["alertMsg"] = "\"" + item.Name + "\" is deleted successfully";
            TempData["alertClass"] = "alert-success";
            return RedirectToAction("Search");
        }


        private void GetBrandsAndCategories()
        {
            ViewBag.Brands = db.Brands.ToList();
            ViewBag.Categories = db.Categories.ToList();
        }

        private void LinkCategories(long productID)
        {
            //Insert new values
            string[] selectedCategories = Request.Form["chkCategory"].ToString().Split(',');
            foreach (string catItem in selectedCategories)
            {
                Product_Category prodCatItem = new Product_Category();
                prodCatItem.ProductID = productID;
                prodCatItem.CategoryID = Convert.ToInt16(catItem);
                prodCatItem.UpdatedBy = ((Employee)Session["CurrentUser"]).EmpID;

                db.Product_Category.Add(prodCatItem);
            }
            db.SaveChanges();
        }

        private void DeleteLinkedCategories(long productID)
        {
            db.Product_Category.Where(x => x.ProductID == productID).ToList().ForEach(p => db.Product_Category.Remove(p));
            db.SaveChanges();

        }


        //Wep Api reference for stockists

        public ActionResult Stock(int id, string name)
        {
            ViewBag.productName = name;

            IEnumerable<Stockist> stockists = null;
           /* using (var client = new HttpClient())
            { 
                string uri = "http://localhost:50521/api/Stock?productID=" + id;
                var responseTask = client.GetAsync(uri);
                responseTask.Wait();

                var result= responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsAsync<IList<Stockist>>();
                    readTask.Wait();

                    stockists = readTask.Result;
                }
                else
                {
                    stockists = Enumerable.Empty<Stockist>();
                    TempData["alertMsg"] = "Error occurred while processing this request with Error Code: "+ result.StatusCode;
                    TempData["alertClass"] = "alert-danger";
                }
            }*/
            return View(stockists);
        }
    }
}