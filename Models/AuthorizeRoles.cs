using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DigitalX.Models
{
    //public enum DigitalXRoles { Admin = 1, Dispatch = 2, Service = 3, Product = 4, HR = 5 }

    public class AuthorizeRoles : AuthorizeAttribute
    {
        private readonly string[] userAssignedRoles;

        public AuthorizeRoles(params string[] roles)
        {
            this.userAssignedRoles = roles;
        }
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            Employee loggedInUser = (Employee)httpContext.Session["CurrentUser"];
           
            foreach (var role in userAssignedRoles)
            {
               for(int i=0;i<loggedInUser.Roles.Count();i++)
               {
                    if (loggedInUser.Roles.ElementAt(i).Name == role) return true;
               }
            }
            return false;
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("~/Home/UnAuthorized");
        }
    }

}