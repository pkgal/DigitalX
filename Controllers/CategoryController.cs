using DigitalX.Models;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System;
using System.Web.Mvc.Filters;

namespace DigitalX.Controllers
{
    [CustomAuthenticationFilter]
    [AuthorizeRoles("Admin", "Product")]
    public class CategoryController : Controller
    {
        private DigitalXDBEntities db = new DigitalXDBEntities();

        public ActionResult List()
        {
            return View(db.Categories.ToList());
        }


        public ActionResult _Edit(int id)
        {
            return PartialView(db.Categories.Find(id));
        }

        [HttpPost]
        public ActionResult _Edit(Category editedCategory)
        {
            if (ModelState.IsValid)
            {
                Category item = db.Categories.Find(editedCategory.CategoryID);

                item.Name = editedCategory.Name;                
                item.UpdatedBy = ((Employee)Session["CurrentUser"]).EmpID;
                //item.UpdatedBy = 1;
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();
                ModelState.Clear();

                TempData["alertMsg"] = "Category is updated successfully ";
                TempData["alertClass"] = "alert-success";
                return View("List", db.Categories.ToList());
            }
            else
            {
                return PartialView(editedCategory.CategoryID);
            }
        }


        public ActionResult Delete(int id)
        {
            //Check if no products linked to this Category
            int productCount = db.Product_Category.Count(x => x.CategoryID == id);

            if (productCount > 0)
            {
                TempData["alertMsg"] = "The Category cannot be deleted, if it has products linked to it";
                TempData["alertClass"] = "alert-warning";
            }
            else
            {
                var CategoryItem = db.Categories.Find(id);
                db.Categories.Remove(CategoryItem);
                db.SaveChanges();
                TempData["alertMsg"] = "\"" + CategoryItem.Name + "\" is deleted successfully";
                TempData["alertClass"] = "alert-success";
            }
            var updatedCategories = db.Categories.ToList();
            return View("List", updatedCategories);
        }

        [HttpPost]
        public ActionResult Create(FormCollection form)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(form["CategoryName"]))
                {
                    Category item = new Category();
                    item.Name = form["CategoryName"].ToString();
                    item.UpdatedBy = ((Employee)Session["CurrentUser"]).EmpID;
                    // item.UpdatedBy = 1;
                    db.Categories.Add(item);
                    ModelState.Clear();
                    db.SaveChanges();
                    
                    TempData["alertMsg"] = "New Category is added successfully";
                    TempData["alertClass"] = "alert-success";
                }
            }
            else
            {
                TempData["alertMsg"] = "Invalid Data State";
                TempData["alertClass"] = "alert-danger";
            }
            return View("List", db.Categories.ToList());
        }

        public PartialViewResult _CategoryList()
        {
            return PartialView(db.Categories.ToList());
        }

        [HttpPost]
        public PartialViewResult _CategoryList(FormCollection form)
        {
            if (form["chkCategory"] != null)
            {
                short[] selectedCategoryIDs = form["chkCategory"].ToString().Split(',').Select(short.Parse).ToArray();
                TempData["selectedCategories"] = selectedCategoryIDs;
            }
            return PartialView(db.Categories.ToList());
        }
    }
}