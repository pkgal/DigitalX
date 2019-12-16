using DigitalX.Models;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalX.Controllers
{
    [CustomAuthenticationFilter]
    [AuthorizeRoles("Admin", "Product")]
    public class BrandController : Controller
    {     
        private DigitalXDBEntities db = new DigitalXDBEntities();

        public ActionResult List()
        {
            return View(db.Brands.ToList());
        }


        public ActionResult _Edit(int id)
        {     
            return PartialView(db.Brands.Find(id));
        }

        [HttpPost]
        public ActionResult _Edit(Brand editedBrand)
        {      
            if (ModelState.IsValid)
            {
                Brand item = db.Brands.Find(editedBrand.BrandID);
               
                item.Name = editedBrand.Name;
                item.UpdatedBy = ((Employee)Session["CurrentUser"]).EmpID;

                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();
                ModelState.Clear();

                TempData["alertMsg"] = "Brand is updated successfully ";
                TempData["alertClass"] = "alert-success";
                return View("List", db.Brands.ToList());
            }
            else
            {
                return PartialView(editedBrand.BrandID);
            }
        }

      
        public ActionResult Delete(int id)
        {
            //Check if no products linked to this brand
            int productCount = db.Products.Count(x => x.BrandID == id);

            if (productCount>0)
            {
                TempData["alertMsg"] = "The brand cannot be deleted unless it has associated products";
                TempData["alertClass"] = "alert-warning";          
            }
            else
            {
                var brandItem = db.Brands.Find(id);
                db.Brands.Remove(brandItem);
                db.SaveChanges();
                TempData["alertMsg"] = "\"" + brandItem.Name + "\" is deleted successfully";
                TempData["alertClass"] = "alert-success";
            }
            var updatedBrands = db.Brands.ToList();
            return View("List", updatedBrands);
        }

        [HttpPost]
        public ActionResult Create(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(form["brandName"]))
                    {
                    Brand item = new Brand();
                    item.Name = form["brandName"].ToString();
                    item.UpdatedBy = ((Employee)Session["CurrentUser"]).EmpID;
                    db.Brands.Add(item);
                    ModelState.Clear();
                    db.SaveChanges();

                    TempData["alertMsg"]  = "New Brand is added successfully";
                    TempData["alertClass"] = "alert-success";
                }
            }
            else
            {
                TempData["alertMsg"] = "Invalid Data State";
                TempData["alertClass"] = "alert-danger";
            }
            return View("List", db.Brands.ToList());
        }

        public ActionResult _BrandList()
        {
            return PartialView(db.Brands.ToList());
        }

        [HttpPost]
        public ActionResult _BrandList(FormCollection form)
        {
            TempData["selectedBrandID"]= form["ddlBrand"].ToString();
            return PartialView(db.Brands.ToList());
        }
    }
}