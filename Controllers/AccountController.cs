using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Data.Entity;
using DigitalX.Models;

namespace DigitalX.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(Employee model)
        {
            if (!ModelState.IsValid) return View(model);

            using (DigitalXDBEntities db = new DigitalXDBEntities())
            {
                var user = db.Employees.Where(p => p.UserName.ToLower() == model.UserName.ToLower() && p.IsActive == true && p.Password.ToLower() == model.Password.ToLower()).Include(m => m.Roles);
                if (user.Count() <= 0)
                {
                    ModelState.AddModelError("", "Invalid User Name or Password");
                    return View(model);
                }

                Employee currentUser = (Employee)user.First();
                HttpContext.Session["CurrentUser"] = currentUser;

                string controller=FindRoute(currentUser.Roles.ElementAt(0).Name.ToString().Trim());
                return RedirectToAction("Search", controller, null);
            }
        }

        [CustomAuthenticationFilter]
        [AuthorizeRoles("Admin", "HR")]
        public ActionResult Create()
        {
            using (DigitalXDBEntities db = new DigitalXDBEntities())
            {
                ViewBag.listRoles = db.Roles.ToList();
            }
            return View();
        }

        [CustomAuthenticationFilter]
        [AuthorizeRoles("Admin", "HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Employee newUser)
        {
            using (DigitalXDBEntities db = new DigitalXDBEntities())
            {
                ViewBag.listRoles = db.Roles.ToList();
                if (!ModelState.IsValid) return View();

                if (Request.Form["chkRoles"] == null)
                {
                    ModelState.AddModelError("", "Employee must have one or more roles");
                    return View();
                }

                newUser.IsActive = true;
                db.Employees.Add(newUser);

                byte[] selectedRolesIDs = Request.Form["chkRoles"].ToString().Split(',').Select(byte.Parse).ToArray(); ;
                foreach (byte roleID in selectedRolesIDs)
                {
                    Role item = db.Roles.Find(roleID);
                    item.Employees.Add(newUser);
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();
                }

                ModelState.Clear();
                TempData["alertMsg"] = "New Employee is added successfully ";
                TempData["alertClass"] = "alert-success";
                return RedirectToAction("Search");
            }
        }

        [CustomAuthenticationFilter]
        [AuthorizeRoles("Admin", "HR")]
        public ActionResult Search()
        {
            List<Employee> empList;
            using (DigitalXDBEntities db = new DigitalXDBEntities())
            {
                empList = db.Employees.Where(p => p.IsActive == true).OrderByDescending(p => p.EmpID).Take(5).Include(m => m.Roles).ToList<Employee>();
                if (empList.Count == 0) DisplayNothingFound();
            }
            return View(empList);
        }

        [CustomAuthenticationFilter]
        [AuthorizeRoles("Admin", "HR")]
        [HttpPost]
        public ActionResult SearchByName(string empName)
        {
            List<Employee> empList;
            using (DigitalXDBEntities db = new DigitalXDBEntities())
            {
                empList = db.Employees.Where(p => p.Name.Contains(empName) && p.IsActive == true).Include(m => m.Roles).ToList<Employee>();
            }
            if (empList.Count == 0) DisplayNothingFound();
            return View("Search", empList);
        }

        [CustomAuthenticationFilter]
        [AuthorizeRoles("Admin", "HR")]
        public ActionResult Edit(int id)
        {
            Employee user;
            using (DigitalXDBEntities db = new DigitalXDBEntities())
            {
                ViewBag.listRoles = db.Roles.ToList();
                user = db.Employees.Where(x => x.EmpID == id).Include(m => m.Roles).First();
            }
            ViewBag.msg = user.Roles.Count();
            return View(user);
        }

        [CustomAuthenticationFilter]
        [AuthorizeRoles("Admin", "HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Employee editedUser)
        {
            using (DigitalXDBEntities db = new DigitalXDBEntities())
            {
                List<Role> listRoles = db.Roles.ToList<Role>();
                ViewBag.listRoles = listRoles;

                if (!ModelState.IsValid) return View(editedUser);

                if (Request.Form["chkRoles"] == null)
                {
                    ModelState.AddModelError("", "Employee must have one or more roles");
                    return View(editedUser);
                }

                Employee emp = db.Employees.Where(x => x.EmpID == editedUser.EmpID && x.IsActive == true).First();
                emp.Name = editedUser.Name;
                emp.Password = editedUser.Password;
                emp.UserName = editedUser.UserName;
                emp.IsActive = true;
                emp.Roles.Clear();

                byte[] selectedRolesIDs = Request.Form["chkRoles"].ToString().Split(',').Select(byte.Parse).ToArray(); ;

                foreach (byte roleID in selectedRolesIDs)
                {
                    Role item = db.Roles.Find(roleID);
                    emp.Roles.Add(item);
                }

                db.Entry(emp).State = EntityState.Modified;
                db.SaveChanges();
                
                if (((Employee)Session["CurrentUser"]).EmpID == emp.EmpID)
                    Session["CurrentUser"] = emp;   //Need to update as roles may have changed
                

                ModelState.Clear();
                TempData["alertMsg"] = "Employee is updated successfully ";
                TempData["alertClass"] = "alert-success";
                return RedirectToAction("Search");
            }
        }

        [CustomAuthenticationFilter]
        [AuthorizeRoles("Admin", "HR")]
        public ActionResult Delete(int id)
        {
            using (DigitalXDBEntities db = new DigitalXDBEntities())
            {
                Employee item = db.Employees.Find(id);
                item.IsActive = false;

                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();
                TempData["alertMsg"] = "\"" + item.UserName + "\" is deleted successfully";
            }

            TempData["alertClass"] = "alert-success";
            return RedirectToAction("Search");
        }

        public ActionResult LogOut()
        {
            Session["UserName"] = "";
            Session["UserID"] = "";
            Session["CurrentUser"] = "";
            return RedirectToAction("Login", "Account");
        }

        private void DisplayNothingFound()
        {
            TempData["alertMsg"] = "No users found matching your search";
            TempData["alertClass"] = "alert-info";
        }

        private string FindRoute(string currentRole)
        {
            string controller = "";
            switch (currentRole)
            {
                case "Admin":
                case "Dispatch":
                    controller= "Order";
                    break;
                case "Service":
                    controller = "Customer";
                    break;
                case "Product":
                    controller = "Product";
                    break;
                case "HR":
                    controller = "Account";
                    break;
            }
            return controller;
        }
    }
}